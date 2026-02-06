using UnityEngine;

public class BackgroundState : MonoBehaviour
{
    // 어떤 배경을 얻었는지
    // 현재 선택된 배경이 뭔지
    public static BackgroundState Instance { get; private set; }

    [Header("Unlocked")]
    public bool unlockedDefault = true;  // 기본 배경은 처음부터 true 추천
    public bool unlockedTower = true;
    public bool unlockedARC = true;

    [Header("Selected")]
    public BackgroundId selected = BackgroundId.Default_01;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsUnlocked(BackgroundId id)
    {
        return id switch
        {
            BackgroundId.Default_01 => unlockedDefault,
            BackgroundId.Tower_02   => unlockedTower,
            BackgroundId.TheARC_03  => unlockedARC,
            _ => false
        };
    }

    public void Unlock(BackgroundId id)
    {
        switch (id)
        {
            case BackgroundId.Default_01: unlockedDefault = true; break;
            case BackgroundId.Tower_02:   unlockedTower = true; break;
            case BackgroundId.TheARC_03:  unlockedARC = true; break;
        }
    }

    // “선택”은 해금된 것만 가능
    public bool TrySelect(BackgroundId id)
    {
        if (!IsUnlocked(id)) return false;
        selected = id;
        return true;
    }
}
