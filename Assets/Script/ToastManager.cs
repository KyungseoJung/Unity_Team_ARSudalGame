using UnityEngine;
using TMPro;
using System.Collections;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance; // 어디서든 접근 가능하게 싱글톤 설정

    public CanvasGroup toastCanvasGroup;
    public TextMeshProUGUI toastText;

    void Awake()
    {
        Instance = this;
        if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
    }

    public void ShowToast(string message, float duration = 1.0f)
    {
        StopAllCoroutines();
        StartCoroutine(ToastFlow(message, duration));
    }

    private IEnumerator ToastFlow(string message, float duration)
    {
        toastText.text = message;

        // 페이드 인 (0.3초)
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            toastCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / 0.3f);
            yield return null;
        }

        yield return new WaitForSeconds(duration);

        // 페이드 아웃 (0.5초)
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            toastCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / 0.5f);
            yield return null;
        }
    }
}