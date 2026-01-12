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

        // ★ 핵심 연결 고리! ★
        RubbableObject rubObj = obj.GetComponent<RubbableObject>();
        if (rubObj != null)
        {
            // "수달아, 네가 청소 끝났다고 방송(Event)하면, 
            // 내가 인벤토리 매니저한테 연락해서 아이템 넣으라고 시킬게."
            rubObj.OnCleaningCompleted += HandleItemCollected;

            // 나중에 수달이 사라지면 나한테도 알려줘 (내 자리 비우게)
            rubObj.OnCleaningCompleted += HandleObjectDestroyed;
        }
    }

    // 인벤토리에 추가하라고 시키는 함수
    void HandleItemCollected(string itemName)
    {
        Debug.Log("얻은 아이템 이름! " + itemName);
        if (InventoryManager.Instance != null)
        { 
            //InventoryManager.Instance.AddItem(itemName); 추후 인벤토리매니저에서 아이템 추가 함수 생기면 수정
        }
    }

    // 자리 비우는 함수
    void HandleObjectDestroyed(string itemName) // 매개변수 맞춰줌
    {
        UnregisterObject();
    }

    // 수집 완료해서 사라질 때 해제하기
    public void UnregisterObject()
    {
        currentActiveObject = null;
        Debug.Log("🔓 오브젝트 해제됨: 이제 다른 소환 가능!");
    }
}