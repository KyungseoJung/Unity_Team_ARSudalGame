using UnityEngine;

public class PlacedItem : MonoBehaviour
{
    Renderer rend;                  //#4-2: 색 변경을 위한 Renderer 캐시
    Color originalColor;            //#4-2
    public Color selectedColor = Color.yellow;   //#4-2: 선택시 표시 색

    void Awake()                    //#4-2
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    private void OnMouseDown()
    {
        // 이전: 클릭하면 Destroy()
        // 지금: "나 선택해줘" 라고 InventoryManager에 알려줌          //#4-2
        if (InventoryManager.Instance != null)                     //#4-2
        {
            InventoryManager.Instance.SelectItem(this);            //#4-2
        }
    }

    // 선택/해제 시 색상 바꾸는 함수                                  //#4-2
    public void SetSelected(bool selected)                         //#4-2
    {
        if (rend == null) return;
        rend.material.color = selected ? selectedColor : originalColor;
    }
}
