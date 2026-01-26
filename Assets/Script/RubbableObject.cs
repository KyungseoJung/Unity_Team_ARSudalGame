using UnityEngine;
using System;

public class RubbableObject : MonoBehaviour
{
    [Header("Settings")]
    public string itemName;
    public InventoryManager.ItemType itemType;
    public float totalRubAmount = 50f;
    public GameObject cleanEffect;
    public MarkerSpawner mySpawner; // 나를 소환한 마커 스포너 참조

    [Header("Visuals")]
    public Renderer dirtyRenderer;

    private float currentRub = 0f;
    private bool isCompleted = false;
    private MaterialPropertyBlock propBlock; // 성능 최적화용

    [Header("UI Settings")]
    public static GameObject nameTagPrefab; // 모든 수달이 공유하는 이름표 프리팹

    public float nameTagHeight = 0.5f;     // 머리 위 높이 조절
    // 완료되었을 때 외부(매니저)에 알리는 이벤트
    public event Action<RubbableObject> OnCleaningCompleted;

    void Start()
    {
        propBlock = new MaterialPropertyBlock();
        itemName = transform.name;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOtterInfo(itemName);
        }
        UpdateAlpha(1.0f);
    }

    // ★ InputManager가 이 함수를 호출합니다.
    public void AddRub(float amount)
    {
        if (isCompleted) return;

        // 1. 입력받은 amount(delta)를 감쇠시켜 적용합니다. 
        // New Input System의 delta는 값이 매우 크기 때문에 0.01~0.1 정도를 곱하는 것이 적당합니다.
        float adjustedAmount = amount * 0.05f;

        // 2. 한 프레임에 너무 많은 양이 쌓이지 않도록 제한 (순간적인 텔레포트 등 방어)
        adjustedAmount = Mathf.Min(adjustedAmount, 2.0f);

        currentRub += adjustedAmount;

        // 3. 진행도 계산
        float progress = 1.0f - (currentRub / totalRubAmount);
        progress = Mathf.Clamp01(progress);

        UpdateAlpha(progress);

        if (progress <= 0f)
        {
            CompleteCleaning();
        }
    }

    void UpdateAlpha(float alpha)
    {
        if (dirtyRenderer != null)
        {
            // MaterialPropertyBlock을 사용하여 배칭 깨짐 방지 & 성능 향상
            dirtyRenderer.GetPropertyBlock(propBlock);

            // 쉐이더 프로퍼티 이름은 사용중인 쉐이더에 맞춰 수정 필요 (_BaseColor or _Color)
            Color currentColor = dirtyRenderer.sharedMaterial.color;
            currentColor.a = alpha;

            // URP Lit Shader 기준 "_BaseColor", Legacy는 "_Color"
            propBlock.SetColor("_BaseColor", currentColor);

            dirtyRenderer.SetPropertyBlock(propBlock);
        }
    }

    void CompleteCleaning()
    {
        isCompleted = true;

        if (dirtyRenderer) dirtyRenderer.gameObject.SetActive(false);
        if (cleanEffect)
        {
            GameObject effect = Instantiate(cleanEffect, transform.position, Quaternion.identity);
            //Destroy(effect, 3.0f);
        }
        Debug.Log("✨ 청소 완료!");

        // ★ "나 끝났어!"라고 외치기 (매니저가 듣고 인벤토리에 넣음)
        OnCleaningCompleted?.Invoke(this);

        // 바로 파괴하지 않고 매니저가 처리하도록 하거나, 잠시 뒤 파괴
        // Destroy(gameObject); // 여기서 바로 파괴하면 이벤트 전달에 문제가 생길 수 있음
        gameObject.SetActive(false);
    }
}