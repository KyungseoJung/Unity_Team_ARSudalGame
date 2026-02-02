using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Fixed Status UI (2D Fixed)")]
    public TextMeshProUGUI nameText;
    public GameObject nameTagPanel;

    [Header("Acquisition Popup")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupText;

    [Header("Info Popup")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoText;


    void Awake()
    {
        if (Instance == null) Instance = this;
        nameTagPanel.SetActive(false);
        popupPanel.SetActive(false);
        infoPanel.SetActive(false);
    }

    public void ShowOtterInfo(string name)
    {
        nameText.text = name + "이 나타났습니다\n깨끗하게 씻겨서 수집해주세요!";
        nameTagPanel.SetActive(true);
    }

    public void ShowAcquirePopup(string itemName)
    {
        nameTagPanel.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(PopupRoutine(itemName));
    }

    public void ShowGeneralInfo(string message)
    {
        StopAllCoroutines(); // 진행 중인 팝업 예약 종료

        infoText.text = message; // 전달받은 메세지로 변경
        infoPanel.SetActive(true); // 패널 활성화

        // 필요하다면 일정 시간 뒤에 꺼지게 하거나, 
        // 유저가 닫기 전까지 계속 띄워둘 수 있습니다.
    }

    public void HideInfoPanel()
    {
        infoPanel.SetActive(false);
    }

    IEnumerator PopupRoutine(string itemName)
    {
        popupText.text = $"{itemName} 획득!";
        popupPanel.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        popupPanel.SetActive(false);
    }
}