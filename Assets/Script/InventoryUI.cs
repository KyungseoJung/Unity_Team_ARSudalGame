using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;


public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;  // �κ��丮 ��ü �г�
    public Transform slotsArea;        // ���Ե��� ������ �θ� Transform
    public GameObject slotPrefab;      // ���� UI ������

    [Header("Target UI")]
    public GameObject mainUI; // 숨길 UI 부모

    [Header("Button Visuals")]
    public Image buttonIcon;   // 자식 Icon 이미지 연결
    public Sprite eyeOpen;     // 표시 중일 때 아이콘
    public Sprite eyeClosed;   // 숨김 중일 때 아이콘

    private string inventoryHelpMessage = "학회장에서 수달과 자연환경을 찾아 수집 후 자신만의 수달을 위한 자연환경을 꾸며주세요!";
    private bool isVisible = true;

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
        else if(sceneName == "Item_Place_Scene")
        {
            // 씬 이동 전에 현재 배치 씬이라면 저장
            var saver = FindFirstObjectByType<ItemSaveManager>();
            if (saver != null)
            {
                saver.SaveAll();
            }
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
                    // var typeName = ((InventoryManager.ItemType)i).ToString();
                    // slotScript.Setup(i, typeName);
                    //#10 인벤토리 각 슬롯에 아이템 이미지 나타나도록 하기 
                    Sprite icon = null;
                    if (manager.itemIcons != null && i < manager.itemIcons.Length)
                    {
                        icon = manager.itemIcons[i];
                    }

                    // 이름은 이제 안 쓰고 싶으면 null로 넘기면 됨
                    slotScript.Setup(i, icon, null);


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
        // InventoryManager.Instance.ReturnSelectedItem();
        //#8 아무것도 안 누르고 Return 눌렀을 때, 빨간 에러 발생하는 것 고치기
        var manager = InventoryManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("InventoryUI: InventoryManager.Instance is null (Return ignored).");
            return;
        }

        manager.ReturnSelectedItem();
    }

    public void OnInfoButtonClicked()
    {
        if (UIManager.Instance != null)
        {
            // UIManager에게 "이 메세지를 띄워줘"라고 명령
            UIManager.Instance.ShowGeneralInfo(inventoryHelpMessage);
        }
    }

    public void OnInfoButtonCloseClicked()
    {
        if (UIManager.Instance != null)
        {
            // UIManager에게 "이 메세지를 띄워줘"라고 명령
            UIManager.Instance.HideInfoPanel();
        }
    }
    public void ToggleUIVisibleBtn()
    {
        isVisible = !isVisible;

        mainUI.SetActive(isVisible);

        // 2. 아이콘 교체
        if (buttonIcon != null)
        {
            buttonIcon.sprite = isVisible ? eyeOpen : eyeClosed;
        }
    }
    public void TakeScreenshot()
    {
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // 1. UI 숨기기
        if (mainUI != null) { mainUI.SetActive(false); }

        yield return new WaitForEndOfFrame();

        // 2. 스크린샷 텍스처 생성 (화면 크기만큼)
        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();

        // 3. 갤러리에 저장 (Native Gallery 플러그인 기능)
        // 첫 번째 인자: 텍스처, 두 번째 인자: 앨범 이름, 세 번째 인자: 파일 이름
        NativeGallery.SaveImageToGallery(screenTexture, "MyARApp", "Screenshot_{0}.png");

        // 4. 사용한 텍스처는 메모리에서 해제
        Object.Destroy(screenTexture);

        // 5. UI 다시 표시
        if (mainUI != null) { mainUI.SetActive(true); }
    }
}