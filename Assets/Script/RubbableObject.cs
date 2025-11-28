using UnityEngine;

public class RubbableObject : MonoBehaviour
{
    [Header("Settings")]
    public string itemName = "아기 수달"; // 인벤토리에 들어갈 이름
    public float totalRubAmount = 50f;    // 깨끗해지기 위해 필요한 문지름 총량
    public GameObject cleanEffect;        // 완료 시 터질 이펙트 (반짝임)

    [Header("Visuals")]
    public Renderer dirtyRenderer; // 더러운 모델의 렌더러 (투명도 조절용)

    private float currentRub = 0f;
    private bool isCompleted = false;

    void Start()
    {
        // 시작 시 더러운 상태(불투명)로 초기화
        UpdateAlpha(1.0f);
    }

    // InputManager에서 호출할 함수
    public void AddRub(float amount)
    {
        if (isCompleted) return;

        // 문지른 만큼 수치 증가
        currentRub += amount;

        // 진행도 (1.0 = 시작, 0.0 = 완료)
        float progress = 1.0f - (currentRub / totalRubAmount);
        progress = Mathf.Clamp01(progress);

        // 시각적 업데이트 (투명도 조절)
        UpdateAlpha(progress);

        // 청소 완료 체크
        if (progress <= 0f)
        {
            CompleteCleaning();
        }
    }

    void UpdateAlpha(float alpha)
    {
        if (dirtyRenderer != null)
        {
            // 머티리얼 색상 가져오기
            Color color = dirtyRenderer.material.color;
            color.a = alpha; // 알파값 변경
            dirtyRenderer.material.color = color;
        }
    }

    void CompleteCleaning()
    {
        isCompleted = true;

        // 1. 더러운 껍데기 완전히 끄기
        if (dirtyRenderer) dirtyRenderer.gameObject.SetActive(false);

        /*
         * 
        // 2. 인벤토리에 추가 (SimpleInventory가 있다면)
        if (SimpleInventory.Instance != null)
        {
            SimpleInventory.Instance.AddItem(itemName);
        }
        else
        {
            Debug.Log($"✨ {itemName} 획득! (인벤토리 시스템 없음)");
        }
        */

        // 3. 이펙트 재생
        if (cleanEffect)
            Instantiate(cleanEffect, transform.position, Quaternion.identity);

        // 4. (선택) 수달이 기뻐하는 애니메이션 재생 등 추가 가능
    }
}