using UnityEngine;

public class ItemDragger2D : MonoBehaviour
{
    // //#1 아이템 (Cube. 큐브) 옮기기 및 배치 코드 ==================================
    Camera cam;
    Transform selected;
    float selectedDist;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // 1) 누를 때: 무엇을 선택했는지 레이캐스트
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                selected = hit.transform;
                // 카메라와 선택한 오브젝트 사이 거리 저장
                selectedDist = Mathf.Abs(cam.transform.position.z - selected.position.z);
                Debug.Log("Selected: " + selected.name);
            }
        }

        // 2) 누르고 있는 동안: 선택된 오브젝트 이동
        if (Input.GetMouseButton(0) && selected != null)
        {
            Vector3 screenPos = Input.mousePosition;
            screenPos.z = selectedDist; // 항상 같은 거리 유지

            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

            selected.position = new Vector3(
                worldPos.x,
                worldPos.y,
                selected.position.z   // z는 그대로
            );
        }

        // 3) 뗐을 때: 선택 해제
        if (Input.GetMouseButtonUp(0))
        {
            selected = null;
        }
    }
}
