using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;  // 인벤토리 전체 패널
    public Transform slotsArea;        // 슬롯들이 생성될 부모 Transform
    public GameObject slotPrefab;      // 슬롯 UI 프리팹

    private void Start()
    {
        // 시작할 때 UI 초기화
        inventoryPanel.SetActive(false); // 기본적으로 닫아둠
        RefreshUI();

        // ★ 핵심: 매니저의 이벤트 구독 (데이터가 변하면 나에게 알려줘)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated += RefreshUI;
        }
    }

    private void OnDestroy()
    {
        // 씬이 바뀌거나 객체가 파괴될 때 구독 해제 (메모리 누수 방지)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated -= RefreshUI;
        }
    }

    // 인벤토리 열고 닫기 (버튼 연결용)
    public void ToggleInventory()
    {
        bool isActive = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isActive);

        if (isActive)
        {
            RefreshUI(); // 열 때 최신 상태로 갱신
        }
    }

    public void ChangeScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if(sceneName == "Item_Get_Scene")
        {
            SceneManager.LoadScene("Item_Place_Scene");
        }
        else
        {
            SceneManager.LoadScene("Item_Get_Scene");
        }
    }

    // 슬롯 그리기 (GenerateSlots)
    void RefreshUI()
    {
        // 1. 기존 슬롯 삭제
        foreach (Transform child in slotsArea)
        {
            Destroy(child.gameObject);
        }

        InventoryManager manager = InventoryManager.Instance;
        if (manager == null) return;

        // 2. 데이터 확인 후 슬롯 생성
        for (int i = 0; i < manager.itemPrefabs.Length; i++)
        {
            // 매니저에게 "나 이 아이템 있어?" 라고 물어봄
            if (manager.HasItem(i))
            {
                GameObject slot = Instantiate(slotPrefab, slotsArea);

                // 슬롯 스크립트 설정
                InventorySlot slotScript = slot.GetComponent<InventorySlot>();
                if (slotScript != null)
                {
                    slotScript.Setup(i, "Item " + i);

                    // 슬롯 클릭 이벤트 연결 (클릭 시 스폰 + 창 닫기)
                    int index = i; // 클로저 문제 방지용 로컬 변수
                    slot.GetComponent<Button>().onClick.AddListener(() => {
                        manager.SpawnItem(index);
                        ToggleInventory(); // 아이템 소환 후 인벤토리 닫기
                    });
                }
            }
        }
    }

    // "회수하기" 버튼에 연결할 함수
    public void OnReturnButtonClicked()
    {
        InventoryManager.Instance.ReturnSelectedItem();
    }
}