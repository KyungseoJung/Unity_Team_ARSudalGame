using UnityEngine;
using System; // Action 이벤트를 위해 필요

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Data & Prefabs")]
    public GameObject[] itemPrefabs;   // 아이템 프리팹 (데이터)

    [Header("World Interaction")]
    public Transform spawnBase;        // 아이템 스폰 위치
    public float spawnHeight = 0.5f;

    // ▼ 이벤트 정의: 데이터가 변경되거나 선택 상태가 바뀔 때 UI에게 알림
    public event Action OnInventoryUpdated;
    public event Action<PlacedItem> OnSelectionChanged;

    // ▼ 내부 로직 변수
    PlacedItem currentSelected;
    int frontOrderCounter = 0;

    public enum ItemType { RED, ORANGE, YELLOW, GREEN, BLUE }

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
        PlayerPrefs.DeleteAll();
        if (!PlayerPrefs.HasKey("Initialized"))
        {
            for (int i = 0; i < 5; i++) PlayerPrefs.SetInt("ITEM_" + i, 0);
            PlayerPrefs.SetInt("Initialized", 1);
            PlayerPrefs.Save();
        }
    }

    private void Update()
    {
        // 테스트용 입력 (데이터 획득 로직)
        if (Input.GetKeyDown(KeyCode.Alpha0)) AcquireItem(ItemType.RED);
        if (Input.GetKeyDown(KeyCode.Alpha1)) AcquireItem(ItemType.ORANGE);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AcquireItem(ItemType.YELLOW);
    }

    // ====================================================
    // 1. 데이터 관리 (Data Logic)
    // ====================================================
    public void AcquireItem(ItemType itemType)
    {
        int index = (int)itemType;
        PlayerPrefs.SetInt("ITEM_" + index, 1);
        PlayerPrefs.Save();

        Debug.Log($"Item acquired: {itemType}");

        // ★ 핵심: 데이터가 변했으니 UI에게 업데이트하라고 알림
        OnInventoryUpdated?.Invoke();
    }

    public bool HasItem(int index)
    {
        return PlayerPrefs.GetInt("ITEM_" + index, 0) == 1;
    }

    // ====================================================
    // 2. 월드 상호작용 (World Interaction Logic)
    // ====================================================
    public void SpawnItem(int index)
    {
        Vector3 basePos = (spawnBase != null) ? spawnBase.position : Vector3.zero;
        Vector3 spawnPos = basePos + Vector3.up * spawnHeight;


 
        GameObject go = Instantiate(itemPrefabs[index], spawnPos, Quaternion.identity);

        // 필요한 컴포넌트 부착 로직 유지
        if (go.GetComponent<ItemDragger2D>() == null) go.AddComponent<ItemDragger2D>();
        if (go.GetComponent<PlacedItem>() == null) go.AddComponent<PlacedItem>();

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
        Renderer rend = item.GetComponent<Renderer>();
        if (rend == null) return;

        int baseQueue = 3000;
        frontOrderCounter++;
        rend.material.renderQueue = baseQueue + frontOrderCounter;
    }
}