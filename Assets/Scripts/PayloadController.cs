using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리용

public class PayloadController : MonoBehaviour
{
    [Tooltip("플레이어가 화물 근처로 간주되는 거리")]
    public float activationRange = 10f;
    [Tooltip("체크포인트 배열 (경로 정의)")]
    public Transform[] checkpoints;
    [Tooltip("체크포인트 도달 후 대기 시간 (초)")]
    public float waitTime = 5f;
    [Tooltip("마지막 체크포인트 도달 후 이동할 씬 이름")]
    public string nextSceneName = "VictoryScene";
    [Tooltip("진행도 증가 속도 (0~1 사이, 클수록 빠름)")]
    public float progressSpeed = 0.5f;
    private int currentCheckpointIndex = 0;
    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private Transform player;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private PlayerGunController playerController; // 플레이어 컨트롤러 참조

    void Start()
    {
        if (checkpoints == null || checkpoints.Length == 0)
        {
            Debug.LogError("체크포인트가 설정되지 않았습니다! Inspector에서 설정하세요.");
            return;
        }
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerGunController>();
            if (playerController == null)
            {
                Debug.LogWarning("PlayerGunController가 플레이어에 없습니다. 일부 기능이 비활성화됩니다.");
            }
        }
        else
        {
            Debug.LogError("플레이어 객체를 찾을 수 없습니다! 'Player' 태그를 설정하세요.");
        }
    }

    void Update()
    {
        if (player == null || checkpoints == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= activationRange && currentCheckpointIndex < checkpoints.Length)
        {
            if (!isWaiting)
            {
                Transform target = checkpoints[currentCheckpointIndex];
                transform.position = Vector3.MoveTowards(transform.position, target.position, 2f * Time.deltaTime); // moveSpeed 제거, 기본값 2f 사용

                if (Vector3.Distance(transform.position, target.position) < 0.5f)
                {
                    currentCheckpointIndex++;
                    targetProgress += 1f / checkpoints.Length; // 체크포인트 수로 균등 분배
                    isWaiting = true;
                    waitTimer = waitTime;
                    Debug.Log($"Reached checkpoint {currentCheckpointIndex}. Target Progress: {targetProgress * 100}%");

                    if (currentCheckpointIndex >= checkpoints.Length && playerController != null)
                    {
                        playerController.StartShakeAndFade();
                    }
                }
            }
            else
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    if (currentCheckpointIndex >= checkpoints.Length)
                    {
                        SceneManager.LoadScene(nextSceneName);
                    }
                }
            }
        }

        // 진행도 부드럽게 전환
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressSpeed);
        if (Mathf.Abs(currentProgress - targetProgress) < 0.01f)
        {
            currentProgress = targetProgress;
        }
    }

    // 진행도 가져오기 (외부 접근용)
    public float GetProgress()
    {
        return Mathf.Clamp01(currentProgress);
    }
}