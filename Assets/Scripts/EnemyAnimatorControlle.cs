using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimatorController : MonoBehaviour
{
    public float detectionRange = 5f; // 플레이어 감지 범위
    private Animator animator;
    private GameObject player;
    private NavMeshAgent agent;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        animator.SetBool("walk_anim", false); // 시작 시 false

        // "Player" 태그를 가진 오브젝트 찾기
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= detectionRange)
        {
            // walk_anim 켜고 추적 시작
            animator.SetBool("walk_anim", true);
            agent.SetDestination(player.transform.position);
        }
        else
        {
            // walk_anim 끄고 이동 멈춤
            animator.SetBool("walk_anim", false);
            agent.ResetPath();
        }
    }
}