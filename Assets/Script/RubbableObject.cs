using System;
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

    public event Action<string> OnCleaningCompleted;

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
            // 1. 머티리얼 복사본 가져오기 (자동 생성됨)
            Material mat = dirtyRenderer.material;

            // 2. URP인지 레거시인지 확인해서 프로퍼티 이름 결정
            string propertyName = "_BaseColor"; // URP 기본 이름
            if (!mat.HasProperty(propertyName))
            {
                propertyName = "_Color"; // 레거시(Built-in) 이름
            }

            // 3. 색상 가져와서 알파값만 바꾸고 다시 넣기
            if (mat.HasProperty(propertyName))
            {
                Color color = mat.GetColor(propertyName);
                color.a = alpha;
                mat.SetColor(propertyName, color);
            }
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

        // 5. 사라지게 만들기
        OnCleaningCompleted?.Invoke(itemName);
        Destroy(gameObject);
    }


}