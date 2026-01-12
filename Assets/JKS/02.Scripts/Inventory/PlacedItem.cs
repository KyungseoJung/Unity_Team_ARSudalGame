using UnityEngine;

public class PlacedItem : MonoBehaviour
{
    Renderer rend;                  //#4-2: 색 변경을 위한 Renderer 캐시
    Color originalColor;            //#4-2
    public Color selectedColor = Color.yellow;   //#4-2: 선택시 표시 색
    // //#6-1: 선택 표시용 오브젝트(자식)
    [SerializeField] private GameObject selectionRing; // "SelectionRing"


    void Awake()                    //#4-2
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }

        // //#6-1: 인스펙터 연결 안 했으면 이름으로 자동 탐색해서 연결해주기
        if (selectionRing == null)
        {
            selectionRing = transform.Find("SelectionRing")?.gameObject;
        }            
        SetSelected(false); // 시작 시 꺼둠

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
        // if (rend == null) return;
        // rend.material.color = selected ? selectedColor : originalColor;
        if (selectionRing != null) 
        {
            selectionRing.SetActive(selected);
        }
    }
}
