using System;
using UnityEngine;

public class BackgroundState : MonoBehaviour
{
    // 어떤 배경을 얻었는지
    // 현재 선택된 배경이 뭔지
    // 앱을 끄기 전 정보 저장 --------------
        // 획득했던 배경 정보 저장
        // 마지막으로 Item_Place_Scene에서 설정했던 배경 저장
    
    public static BackgroundState Instance { get; private set; }

    [Header("Unlocked")]
    public bool unlockedDefault = true;
    public bool unlockedTower = false;
    public bool unlockedARC = false;

    [Header("Selected")]
    public BackgroundId selected = BackgroundId.Default_01;
    
    [Header("Debug Test - Reset Background")] // 테스트용 - 배경 초기화하기 위한 코드================================
    public bool forceResetOnAwake = false;

    // UI/적용 스크립트가 갱신할 수 있도록 이벤트(선택)
    public event Action OnChanged;

    // 저장 키
    const string SaveKey = "BackgroundState_v1";

    [Serializable]
    class SaveData
    {
        public bool unlockedDefault;
        public bool unlockedTower;
        public bool unlockedARC;
        public BackgroundId selected;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);


        if (forceResetOnAwake)
            ResetToFreshStart(true);
        else
            Load();          // 앱 재실행/씬전환에도 상태 복원
        OnChanged?.Invoke();
    }

    private void OnApplicationQuit() => Save();
    private void OnApplicationPause(bool pause) { if (pause) Save(); }

    // ---------------------------
    // 상태 조회
    // ---------------------------
    public bool IsUnlocked(BackgroundId id)
    {
        return id switch
        {
            BackgroundId.Default_01 => unlockedDefault,
            BackgroundId.Tower_02 => unlockedTower,
            BackgroundId.TheARC_03 => unlockedARC,
            _ => false
        };
    }

    // ---------------------------
    // 해금
    // ---------------------------
    public void Unlock(BackgroundId id)
    {
        bool changed = false;

        switch (id)
        {
            case BackgroundId.Default_01:
                if (!unlockedDefault) { unlockedDefault = true; changed = true; }
                break;
            case BackgroundId.Tower_02:
                if (!unlockedTower) { unlockedTower = true; changed = true; }
                break;
            case BackgroundId.TheARC_03:
                if (!unlockedARC) { unlockedARC = true; changed = true; }
                break;
        }

        if (changed)
        {
            Save();              // ✅ 해금되면 즉시 저장
            OnChanged?.Invoke(); // (선택) UI/배경 적용 갱신 트리거
        }
    }

    // ---------------------------
    // 선택 저장
    // ---------------------------
    public bool TrySelect(BackgroundId id)
    {
        if (!IsUnlocked(id)) return false;

        if (selected != id)
        {
            selected = id;
            Save();              // ✅ 마지막 선택 즉시 저장
            OnChanged?.Invoke(); // (선택)
        }
        return true;
    }

    // ---------------------------
    // Save / Load
    // ---------------------------
    public void Save()
    {
        var data = new SaveData
        {
            unlockedDefault = unlockedDefault,
            unlockedTower = unlockedTower,
            unlockedARC = unlockedARC,
            selected = selected
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        string json = PlayerPrefs.GetString(SaveKey);
        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

        unlockedDefault = data.unlockedDefault;
        unlockedTower = data.unlockedTower;
        unlockedARC = data.unlockedARC;

        selected = data.selected;

        // 선택값이 잠겨있다면 기본으로 보정
        if (!IsUnlocked(selected))
            selected = BackgroundId.Default_01;
    }

    // (테스트/디버그용) ===========================
    [ContextMenu("Clear Background Save")]
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
    // 테스트용 - 배경 초기화하기 위한 코드 ===========================
    public void ResetToFreshStart(bool saveImmediately = true)
    {
        // 1) 메모리 상태 초기화 (Default만 true)
        unlockedDefault = true;
        unlockedTower = false;
        unlockedARC = false;

        selected = BackgroundId.Default_01;

        // 2) 저장 데이터 삭제
        PlayerPrefs.DeleteKey(SaveKey);

        // 3) 필요하면 즉시 저장(기본 true 추천)
        if (saveImmediately)
        {
            Save();
        }
        else
        {
            PlayerPrefs.Save();
        }

        // 4) UI / BackgroundApplier 갱신
        OnChanged?.Invoke();
    }

    [ContextMenu("Reset Background Progress (Default Only)")]
    public void ResetFromContextMenu()
    {
        ResetToFreshStart(true);
    }

}
