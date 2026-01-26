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
        nameText.text = name + "이 나타났습니다\n깨끗하게 씻겨서 수집해주세요!";
        nameTagPanel.SetActive(true);
    }

    public void ShowAcquirePopup(string itemName)
    {
        nameTagPanel.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(PopupRoutine(itemName));
    }

    IEnumerator PopupRoutine(string itemName)
    {
        popupText.text = $"{itemName} 획득!";
        popupPanel.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        popupPanel.SetActive(false);
    }
}