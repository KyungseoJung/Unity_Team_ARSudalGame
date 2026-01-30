using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤

    // 현재 화면에 나와 있는 인터랙션 오브젝트 (없으면 null)
    public GameObject currentActiveObject = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 2. 인스턴스 할당 및 씬 전환 시 파괴 방지
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //PlayerPrefs.DeleteAll();
    }

    // Q: 지금 소환해도 되나요?
    public bool CanSpawn()
    {
        // 현재 활성화된 오브젝트가 없어야 소환 가능
        return currentActiveObject == null;
    }

    // 소환했을 때 등록하기
    public void RegisterObject(GameObject obj)
    {
        currentActiveObject = obj;

        RubbableObject rubObj = obj.GetComponent<RubbableObject>();
        if (rubObj != null)
        {
            // 1. 기존에 혹시 연결된게 있다면 중복 방지를 위해 끊어줌 (안전장치)
            rubObj.OnCleaningCompleted -= HandleCleaningComplete;

            // 2. 이벤트 구독: "청소 끝나면 HandleCleaningComplete 함수를 실행해!"
            rubObj.OnCleaningCompleted += HandleCleaningComplete;

            Debug.Log($"[GameManager] {rubObj.itemName} 추적 시작");
        }
    }

    // ★ 수달이 청소를 끝내고(OnCleaningCompleted) 호출할 함수
    // RubbableObject 스크립트에서 invoke할 때 자기 자신(this)을 매개변수로 넘겨줘야 함
    void HandleCleaningComplete(RubbableObject completedItem)
    {
        Debug.Log($"[GameManager] 청소 완료 확인: {completedItem.itemName}" + completedItem.transform.childCount);


        UIManager.Instance.ShowAcquirePopup(completedItem.itemName);

        if (completedItem.mySpawner != null)
        {
            completedItem.mySpawner.MarkAsCollected();
        }
        else
        {
            // 예외 상황: 혹시 모르니 전체 리셋 (기존 방식)
            ResetAllTrackingSystems();
        }

        // 1. 인벤토리에 아이템 추가
        if (InventoryManager.Instance != null)
        {
            // 수달이 가지고 있는 itemType 정보를 넘겨줌
            InventoryManager.Instance.AcquireItem(completedItem.itemType);
        }

        // 2. 현재 활성화된 오브젝트 비우기 (이제 다음 수달 소환 가능)
        if (currentActiveObject == completedItem.gameObject)
        {
            currentActiveObject = null;
        }
    }
    private void ResetAllTrackingSystems()
    {
        // 최신 유니티 방식: 씬에 있는 모든 스포너를 찾아 초기화
        // (마커가 여러 개일 수도 있으므로 FindAny보다는 Objects(복수형)가 안전합니다)
        MarkerSpawner[] spawners = Object.FindObjectsByType<MarkerSpawner>(FindObjectsSortMode.None);

        foreach (var spawner in spawners)
        {
            spawner.ResetTracking();
        }
    }
}