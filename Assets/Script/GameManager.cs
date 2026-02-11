using UnityEngine;
using Vuforia;

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
        // 주석처리
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

    // GetComponent 대신 GetComponentInChildren을 사용하여 자식들까지 탐색합니다.
    RubbableObject rubObj = obj.GetComponentInChildren<RubbableObject>();
    

    if (rubObj != null)
    {
        // 1. 기존에 혹시 연결된게 있다면 중복 방지를 위해 끊어줌
        rubObj.OnCleaningCompleted -= HandleCleaningComplete;

        // 2. 이벤트 구독
        rubObj.OnCleaningCompleted += HandleCleaningComplete;

        Debug.Log($"[GameManager] {rubObj.itemName} 추적 시작");
    }
    else
    {
        Debug.LogWarning($"{obj.name}과 그 자식들에게서 RubbableObject를 찾을 수 없습니다!");
    }
}

    // ★ 수달이 청소를 끝내고(OnCleaningCompleted) 호출할 함수
    // RubbableObject 스크립트에서 invoke할 때 자기 자신(this)을 매개변수로 넘겨줘야 함
    void HandleCleaningComplete(RubbableObject completedItem)
    {
        Debug.Log($"[GameManager] 청소 완료 확인: {completedItem.itemName}");

        // string cleanedName = completedItem.itemName.Replace("(Clone)", "");
        // UIManager.Instance.ShowAcquirePopup(cleanedName);
        // *** 루트 오브젝트 이름(= Hairy-nosed_otter)로 표시

        string displayName;

        if (completedItem.CompareTag("Otter"))
        {
            // 1) Otter_Mesh에서 시작해서 부모로 올라가며,
            //    "(Clone)"이 붙어있는 실체 프리팹(Eurasian_otter(Clone))을 찾는다.
            Transform t = completedItem.transform;
            Transform best = null;

            while (t != null)
            {
                if (t.name.Contains("(Clone)"))
                {
                    best = t;            // Eurasian_otter(Clone) 같은 애를 잡음
                    break;
                }
                t = t.parent;
            }

            displayName = (best != null) ? best.name : completedItem.transform.name;
        }
        else
        {
            displayName = completedItem.itemName;
        }

        // 2) 문자열 정리: Wrapper 제거 + (Clone) 제거 + _ -> 공백
        displayName = displayName.Replace("Wrapper_", "");
        displayName = displayName.Replace("(Clone)", "");
        displayName = displayName.Replace("_", " ");
        displayName = displayName.Trim();

        UIManager.Instance.ShowAcquirePopup(displayName);


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

    public void SetVuforiaActive(bool isActive)
    {
        // VuforiaBehaviour가 인식 엔진을 담당합니다.
        VuforiaBehaviour.Instance.enabled = isActive;
    }

}