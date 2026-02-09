using UnityEngine;
using UnityEngine.UI;
using TMPro;    //#3-2

public class InventorySlot : MonoBehaviour
{
    private int itemIndex;

    [Header("Optional")]
    // public Text label;   // 슬롯에 표시할 글자 (없으면 null로 둬도 됨)
    public TMP_Text label;   // //#3-2 슬롯에 표시할 글자 (없으면 null로 둬도 됨)

    [Header("Icon UI")]
    public Image iconImage;      // #10 추가: 아이템 아이콘 표시용

    private bool warnedOnce = false;

    private void Awake()
    {
        AutoBindReferences();
    }

    private void AutoBindReferences()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);

        // 1) 자식 Icon(Image)을 우선 찾기 (프리팹 구조가 "Icon" 자식일 때)
        if (iconImage == null)
        {
            var iconTf = transform.Find("ItemImage");
            if (iconTf != null) iconImage = iconTf.GetComponent<Image>();
        }

        // 2) 그래도 없으면 자기 자신 Image 사용 (Button 배경 Image 등)
        if (iconImage == null) iconImage = GetComponent<Image>();
    }

    // 2개 인자로도 호출 가능하게(편의용)
    public void Setup(int index, Sprite icon)
    {
        Setup(index, icon, null);
    }

    // InventoryManager에서 호출해서 셋업   /InventoryUI에서 호출
    public void Setup(int index, Sprite icon, string displayName = null)
    {
        itemIndex = index;

        if (iconImage == null || label == null) 
        {
            AutoBindReferences();
        }
        // if (label != null)
        // {
        //     label.text = displayName;
        // }

        // 텍스트는 이제 안 쓰고 싶으면 숨김 처리
        if (label != null)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                label.gameObject.SetActive(false);
            }
            else
            {
                label.gameObject.SetActive(true);
                label.text = displayName;
            }
        }

        // 아이콘 세팅
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
            iconImage.preserveAspect = true;
        }
        else if (iconImage == null)
        {
            if (!warnedOnce)
            {
                warnedOnce = true;
                Debug.LogWarning("InventorySlot: iconImage could not be found. Add an Image component or a child named 'Icon' with Image.");
            }
            return;
        }
    }

    // Button 컴포넌트의 OnClick에 이 함수를 연결
    public void OnClick()
    {
        InventoryManager.Instance.SpawnItem(itemIndex);
    }
}
