using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Fixed Status UI (2D Fixed)")]
    public GameObject nameTagPanel;    // 이름표 패널 (상단이나 하단에 고정된 부모 오브젝트)
    public TextMeshProUGUI nameText;   // 수달 이름 텍스트

    [Header("Acquisition Popup")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupText;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // 초기화: 모든 UI 끄기
        nameTagPanel.SetActive(false);
        popupPanel.SetActive(false);
    }

    // 1. 수달이 발견되었을 때 호출 (상단 고정 UI 켜기)
    public void ShowOtterInfo(string name)
    {
        nameText.text = name;
        nameTagPanel.SetActive(true);
    }

    // 2. 획득 팝업 함수
    public void ShowAcquirePopup(string itemName)
    {
        // 이름 정보 UI는 끄고 획득 팝업 띄우기
        nameTagPanel.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(PopupRoutine(itemName));
    }

    IEnumerator PopupRoutine(string itemName)
    {
        popupText.text = $"✨ {itemName} 획득!";
        popupPanel.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        popupPanel.SetActive(false);
    }
}