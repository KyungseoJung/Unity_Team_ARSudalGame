using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(Collider))]
public class ItemDragger2D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    public Camera cam;
    private ItemSaveManager saveManager;

    [Header("Clamp (pixels)")]
    public float screenMargin = 10f;

    // 드래그 상태
    bool isDragging = false;
    float depth;
    Vector3 offset;

    [Header("Pinch Scale")]
    public bool enablePinchScale = true;
    public float pinchSensitivity = 0.005f;

    // *** "시작 스케일 기준 배수 제한" (NEW)
    [Header("Pinch Scale Limits (Multiplier based on START scale)")]
    public float minScaleMultiplier = 0.4f; // 시작 스케일의 0.6배까지 -> 0.4배로 범위 넓히기
    public float maxScaleMultiplier = 1.6f; // 시작 스케일의 1.6배까지

    float pinchStartDist;
    Vector3 pinchStartScale;
    bool isPinching = false;
    static ItemDragger2D active;

    float pinchSignX = 1f;  //#16 size 조정할 때, 좌우 반전된 상태이면 그 상태를 유지하면서 사이즈 조절되도록

    bool pointerDownOnMe = false;
    private TrashCanArea currentHoveredTrash;

    //#11 (수달의 특이한 구조 때문에 추가 작업) Mesh Collider를 직접 찾아서 ItemDragger2D.cs와 PlacedItem.cs를 붙이도록
    [SerializeField] private Transform moveTarget;
    private Transform MoveT => moveTarget != null ? moveTarget : transform;

    // *** 시작 스케일 저장 (NEW)
    private Vector3 baseScale;

    //#13 더블클릭하면 배치된 아이템 좌우반전하도록
    [Header("Double Tap Flip")]
    public bool enableDoubleTapFlip = true;
    public float doubleTapMaxDelay = 0.25f;

    private float lastTapTime = -999f;
    private static ItemDragger2D lastTapped;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        saveManager = FindFirstObjectByType<ItemSaveManager>();

        if (moveTarget == null) moveTarget = transform; //#11 기본은 자기 자신

        // ✅ baseScale 기본값(현재 스케일 abs)
        baseScale = new Vector3(
            Mathf.Abs(MoveT.localScale.x),
            Mathf.Abs(MoveT.localScale.y),
            Mathf.Abs(MoveT.localScale.z)
        );

        // ✅ #15 핵심: "새로 배치된 아이템"은 baseScale이 아직 저장/로드가 안 되어있을 수 있음
        // 이 경우 PlacedItem.baseScale을 한 번만 초기화해서, 이후 SaveAll에 제대로 저장되게 만든다.
        var placed = GetComponentInParent<PlacedItem>();
        if (placed != null)
        {
            if (placed.baseScale == Vector3.zero)
            {
                // 처음 배치된 상태: 지금 스케일을 기준으로 저장
                placed.baseScale = baseScale;
            }
            else
            {
                // 이미 저장된 기준이 있으면 그걸 우선시
                baseScale = placed.baseScale;
            }
        }
    }

    void Update()
    {
        // 1. 핀치 줌 로직 (기존 유지 + 배수 제한 적용)
        HandlePinchScale();

        if (Pointer.current == null || isPinching) return;

        // 2. 드래그 로직 (New Input System 기반)
        HandleMouseOrSingleTouch();

#if UNITY_EDITOR
        // 에디터에서만: 마우스 휠로 스케일 테스트
        if (enablePinchScale && Mouse.current != null && Mouse.current.scroll.ReadValue().y != 0)
        {
            var manager = InventoryManager.Instance; // *** manager 변수 없어서 추가
            var placedRoot = GetComponentInParent<PlacedItem>();
            if (placedRoot != null && manager != null && manager.GetSelected() == placedRoot)
            {
                // float delta = Mouse.current.scroll.ReadValue().y > 0 ? 1.05f : 0.95f;

                // float s = MoveT.localScale.x * delta;

                // // *** 시작 스케일 기준 배수 제한 적용
                // float minAbs = baseScale.x * minScaleMultiplier;
                // float maxAbs = baseScale.x * maxScaleMultiplier;
                // s = Mathf.Clamp(s, minAbs, maxAbs);

                // //#13 ------------------------------
                // float signX = Mathf.Sign(MoveT.localScale.x);
                // if (signX == 0) signX = 1f;

                // MoveT.localScale = new Vector3(signX * s, s, s);

                float delta = Mouse.current.scroll.ReadValue().y > 0 ? 1.05f : 0.95f;

                Vector3 currentScale = MoveT.localScale;

                // 🔥 실제 뒤집힌 축 찾기
                bool flippedOnX = currentScale.x < 0f;
                bool flippedOnZ = currentScale.z < 0f;

                // 현재 절댓값 크기 (뒤집힌 축 기준으로 계산)
                float currentSize = flippedOnX 
                    ? Mathf.Abs(currentScale.x) 
                    : Mathf.Abs(currentScale.z);

                float s = currentSize * delta;

                // 절대 한계 (baseScale 기준)
                float globalMin = baseScale.x * minScaleMultiplier;
                float globalMax = baseScale.x * maxScaleMultiplier;

                s = Mathf.Clamp(s, globalMin, globalMax);

                // 🔥 flip 상태 그대로 유지
                if (flippedOnX)
                {
                    MoveT.localScale = new Vector3(-s, s, s);
                }
                else if (flippedOnZ)
                {
                    MoveT.localScale = new Vector3(s, s, -s);
                }
                else
                {
                    MoveT.localScale = new Vector3(s, s, s);
                }
            }
        }
#endif
    }

    private void HandlePinchScale()
    {
        if (!enablePinchScale || Touch.activeTouches.Count < 2)
        {
            if (isPinching)
            {
                isPinching = false;
                if (active == this) active = null;
            }
            return;
        }

        // 선택된 아이템 체크 및 핀치 계산
        var manager = InventoryManager.Instance;

        // 선택된 아이템만 크기 변경되도록 (자식에 붙어있어도 동작하게)
        var placedRoot = GetComponentInParent<PlacedItem>();
        if (manager == null || placedRoot == null) return;
        if (manager.GetSelected() != placedRoot) return;

        if (active != null && active != this) return;
        if (active == null) active = this;

        if (IsPointerOverUI()) return;

        var t0 = Touch.activeTouches[0];
        var t1 = Touch.activeTouches[1];
        float dist = Vector2.Distance(t0.screenPosition, t1.screenPosition);

        if (!isPinching)
        {
            isPinching = true;
            isDragging = false;
            pinchStartDist = dist;
            pinchStartScale = MoveT.localScale; //#11

            //#16 현재 방향 저장
            pinchSignX = Mathf.Sign(MoveT.localScale.x);
            if (pinchSignX == 0) pinchSignX = 1f;

            return;
        }

        float scaleFactor = 1f + ((dist - pinchStartDist) * pinchSensitivity);

        // *** 기준 스케일(baseScale) 기준 배수 제한
        float minAbs = baseScale.x * minScaleMultiplier;
        float maxAbs = baseScale.x * maxScaleMultiplier;

        // float target = pinchStartScale.x * scaleFactor;
        float target = Mathf.Abs(pinchStartScale.x) * scaleFactor;  //#16 
        float clamped = Mathf.Clamp(target, minAbs, maxAbs);

        //#13 --------------------------------
        // float signX = Mathf.Sign(MoveT.localScale.x);
        // if (signX == 0) signX = 1f;

        // MoveT.localScale = new Vector3(signX * clamped, clamped, clamped);
        MoveT.localScale = new Vector3(pinchSignX * clamped, clamped, clamped); //#16 
    }

    //#15 fix: 기준 스케일을 외부에서 주입(로드 시)
    // (이름은 prefabLocalScale 이지만, "기준 스케일" setter로 쓰는 중)
    public void SetBaseScaleFromPrefab(Vector3 prefabLocalScale)
    {
        baseScale = new Vector3(
            Mathf.Abs(prefabLocalScale.x),
            Mathf.Abs(prefabLocalScale.y),
            Mathf.Abs(prefabLocalScale.z)
        );

        // ✅ PlacedItem에도 동기화 (저장될 때 baseScale이 유지되도록)
        var placed = GetComponentInParent<PlacedItem>();
        if (placed != null)
        {
            placed.baseScale = baseScale;
        }
    }

    private void HandleMouseOrSingleTouch()
    {
        if (Pointer.current.press.wasPressedThisFrame)
        {
            if (IsPointerOverUI()) return;

            pointerDownOnMe = IsPointerOnThisObject(Pointer.current.position.ReadValue());

            if (pointerDownOnMe)    //#13 -----------------------------
            {
                var placed = GetComponentInParent<PlacedItem>();
                var manager = InventoryManager.Instance;
                if (placed != null && manager != null)
                    manager.SelectItem(placed);

                // *** 더블탭 체크 (선택된 아이템일 때만)
                if (enableDoubleTapFlip && placed != null && manager != null && manager.GetSelected() == placed)
                {
                    float now = Time.unscaledTime;
                    bool isDoubleTap = (lastTapped == this) && (now - lastTapTime <= doubleTapMaxDelay);

                    lastTapTime = now;
                    lastTapped = this;

                    if (isDoubleTap)
                    {
                        FlipRootHorizontally();

                        // 저장(조금 딜레이 주는 게 안전)
                        if (saveManager != null)
                            saveManager.Invoke("SaveAll", 0.05f);

                        return; // *** 더블탭은 드래그로 안 넘어가게
                    }
                }

                StartDrag(Pointer.current.position.ReadValue());
            }
        }

        if (Pointer.current.press.isPressed && isDragging)
        {
            MoveTo(Pointer.current.position.ReadValue());
        }

        if (Pointer.current.press.wasReleasedThisFrame)
        {
            if (isDragging) CheckTrashCan();
            EndDrag();
            pointerDownOnMe = false;
        }
    }

    // --- 쓰레기통 삭제 로직 (인터페이스 활용 및 통합) ---
    public void OnBeginDrag(PointerEventData eventData) { /* 시각 효과 필요 시 작성 */ }
    public void OnDrag(PointerEventData eventData) { /* Update에서 이동을 처리하므로 비워둠 */ }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            if (eventData.pointerCurrentRaycast.gameObject.name == "TrashCan")
            {
                DeleteProcess();
            }
        }
    }

    private void CheckTrashCan()
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Pointer.current.position.ReadValue();
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject.name == "TrashCan")
            {
                DeleteProcess();
                break;
            }
        }
    }

    private void DeleteProcess()
    {
        Debug.Log("🗑️ 아이템 삭제 및 저장 중...");
        isDragging = false;
        if (currentHoveredTrash != null)
        {
            currentHoveredTrash.OnHoverExit();
            currentHoveredTrash = null;
        }

        Destroy(MoveT.gameObject);  //#11

        if (saveManager != null)
        {
            saveManager.Invoke("SaveAll", 0.1f);
        }
    }

    // --- 기존 드래그 헬퍼 로직 유지 ---
    void StartDrag(Vector2 screenPos)
    {
        active = this;
        isDragging = true;

        Vector3 sp = cam.WorldToScreenPoint(MoveT.position);
        depth = sp.z;
        Vector3 worldUnderPointer = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        offset = MoveT.position - worldUnderPointer;
    }

    void EndDrag()
    {
        isDragging = false;
        if (active == this) active = null;
    }

    void MoveTo(Vector2 screenPos)
    {
        float x = Mathf.Clamp(screenPos.x, screenMargin, Screen.width - screenMargin);
        float y = Mathf.Clamp(screenPos.y, screenMargin, Screen.height - screenMargin);

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(x, y, depth));
        MoveT.position = world + offset;

        CheckTrashHover(screenPos);
    }

    bool IsPointerOnThisObject(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);

        // *** hit.transform == transform 만 보지 말고, 자식/부모 구조도 허용
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (hit.transform == transform) return true;
            if (hit.transform == MoveT) return true;

            // hit가 자식일 수도 있으니, 루트 기준으로 포함 관계 체크
            if (hit.transform.IsChildOf(transform)) return true;
        }
        return false;
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Pointer.current.position.ReadValue();
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    // 실시간 쓰레기통 감지 함수
    void CheckTrashHover(Vector2 screenPos)
    {
        if (EventSystem.current == null) return;

        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        TrashCanArea foundTrash = null;

        foreach (var r in results)
        {
            if (r.gameObject.name == "TrashCan")
            {
                foundTrash = r.gameObject.GetComponent<TrashCanArea>();
                break;
            }
        }

        if (foundTrash != currentHoveredTrash)
        {
            if (currentHoveredTrash != null)
            {
                currentHoveredTrash.OnHoverExit();
            }

            currentHoveredTrash = foundTrash;

            if (currentHoveredTrash != null)
            {
                currentHoveredTrash.OnHoverEnter();
#if UNITY_ANDROID || UNITY_IOS
                //Handheld.Vibrate();
#endif
            }
        }
    }

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    //#11 (수달의 특이한 구조 때문에 추가 작업) Mesh Collider를 직접 찾아서 ItemDragger2D.cs와 PlacedItem.cs를 붙이도록
    public void SetMoveTarget(Transform t)
    {
        moveTarget = t;

        // *** moveTarget이 바뀌면, 그 시점 기준으로 시작 스케일도 다시 잡아주는 게 안전함
        // baseScale = MoveT.localScale;
        //#13 ----------------------------  //#15 fix
        // baseScale = new Vector3(Mathf.Abs(MoveT.localScale.x), Mathf.Abs(MoveT.localScale.y), Mathf.Abs(MoveT.localScale.z));
    }

    //#13 -----------------------------
    private void FlipRootHorizontally() // 이미 90도로 뒤집혀져있는 물고기 & 수달도 좌우 반전되는 것이 보이도록
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Transform t = MoveT;

        // "화면의 오른쪽 방향"을 오브젝트 로컬 공간으로 변환
        Vector3 localCamRight = t.InverseTransformDirection(cam.transform.right);

        // 화면 Right가 로컬 X에 더 가까우면 X를 뒤집고,
        // 화면 Right가 로컬 Z에 더 가까우면 Z를 뒤집는다.
        bool flipX = Mathf.Abs(localCamRight.x) >= Mathf.Abs(localCamRight.z);

        Vector3 s = t.localScale;
        if (flipX) s.x *= -1f;
        else       s.z *= -1f;

        t.localScale = s;
    }
}