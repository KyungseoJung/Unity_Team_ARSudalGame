using UnityEngine;
using Vuforia;

public class MarkerSpawner : MonoBehaviour
{
    [Header("Marker Settings")]
    public string markerID;
    public bool isCollected = false;    // ★ [추가] 수집 완료 여부

    [Header("Settings")]
    public GameObject contentPrefab;
    public float spawnDistance = 0.5f;

    [Header("Size Control")]
    [Range(0.1f, 1.0f)]
    public float screenFillRatio = 0.4f;

    [Header("Fine Tuning")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = new Vector3(0, 180, 0);

    public bool hasSpawned = false;
    private ObserverBehaviour observerBehaviour;

void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();
        
        // [저장된 데이터 로드] 이전에 수집했었다면 바로 끄기
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
        if (isCollected) return; // 수집 완료 시 원천 차단

        if (!hasSpawned && (targetStatus.Status == Status.TRACKED || targetStatus.Status == Status.EXTENDED_TRACKED))
        {
            SpawnAndLock();
        }
    }

    void SpawnAndLock()
    {
        if (GameManager.Instance.CanSpawn() == false || isCollected) return;

        Transform camTrans = Camera.main.transform;

        // 1. 생성
        GameObject obj = Instantiate(contentPrefab, camTrans.position, Quaternion.identity);
        obj.transform.SetParent(camTrans);

        // 2. 기본 배치 및 회전
        obj.transform.localRotation = Quaternion.Euler(rotationOffset);
        obj.transform.localPosition = new Vector3(0, 0, spawnDistance);

        RubbableObject rubScript = obj.GetComponent<RubbableObject>();
        if (rubScript != null)
        {
            rubScript.mySpawner = this; // RubbableObject에 이 변수를 추가해야 함
        }

        // 렌더러 기반 스케일 조절
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) rend = obj.GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            // Auto-Scaling 로직
            float frustumHeight = 2.0f * spawnDistance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float targetSize = frustumHeight * screenFillRatio;
            float currentSize = Mathf.Max(rend.bounds.size.x, rend.bounds.size.y, rend.bounds.size.z);

            if (currentSize > 0)
            {
                float scaleFactor = targetSize / currentSize;
                obj.transform.localScale *= scaleFactor;
            }

            // 중앙 정렬
            Vector3 centerOffset = rend.bounds.center - obj.transform.position;
            obj.transform.position -= centerOffset;

            // ★ [추가] UI 매니저를 통해 정수리 이름표 띄우기 (소환 시 1회 계산)
            if (UIManager.Instance != null)
            {
                string itemName = obj.name.Replace("(Clone)", "");
                UIManager.Instance.ShowOtterInfo(itemName);
            }
        }

        // 3. 미세 위치 조정
        obj.transform.localPosition += positionOffset;

        // 4. 소환 즉시 마커 인식 일시 중지 (각도/조명 떨림 방지)
        if (observerBehaviour != null)
        {
            observerBehaviour.enabled = false;
            Debug.Log("<color=yellow>마커 트래킹 일시 중지 (오브젝트 고정)</color>");
        }

        if (observerBehaviour) observerBehaviour.enabled = false;

        GameManager.Instance.RegisterObject(obj);
        hasSpawned = true;
    }

    // ★ [추가] 수집 완료 시 GameManager가 호출할 함수
    public void MarkAsCollected()
    {
        isCollected = true;
        hasSpawned = false;

        // 마커 기능을 영구적으로 꺼서 다시는 인식되지 않게 함
        if (observerBehaviour != null)
        {
            observerBehaviour.enabled = false;
        }
        Debug.Log($"<color=red>✔수집 완료: 마커가 비활성화되었습니다.</color>");
    }

    public void ResetTracking()
    {
        // ★ 이미 수집된 마커는 리셋되지 않음
        if (isCollected) return;

        if (observerBehaviour != null)
        {
            observerBehaviour.enabled = true;
            hasSpawned = false;
            Debug.Log("🔄 [Marker] 트래킹 리셋");
        }
    }

    void OnDestroy()
    {
        if (observerBehaviour)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }
}