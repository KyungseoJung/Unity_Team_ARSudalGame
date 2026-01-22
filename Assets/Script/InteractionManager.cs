using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask interactableLayer; // RubbableObject�� �ִ� ���̾�
    public float rubSensitivity = 5.0f; // ������ ����

    private Vector3 lastMousePosition;

    void Update()
    {
        // ���콺 Ŭ��(�巡��) ���� ��
        if (Pointer.current == null) return;

        if (Pointer.current.press.isPressed)
        {
            Vector2 pointerPos = Pointer.current.position.ReadValue();

            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            RaycastHit hit;

            // ����ĳ��Ʈ�� ��ü ����v
            if (Physics.Raycast(ray, out hit, 100f, interactableLayer))
            {
                // ������ ��ü�� RubbableObject�� �ִ��� Ȯ��
                RubbableObject target = hit.collider.GetComponent<RubbableObject>();

                if (target != null)
                {
                    // 5. 움직임(Delta) 값 가져오기
                    // New Input System은 프레임 간의 이동량을 바로 줍니다.
                    Vector2 delta = Pointer.current.delta.ReadValue();

                    // 문지른 양 계산
                    float rubAmount = delta.magnitude;

                    if (rubAmount > 0)
                    {
                        // 문지름 적용 (Time.deltaTime을 곱하지 않아도 됨, Delta 자체가 프레임 차이값)
                        target.AddRub(rubAmount * rubSensitivity);
                    }
                }
            }
        }
    }
}