using UnityEngine;
using System; // Action 이벤트를 위해 필요
//#11 (수달의 특이한 구조 때문에 추가 작업)
//    Collider가 자식(Otter_Mesh)에 있는 프리팹도 있기 때문에,
//    "히트(클릭) 대상"은 Collider가 있는 Transform,
//    "실제 이동/저장 대상"은 루트 Transform(go)로 분리해서 처리한다.

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Data & Prefabs")]
    public GameObject[] itemPrefabs;   // 아이템 프리팹 (데이터)

    [Header("UI Icons (same order as itemPrefabs)")]
    public Sprite[] itemIcons;         //#10 인벤토리 슬롯에 "텍스트" 대신 "이미지" 넣기

    [Header("World Interaction")]
    public Transform spawnBase;        // 아이템 스폰 위치
    public float spawnHeight = 0.5f;

    // ▼ 이벤트 정의: 데이터가 변경되거나 선택 상태가 바뀔 때 UI에게 알림
    public event Action OnInventoryUpdated;
    public event Action<PlacedItem> OnSelectionChanged;

    // ▼ 내부 로직 변수
    PlacedItem currentSelected;
    int frontOrderCounter = 0;

    public enum ItemType
    {
        Blue_ConeTree, Blue_CubeTree, Green_ConeTree, Blue_Flower, Red_Flower, White_Flower,
        Blue_Fish, Green_Fish, Red_Fish, Grass, Log, Mushroom,
        Eurasian_Otter, Hairy_nosed_Otter, African_clawless_Otter,
        Beaver, Sparrow, Turtle,
        Seashell, Mossy_Stone, Stone
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 초기화 (최초 1회만 실행된다고 가정)
        // 실제 게임에서는 별도 SaveManager가 있는게 좋지만 일단 여기 유지
        // PlayerPrefs.DeleteAll(); 
        // ┗> 배치된 아이템 위치를 저장하고 다시 불러오기 위해, 이 코드는 일단 주석처리함
        if (!PlayerPrefs.HasKey("Initialized"))
        {
            // for (int i = 0; i < 5; i++) PlayerPrefs.SetInt("ITEM_" + i, 0);
            //#7 아이템 이름 변경 -----------------------
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                PlayerPrefs.SetInt(GetItemKey(type), 0);
            }

            PlayerPrefs.SetInt("Initialized", 1);
            PlayerPrefs.Save();
        }
    }

    private void Update()
    {
        // // 테스트용 입력 (데이터 획득 로직)
        // if (Input.GetKeyDown(KeyCode.Alpha0)) AcquireItem(ItemType.OTTER);
        // if (Input.GetKeyDown(KeyCode.Alpha1)) AcquireItem(ItemType.TREE);
        // if (Input.GetKeyDown(KeyCode.Alpha2)) AcquireItem(ItemType.PLANT);
        // if (Input.GetKeyDown(KeyCode.Alpha3)) AcquireItem(ItemType.STONE);
        // if (Input.GetKeyDown(KeyCode.Alpha4)) AcquireItem(ItemType.MOUNTAIN);
    }

    // ====================================================
    // 1. 데이터 관리 (Data Logic)
    // ====================================================
    public void AcquireItem(ItemType itemType)
    {
        //#7 아이템 이름 변경 : 이제는 아이템 이름으로 설정 ------------------
        string key = GetItemKey(itemType);
        PlayerPrefs.SetInt(key, 1);

        PlayerPrefs.Save();
        Debug.Log($"Item acquired: {itemType}");

        // ★ 핵심: 데이터가 변했으니 UI에게 업데이트하라고 알림
        OnInventoryUpdated?.Invoke();
    }

    public bool HasItem(ItemType type)
    {
        //#7 아이템 이름 변경 -----------------------
        return PlayerPrefs.GetInt(GetItemKey(type), 0) == 1;
        // 아이템을 가지고 있으면 true/ 없으면 false 리턴~~
    }

    //#7 int 버전도 만들어줘서, 기존에 InventoryUI.cs와 연결된 부분을 그대로 유지해주자
    public bool HasItem(int index)
    {
        return HasItem((ItemType)index);
    }

    private string GetItemKey(ItemType type)    //#7 아이템 이름 변경 : 이제는 아이템 이름으로 설정 ------------------
    {
        return $"ITEM_{type}";
    }

    // ====================================================
    // 2. 월드 상호작용 (World Interaction Logic)
    // ====================================================
    public void SpawnItem(int index)
    {
        Vector3 basePos = (spawnBase != null) ? spawnBase.position : Vector3.zero;
        Vector3 spawnPos = basePos + Vector3.up * spawnHeight;

        Debug.Log("아이템 소환: " + itemPrefabs[index]);

        GameObject go = Instantiate(itemPrefabs[index], spawnPos, Quaternion.identity);

        //#11 여기부터--------------------------------------
        // 1) 드래그 판정은 "Collider가 있는 오브젝트"가 하게 만들기
        Transform hitT = FindColliderTransform(go);

        // 2) PlacedItem은 루트(go)에 붙인다 (아이템 1개 단위로 저장/선택/삭제 기준)
        var placed = go.GetComponent<PlacedItem>();
        if (placed == null) placed = go.AddComponent<PlacedItem>();
        placed.itemIndex = index;

        // 3) ItemDragger2D는 hitT에 붙인다 (레이캐스트가 hitT에 맞으니까)
        var dragger = hitT.GetComponent<ItemDragger2D>();
        if (dragger == null) dragger = hitT.gameObject.AddComponent<ItemDragger2D>();

        // 4) 핵심: 실제로 움직일 대상은 "루트"로 지정
        dragger.SetMoveTarget(go.transform);
        //#11 여기까지--------------------------------------

        // 수달처럼 RubbableObject가 자식에 붙어있을 수 있으므로 InChildren로 처리
        go.GetComponentInChildren<RubbableObject>(true)?.ApplyCleanedState();

        // 아이템 생성 후 인벤토리를 닫고 싶다면 UI 쪽에서 처리하거나,
        // 여기서 이벤트를 보낼 수도 있습니다. (여기서는 UI가 알아서 닫도록 유도)
    }

    public void SelectItem(PlacedItem item)
    {
        if (currentSelected != null && currentSelected != item)
        {
            currentSelected.SetSelected(false);
        }

        currentSelected = item;

        if (currentSelected != null)
        {
            currentSelected.SetSelected(true);
            BringToFront(currentSelected);
        }

        // 선택 변경 알림 (UI가 '회수 버튼'을 활성화할지 결정할 수 있음)
        OnSelectionChanged?.Invoke(currentSelected);
    }

    public PlacedItem GetSelected() //#9-2 선택한 아이템에 대해서만 크기 변경되도록
    {
        return currentSelected;
    }

    public void ClearSelection()
    {
        if (currentSelected != null)
        {
            currentSelected.SetSelected(false);
        }
        currentSelected = null;
        OnSelectionChanged?.Invoke(null);
    }

    public void ReturnSelectedItem()
    {
        if (currentSelected == null) return;

        Destroy(currentSelected.gameObject);
        ClearSelection();
    }

    void BringToFront(PlacedItem item)
    {
        // 수달처럼 Renderer가 자식에 있는 경우도 있으니 InChildren로 찾는다
        Renderer rend = item.GetComponentInChildren<Renderer>(true);
        if (rend == null) return;

        int baseQueue = 3000;
        frontOrderCounter++;
        rend.material.renderQueue = baseQueue + frontOrderCounter;
    }

    //#11 (수달의 특이한 구조 때문에 추가 작업)
    //    Collider가 루트에 있으면 루트가 hitT,
    //    아니면 자식 Collider(예: Otter_Mesh)가 hitT가 된다.
    private Transform FindColliderTransform(GameObject root)    //#11
    {
        // 루트에 Collider 있으면 그게 타겟
        var col = root.GetComponent<Collider>();
        if (col != null) return col.transform;

        // 자식에서 Collider 찾기(비활성 포함)
        col = root.GetComponentInChildren<Collider>(true);
        if (col != null) return col.transform;

        // 없으면 루트
        return root.transform;
    }
}
