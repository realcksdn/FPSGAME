using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("스폰 포인트 배열")]
    public Transform[] spawnPoints; // 스폰 포인트 배열

    [Tooltip("적 프리팹")]
    public GameObject enemyPrefab;  // 적 프리팹

    [Tooltip("스폰 간격 (초)")]
    public float spawnInterval = 15f; // 스폰 간격 (초)

    private float nextSpawnTime;

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        // 스폰 포인트와 적 프리팹이 유효한지 확인
        if (spawnPoints == null || spawnPoints.Length == 0 || enemyPrefab == null)
        {
            Debug.LogError("Spawn Points 또는 Enemy Prefab이 설정되지 않았습니다!");
            return;
        }

        // 랜덤 인덱스 선택
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        // 적 생성
        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        Debug.Log("Enemy spawned at " + spawnPoint.position);
    }
}