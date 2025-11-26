using UnityEngine;
using UnityEngine.InputSystem; // ★ 필수 네임스페이스

public class BallThrower : MonoBehaviour
{
    [Header("Settings")]
    public GameObject ballPrefab;
    public Transform throwSpawnPoint;
    public float throwForceMultiplier = 50f;

    // 입력 액션 정의
    private InputAction pressAction;   // 누르는 것 감지 (터치/클릭)
    private InputAction positionAction; // 위치 감지 (터치좌표/마우스좌표)

    private Vector2 startTouchPos;
    private float touchStartTime;

    private void Awake()
    {
        // 1. 누름(Press) 액션 생성 (마우스 왼쪽 버튼 OR 터치 압력)
        pressAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        pressAction.AddBinding("<Touchscreen>/primaryTouch/press");

        // 2. 위치(Position) 액션 생성 (마우스 좌표 OR 터치 좌표)
        positionAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/position");
        positionAction.AddBinding("<Touchscreen>/primaryTouch/position");
    }

    private void OnEnable()
    {
        // 액션 활성화 (반드시 해야 함)
        pressAction.Enable();
        positionAction.Enable();
    }

    private void OnDisable()
    {
        // 액션 비활성화
        pressAction.Disable();
        positionAction.Disable();
    }

    void Update()
    {
        // 현재 포인터(마우스 또는 손가락) 위치 읽기
        Vector2 currentPos = positionAction.ReadValue<Vector2>();

        // 1. 누른 순간 (Began)
        if (pressAction.WasPressedThisFrame())
        {
            startTouchPos = currentPos;
            touchStartTime = Time.time;
        }

        // 2. 뗀 순간 (Ended)
        if (pressAction.WasReleasedThisFrame())
        {
            float touchDuration = Time.time - touchStartTime;

            // 너무 짧은 터치(탭)나 제자리 클릭은 무시 (드래그 아님)
            if (touchDuration < 0.1f || Vector2.Distance(startTouchPos, currentPos) < 10f)
                return;

            ThrowBall(startTouchPos, currentPos, touchDuration);
        }
    }

    void ThrowBall(Vector2 startPos, Vector2 endPos, float duration)
    {
        // SpawnPoint가 없으면 카메라 앞에서 생성
        Vector3 spawnPos = throwSpawnPoint != null ? throwSpawnPoint.position : Camera.main.transform.position + Camera.main.transform.forward;
        Quaternion spawnRot = throwSpawnPoint != null ? throwSpawnPoint.rotation : Camera.main.transform.rotation;

        GameObject ball = Instantiate(ballPrefab, spawnPos, spawnRot);
        Rigidbody rb = ball.GetComponent<Rigidbody>();

        // 던지는 방향 계산
        Vector3 throwDirection = new Vector3(endPos.x - startPos.x, endPos.y - startPos.y, 0).normalized;

        // 카메라 기준 방향 + 화면 드래그 방향 혼합
        Vector3 finalForce = (Camera.main.transform.forward * 1.0f +
                              Camera.main.transform.up * throwDirection.y * 0.5f +
                              Camera.main.transform.right * throwDirection.x * 0.5f).normalized;

        // 힘 계산 (드래그 길이 / 시간)
        duration = Mathf.Max(duration, 0.1f); // 0으로 나누기 방지
        float dragDistance = Vector2.Distance(startPos, endPos);
        float force = (dragDistance / duration) * throwForceMultiplier * 0.01f;

        rb.AddForce(finalForce * force);
    }
}