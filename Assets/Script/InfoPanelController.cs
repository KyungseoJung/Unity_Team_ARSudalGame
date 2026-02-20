using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class InfoPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private List<ScrollRect> scrollViews; // 여러 개의 ScrollView 등록
    [SerializeField] private Button prevButton; // 이전 버튼
    [SerializeField] private Button nextButton; // 다음 버튼

    private int currentIndex = 0; // 현재 활성화된 화면 인덱스

    public static InfoPanelController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 버튼 이벤트 연결
        if (prevButton != null) prevButton.onClick.AddListener(ShowPreviousView);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextView);

        // 초기 화면 설정
        UpdateDisplay();
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
        for (int i = 0; i < scrollViews.Count; i++)
        {
            // 현재 인덱스인 ScrollView만 활성화
            bool isActive = (i == currentIndex);
            scrollViews[i].gameObject.SetActive(isActive);

            // 화면이 켜질 때 스크롤 위치를 맨 위(1f)로 초기화
            if (isActive)
            {
                StartCoroutine(ResetScrollRoutine(scrollViews[i]));
            }
        }

        // 첫 페이지나 마지막 페이지에서 버튼 비활성화 (선택 사항)
        if (prevButton != null) prevButton.interactable = (currentIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentIndex < scrollViews.Count - 1);
    }

    private IEnumerator ResetScrollRoutine(ScrollRect scrollRect)
    {
        // UI가 재배치되어 크기가 확정될 때까지 대기
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    // 외부(메뉴 등)에서 특정 페이지로 Info 창을 열 때 사용
    public void OpenPanel(int index = 0)
    {
        gameObject.SetActive(true);
        currentIndex = Mathf.Clamp(index, 0, scrollViews.Count - 1);
        UpdateDisplay();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}