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
    public float minScale = 0.5f;
    public float maxScale = 0.9f;

    float pinchStartDist;
    Vector3 pinchStartScale;
    bool isPinching = false;
    static ItemDragger2D active;

    bool pointerDownOnMe = false;
    private TrashCanArea currentHoveredTrash;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        saveManager = FindFirstObjectByType<ItemSaveManager>();
    }

    void Update()
    {
        // 1. 핀치 줌 로직 (기존 유지)
        HandlePinchScale();

        if (Pointer.current == null || isPinching) return;

        // 2. 드래그 로직 (New Input System 기반)
        HandleMouseOrSingleTouch();
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

        // 선택된 아이템 체크 및 핀치 계산 (기존 로직 동일)
        var manager = InventoryManager.Instance;
        if (manager == null || manager.GetSelected()?.gameObject != gameObject) return;

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
            pinchStartScale = transform.localScale;
            return;
        }

        float scaleFactor = 1f + ((dist - pinchStartDist) * pinchSensitivity);
        float clamped = Mathf.Clamp(pinchStartScale.x * scaleFactor, minScale, maxScale);
        transform.localScale = new Vector3(clamped, clamped, clamped);
    }

    private void HandleMouseOrSingleTouch()
    {
        if (Pointer.current.press.wasPressedThisFrame)
        {
            if (IsPointerOverUI()) return;

            pointerDownOnMe = IsPointerOnThisObject(Pointer.current.position.ReadValue());
            if (pointerDownOnMe)
            {
                StartDrag(Pointer.current.position.ReadValue());
                var placed = GetComponent<PlacedItem>();
                if (placed != null && InventoryManager.Instance != null)
                    InventoryManager.Instance.SelectItem(placed);
            }
        }

        if (Pointer.current.press.isPressed && isDragging)
        {
            MoveTo(Pointer.current.position.ReadValue());
        }

        if (Pointer.current.press.wasReleasedThisFrame)
        {
            // [중요] 여기서 인터페이스의 OnEndDrag와 유사한 처리를 수행하거나 
            // 아래 인터페이스 구현부를 통해 쓰레기통을 체크합니다.
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
        // 인터페이스를 통한 쓰레기통 체크 (모바일 멀티터치 대응용)
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
        // 포인터 기반 쓰레기통 체크 (PC/단일 터치 대응용)
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
        Destroy(gameObject);
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
        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        depth = sp.z;
        Vector3 worldUnderPointer = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        offset = transform.position - worldUnderPointer;
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
        transform.position = world + offset;

        CheckTrashHover(screenPos);
    }

    bool IsPointerOnThisObject(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f) && hit.transform == transform;
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

        // UI 레이캐스트 실행
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

        // 상태 변화 감지 (새로 들어옴 / 나감)
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
                // --- [핵심 추가] 쓰레기통 영역에 처음 진입했을 때 진동 발생 ---
#if UNITY_ANDROID || UNITY_IOS
                //Handheld.Vibrate(); // 삭제 성공 피드백
#endif
            }
        }
    }

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }
}