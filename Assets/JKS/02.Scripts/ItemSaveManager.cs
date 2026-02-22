using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemSaveManager : MonoBehaviour
{
    // #2 인벤토리에서 쓰는 것과 같은 순서의 프리팹 배열을 Inspector에 넣어줘야 함
    // ㄴ어떤 아이템인지를 숫자(index)로 저장하기 때문!!! ㄴ 그리고 저장된 아이템 로드할 때 필요해서 프리팹으로 넣어두는 것
    [Header("아이템 프리팹 목록 (InventoryManager와 동일 순서)")]
    public GameObject[] itemPrefabs;

    const string SaveKey = "ItemPositions_v2"; // v1과 구분 (이전 데이터와 충돌 방지)

    [Serializable]
    public class ItemEntry  //#15 fix
    {
        public int index;              // 어떤 아이템인지 (prefab index)
        public Vector3 position;       // 어디에 있는지
        public Quaternion rotation;    // (선택) 회전도 저장하고 싶으면
        public Vector3 scale;          // (선택) 스케일도 저장하고 싶으면

        public Vector3 baseScale;      // ✅ #15 fix: "기준 스케일" 저장
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

    void OnApplicationQuit()
    {
        SaveAll();
    }

    // 모바일에서 홈버튼 눌러서 나갈 때 같은 상황
    void OnApplicationPause(bool pause)
    {
        if (pause) SaveAll();
    }

    [ContextMenu("Save Now")]
    public void SaveAll()
    {
        SaveData data = new SaveData();

        // 씬에 있는 배치 아이템 전부 수집
        //#8 노란 경고 고치기 -----------
        PlacedItem[] placedItems = FindObjectsByType<PlacedItem>(FindObjectsSortMode.None);

        foreach (var p in placedItems)
        {
            if (p == null) continue;

            ItemEntry e = new ItemEntry();
            e.index = p.itemIndex;
            e.position = p.transform.position;

            // (선택) 회전/스케일까지 저장해두면 방꾸미기 느낌이 더 좋아짐
            e.rotation = p.transform.rotation;
            e.scale = p.transform.localScale;

            // ✅ #15 fix: 기준 스케일 저장 (PlacedItem이 들고 있는 값)
            e.baseScale = p.baseScale;

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
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 기존 배치 아이템 제거 (중복 방지)
        //#8 노란 경고 고치기 -----------
        foreach (var p in FindObjectsByType<PlacedItem>(FindObjectsSortMode.None))
        {
            Destroy(p.gameObject);
        }

        // 저장된 데이터대로 다시 생성
        foreach (var entry in data.items)
        {
            if (entry.index < 0 || entry.index >= itemPrefabs.Length)
            {
                Debug.LogWarning($"Invalid item index in save: {entry.index}");
                continue;
            }

            GameObject go = Instantiate(itemPrefabs[entry.index], entry.position, entry.rotation);

            // ✅ #15 fix: Instantiate 직후의 "기본 스케일"을 fallback으로 잡아둠
            // (프리팹 루트 스케일(itemPrefabs[index].transform.localScale)은 구조 따라 틀어질 수 있어서 위험)
            Vector3 fallbackBase = new Vector3(
                Mathf.Abs(go.transform.localScale.x),
                Mathf.Abs(go.transform.localScale.y),
                Mathf.Abs(go.transform.localScale.z)
            );

            go.GetComponentInChildren<RubbableObject>()?.ApplyCleanedState();

            // (선택) 스케일 복원
            go.transform.localScale = entry.scale;

            // PlacedItem 세팅
            var placed = go.GetComponentInChildren<PlacedItem>();
            if (placed == null) placed = go.AddComponent<PlacedItem>();
            placed.itemIndex = entry.index;

            // ✅ #15 fix: baseScale 복원 (저장값 우선, 없으면 fallbackBase)
            placed.baseScale = (entry.baseScale == Vector3.zero) ? fallbackBase : entry.baseScale;

            // 드래그 컴포넌트가 필요하면 붙이기
            var col = go.GetComponentInChildren<Collider>(true);
            GameObject dragHost = (col != null) ? col.gameObject : go;

            var dragger = dragHost.GetComponent<ItemDragger2D>();
            if (dragger == null) dragger = dragHost.AddComponent<ItemDragger2D>();

            // *** 드래그로 움직일 대상은 항상 "루트"
            dragger.SetMoveTarget(go.transform);

            // ✅ #15 fix: 드래거에게도 동일한 baseScale 주입 (핀치 min/max 기준 고정)
            dragger.SetBaseScaleFromPrefab(placed.baseScale);
        }

        Debug.Log("ItemSaveManager: Loaded\n" + json);
    }

    [ContextMenu("Clear Save")]
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("ItemSaveManager: Save cleared");
    }
}