using UnityEngine;
using System;

public class RubbableObject : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;
    public InventoryManager.ItemType itemType;

    [Header("Cleaning Settings")]
    [Tooltip("5초 정도 걸리게 하려면 0.00005f 내외로 조절하세요.")]
    public float cleaningSensitivity = 0.00005f;
    private float currentDirt = 1.0f; // 1.0(더러움) -> 0.0(깨끗함)
    private bool isCompleted = false;

    [Header("Visual References")]
    [Tooltip("여기에 'Dirt_Overlay' 자식 오브젝트의 Renderer를 넣으세요.")]
    public Renderer dirtyRenderer;
    public GameObject cleanEffect;
    public MarkerSpawner mySpawner;

    [Header("Internal References")]
    private MaterialPropertyBlock propBlock;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    public event Action<RubbableObject> OnCleaningCompleted;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        itemName = transform.name;
    }

    void Start()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOtterInfo(itemName);
        }

        // 시작할 때 껍데기를 완전히 불투명(더러움)하게 설정
        UpdateAlpha(1.0f);
    }

    /// <summary>
    /// InteractionManager에서 호출되는 함수 (amount는 마우스 Delta * Sensitivity)
    /// </summary>
    public void AddRub(float amount)
    {
        if (isCompleted || currentDirt <= 0) return;

        // 1. 더러움 수치 감소 (amount가 크므로 매우 작은 감도를 곱함)
        currentDirt -= amount * cleaningSensitivity;
        Debug.Log("호에엥 " + currentDirt);
        currentDirt = Mathf.Clamp01(currentDirt);

        // 2. 시각적 업데이트 (껍데기의 투명도 조절)
        UpdateAlpha(currentDirt);

        // 3. 완료 체크 (거의 다 닦였을 때)
        if (currentDirt <= 0.01f)
        {
            CompleteCleaning();
        }
    }

    void UpdateAlpha(float alpha)
    {
        if (dirtyRenderer != null)
        {
            // MaterialPropertyBlock을 사용하여 원본 에셋 수정 없이 개별 오브젝트만 조절
            dirtyRenderer.GetPropertyBlock(propBlock);

            // 껍데기 셰이더의 컬러를 가져와서 알파값만 수정
            // (주의: 껍데기 셰이더의 Surface Type이 Transparent여야 합니다)
            Color currentColor = Color.white;
            currentColor.a = alpha;
            Debug.Log("Current Color = " + alpha);

            propBlock.SetFloat("_Alpha", alpha);
            dirtyRenderer.SetPropertyBlock(propBlock);
        }
    }

    void CompleteCleaning()
    {
        if (isCompleted) return;
        isCompleted = true;

        // 더러운 껍데기 완전히 제거
        if (dirtyRenderer) dirtyRenderer.gameObject.SetActive(false);

        // 청소 완료 효과
        if (cleanEffect)
        {
            Instantiate(cleanEffect, transform.position, Quaternion.identity);
        }

        Debug.Log($"✨ {itemName} 청소 완료!");
        OnCleaningCompleted?.Invoke(this);

        // 오브젝트 비활성화 (매니저가 수집 처리)
        gameObject.SetActive(false);
    }
}