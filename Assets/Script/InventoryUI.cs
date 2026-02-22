using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
//#14 인벤토리 카테고리화
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;  // �κ��丮 ��ü �г�
    public Transform slotsArea;        // ���Ե��� ������ �θ� Transform
    public GameObject slotPrefab;      // ���� UI ������
    public TMP_FontAsset myCustomFont;

    [Header("Target UI")]
    public GameObject mainUI; // 숨길 UI 부모

    [Header("Button Visuals")]
    public Image buttonIcon;   // 자식 Icon 이미지 연결
    public Sprite eyeOpen;     // 표시 중일 때 아이콘
    public Sprite eyeClosed;   // 숨김 중일 때 아이콘

    private bool isVisible = true;
    
    public ScrollRect scrollRect;  //#14-2 인벤토리 첫 시작에 스크롤이 맨 위로 가있도록

    private bool snappedOnce = false;   //#14-3 맨 처음 인벤토리를 열 때만 스크롤이 맨 위로 가있도록

    //#14 카테고리 이름(표시용)
    private enum Category
    {
        Tree, Flower, Fish, GroundObjects, Otters, AnimalFriends, OtterHomeItems
    }
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

            //#14-3 맨 처음 인벤토리 열 때만 스크롤이 맨 위로 가있도록
            if (!snappedOnce && scrollRect != null)
            {
                snappedOnce = true;
                StartCoroutine(SnapScrollToTopNextFrame());
            }
        }
    }

    private IEnumerator SnapScrollToTopNextFrame()  //#14-3 맨 처음 인벤토리 열 때만 스크롤이 맨 위로 가있도록
    {
        yield return null; // 다음 프레임 (레이아웃 계산 이후에 가까움)

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f; // 맨 위
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

    // ���� �׸��� (GenerateSlots)   //#14 인벤토리 카테고리화
    void RefreshUI()
    {
        foreach (Transform child in slotsArea)
            Destroy(child.gameObject);

        InventoryManager manager = InventoryManager.Instance;
        if (manager == null) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        // 1) 카테고리별로 인덱스 모으기
        var buckets = new Dictionary<Category, List<int>>();
        foreach (Category c in System.Enum.GetValues(typeof(Category)))
            buckets[c] = new List<int>();

        for (int i = 0; i < manager.itemPrefabs.Length; i++)
        {
            if (!manager.HasItem(i)) continue;

            var cat = GetCategoryByItemType(i);
            buckets[cat].Add(i);
        }

        // 2) 원하는 순서대로 출력
        Category[] order =
        {
            Category.Tree,
            Category.Flower,
            Category.Fish,
            Category.GroundObjects,
            Category.Otters,
            Category.AnimalFriends,
            Category.OtterHomeItems
        };

        foreach (var cat in order)
        {
            // 해당 카테고리에 아무것도 없으면 스킵(원하면 빈 줄로 유도할 수도 있음)
            if (buckets[cat].Count == 0) continue;

            CreateCategoryHeader(SplitCamelCase(cat.ToString()));
            Transform row = CreateRowContainer(cat.ToString());

            foreach (int i in buckets[cat])
            {
                GameObject slot = Instantiate(slotPrefab, row);

                InventorySlot slotScript = slot.GetComponent<InventorySlot>();
                if (slotScript != null)
                {
                    Sprite icon = null;
                    if (manager.itemIcons != null && i < manager.itemIcons.Length)
                        icon = manager.itemIcons[i];

                    slotScript.Setup(i, icon, null);

                    if (currentSceneName != "Item_Get_Scene")
                    {
                        int index = i;
                        Button slotBtn = slot.GetComponent<Button>();
                        slotBtn.onClick.AddListener(() => {
                            manager.SpawnItem(index);
                            ToggleInventory();
                        });
                    }
                }
            }
        }

    //#14-3 삭제
    // //#14-2 인벤토리 첫 시작에 스크롤이 맨 위로 가있도록
    // Canvas.ForceUpdateCanvases(); // 레이아웃 강제 갱신
    // if (scrollRect != null)
    // {
    //     scrollRect.verticalNormalizedPosition = 1f; // 맨 위
    //     scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, 0f);
    // }

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
           UIManager.Instance.ShowGeneralInfo();
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
        if (mainUI != null) 
        { 
            mainUI.SetActive(false);
            buttonIcon.enabled = false;
        }

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
        if (mainUI != null) 
        { 
            mainUI.SetActive(true);
            buttonIcon.enabled = true;
        }
        ToastManager.Instance.ShowToast("Screen captured! Saved to gallery.");
    }

    //#14 인벤토리 카테고리화 (아래 3개 함수)------------------------------------
    // 아이템 인덱스 -> 카테고리 매핑 (가장 안전/간단: ItemType 기준)
    private Category GetCategoryByItemType(int index)
    {
        var type = (InventoryManager.ItemType)index;

        switch (type)
        {
            // (1) Tree 3가지
            case InventoryManager.ItemType.Blue_ConeTree:
            case InventoryManager.ItemType.Blue_CubeTree:
            case InventoryManager.ItemType.Green_ConeTree: 
                return Category.Tree;

            // (2) Flower 3가지
            case InventoryManager.ItemType.Blue_Flower:
            case InventoryManager.ItemType.Red_Flower:
            case InventoryManager.ItemType.White_Flower:
                return Category.Flower;

            // (3) Fish 3가지
            case InventoryManager.ItemType.Blue_Fish:
            case InventoryManager.ItemType.Green_Fish:
            case InventoryManager.ItemType.Red_Fish:
                return Category.Fish;

            // (4) Ground Objects 3가지
            case InventoryManager.ItemType.Grass:
            case InventoryManager.ItemType.Log:
            case InventoryManager.ItemType.Mushroom:
                return Category.GroundObjects;

            // (5) Otters 3가지
            case InventoryManager.ItemType.Eurasian_Otter:
            case InventoryManager.ItemType.Hairy_nosed_Otter:
            case InventoryManager.ItemType.African_clawless_Otter:
                return Category.Otters;

            // (6) Animal Friends 3가지
            case InventoryManager.ItemType.Beaver:
            case InventoryManager.ItemType.Sparrow:
            case InventoryManager.ItemType.Turtle:
                return Category.AnimalFriends;

            // (7) Otter Home Items
            case InventoryManager.ItemType.Seashell:
            case InventoryManager.ItemType.Mossy_Stone:
            case InventoryManager.ItemType.Stone:
                return Category.OtterHomeItems;

            default:
                // 혹시 빠진 것들은 일단 Ground로 보내든지 원하는 곳으로
                return Category.GroundObjects;
        }
    }

    private string SplitCamelCase(string s)
    {
        if(string.IsNullOrEmpty(s))
        {
            return s;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(s[0]);    // 첫 글자는 그대로

        for(int i=1; i<s.Length; i++)
        {
            char c = s[i];
            if(char.IsUpper(c))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }

        return sb.ToString();
    }


    // 카테고리 헤더 생성 (TextMeshProUGUI)
    private void CreateCategoryHeader(string title)
    {
        GameObject headerGO = new GameObject($"Header_{title}", typeof(RectTransform));
        headerGO.transform.SetParent(slotsArea, false);

        var text = headerGO.AddComponent<TextMeshProUGUI>();
        if (myCustomFont != null)
        {
            text.font = myCustomFont;
        }
        text.text = title;

        // ✅ 글자색/정렬
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Left;

        // ✅ 자동 크기
        text.enableAutoSizing = true;
        text.fontSizeMax = 25;   // UI 기준으로 대충
        text.fontSizeMin = 20;

        // ✅ 패딩(너가 쓰던 margin 유지)
        text.margin = new Vector4(20, 6, 0, 2); // 아래쪽 여백 줄이기   // (20, 8, 0, 8);

        // Layout
        var le = headerGO.AddComponent<LayoutElement>();
        le.preferredHeight = 30;   // 기존 60이 크면 줄여
        le.preferredWidth = 300;
    }


    // 카테고리 한 줄(가로 row) 컨테이너 생성
    private Transform CreateRowContainer(string name)
    {
        GameObject rowGO = new GameObject($"Row_{name}", typeof(RectTransform));
        rowGO.transform.SetParent(slotsArea, false);

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 10;   // 16;

        // *** Inspector에서 ON 한 옵션들   (이걸 켜야, 각 슬롯 버튼의 테두리가 보임)
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;

        // (권장) 슬롯이 갑자기 늘어나지 않게
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        // (선택) 슬롯 스케일로 흔들리는 거 방지(보통 false 권장)
        hlg.childScaleWidth  = false;
        hlg.childScaleHeight = false;

        var fitter = rowGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var le = rowGO.AddComponent<LayoutElement>();
        le.preferredHeight = 100;   // 180;

        return rowGO.transform;
    }

}