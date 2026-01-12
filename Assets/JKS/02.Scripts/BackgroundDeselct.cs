using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; //#6추가


public class BackgroundDeselct : MonoBehaviour
{
    // //#6 초록 배경을 클릭함으로써, 기존에 선택했던 아이템을 선택해제하는 코드
    
    private void OnMouseDown()
    {

        //#6 추가: UI(버튼 등) 위를 누른 경우엔 배경 클릭으로 처리하지 않음 ---------------------
        //왜?: 아이템을 클릭 후, Return 버튼이 먹통이 됨 -> 이유를 살펴보니 Return 버튼을 누를 때 자동으로 배경이 먼저 선택된 것으로 판단해서
        //  ┗> Return 하기 전에 '선택된 아이템'을 없애버리게 된 것.
        // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        // {
        //     return;
        // }

        // 위 코드를 이용하면 PC에서 잘 작동함.
        // 하지만, IsPointerOverGameObject()함수는 모바일에서 fingerId가 필요한 경우가 있어서 아래처럼 보강하는 게 더 안전함
    //     if (EventSystem.current != null)
    //     {
    // #if UNITY_ANDROID || UNITY_IOS
    //         if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
    //             return;
    // #else
    //         if (EventSystem.current.IsPointerOverGameObject())
    //             return;
    // #endif
    //     }


        if (IsPointerOverUI())
        {
            return;
        }

        // 선택된 아이템이 있는데 배경을 클릭했다면, 그 선택한 아이템을 선택 취소하기
        // if (InventoryManager.Instance != null)
        // {
        InventoryManager.Instance.ClearSelection();
        // }
    }

    // PC/모바일 모두 대응: 현재 포인터가 UI 위면 true
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition; // PC는 mousePosition으로 OK (모바일도 동작)

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

}
