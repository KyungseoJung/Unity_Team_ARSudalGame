using UnityEngine;
// #8 fix: 모바일 빌드 - 손가락 터치 안되는 현상 고치기
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

    // "내 오브젝트를 눌렀을 때만" 드래그 시작하기 위한 플래그
    bool pointerDownOnMe = false;

    void Awake()
    {
        if (cam == null) cam = Camera.main; // AR카메라에 MainCamera 태그가 있어야 잡힘
    }

    void Update()
    {
        if (Pointer.current == null) return;

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
        if (cam == null) return;

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
}
