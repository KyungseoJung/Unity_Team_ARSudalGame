using UnityEngine;
using Vuforia;

public class ScreenFixedSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject contentPrefab; // 수달 프리팹
    public float spawnDistance = 0.5f; // 카메라 앞 0.5m

    [Header("Fine Tuning")]
    // 만약 중앙이 안 맞으면 이 값들을 조절해서 맞추세요 (모델 피벗 보정용)
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = new Vector3(0, 180, 0); // 기본적으로 카메라 마주보기

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
            Debug.Log("🚫 이미 다른 오브젝트가 있어서 소환할 수 없습니다.");
            return;
        }

        Transform camTrans = Camera.main.transform;

        // 1. 생성
        GameObject obj = Instantiate(contentPrefab, camTrans.position, Quaternion.identity);

        // 2. 카메라의 자식으로 등록 (화면 고정의 핵심)
        obj.transform.SetParent(camTrans);

        // 3. 위치 고정 (Local Position 사용)
        // (0, 0, 거리)는 수학적으로 카메라의 정중앙입니다.
        // 여기에 offset을 더해서 모델의 피벗 위치를 보정합니다.
        obj.transform.localPosition = new Vector3(0, 0, spawnDistance) + positionOffset;

        // 4. 회전 고정
        obj.transform.localRotation = Quaternion.Euler(rotationOffset);
        GameManager.Instance.RegisterObject(obj);

        hasSpawned = true;
        Debug.Log("✨ 수달 화면 중앙 고정 완료!");
    }

    void OnDestroy()
    {
        if (observerBehaviour)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
    }
}