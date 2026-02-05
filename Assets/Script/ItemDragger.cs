using UnityEngine;
using UnityEngine.EventSystems; // 필수
using System.Collections.Generic;

// 인터페이스를 상속받으면 유니티가 터치 이벤트를 훨씬 정확하게 전달합니다.
public class ItemDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Camera mainCam;
    private ItemSaveManager saveManager;

    void Start()
    {
        mainCam = Camera.main;
        saveManager = FindFirstObjectByType<ItemSaveManager>();
    }

    // 1. 터치 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 시작 시 시각적 효과 (살짝 커지게 등)
    }

    // 2. 터치 중 (손가락 따라 이동)
    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // 카메라와의 거리
        transform.position = mainCam.ScreenToWorldPoint(mousePos);
    }

    // 3. 터치 종료 (손가락을 뗄 때)
    public void OnEndDrag(PointerEventData eventData)
    {
        // PointerEventData가 제공하는 기능을 사용하여 UI를 체크합니다.
        // 모바일 멀티 터치 환경에서도 현재 떼어진 손가락 밑에 뭐가 있는지 정확히 알 수 있습니다.
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            GameObject hitObj = eventData.pointerCurrentRaycast.gameObject;

            // UI 이름이 "TrashCan"인지 확인
            if (hitObj.name == "TrashCan")
            {
                DeleteProcess();
            }
        }
    }

    private void DeleteProcess()
    {
        Debug.Log("🗑️ 모바일에서 아이템 삭제 완료");
        Destroy(gameObject);

        // 데이터 저장
        if (saveManager != null)
        {
            saveManager.Invoke("SaveAll", 0.1f);
        }
    }
}