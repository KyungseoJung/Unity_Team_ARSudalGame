using UnityEngine;
// #8 fix: 모바일 빌드 - 손가락 터치 안되는 현상 고치기
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
//#9 아이템 크기 변경 (두 손가락을 이용해서)
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(Collider))]
public class ItemDragger2D : MonoBehaviour
{
    [Header("References")]
    public Camera cam; // 비워두면 MainCamera 사용

    [Header("Clamp (pixels)")]
    public float screenMargin = 10f; // 화면 가장자리 여백

    // 드래그 상태
    bool isDragging = false;
    float depth;
    Vector3 offset;

    //#9 아이템 크기 변경 (두 손가락을 이용해서) -------------------
    [Header("Pinch Scale")]
    public bool enablePinchScale = true;
    public float pinchSensitivity = 0.005f;  // 값이 크면 확대가 빠름
    public float minScale = 0.5f;
    public float maxScale = 0.9f;

    float pinchStartDist;
    Vector3 pinchStartScale;
    bool isPinching = false;

    static ItemDragger2D active;   //#9-1 현재 조작 중인 아이템만 크기가 조작되도록!!! (전역 1개)


    // "내 오브젝트를 눌렀을 때만" 드래그 시작하기 위한 플래그
    bool pointerDownOnMe = false;

    void Awake()
    {
        if (cam == null) cam = Camera.main; // AR카메라에 MainCamera 태그가 있어야 잡힘
    }

    void Update()
    {

        //#9 아이템 크기 변경 (두 손가락을 이용해서) ----------------------------
        // 드래그보다 핀치 우선 처리
        if (enablePinchScale)
        {
            // 터치 2개면 핀치로 간주
            if (Touch.activeTouches.Count >= 2)
            {
                //#9-2 ----------------------
                // 선택된 아이템만 핀치 허용
                var manager = InventoryManager.Instance;
                if (manager == null) return;

                var selected = manager.GetSelected();
                if (selected == null || selected.gameObject != gameObject)
                {
                    // 선택이 없거나, 내가 선택된 아이템이 아니면 핀치 금지
                    isPinching = false;
                    return;
                }

                //#9-1 한 아이템씩만 크기 조작되도록 ----------------------
                // 이미 조작중인 아이템이 있다면
                if (active != null && active != this)
                {
                    return;
                } 
                // 지금 조작중인 아이템이 없다면(선택된 아이템 기준으로 시작하고 싶다면) "이 아이템이 조작자"로 선점
                if (active == null) 
                {
                    active = this;
                }



                // UI 위에서 시작된 터치는 무시하고 싶으면(선택) 더 복잡해지니,
                // 일단 기존 UI 방지 로직만 적용:
                if (IsPointerOverUI()) return;

                var t0 = Touch.activeTouches[0];
                var t1 = Touch.activeTouches[1];

                Vector2 p0 = t0.screenPosition;
                Vector2 p1 = t1.screenPosition;

                float dist = Vector2.Distance(p0, p1);

                // 핀치 시작
                if (!isPinching)
                {
                    isPinching = true;
                    isDragging = false; // 핀치 중에는 드래그 끔

                    pinchStartDist = dist;
                    pinchStartScale = transform.localScale;
                    return;
                }

                // 핀치 진행
                float delta = dist - pinchStartDist;
                float scaleFactor = 1f + (delta * pinchSensitivity);

                Vector3 target = pinchStartScale * scaleFactor;

                // 최소/최대 제한(균일 스케일)
                float clamped = Mathf.Clamp(target.x, minScale, maxScale);
                transform.localScale = new Vector3(clamped, clamped, clamped);

                return; // 핀치 중이면 여기서 종료(드래그 로직 실행 안 함)
            }
            else
            {
                //#9-1 한 아이템씩만 크기 조작되도록
                // 터치가 2개에서 1개/0개로 줄면 핀치 종료
                if (isPinching)
                {
                    isPinching = false;
                    if (active == this)   // 해제
                    {
                        active = null;
                    }
                }
            }
        }


        if (Pointer.current == null) 
        {
            return;
        }
        // 카메라가 런타임에 바뀌는 프로젝트(AR)면 매 프레임 보정
        if (cam == null) cam = Camera.main;

        // 1) 포인터 눌림 시작
        if (Pointer.current.press.wasPressedThisFrame)
        {
            // UI 위 클릭/터치면 월드 드래그 시작하지 않음
            if (IsPointerOverUI()) return;

            pointerDownOnMe = IsPointerOnThisObject(Pointer.current.position.ReadValue());
            if (pointerDownOnMe)
            {
                StartDrag(Pointer.current.position.ReadValue());
                // (선택) 드래그 시작과 동시에 선택 처리
                var placed = GetComponent<PlacedItem>();
                if (placed != null && InventoryManager.Instance != null)
                    InventoryManager.Instance.SelectItem(placed);
            }
        }

        // 2) 누르고 있는 동안 이동
        if (Pointer.current.press.isPressed && isDragging)
        {
            MoveTo(Pointer.current.position.ReadValue());
        }

        // 3) 포인터 떼면 종료
        if (Pointer.current.press.wasReleasedThisFrame)
        {
            EndDrag();
            pointerDownOnMe = false;
        }
    }

    void StartDrag(Vector2 screenPos)
    {
        active = this;  //#9-1

        if (cam == null) 
        {
            return;
        }
        
        isDragging = true;

        // 현재 오브젝트의 화면좌표(z) 깊이 확보
        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        depth = sp.z;

        // 포인터 아래 월드 좌표 계산
        Vector3 worldUnderPointer = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        offset = transform.position - worldUnderPointer;
    }

    void EndDrag()
    {
        isDragging = false;
        if (active == this) //#9-1
        {
            active = null;
        }
    }

    void MoveTo(Vector2 screenPos)
    {
        if (cam == null) return;

        // 화면 밖으로 못 나가게 clamp
        float x = Mathf.Clamp(screenPos.x, screenMargin, Screen.width - screenMargin);
        float y = Mathf.Clamp(screenPos.y, screenMargin, Screen.height - screenMargin);

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(x, y, depth));
        transform.position = world + offset;
    }

    // --- Helpers ------------------------------------------------------------

    bool IsPointerOnThisObject(Vector2 screenPos)
    {
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(screenPos);

        // 3D Collider 기반 Raycast
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return hit.transform == transform;
        }
        return false;
    }

    bool IsPointerOverUI()
    {
        // EventSystem 없으면 UI 체크 불가
        if (EventSystem.current == null) return false;

        // New Input System의 Pointer는 PointerId를 제공
        // -1일 수 있어도, 아래 RaycastAll로 대부분 커버 가능
        // 우선 간단 체크:
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // 더 확실한 방식: UI Raycast 결과가 있으면 UI 위로 판단
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Pointer.current.position.ReadValue();

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    //#9 아이템 크기 변경 (두 손가락을 이용해서) -----------------------------------
    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

}
