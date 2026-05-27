using BigRookGames.Weapons;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerGunController : MonoBehaviour
{
    public GunfireController equippedGun;
    public Slider payloadProgress; // 화물 진행 상황 슬라이더
    public Slider playerHP;
    private float fireRate = 0.1f;
    private float nextFireTime;
    public int currentHP = 100; // Inspector에서 조절 가능, 풀피
    public int maxHP = 100;     // Inspector에서 조절 가능, 최대 HP
    private Transform payload;
    private Vector3 initialPayloadPos;
    public float maxDistance = 100f; // 화물 경로 총 거리 (Inspector에서 조정 가능)
    private Camera mainCamera;
    private bool isShaking = false;
    private float shakeDuration = 2f;
    private float shakeMagnitude = 0.1f;
    private float fadeDuration = 5f; // 페이드 지속 시간
    private float fadeTimer = 0f;
    private CanvasGroup fadeCanvasGroup;
    private CharacterController controller;
    private Vector3 playerVelocity;
    private float moveSpeed = 5f;
    private float jumpForce = 5f;
    private float gravity = -9.81f;
    private float mouseSensitivity = 100f;
    private float xRotation = 0f;
    private float currentCameraRecoil = 0f;
    private bool isCameraRecoiling = false;
    private float cameraRecoilMaxAngle = 2f;
    private float cameraRecoilReturnSpeed = 5f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) Debug.LogError("CharacterController 필요!");
        mainCamera = Camera.main;
        if (mainCamera == null) Debug.LogError("카메라 필요!");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameObject payloadObj = GameObject.Find("Payload");
        if (payloadObj != null)
        {
            payload = payloadObj.transform;
            initialPayloadPos = payload.position;
            Debug.Log($"Payload found at {payload.position}");
        }
        else
        {
            Debug.LogError("Payload 객체를 찾을 수 없습니다! 'Payload'라는 이름으로 설정하세요.");
        }
        if (payloadProgress == null) Debug.LogError("PayloadProgress Slider 필요!");
        if (playerHP == null) Debug.LogError("PlayerHP Slider 필요!");
        playerHP.maxValue = maxHP; // 최대 HP로 설정
        playerHP.value = maxHP - currentHP; // Right to Left로 초기화 (풀피 = 0)
        if (payloadProgress != null)
        {
            payloadProgress.maxValue = 1f; // 진행도 최대값 1
            payloadProgress.value = 0f;    // 초기 진행도 0
        }
        Debug.Log($"HP initialized: Current = {currentHP}, Max = {maxHP}, Slider Value = {playerHP.value}");
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && equippedGun != null && Time.time >= nextFireTime)
        {
            equippedGun.FireWeapon();
            if (equippedGun.currentAmmo > 0) ApplyCameraRecoil();
            nextFireTime = Time.time + fireRate;
        }
        HandleMovement();
        HandleRotation();
        UpdateCameraRecoil();

        

       

        // 쉐이크 및 페이드 업데이트
        if (isShaking)
        {
            ShakeCamera();
            fadeTimer += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(fadeTimer / fadeDuration);
                Debug.Log($"Fading... Alpha: {fadeCanvasGroup.alpha}");
                if (fadeTimer >= fadeDuration)
                {
                    SwitchScene();
                }
            }
        }
    }

    // 외부에서 HP 조절용 (Inspector나 다른 스크립트에서 호출)
    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);
        if (playerHP != null) playerHP.value = maxHP - currentHP; // Right to Left로 반영
        Debug.Log($"HP damaged: Current = {currentHP}, Damage = {damage}, Slider Value = {playerHP.value}");
        if (currentHP <= 0) Debug.Log("Player Dead!");
    }

    public void Heal(int healAmount)
    {
        currentHP = Mathf.Clamp(currentHP + healAmount, 0, maxHP);
        if (playerHP != null) playerHP.value = maxHP - currentHP; // Right to Left로 반영
        Debug.Log($"HP healed: Current = {currentHP}, Heal = {healAmount}, Slider Value = {playerHP.value}");
    }

    private void HandleMovement()
    {
        if (controller == null) return;

        bool isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move = move.normalized * moveSpeed;

        controller.Move(move * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        Debug.Log($"Mouse X: {mouseX}, Mouse Y: {mouseY}");

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        if (mainCamera != null)
        {
            mainCamera.transform.localRotation = Quaternion.Euler(xRotation + currentCameraRecoil, 0f, 0f);
        }
    }

    private void ApplyCameraRecoil()
    {
        if (mainCamera != null)
        {
            currentCameraRecoil = Random.Range(-cameraRecoilMaxAngle, cameraRecoilMaxAngle);
            isCameraRecoiling = true;
            Debug.Log("Applied camera recoil. Current Ammo: " + (equippedGun != null ? equippedGun.currentAmmo : "N/A"));
        }
    }

    private void UpdateCameraRecoil()
    {
        if (isCameraRecoiling && mainCamera != null)
        {
            currentCameraRecoil = Mathf.Lerp(currentCameraRecoil, 0f, Time.deltaTime * cameraRecoilReturnSpeed);
            if (Mathf.Abs(currentCameraRecoil) < 0.01f)
            {
                currentCameraRecoil = 0f;
                isCameraRecoiling = false;
            }
        }
    }

    public void StartShakeAndFade()
    {
        isShaking = true;
        fadeTimer = 0f;
        Debug.Log("Started shake and fade transition.");
    }

    private void ShakeCamera()
    {
        if (mainCamera != null && shakeDuration > 0)
        {
            mainCamera.transform.localPosition = Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
            if (shakeDuration <= 0)
            {
                mainCamera.transform.localPosition = Vector3.zero;
                // isShaking = false; // 페이드 완료까지 유지
            }
        }
    }

    private void SwitchScene()
    {
        PayloadController payloadCtrl = FindObjectOfType<PayloadController>();
        if (payloadCtrl != null && !string.IsNullOrEmpty(payloadCtrl.nextSceneName))
        {
            SceneManager.LoadScene(payloadCtrl.nextSceneName);
            Debug.Log($"Scene switched to {payloadCtrl.nextSceneName}");
        }
        else
        {
            Debug.LogError("nextSceneName이 설정되지 않았거나 PayloadController를 찾을 수 없습니다!");
        }
    }
}