using UnityEngine;
using Vuforia;

public class MarkerSpawner : MonoBehaviour
{
    [Header("Marker Settings")]
    public string markerID;
    public bool isCollected = false;

    [Header("Settings")]
    public GameObject contentPrefab;
    public float spawnDistance = 0.5f; // 카메라 앞 거리

    [Header("Size Control")]
    [Range(0.1f, 1.0f)]
    public float screenFillRatio = 0.4f;

    [Header("Fine Tuning")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = new Vector3(0, 180, 0);

    private ObserverBehaviour observerBehaviour;
    private bool hasSpawned = false;

    void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();

        markerID = contentPrefab.name;
        if (PlayerPrefs.GetInt(markerID, 0) == 1)
        {
            isCollected = true;
            if (observerBehaviour) observerBehaviour.enabled = false;
            return;
        }

        if (observerBehaviour)
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        if (isCollected) return;

        if (!hasSpawned && (targetStatus.Status == Status.TRACKED || targetStatus.Status == Status.EXTENDED_TRACKED))
        {
            SpawnAndLock();
        }
    }

    void SpawnAndLock()
    {
        if (GameManager.Instance.CanSpawn() == false || isCollected) return;

        Transform camTrans = Camera.main.transform;

        // 1. 빈 부모(Wrapper) 생성
        GameObject wrapper = new GameObject($"Wrapper_{contentPrefab.name}");

        // ★ [핵심 변경] 생성 즉시 카메라의 자식으로 넣습니다.
        wrapper.transform.SetParent(camTrans);

        // ★ [핵심 변경] 월드 좌표가 아닌 '로컬 좌표'를 사용하여 카메라 정중앙 앞(Z축)에 배치합니다.
        wrapper.transform.localPosition = new Vector3(0, 0, spawnDistance);

        // 회전도 로컬 기준으로 설정 (카메라 기준 180도 돌리기 등)
        wrapper.transform.localRotation = Quaternion.Euler(rotationOffset);
        wrapper.transform.localScale = Vector3.one;

        // 2. 실제 모델 생성 및 Wrapper 자식 설정
        GameObject model = Instantiate(contentPrefab);
        model.transform.SetParent(wrapper.transform);

        // 모델 초기화 (일단 부모인 Wrapper의 원점에 둠)
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        // 3. Rubbable 연결
        RubbableObject rubScript = model.GetComponent<RubbableObject>();
        if (rubScript != null) rubScript.mySpawner = this;

        // 4. ★ [자동 보정] 크기 조절 및 피벗(중심점) 맞추기
        NormalizeSizeAndPivot(wrapper, model);

        // 5. 사용자 지정 미세 조정 (Local 기준)
        wrapper.transform.localPosition += positionOffset;

        // 6. UI 표시
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOtterInfo(model.name.Replace("(Clone)", ""));
        }

        // 7. 마커 트래킹 중지
        if (observerBehaviour != null) observerBehaviour.enabled = false;

        GameManager.Instance.RegisterObject(model);
        hasSpawned = true;
    }

    void NormalizeSizeAndPivot(GameObject wrapper, GameObject model)
    {
        // 1. 모델의 전체 Bounds 구하기 (월드 기준)
        Bounds totalBounds = GetTotalBounds(model);
        if (totalBounds.size == Vector3.zero) return;

        // --- 크기(Scale) 보정 ---
        float frustumHeight = 2.0f * spawnDistance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float targetSize = frustumHeight * screenFillRatio;
        float currentMaxSize = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);

        if (currentMaxSize > 0)
        {
            float scaleFactor = targetSize / currentMaxSize;
            wrapper.transform.localScale = Vector3.one * scaleFactor;
        }

        // --- 중심점(Pivot) 보정 ---
        // 스케일이 변했으므로 Bounds를 다시 계산하는 것이 가장 안전함
        totalBounds = GetTotalBounds(model);

        // 모델의 '시각적 중심(Center)'이 Wrapper의 '원점(Position)'과 얼마나 차이나는지 계산
        // ★ 중요: 부모(Wrapper) 기준 로컬 좌표계로 변환해서 계산해야 정확함
        Vector3 centerInWrapperSpace = wrapper.transform.InverseTransformPoint(totalBounds.center);

        // 모델을 반대 방향으로 이동시켜서 시각적 중심을 0,0,0에 맞춤
        model.transform.localPosition = -centerInWrapperSpace;
    }

    Bounds GetTotalBounds(GameObject rootObj)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            if (!hasBounds)
            {
                bounds = rend.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rend.bounds);
            }
        }
        return bounds;
    }

    public void MarkAsCollected()
    {
        isCollected = true;
        hasSpawned = false;
        PlayerPrefs.SetInt(markerID, 1);
        PlayerPrefs.Save();
        if (observerBehaviour != null) observerBehaviour.enabled = false;
    }

    public void ResetTracking()
    {
        if (isCollected) return;
        if (observerBehaviour != null)
        {
            observerBehaviour.enabled = true;
            hasSpawned = false;
        }
    }

    void OnDestroy()
    {
        if (observerBehaviour) observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }
}