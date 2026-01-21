using UnityEngine;
using Vuforia;

public class MarkerSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject contentPrefab;
    public float spawnDistance = 0.5f;

    [Header("Size Control")]
    [Range(0.1f, 1.0f)]
    public float screenFillRatio = 0.4f; // 화면 세로 높이의 40%만큼 차지하게 설정

    [Header("Fine Tuning")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = new Vector3(0, 180, 0);

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
        if (!hasSpawned && (targetStatus.Status == Status.TRACKED || targetStatus.Status == Status.EXTENDED_TRACKED))
        {
            SpawnAndLock();
        }
    }

    void SpawnAndLock()
    {
        if (GameManager.Instance.CanSpawn() == false)
        {
            Debug.Log("🚫 이미 소환됨");
            return;
        }

        Transform camTrans = Camera.main.transform;

        // 1. 생성
        GameObject obj = Instantiate(contentPrefab, camTrans.position, Quaternion.identity);
        obj.transform.SetParent(camTrans);

        // 2. 회전 & 기본 위치 설정 (스케일 계산을 위해 먼저 배치)
        obj.transform.localRotation = Quaternion.Euler(rotationOffset);
        obj.transform.localPosition = new Vector3(0, 0, spawnDistance);

        // 렌더러 가져오기 (크기 계산용)
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) rend = obj.GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            // ================================================================
            // ★ [추가된 부분 1] 화면 비율에 맞춰 스케일 자동 조절 (Auto-Scaling)
            // ================================================================

            // A. 현재 거리에서 카메라가 볼 수 있는 '실제 월드 높이' 계산 (공식)
            float frustumHeight = 2.0f * spawnDistance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // B. 우리가 원하는 물체의 목표 크기 (화면 높이의 N%)
            float targetSize = frustumHeight * screenFillRatio;

            // C. 현재 물체의 크기 (Bounds의 최대 축 사용)
            float currentSize = Mathf.Max(rend.bounds.size.x, rend.bounds.size.y, rend.bounds.size.z);

            // D. 비율 계산 (목표 / 현재)
            if (currentSize > 0)
            {
                float scaleFactor = targetSize / currentSize;
                obj.transform.localScale *= scaleFactor;
            }

            // ================================================================
            // ★ [추가된 부분 2] 스케일 변경 후 중앙 정렬 (Auto-Centering)
            // ================================================================
            // 스케일이 변하면 Bounds도 변하므로, 스케일 조정 후에 위치를 다시 잡아야 정확함

            Vector3 centerOffset = rend.bounds.center - obj.transform.position;
            obj.transform.position -= centerOffset;
        }

        // 3. 미세 위치 조정 적용
        obj.transform.localPosition += positionOffset;

        GameManager.Instance.RegisterObject(obj);
        hasSpawned = true;

        Debug.Log($"✨ 소환 완료! (화면 비율: {screenFillRatio * 100}%)");
    }

    void OnDestroy()
    {
        if (observerBehaviour)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }
}