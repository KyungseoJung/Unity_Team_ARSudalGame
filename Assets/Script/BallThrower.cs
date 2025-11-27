using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BallThrower : MonoBehaviour
{
    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public float throwForceMultiplier = 2.5f;

    [Header("Position Settings (Viewport)")]
    [Tooltip("가로 위치 (0:왼쪽 ~ 1:오른쪽). 0.5는 중앙")]
    [Range(0f, 1f)] public float horizontalRatio = 0.5f;

    [Tooltip("세로 위치 (0:바닥 ~ 1:천장). 0.15는 하단부")]
    [Range(0f, 1f)] public float verticalRatio = 0.15f;

    [Tooltip("카메라로부터의 거리 (미터 단위)")]
    public float distance = 0.5f;

    [Header("Input Settings")]
    public float resetDelay = 2.0f;

    private InputAction pressAction;
    private InputAction positionAction;

    private GameObject currentBall;
    private Rigidbody currentRb;
    private bool isHolding = false;
    private Vector2 startHoldPos;
    private float holdStartTime;
    private Vector2 lastFramePos;

    private void Awake()
    {
        pressAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        pressAction.AddBinding("<Touchscreen>/primaryTouch/press");

        positionAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/position");
        positionAction.AddBinding("<Touchscreen>/primaryTouch/position");
    }

    private void OnEnable() { pressAction.Enable(); positionAction.Enable(); }
    private void OnDisable() { pressAction.Disable(); positionAction.Disable(); }

    private void Start()
    {
        SpawnNewBall();
    }

    private void Update()
    {
        // ★ 공이 대기 상태(아직 안 던짐)일 때, 항상 화면 비율 위치에 고정시킴
        if (currentBall != null && !isHolding && currentRb.isKinematic)
        {
            KeepBallInPosition();
        }

        if (currentBall == null) return;

        Vector2 screenPos = positionAction.ReadValue<Vector2>();

        // 1. 터치 시작 (잡기)
        if (pressAction.WasPressedThisFrame())
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            RaycastHit hit;
            // SphereCollider가 있는 공을 정확히 터치했는지 확인
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == currentBall)
                {
                    StartHolding(screenPos);
                }
            }
        }

        // 2. 드래그 중
        if (isHolding && pressAction.IsPressed())
        {
            MoveBallWithFinger(screenPos);
            lastFramePos = screenPos;
        }

        // 3. 터치 뗌 (던지기)
        if (isHolding && pressAction.WasReleasedThisFrame())
        {
            ThrowBall(screenPos);
        }
    }

    // --- 기능 구현부 ---

    // ★ 핵심: 뷰포트 좌표를 월드 좌표로 변환하여 공 위치 고정
    void KeepBallInPosition()
    {
        // (0.5, 0.15, 0.5m) -> 카메라 앞 3D 좌표로 변환
        Vector3 viewportPos = new Vector3(horizontalRatio, verticalRatio, distance);
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(viewportPos);

        currentBall.transform.position = worldPos;
        currentBall.transform.rotation = Camera.main.transform.rotation;
    }

    void SpawnNewBall()
    {
        if (currentBall != null) return;

        // 일단 생성 (위치는 KeepBallInPosition에서 바로잡아줌)
        currentBall = Instantiate(ballPrefab);
        currentRb = currentBall.GetComponent<Rigidbody>();

        currentRb.useGravity = false;
        currentRb.isKinematic = true;

        // 이제 부모 설정을 안 해도 KeepBallInPosition이 매 프레임 따라다니게 함
    }

    void StartHolding(Vector2 pos)
    {
        isHolding = true;
        startHoldPos = pos;
        lastFramePos = pos;
        holdStartTime = Time.time;
    }

    void MoveBallWithFinger(Vector2 screenPos)
    {
        // 잡고 있을 때는 손가락 위치를 따라감
        // z값(거리)은 원래 설정된 distance 유지
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distance));
        currentBall.transform.position = worldPos;
    }

    void ThrowBall(Vector2 releasePos, float duration = 0f)
    {
        isHolding = false;

        currentRb.useGravity = true;
        currentRb.isKinematic = false;

        Vector3 throwVector = (releasePos - startHoldPos);

        float timeDiff = Time.time - holdStartTime;
        if (timeDiff < 0.05f) timeDiff = 0.05f;

        // 화면 높이 기준으로 속도 보정 (해상도 대응)
        float speed = throwVector.y / Screen.height;
        float speedMultiplier = (speed / timeDiff) * throwForceMultiplier;

        Vector3 force = Camera.main.transform.forward * speedMultiplier * 2.0f +
                        Camera.main.transform.up * speedMultiplier * 0.8f;

        float sideForce = (releasePos.x - startHoldPos.x) / Screen.width * throwForceMultiplier;
        force += Camera.main.transform.right * sideForce;

        currentRb.AddForce(force, ForceMode.Impulse);
        currentRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

        currentBall = null;
        currentRb = null;
        StartCoroutine(WaitAndSpawn());
    }

    IEnumerator WaitAndSpawn()
    {
        yield return new WaitForSeconds(resetDelay);
        SpawnNewBall();
    }
}