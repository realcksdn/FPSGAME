using BigRookGames.Weapons;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("총알의 이동 속도")] public float speed = 40f;
    [Tooltip("총알의 생존 시간 (초)")] public float lifetime = 2f;
    [Tooltip("적에게 입힐 데미지")] public int damage = 10;

    private Rigidbody rb;

    void Start()
    {
        // Rigidbody 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Bullet에 Rigidbody 컴포넌트가 필요합니다!");
            return;
        }

        // Z축 방향으로 속도 적용 (transform.right을 따름)
        rb.velocity = transform.right * speed;

        // 생존 시간 후 파괴
        Destroy(gameObject, lifetime);

        // 디버깅 로그
        Debug.Log($"Bullet spawned at {transform.position}, Velocity: {rb.velocity}, Direction: {transform.right}");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Bullet triggered with {other.gameObject.name} at {transform.position}, Tag: {other.tag}");

        // 적 태그 확인
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); // 적에게 데미지 적용
                Debug.Log($"Bullet hit {other.gameObject.name}, applying {damage} damage, Enemy HP: {enemy.currentHealth}");
            }
            else
            {
                Debug.LogWarning($"EnemyAI component not found on {other.gameObject.name}");
            }
        }

        // 충돌 시 총알 파괴
        Destroy(gameObject);
    }
}