using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemSaveManager : MonoBehaviour
{
    // //#2 각 아이템들 위치를 JSON으로 저장하기. 어플 실행할 땐, 항상 가장 마지막 위치를 불러오기.
    // 이름으로 매칭하기 때문에, 아이템들간의 이름이 서로 겹치지 않도록 주의.
    [Header("저장할 아이템들 (큐브들)")]
    public Transform[] items;

    const string SaveKey = "ItemPositions_v1";

    [Serializable]
    public class ItemEntry
    {
        public string name;
        public Vector3 position;
    }

    [Serializable]
    public class SaveData
    {
        public List<ItemEntry> items = new List<ItemEntry>();
    }

    void Start()
    {
        LoadAll();
    }

    // 앱이 종료될 때 자동 저장 (에디터 / PC)
    void OnApplicationQuit()
    {
        SaveAll();
    }

    // 모바일에서 홈버튼 눌러서 나갈 때 같은 상황
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveAll();
        }
    }

    [ContextMenu("Save Now")]
    public void SaveAll()
    {
        SaveData data = new SaveData();

        foreach (var t in items)
        {
            if (t == null) continue;

            ItemEntry e = new ItemEntry();
            e.name = t.name;               // 이름으로 구분
            e.position = t.position;
            data.items.Add(e);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        Debug.Log("ItemSaveManager: Saved\n" + json);
    }

    [ContextMenu("Load Now")]
    public void LoadAll()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("ItemSaveManager: No save found, use default positions.");
            return; // 첫 실행 같은 경우
        }

        string json = PlayerPrefs.GetString(SaveKey);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 이름 기준으로 매칭해서 위치 되돌리기
        foreach (var entry in data.items)
        {
            // items 배열에서 같은 이름 가진 Transform 찾기
            foreach (var t in items)
            {
                if (t != null && t.name == entry.name)
                {
                    t.position = entry.position;
                    break;
                }
            }
        }

        Debug.Log("ItemSaveManager: Loaded\n" + json);
    }
}
