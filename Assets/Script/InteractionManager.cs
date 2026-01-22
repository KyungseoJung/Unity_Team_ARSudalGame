using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask interactableLayer; // RubbableObject가 있는 레이어
    public float rubSensitivity = 5.0f; // 문지름 강도

    private Vector3 lastMousePosition;

    void Update()
    {
        // 마우스 클릭(드래그) 중일 때
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Debug.Log("으엥 " + ray.GetType().Name);

            // 레이캐스트로 물체 감지v
            if (Physics.Raycast(ray, out hit, 100f, interactableLayer))
            {
                // 감지된 물체에 RubbableObject가 있는지 확인
                RubbableObject target = hit.collider.GetComponent<RubbableObject>();

                Debug.Log("target = " +  target);

                if (target != null)
                {
                    // 마우스 움직임(delta) 만큼 문지름 수치 전달
                    float mouseDelta = (Input.mousePosition - lastMousePosition).magnitude;

                    if (mouseDelta > 0)
                    {
                        // 너무 빠른 움직임 보정 등을 원하면 여기서 조절
                        target.AddRub(mouseDelta * rubSensitivity * Time.deltaTime);
                    }
                }
            }
        }

        lastMousePosition = Input.mousePosition;
    }
}