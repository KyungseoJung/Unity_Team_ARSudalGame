using UnityEngine;
using Vuforia;

public class MarkerSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject contentPrefab; // 소환할 수달 프리팹
    public bool spawnOnlyOnce = true;

    [Header("Position Settings")]
    public float spawnDistance = 1.0f; // 카메라 앞 1미터 지점에 소환
    public bool lookAtCamera = true;   // 소환될 때 나를 쳐다볼지 여부

    private bool hasSpawned = false;
    private ObserverBehaviour observerBehaviour;

    void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        if (observerBehaviour)
        {
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        if (targetStatus.Status == Status.TRACKED || targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            SpawnContent();
        }
    }

    void SpawnContent()
    {
        if (spawnOnlyOnce && hasSpawned) return;

        // ★ 핵심 변경 사항 ★
        // 마커 위치(transform.position)가 아니라, 카메라 위치를 기준으로 계산합니다.
        Transform camTrans = Camera.main.transform;

        // 1. 위치: 카메라 위치 + (카메라가 보는 방향 * 거리)
        Vector3 spawnPos = camTrans.position + (camTrans.forward * spawnDistance);

        // 2. 회전: 일단 기본 회전으로 생성
        Quaternion spawnRot = Quaternion.identity;

        // 3. 생성 (Instantiate)
        GameObject obj = Instantiate(contentPrefab, spawnPos, spawnRot);

        // 4. 부모 해제 (월드 고정)
        obj.transform.parent = null;

        // 5. (옵션) 수달이 나를 바라보게 회전
        if (lookAtCamera)
        {
            // 수달의 높이(Y)는 유지하면서 고개만 돌려 나를 보게 함
            Vector3 targetPos = new Vector3(camTrans.position.x, obj.transform.position.y, camTrans.position.z);
            obj.transform.LookAt(targetPos);
        }

        hasSpawned = true;
        Debug.Log("✨ 화면 중앙에 소환 완료!");
    }

    void OnDestroy()
    {
        if (observerBehaviour)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }
}