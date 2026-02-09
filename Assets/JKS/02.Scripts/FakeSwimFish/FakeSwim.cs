using UnityEngine;

public class FakeSwim : MonoBehaviour
{
    [Header("Swim Speed")]
    public float swimSpeed = 1.5f;

    [Header("Rotation Amounts")]
    public float yawAmount = 15f;   // 좌우 흔들림
    public float rollAmount = 5f;   // 몸 비틀림

    private Quaternion startLocalRot;

    void Start()
    {
        // 회전 기준만 저장 (위치는 저장 X)
        startLocalRot = transform.localRotation;
    }

    void Update()
    {
        float t = Time.time * swimSpeed;

        float yaw = Mathf.Sin(t) * yawAmount;
        float roll = Mathf.Sin(t * 1.5f + 1f) * rollAmount;

        Quaternion swimRot = Quaternion.Euler(0f, yaw, roll);

        // 위치는 건드리지 않고 회전만 적용
        transform.localRotation = startLocalRot * swimRot;
    }
}
