using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask interactableLayer; // RubbableObject�� �ִ� ���̾�
    public float rubSensitivity = 5.0f; // ������ ����

    private Vector3 lastMousePosition;

    void Update()
    {
        // ���콺 Ŭ��(�巡��) ���� ��
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // ����ĳ��Ʈ�� ��ü ����v
            if (Physics.Raycast(ray, out hit, 100f, interactableLayer))
            {
                // ������ ��ü�� RubbableObject�� �ִ��� Ȯ��
                RubbableObject target = hit.collider.GetComponent<RubbableObject>();

                if (target != null)
                {
                    // ���콺 ������(delta) ��ŭ ������ ��ġ ����
                    float mouseDelta = (Input.mousePosition - lastMousePosition).magnitude;

                    if (mouseDelta > 0)
                    {
                        // �ʹ� ���� ������ ���� ���� ���ϸ� ���⼭ ����
                        target.AddRub(mouseDelta * rubSensitivity * Time.deltaTime);
                    }
                }
            }
        }

        lastMousePosition = Input.mousePosition;
    }
}