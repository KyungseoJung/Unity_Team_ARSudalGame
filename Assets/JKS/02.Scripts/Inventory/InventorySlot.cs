using UnityEngine;
using UnityEngine.UI;
using TMPro;    //#3-2

public class InventorySlot : MonoBehaviour
{
    private int itemIndex;
    // public Text label;   // 슬롯에 표시할 글자 (없으면 null로 둬도 됨)
    public TMP_Text label;   // //#3-2 슬롯에 표시할 글자 (없으면 null로 둬도 됨)

    // InventoryManager에서 호출해서 셋업
    public void Setup(int index, string displayName)
    {
        itemIndex = index;

        if (label != null)
        {
            label.text = displayName;
        }
    }

    // Button 컴포넌트의 OnClick에 이 함수를 연결
    public void OnClick()
    {
        InventoryManager.Instance.SpawnItem(itemIndex);
    }
}
