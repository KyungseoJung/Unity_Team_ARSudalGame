using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;  // �κ��丮 ��ü �г�
    public Transform slotsArea;        // ���Ե��� ������ �θ� Transform
    public GameObject slotPrefab;      // ���� UI ������

    private void Start()
    {
        // ������ �� UI �ʱ�ȭ
        inventoryPanel.SetActive(false); // �⺻������ �ݾƵ�
        RefreshUI();

        // �� �ٽ�: �Ŵ����� �̺�Ʈ ���� (�����Ͱ� ���ϸ� ������ �˷���)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated += RefreshUI;
        }
    }

    private void OnDestroy()
    {
        // ���� �ٲ�ų� ��ü�� �ı��� �� ���� ���� (�޸� ���� ����)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated -= RefreshUI;
        }
    }

    // �κ��丮 ���� �ݱ� (��ư �����)
    public void ToggleInventory()
    {
        bool isActive = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isActive);

        if (isActive)
        {
            RefreshUI(); // �� �� �ֽ� ���·� ����
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

    // ���� �׸��� (GenerateSlots)
    void RefreshUI()
    {
        // 1. ���� ���� ����
        foreach (Transform child in slotsArea)
        {
            Destroy(child.gameObject);
        }

        InventoryManager manager = InventoryManager.Instance;
        if (manager == null) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        // 2. ������ Ȯ�� �� ���� ����
        for (int i = 0; i < manager.itemPrefabs.Length; i++)
        {
            // �Ŵ������� "�� �� ������ �־�?" ��� ���
            if (manager.HasItem(i))
            {
                GameObject slot = Instantiate(slotPrefab, slotsArea);

                // ���� ��ũ��Ʈ ����
                InventorySlot slotScript = slot.GetComponent<InventorySlot>();
                if (slotScript != null)
                {
                    // slotScript.Setup(i, "Item " + i);
                    //#7 아이템 이름 변경 -----------------------
                    var typeName = ((InventoryManager.ItemType)i).ToString();
                    slotScript.Setup(i, typeName);
                    Button slotBtn = slot.GetComponent<Button>();

                    // ���� Ŭ�� �̺�Ʈ ���� (Ŭ�� �� ���� + â �ݱ�)

                    if (currentSceneName != "Item_Get_Scene")
                    {
                        int index = i;
                        slotBtn.onClick.AddListener(() => {
                            manager.SpawnItem(index);
                            ToggleInventory();
                        });
                    }
                }
            }
        }
    }

    // "ȸ���ϱ�" ��ư�� ������ �Լ�
    public void OnReturnButtonClicked()
    {
        InventoryManager.Instance.ReturnSelectedItem();
    }
}