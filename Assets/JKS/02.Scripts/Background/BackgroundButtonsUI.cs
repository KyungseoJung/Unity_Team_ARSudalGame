using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BackgroundButtonsUI : MonoBehaviour
{
    [System.Serializable]
    public class BgButton
    {
        public BackgroundId id;      // 이 버튼이 의미하는 배경
        public Button button;        // 버튼 컴포넌트
        public Image icon;           // 버튼에 표시될 이미지(Image 컴포넌트)

        public Sprite coloredSprite; // 획득(Unlock) 상태일 때 보여줄 스프라이트
        public Sprite graySprite;    // 미획득(Locked) 상태일 때 보여줄 스프라이트
    }

    [Header("Buttons (size = 3)")]
    public BgButton[] buttons;

    [Header("Only this scene allows clicking background buttons")]
    public string clickableSceneName = "Item_Place_Scene";

    [Header("Optional: CanvasGroup to block clicks in non-clickable scenes")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    private void Awake()
    {
        // CanvasGroup을 인스펙터에 안 넣었으면, 같은 오브젝트에서 자동으로 찾아봄
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // 씬 전환/활성화 시 UI 상태 갱신
        Refresh();
    }

    private void Start()
    {
        // 버튼 클릭 이벤트 등록(한 번만)
        if (buttons == null) return;

        foreach (var b in buttons)
        {
            if (b == null || b.button == null) continue;

            var captured = b; // 람다 캡처 안정용
            captured.button.onClick.AddListener(() =>
            {
                // Place 씬이 아니면 클릭 무시 (안전장치)
                if (!IsClickAllowedInThisScene()) return;

                // BackgroundState가 없으면 무시
                if (BackgroundState.Instance == null) return;

                // 해금된 것만 선택 저장
                if (BackgroundState.Instance.TrySelect(captured.id))
                {
                    Refresh();
                }
            });
        }

        Refresh();
    }

    public void Refresh()
    {
        // BackgroundState가 아직 준비 전이면 종료 (NullReference 방지)
        if (BackgroundState.Instance == null) return;

        bool clickAllowed = IsClickAllowedInThisScene();

        // ***** Get 씬에서는 '클릭만' 막기 (보이는 색은 유지) *****  ---------------------------
        // CanvasGroup이 있으면 패널 전체 입력 차단이 가능
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.blocksRaycasts = clickAllowed;
            panelCanvasGroup.interactable = clickAllowed;
        }

        // 버튼별 표시 갱신
        foreach (var b in buttons)
        {
            if (b == null) continue;

            bool unlocked = BackgroundState.Instance.IsUnlocked(b.id);

            // 1) 아이콘 스프라이트(회색/컬러)
            if (b.icon != null)
                b.icon.sprite = unlocked ? b.coloredSprite : b.graySprite;

            // 2) 클릭 가능 여부
            // - Place 씬에서만 클릭 가능하게 하고 싶지만,
            // - Get 씬에서는 클릭을 CanvasGroup으로 막을 거라서
            //   여기서는 "획득 여부"만으로 interactable을 결정해 색 틴트 문제를 피함
            if (b.button != null)
                b.button.interactable = unlocked;
        }
    }

    private bool IsClickAllowedInThisScene()
    {
        return SceneManager.GetActiveScene().name == clickableSceneName;
    }
}
