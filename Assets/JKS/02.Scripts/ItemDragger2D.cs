using UnityEngine;

public class ItemDragger2D : MonoBehaviour
{
    // //#1 아이템 (Cube. 큐브) 옮기기 및 배치 코드 ==================================
    Camera cam;                       //#4-2: selected 대신 이 오브젝트만 다룸
    bool isDragging = false;          //#4-2: 드래그 중인지 여부
    float depth;                      //#4-2: 카메라와의 z거리
    Vector3 offset;                   //#4-2: 마우스 위치와 오브젝트 위치의 차이

    [Header("화면 여백 (픽셀)")]      //#4-2: 화면 가장자리 여백 설정
    public float screenMargin = 10f;  //#4-2

    void Awake()
    {
        cam = Camera.main;
    }

    void OnMouseDown()                //#4-2: Raycast 대신 개별 오브젝트에서 직접 처리
    {
        isDragging = true;

        // 현재 오브젝트의 화면 좌표 → 깊이(z) 기억
        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
        depth = screenPos.z;

        // 마우스 아래의 월드 좌표 계산
        Vector3 worldUnderMouse = cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth)
        );

        // 클릭 시 손가락과 오브젝트 위치 차이 저장
        offset = transform.position - worldUnderMouse;
    }

    void OnMouseUp()                  //#4-2
    {
        isDragging = false;
    }

    void Update()
    {
        if (!isDragging) return;      //#4-2

        // 1) 현재 마우스/터치 위치 (스크린 좌표)
        Vector3 mouse = Input.mousePosition;

        // 2) 화면 안으로 clamp (기종/해상도와 무관하게 동작)
        mouse.x = Mathf.Clamp(mouse.x, screenMargin, Screen.width  - screenMargin);   //#4-2
        mouse.y = Mathf.Clamp(mouse.y, screenMargin, Screen.height - screenMargin);   //#4-2

        // 3) 다시 월드 좌표로 변환
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, depth)); //#4-2

        // 4) offset 더해서 실제 이동
        transform.position = world + offset;                                          //#4-2

        // 기존 Raycast 기반 선택/이동 코드는 전부 삭제됨                          //#4-2
    }
}
