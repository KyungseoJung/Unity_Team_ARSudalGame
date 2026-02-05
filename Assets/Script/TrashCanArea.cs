using UnityEngine;
using UnityEngine.UI;

public class TrashCanArea : MonoBehaviour
{
    private Image img;
    private Vector3 originalScale;

    void Start()
    {
        img = GetComponent<Image>();
        originalScale = transform.localScale;
    }

    // 아이템이 영역에 들어오면 호출 (ItemDragger2D에서 부름)
    public void OnHoverEnter()
    {
        img.color = Color.red; // 강조색
        transform.localScale = originalScale * 1.2f; // 커지는 효과
    }

    public void OnHoverExit()
    {
        img.color = Color.white;
        transform.localScale = originalScale;
    }
}