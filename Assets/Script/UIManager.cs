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


    void Awake()
    {
        if (Instance == null) Instance = this;
        nameTagPanel.SetActive(false);
        popupPanel.SetActive(false);
    }

    public void ShowOtterInfo(string name)
    {
        // "_"를 공백으로 변환
        string displayName = name.Replace("_", " ");

        nameText.text = displayName + " has appeared!\nRub the screen to clean it and collect it!";
        nameTagPanel.SetActive(true);
    }

    public void ShowAcquirePopup(string itemName)
    {
        nameTagPanel.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(PopupRoutine(itemName));
    }

    public void ShowGeneralInfo()
    {
        StopAllCoroutines(); // 진행 중인 팝업 예약 종료

        GameManager.Instance.SetVuforiaActive(false);
        InfoPanelController.Instance.OpenPanel();

        // 필요하다면 일정 시간 뒤에 꺼지게 하거나, 
        // 유저가 닫기 전까지 계속 띄워둘 수 있습니다.
    }

    public void HideInfoPanel()
    {
        GameManager.Instance.SetVuforiaActive(true);
        InfoPanelController.Instance.ClosePanel();
    }

    IEnumerator PopupRoutine(string itemName)
    {
        // "_"를 공백으로 변환
        string displayName = itemName.Replace("_", " ");

        popupText.text = $"{displayName} obtained!";

        popupPanel.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        popupPanel.SetActive(false);
    }
}