using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PayloadShakeTransition : MonoBehaviour
{
    [Tooltip("화물이 도달해야 할 목표 지점")]
    public Transform targetPoint;

    [Tooltip("화물과의 최대 감지 거리")]
    public float detectionRange = 2f;

    [Tooltip("화면 흔들림 강도")]
    public float shakeIntensity = 0.1f;

    [Tooltip("화면 흔들림 지속 시간 (초)")]
    public float shakeDuration = 4f; // 흔들림 시간 늘림 (기존 2초 -> 4초)

    [Tooltip("페이드 아웃 지속 시간 (초)")]
    public float fadeDuration = 2f; // 페이드 아웃 시간 늘림 (기존 1초 -> 2초)

    [Tooltip("다음 씬 이름")]
    public string nextSceneName = "VictoryScene";

    [Tooltip("흔들림 증가 속도")]
    public float shakeIncreaseSpeed = 0.025f; // 흔들림 증가 속도 반감 (천천히)

    [Tooltip("페이드 아웃에 사용할 UI 패널 (Image)")]
    public Image fadePanel; // 기존 하얀색 투명 패널 연결

    private Camera mainCamera;
    private float shakeTimer;
    private float currentIntensity;
    private Vector3 originalCameraPos;
    private bool isShaking = false;
    private bool isFading = false;
    private float fadeTimer;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera를 찾을 수 없습니다!");
            return;
        }
        originalCameraPos = mainCamera.transform.localPosition;

        if (fadePanel == null)
        {
            Debug.LogError("Fade Panel (Image)을 Inspector에서 연결해 주세요!");
            return;
        }
        fadePanel.color = new Color(1f, 1f, 1f, 0f); // 초기 투명
    }

    void Update()
    {
        if (targetPoint != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetPoint.position);

            if (distanceToTarget <= detectionRange && !isShaking && !isFading)
            {
                StartShakeAndTransition();
            }
        }

        if (isShaking)
        {
            ShakeCamera();
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0 && !isFading)
            {
                StartFade();
            }
        }

        if (isFading)
        {
            FadeScreen();
            fadeTimer -= Time.deltaTime;
            if (fadeTimer <= 0)
            {
                EndTransition();
            }
        }
    }

    void StartShakeAndTransition()
    {
        isShaking = true;
        shakeTimer = shakeDuration;
        currentIntensity = 0f;
    }

    void ShakeCamera()
    {
        if (mainCamera != null)
        {
            currentIntensity = Mathf.Min(currentIntensity + shakeIncreaseSpeed * Time.deltaTime, shakeIntensity);
            Vector3 shakeOffset = Random.insideUnitSphere * currentIntensity;
            mainCamera.transform.localPosition = originalCameraPos + shakeOffset;
        }
    }

    void StartFade()
    {
        isShaking = false;
        isFading = true;
        fadeTimer = fadeDuration;
        mainCamera.transform.localPosition = originalCameraPos; // 흔들림 복원
    }

    void FadeScreen()
    {
        if (fadePanel != null)
        {
            float alpha = 1f - (fadeTimer / fadeDuration);
            fadePanel.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    void EndTransition()
    {
        isFading = false;
        SceneManager.LoadScene(nextSceneName);
    }
}