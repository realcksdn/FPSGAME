using UnityEngine;
using UnityEngine.UI;

public class PayloadMover : MonoBehaviour
{
    [Tooltip("이동 경로")]
    public Transform[] waypoints;           // 이동 경로

    [Tooltip("화물 속도")]
    public float speed = 2f;                // 화물 속도

    [Tooltip("플레이어 감지 범위")]
    public float playerDetectionRange = 5f; // 플레이어 감지 범위

    [Tooltip("적 감지 범위")]
    public float enemyDetectionRange = 5f;  // 적 감지 범위

    [Tooltip("플레이어 참조")]
    public Transform player;                // 플레이어 참조

    [Tooltip("진행도 표시용 UI 슬라이더")]
    public Slider progressSlider;           // 진행도용 Slider

    [Tooltip("HP 표시용 UI 슬라이더")]
    public Slider hpSlider;                 // HP용 Slider

    [Tooltip("최대 HP")]
    public float maxHealth = 100f;          // 최대 HP

    [Tooltip("HP 표시용 UI 텍스트")]
    public Text hpText;                     // HP 표시용 Text

    [Tooltip("진행도 표시용 UI 텍스트")]
    public Text progressText;               // 진행도 표시용 Text

    private int currentWaypointIndex = 0;
    private float totalPathLength;
    private float movedDistance = 0f;
    private float currentHealth;            // 현재 HP

    void Start()
    {
        currentHealth = maxHealth;           // 시작 시 최대 HP로 설정
        totalPathLength = CalculateTotalPathLength();
        UpdateSlidersAndText(); // 초기값
    }

    void Update()
    {
        if (player == null || waypoints == null || currentWaypointIndex >= waypoints.Length) return;

        float distanceToPlayer = Vector3.Distance(player.position, transform.position);
        bool isEnemyNear = CheckForEnemies();

        if (distanceToPlayer < playerDetectionRange && !isEnemyNear)
        {
            MoveTowardsWaypoint();
        }
        else
        {
            Debug.Log("Payload stopped. Player too far or enemy nearby.");
        }
    }

    void MoveTowardsWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Length) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        float distance = speed * Time.deltaTime;

        float distanceToTarget = Vector3.Distance(transform.position, targetWaypoint.position);

        if (distanceToTarget <= distance)
        {
            transform.position = targetWaypoint.position;
            movedDistance += distanceToTarget;
            currentWaypointIndex++;
        }
        else
        {
            transform.position += direction * distance;
            movedDistance += distance;
        }

        // movedDistance가 totalPathLength를 초과하지 않도록 제한
        movedDistance = Mathf.Min(movedDistance, totalPathLength);
        UpdateSlidersAndText();
    }

    bool CheckForEnemies()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, enemyDetectionRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                Debug.Log("Enemy detected nearby!");
                return true;
            }
        }
        return false;
    }

    void UpdateSlidersAndText()
    {
        if (progressSlider != null && totalPathLength > 0f)
        {
            // 진행도 슬라이더 업데이트 (0~1로 제한)
            float progress = Mathf.Min(movedDistance / totalPathLength, 1f);
            progressSlider.value = progress;

            // 진행도 텍스트 업데이트
            if (progressText != null)
            {
                progressText.text = $"{(progress * 100f):F1}%"; // 백분율로 표시 (소수점 1자리, 100%로 제한)
            }
        }

        if (hpSlider != null)
        {
            // HP 슬라이더 업데이트
            float healthRatio = currentHealth / maxHealth;
            hpSlider.value = healthRatio;
            hpSlider.maxValue = 1f;
            hpSlider.minValue = 0f;

            // HP 텍스트 업데이트
            if (hpText != null)
            {
                hpText.text = $"{currentHealth:F0}/{maxHealth:F0}"; // HP 표시 (소수점 없이)
            }
        }
    }

    float CalculateTotalPathLength()
    {
        float length = 0f;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            length += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        }
        return length;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f); // HP가 0 아래로 내려가지 않도록
        Debug.Log($"Payload took {damage} damage! Current Health: {currentHealth}");
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("Payload destroyed!");
            // 필요 시 추가 로직 (예: 게임 오버)
        }
        UpdateSlidersAndText(); // 데미지 후 슬라이더와 텍스트 업데이트
    }
}