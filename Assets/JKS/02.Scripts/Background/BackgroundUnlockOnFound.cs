using UnityEngine;

public class BackgroundUnlockOnFound : MonoBehaviour
{
    [Header("Which background to unlock when this target is found")]
    public BackgroundId backgroundToUnlock;

    [Header("Popup text to show (English)")]
    public string popupName = "83Tower"; // 예: "83Tower", "TheARC", "Default"

    // DefaultObserverEventHandler의 On Target Found() 이벤트에 연결할 함수
    public void Unlock()
    {
        // 1) 필수 싱글톤 체크
        if (BackgroundState.Instance == null)
        {
            Debug.LogWarning("BackgroundState.Instance is null. Make sure BackgroundState exists and is active.");
            return;
        }

        // 2) 이미 획득했는지 확인
        bool wasUnlocked = BackgroundState.Instance.IsUnlocked(backgroundToUnlock);

        // 3) Unlock 시도 (이미 true여도 문제 없음)
        BackgroundState.Instance.Unlock(backgroundToUnlock);

        // 4) 처음 획득했을 때만 팝업 표시
        if (!wasUnlocked && UIManager.Instance != null)
        {
            // UIManager.ShowAcquirePopup은 내부에서 "OOO 획득!" 형태로 보여주고 2초 뒤 닫힘
            UIManager.Instance.ShowAcquirePopup($"{popupName}");
        }

        // 5) 배경 버튼 UI 즉시 갱신 (ItemSelectPanel 안에 있는 BackgroundButtonsUI)
        // var ui = FindObjectOfType<BackgroundButtonsUI>(true);
        var ui = Object.FindAnyObjectByType<BackgroundButtonsUI>();

        if (ui != null) ui.Refresh();

        Debug.Log($"Background unlock trigger: {backgroundToUnlock} (wasUnlocked={wasUnlocked})");
    }
}
