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
        // 1. 공통 부모 클래스인 Renderer로 찾기
        Renderer originRenderer = GetComponentInChildren<Renderer>();

        if (originRenderer == null)
        {
            Debug.LogWarning("렌더러를 찾을 수 없습니다.");
            return;
        }

        // 2. 새로운 자식 오브젝트 생성 및 기본 설정
        GameObject overlayObj = new GameObject("Generated_DirtOverlay");
        overlayObj.transform.SetParent(originRenderer.transform);
        overlayObj.transform.localPosition = Vector3.zero;
        overlayObj.transform.localRotation = Quaternion.identity;
        overlayObj.transform.localScale = Vector3.one * overlayScale;

        // 3. 타입에 따른 분기 처리 (Skinned Mesh vs 일반 Mesh)
        if (originRenderer is SkinnedMeshRenderer originSMR)
        {
            // --- SkinnedMeshRenderer 대응 ---
            SkinnedMeshRenderer overlaySMR = overlayObj.AddComponent<SkinnedMeshRenderer>();

            // 중요: 메쉬뿐만 아니라 뼈대(Bones) 정보를 복사해야 애니메이션을 따라갑니다.
            overlaySMR.sharedMesh = originSMR.sharedMesh;
            overlaySMR.bones = originSMR.bones;
            overlaySMR.rootBone = originSMR.rootBone;

            dirtyRenderer = overlaySMR;
        }
        else if (originRenderer is MeshRenderer originMR)
        {
            // --- 일반 MeshRenderer 대응 ---
            MeshFilter originFilter = originMR.GetComponent<MeshFilter>();
            if (originFilter == null) return;

            MeshFilter mf = overlayObj.AddComponent<MeshFilter>();
            mf.sharedMesh = originFilter.sharedMesh;

            dirtyRenderer = overlayObj.AddComponent<MeshRenderer>();
        }

        // 4. 머터리얼 설정 (공통)
        Material dirtMat = dirtyMaterial;
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