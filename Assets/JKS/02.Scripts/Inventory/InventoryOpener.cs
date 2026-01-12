using UnityEngine;

public class InventoryOpener : MonoBehaviour
{
    //#3 UI 버튼의 OnClick에서 직접 이 함수를 호출하게 만들 것
    public void OnClickToggleInventory()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ToggleInventory();
        }
    }
}
