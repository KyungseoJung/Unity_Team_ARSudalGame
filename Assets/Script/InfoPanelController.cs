using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class InfoPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private List<ScrollRect> scrollViews;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private int currentIndex = 0;
    private static bool isFirstLaunch = true; // 앱 실행 후 최초 1회 체크를 위한 정적 변수

    public static InfoPanelController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 기본적으로 꺼진 상태로 시작하지만, Start에서 첫 실행 여부를 확인합니다.
    }

    void Start()
    {
        // 버튼 이벤트 연결
        if (prevButton != null) prevButton.onClick.AddListener(ShowPreviousView);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextView);

        // 앱이 처음 켜졌을 때만 자동으로 패널 열기
        if (isFirstLaunch)
        {
            OpenPanel(0);
            isFirstLaunch = false; // 이후에는 자동으로 켜지지 않도록 플래그 변경
        }
        else
        {
            ClosePanel();
        }
    }

    public void ShowNextView()
    {
        if (currentIndex < scrollViews.Count - 1)
        {
            currentIndex++;
            UpdateDisplay();
        }
    }

    public void ShowPreviousView()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (!gameObject.activeSelf) return;

        for (int i = 0; i < scrollViews.Count; i++)
        {
            bool isActive = (i == currentIndex);
            scrollViews[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                StartCoroutine(ResetScrollRoutine(scrollViews[i]));
            }
        }

        if (prevButton != null) prevButton.interactable = (currentIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentIndex < scrollViews.Count - 1);
    }

    private IEnumerator ResetScrollRoutine(ScrollRect scrollRect)
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    public void OpenPanel(int index = 0)
    {
        currentIndex = Mathf.Clamp(index, 0, scrollViews.Count - 1);
        gameObject.SetActive(true);
        UpdateDisplay();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}