using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class AutoWidthLimiter : MonoBehaviour
{
    [Header("화면 너비 대비 최대 폭 (%)")]
    [Range(0.1f, 0.95f)] public float widthPercentage = 0.8f; // 화면의 80%를 최대치로 설정

    private LayoutElement layoutElement;
    private RectTransform canvasRect;

    void Update()
    {
        if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
        if (canvasRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvasRect = canvas.GetComponent<RectTransform>();
        }

        if (layoutElement != null && canvasRect != null)
        {
            // [자동 인식 핵심]
            // 수동으로 숫자를 넣는 대신, 현재 캔버스의 실제 너비에 비율을 곱해 계산합니다.
            float screenMaxWidth = canvasRect.rect.width * widthPercentage;

            // 이 값이 Layout Element의 Preferred Width에 자동으로 할당되어 줄바꿈 기준이 됩니다.
            layoutElement.preferredWidth = screenMaxWidth;
        }
    }
}