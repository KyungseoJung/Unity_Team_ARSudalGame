using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    // //#3 (위치:InventoryManager라는 Empty 오브젝트에 부착) 
    /* 역할
    1) 어떤 아이템을 가지고 있는지 확인
    2) 슬롯 생성
    3) 슬롯 클릭 시 아이템 Instantiate
    4) 인벤토리 열고 닫기
    */
    // //#4 인벤토리에서 클릭하면 화면에 Instantiate 되도록
    public static InventoryManager Instance;

    public GameObject inventoryPanel;  
    public Transform slotsArea;        
    public GameObject slotPrefab;      

    public GameObject[] itemPrefabs;   // Cube 프리팹 1~5개

    public Transform spawnBase;        //#4
    public float spawnHeight = 0.5f;   //#4

    // ▼▼▼ 선택 & 렌더 순서 관리를 위한 변수들 추가                            //#4-2
    PlacedItem currentSelected;        //#4-2: 현재 선택된 아이템
    int frontOrderCounter = 0;         //#4-2: 최근에 선택된 순서를 반영할 카운터
    // ▲▲▲                                                                  //#4-2

    private void Awake()
    {
        // 1. 싱글톤 체크: 이미 인스턴스가 있다면 새로 생긴 놈은 파괴
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // 2. 인스턴스 할당 및 씬 전환 시 파괴 방지
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        //#4 임시로 모든 프리팹이 존재한다고 가정하자.
        PlayerPrefs.SetInt("ITEM_0", 1);
        PlayerPrefs.SetInt("ITEM_1", 0);
        PlayerPrefs.SetInt("ITEM_2", 1);
        PlayerPrefs.SetInt("ITEM_3", 1);
        PlayerPrefs.SetInt("ITEM_4", 1);
        PlayerPrefs.Save();

        inventoryPanel.SetActive(false);
        GenerateSlots();
    }

    void GenerateSlots()
    {
        foreach (Transform child in slotsArea)   // 변경됨 (새로 추가)
        {
            Destroy(child.gameObject);
        }
        
        // 슬롯 5개 생성
        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            // PlayerPrefs에서 1이면 “획득한 아이템”
            if (PlayerPrefs.GetInt("ITEM_" + i, 0) == 0)
            {
                continue;
            }

            GameObject slot = Instantiate(slotPrefab, slotsArea);

            InventorySlot slotScript = slot.GetComponent<InventorySlot>();   //#4
            slotScript.Setup(i, "Item " + (i + 1));                        //#4

            /* //#4 삭제
            int index = i;

            // 버튼 클릭 시 아이템 소환
            slot.GetComponent<Button>().onClick.AddListener(() =>
            {
                SpawnItem(index);
            });

            // 슬롯 텍스트 변경 (선택 사항)
            slot.GetComponentInChildren<Text>().text = "아이템 " + (i + 1);
            */
        }
    }

    public void SpawnItem(int index)
    {
        //#4 [변경됨] : 카메라 앞이 아니라 "초록 바닥(Quad) 위"에 생성하도록 변경
        Vector3 basePos = (spawnBase != null) ? spawnBase.position : Vector3.zero;   // 변경됨
        Vector3 spawnPos = basePos + Vector3.up * spawnHeight;                       // 변경됨

        // Instantiate(itemPrefabs[index], spawnPos, Quaternion.identity);    
        GameObject go = Instantiate(itemPrefabs[index], spawnPos, Quaternion.identity);

        // Camera cam = Camera.main;
        // Vector3 pos = cam.transform.position + cam.transform.forward * 0.5f;

        // Instantiate(itemPrefabs[index], pos, Quaternion.identity);

        //#4 새로 생성된 아이템에 PlacedItem 컴포넌트 추가
        // go.AddComponent<PlacedItem>();  // 기존 코드

        // ▼▼▼ 새로 생성된 아이템에 드래그/선택 기능을 붙여준다                 //#4-2
        if (go.GetComponent<ItemDragger2D>() == null)                      //#4-2
        {
            go.AddComponent<ItemDragger2D>();                              //#4-2
        }

        if (go.GetComponent<PlacedItem>() == null)                         //#4-2
        {
            go.AddComponent<PlacedItem>();                                 //#4-2
        }
        // ▲▲▲                                                                //#4-2
        
        inventoryPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    // ▼▼▼ 아이템 "선택" 처리: PlacedItem에서 호출                          //#4-2
    public void SelectItem(PlacedItem item)                                //#4-2
    {
        // 이전에 선택된 아이템이 있으면 선택 해제                          //#4-2
        if (currentSelected != null && currentSelected != item)            //#4-2
        {
            currentSelected.SetSelected(false);                            //#4-2
        }

        currentSelected = item;                                            //#4-2

        if (currentSelected != null)                                       //#4-2
        {
            currentSelected.SetSelected(true);                             //#4-2
            BringToFront(currentSelected);                                 //#4-2
        }
    }
    // ▲▲▲                                                                   //#4-2

    // ▼▼▼ 가장 최근에 선택된 아이템을 "맨 위"로 보이게 하는 함수           //#4-2
    void BringToFront(PlacedItem item)                                     //#4-2
    {
        Renderer rend = item.GetComponent<Renderer>();                     //#4-2
        if (rend == null) return;                                         //#4-2

        Material mat = rend.material;                                     //#4-2
        int baseQueue = 3000; // 기본 렌더 큐 (Opaque 기준)                //#4-2

        frontOrderCounter++;                                              //#4-2
        mat.renderQueue = baseQueue + frontOrderCounter;                  //#4-2
    }
    // ▲▲▲                                                                   //#4-2

    // ▼▼▼ UI에서 "회수" 버튼이 눌렸을 때 호출할 함수                       //#4-2
    public void ReturnSelectedItem()                                      //#4-2
    {
        if (currentSelected == null) return;                               //#4-2

        Destroy(currentSelected.gameObject);                               //#4-2
        currentSelected = null;                                            //#4-2
    }
    // ▲▲▲                                                                   //#4-2
}
