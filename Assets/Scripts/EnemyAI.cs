using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("General Settings")]
    public Transform payload;        // 화물 참조 (Payload 태그)
    [Tooltip("최대 HP")] public float maxHealth = 50f; // 기본 최대 HP
    public float currentHealth; // 현재 HP, Inspector에 표시

    [Header("Attack Settings")]
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    private float nextAttackTime;
    public int damage = 10;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 15f;

    [Header("Animation Settings")]
    private Animator animator;        // Animator 컴포넌트

    [Header("Death Effects")]
    public GameObject destroyEffect; // 파괴 이펙트 프리팹
    public AudioClip destroySound;   // 파괴 소리 클립
    public float effectDuration = 1f; // 이펙트 지속 시간 (초)
    private AudioSource audioSource; // 오디오 소스 컴포넌트

    void Start()
    {
        if (payload == null)
        {
            payload = GameObject.FindWithTag("Payload").transform;
            if (payload == null) Debug.LogError("Payload 태그가 달린 객체를 찾을 수 없습니다!");
        }
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogError("Animator 컴포넌트가 없습니다!");
        audioSource = gameObject.AddComponent<AudioSource>(); // AudioSource 추가
        currentHealth = maxHealth; // 시작 시 최대 HP로 설정
        animator.SetBool("Walk_Anim", false);  // 기본 Idle 상태
        animator.SetBool("Roll_Anim", false);
        animator.SetBool("Open_Anim", true);   // 시작 시 Open_Anim 활성화

        // 디버깅 로그 추가
        Debug.Log($"Enemy initialized at {transform.position}, HP: {currentHealth}, Tag: {gameObject.tag}");
    }

    void Update()
    {
        if (payload == null || animator == null) return;

        if (currentHealth > 0) // HP가 0보다 클 때만 동작
        {
            float distanceToPayload = Vector3.Distance(transform.position, payload.position);

            if (distanceToPayload <= detectionRange)
            {
                if (distanceToPayload > attackRange) // 이동
                {
                    MoveTowardsTarget();
                    LookAtTarget(); // 화물 방향으로 회전
                    animator.SetBool("Walk_Anim", true);  // 이동 시 Walk_Anim
                    animator.SetBool("Roll_Anim", false);
                }
                else // 화물에 붙어 있을 때 공격
                {
                    animator.SetBool("Walk_Anim", false);
                    animator.SetBool("Roll_Anim", true); // 공격 시 Attack_Anim
                    ApplyDamageToPayload();
                }
            }
            else // 아무것도 안 함 (Idle)
            {
                animator.SetBool("Walk_Anim", false);
                animator.SetBool("Roll_Anim", false);
            }
        }
    }

    void MoveTowardsTarget()
    {
        Vector3 direction = (payload.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    void LookAtTarget()
    {
        Vector3 direction = (payload.position - transform.position).normalized;
        if (direction != Vector3.zero) // 방향이 유효할 때만 회전
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void ApplyDamageToPayload()
    {
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;

            PayloadMover payloadCtrl = payload.GetComponent<PayloadMover>();
            if (payloadCtrl != null) payloadCtrl.TakeDamage(damage);
            else Debug.LogError("PayloadMover component not found on payload!");
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Enemy took {damage} damage! Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 파괴 이펙트 재생 후 지정된 시간 후 파괴
        if (destroyEffect != null)
        {
            GameObject effectInstance = Instantiate(destroyEffect, transform.position, Quaternion.identity);
            Destroy(effectInstance, effectDuration); // 이펙트 지속 시간 후 파괴
        }

        // 파괴 소리 재생
        if (destroySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destroySound);
        }

        // 적 파괴
        Destroy(gameObject);
    }
}