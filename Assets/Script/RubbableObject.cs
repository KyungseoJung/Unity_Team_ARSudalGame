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
    public float overlayScale = 1.01f;

    [Header("Visual References")]
    private Renderer dirtyRenderer;
    public GameObject cleanEffect;
    public MarkerSpawner mySpawner;

    private MaterialPropertyBlock propBlock;
    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");

    public event Action<RubbableObject> OnCleaningCompleted;

    // *** NEW: 이 RubbableObject가 속한 "스폰 루트"
    public Transform OwnerRoot { get; private set; }
    public void SetOwnerRoot(Transform root) => OwnerRoot = root;

    // *** NEW: overlay 캐싱
    private Transform overlayTransform;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        itemName = transform.name;
        CreateDirtOverlay();
    }

    private void CreateDirtOverlay()
    {
        Renderer originRenderer = GetComponentInChildren<Renderer>();
        if (originRenderer == null)
        {
            Debug.LogWarning("렌더러를 찾을 수 없습니다.");
            return;
        }

        GameObject overlayObj = new GameObject("Generated_DirtOverlay");
        overlayObj.transform.SetParent(originRenderer.transform);
        overlayObj.transform.localPosition = Vector3.zero;
        overlayObj.transform.localRotation = Quaternion.identity;
        overlayObj.transform.localScale = Vector3.one * overlayScale;

        // *** 캐싱
        overlayTransform = overlayObj.transform;

        if (originRenderer is SkinnedMeshRenderer originSMR)
        {
            SkinnedMeshRenderer overlaySMR = overlayObj.AddComponent<SkinnedMeshRenderer>();
            overlaySMR.sharedMesh = originSMR.sharedMesh;
            overlaySMR.bones = originSMR.bones;
            overlaySMR.rootBone = originSMR.rootBone;
            dirtyRenderer = overlaySMR;
        }
        else if (originRenderer is MeshRenderer originMR)
        {
            MeshFilter originFilter = originMR.GetComponent<MeshFilter>();
            if (originFilter == null) return;

            MeshFilter mf = overlayObj.AddComponent<MeshFilter>();
            mf.sharedMesh = originFilter.sharedMesh;

            dirtyRenderer = overlayObj.AddComponent<MeshRenderer>();
        }

        Material dirtMat = dirtyMaterial;
        Material[] mats = new Material[originRenderer.sharedMaterials.Length];
        for (int i = 0; i < mats.Length; i++) mats[i] = dirtMat;

        dirtyRenderer.materials = mats;
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
            propBlock.SetFloat(AlphaID, alpha);
            dirtyRenderer.SetPropertyBlock(propBlock);
        }
    }

    void CompleteCleaning()
    {
        if (isCompleted) return;
        isCompleted = true;

        if (cleanEffect)
            Instantiate(cleanEffect, transform.position, Quaternion.identity);

        OnCleaningCompleted?.Invoke(this);

        gameObject.SetActive(false);
    }

    public void ApplyCleanedState()
    {
        // *** overlay 제거는 캐싱된 참조로
        if (overlayTransform != null)
        {
            Destroy(overlayTransform.gameObject);
            overlayTransform = null;
        }

        if (CompareTag("Fish"))
        {
            transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (CompareTag("Otter") || CompareTag("OtterFriends"))
        {
            Transform root = OwnerRoot != null ? OwnerRoot : transform.parent;
            if (root != null)
                root.rotation = Quaternion.Euler(0, 270, 0);
        }
    }
}