using UnityEngine;
using UnityEngine.InputSystem; // ★ 필수 네임스페이스

public class RubbingInput : MonoBehaviour
{
    [Header("Sensitivity")]
    public float rubSensitivity = 0.01f; // 문지름 강도 조절 (픽셀 단위라 값을 작게 잡음)

    // 입력 액션 정의
    private InputAction pressAction;    // 누름 (터치/클릭)
    private InputAction deltaAction;    // 움직임 양 (델타값)
    private InputAction positionAction; // 포인터 위치 (좌표)

    private void Awake()
    {
        // 1. 누름(Press) 액션
        pressAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        pressAction.AddBinding("<Touchscreen>/primaryTouch/press");

        // 2. 움직임(Delta) 액션 - 손가락이 얼마나 이동했는지 감지
        deltaAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/delta");
        deltaAction.AddBinding("<Touchscreen>/primaryTouch/delta");

        // 3. 위치(Position) 액션 - Ray를 쏘기 위한 좌표
        positionAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/position");
        positionAction.AddBinding("<Touchscreen>/primaryTouch/position");
    }

    private void OnEnable()
    {
        pressAction.Enable();
        deltaAction.Enable();
        positionAction.Enable();
    }

    private void OnDisable()
    {
        pressAction.Disable();
        deltaAction.Disable();
        positionAction.Disable();
    }

    void Update()
    {
        // 누르고 있는 상태일 때만 실행
        if (pressAction.IsPressed())
        {
            // 이번 프레임의 움직임(Delta) 가져오기
            Vector2 delta = deltaAction.ReadValue<Vector2>();

            // 움직임의 크기 계산 (X축 이동량 + Y축 이동량)
            float movement = (Mathf.Abs(delta.x) + Mathf.Abs(delta.y));

            // 일정 이상 움직였을 때만 처리 (떨림 방지)
            // New Input System의 Delta는 픽셀 단위라 값이 큽니다. (예: 10, 20...)
            if (movement > 1.0f)
            {
                // 현재 포인터 위치 가져오기
                Vector2 pointerPos = positionAction.ReadValue<Vector2>();

                // Ray 발사
                Ray ray = Camera.main.ScreenPointToRay(pointerPos);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    // 맞은 물체에게 "문질러짐" 신호 보내기
                    RubbableObject target = hit.collider.GetComponent<RubbableObject>();
                    if (target != null)
                    {
                        // 픽셀 이동량에 민감도를 곱해서 전달
                        target.AddRub(movement * rubSensitivity);
                    }
                }
            }
        }
    }
}