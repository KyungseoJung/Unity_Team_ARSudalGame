using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤

    // 현재 화면에 나와 있는 인터랙션 오브젝트 (없으면 null)
    public GameObject currentActiveObject = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Q: 지금 소환해도 되나요?
    public bool CanSpawn()
    {
        // 현재 활성화된 오브젝트가 없어야 소환 가능
        return currentActiveObject == null;
    }

    // 소환했을 때 등록하기
    public void RegisterObject(GameObject obj)
    {
        currentActiveObject = obj;
        Debug.Log("🔒 오브젝트 등록됨: 다른 소환 차단!");
    }

    // 수집 완료해서 사라질 때 해제하기
    public void UnregisterObject()
    {
        currentActiveObject = null;
        Debug.Log("🔓 오브젝트 해제됨: 이제 다른 소환 가능!");
    }
}