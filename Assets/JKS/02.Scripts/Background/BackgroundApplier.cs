using UnityEngine;

public class BackgroundApplier : MonoBehaviour
{
    public GameObject bgDefault;
    public GameObject bgTower;
    public GameObject bgARC;

    private BackgroundId lastApplied;

    private void Start()
    {
        Apply();
    }

    private void Update()
    {
        // 선택이 바뀌면 즉시 반영
        if (BackgroundState.Instance.selected != lastApplied)
        {
            Apply();
        }
    }

    public void Apply()
    {
        lastApplied = BackgroundState.Instance.selected;

        bgDefault.SetActive(lastApplied == BackgroundId.Default_01);
        bgTower.SetActive(lastApplied == BackgroundId.Tower_02);
        bgARC.SetActive(lastApplied == BackgroundId.TheARC_03);
    }
}
