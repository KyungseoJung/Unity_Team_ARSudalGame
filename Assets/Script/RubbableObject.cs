using UnityEngine;
using System;

public class RubbableObject : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;
    public InventoryManager.ItemType itemType;

    [Header("Cleaning Settings")]
    public float cleaningSensitivity = 0.00005f;
    [SerializeField] private float currentDirt = 5.0f;
    private bool isCompleted = false;

    [Header("Auto Overlay Settings")]
    public Material dirtyMaterial;
    public float overlayScale = 1.01f; // 1.1은 너무 클 수 있어 1.01 권장

    [Header("Visual References")]
    private Renderer dirtyRenderer; // 자동으로 생성될 껍데기 렌더러
    public GameObject cleanEffect;
    public MarkerSpawner mySpawner;

    private MaterialPropertyBlock propBlock;
    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");
   // private static readonly int ColorID = Shader.PropertyToID("_DirtColor");
   // private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

    public event Action<RubbableObject> OnCleaningCompleted;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        itemName = transform.name;

        // ★ 핵심: 실행 시 껍데기 자동 생성
        CreateDirtOverlay();
    }

    private void CreateDirtOverlay()
    {
        // 1. 원본 메쉬 렌더러 찾기
        MeshRenderer originRenderer = GetComponentInChildren<MeshRenderer>();
        MeshFilter originFilter = GetComponentInChildren<MeshFilter>();

        if (originRenderer == null || originFilter == null) return;

        // 2. 새로운 자식 오브젝트 생성
        GameObject overlayObj = new GameObject("Generated_DirtOverlay");
        overlayObj.transform.SetParent(originRenderer.transform);

        // 위치와 회전은 원본과 똑같이, 스케일만 살짝 키우기
        overlayObj.transform.localPosition = Vector3.zero;
        overlayObj.transform.localRotation = Quaternion.identity;
        overlayObj.transform.localScale = Vector3.one * overlayScale;

        // 3. 메쉬 정보 복사
        MeshFilter mf = overlayObj.AddComponent<MeshFilter>();
        mf.sharedMesh = originFilter.sharedMesh;

        // 4. 렌더러 추가 및 머터리얼 설정
        dirtyRenderer = overlayObj.AddComponent<MeshRenderer>();

        // 껍데기용 새 머터리얼 생성 및 셰이더 할당
        Material dirtMat = dirtyMaterial;

        // 원본 머터리얼 개수만큼 슬롯을 채워줍니다
        Material[] mats = new Material[originRenderer.sharedMaterials.Length];
        for (int i = 0; i < mats.Length; i++) mats[i] = dirtMat;
        dirtyRenderer.materials = mats;

        // 초기 알파값 적용
        UpdateAlpha(1.0f);
    }

    public void AddRub(float amount)
    {
        if (isCompleted || currentDirt <= 0) return;

        currentDirt -= amount * cleaningSensitivity;
        currentDirt = Mathf.Clamp01(currentDirt);

        UpdateAlpha(currentDirt);

        if (currentDirt <= 0.01f) CompleteCleaning();
    }

    void UpdateAlpha(float alpha)
    {
        if (dirtyRenderer != null)
        {
            dirtyRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(AlphaID, alpha); // image_335771.png의 _Alpha와 연결
            dirtyRenderer.SetPropertyBlock(propBlock);
        }
    }

    void CompleteCleaning()
    {
        if (isCompleted) return;
        isCompleted = true;

        // 3. 이펙트 생성
        if (cleanEffect)
        {
            Instantiate(cleanEffect, transform.position, Quaternion.identity);
        }

        OnCleaningCompleted?.Invoke(this);

        // 원본(나무 등)은 남겨두고 싶다면 아래 줄을 주석 처리, 수집형이라면 유지
         gameObject.SetActive(false); 
    }

    public void ApplyCleanedState()
    {
        // 1. 껍데기 제거 로직
        Transform overlay = transform.Find("Generated_DirtOverlay");
        if (overlay != null)
        {
            overlay.gameObject.SetActive(false);
            overlay.SetParent(null);
            Destroy(overlay.gameObject);
        }

        // 2. 물고기 회전 로직
        if (CompareTag("Fish"))
        {
            transform.rotation = Quaternion.Euler(0, 90, 0);
        }
    }
}