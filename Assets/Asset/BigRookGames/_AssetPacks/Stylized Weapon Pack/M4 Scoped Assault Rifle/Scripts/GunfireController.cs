using UnityEngine;
using UnityEngine.UI; // Legacy Text 사용 시 필요

namespace BigRookGames.Weapons
{
    public class GunfireController : MonoBehaviour
    {
        // --- Audio ---
        public AudioClip GunShotClip;
        public AudioClip noAmmoClip; // 총알이 없을 때 재생할 오디오 클립
        public AudioClip reloadClip; // 재장전 소리용 오디오 클립
        public AudioSource source;
        public Vector2 audioPitch = new Vector2(.9f, 1.1f);

        // --- Muzzle ---
        public GameObject muzzlePrefab;
        public GameObject muzzlePosition;

        // --- Config ---
        public bool autoFire;
        public float shotDelay = .5f;
        public bool rotate = true;
        public float rotationSpeed = .25f;

        // --- Options ---
        public GameObject scope;
        public bool scopeActive = true;
        private bool lastScopeState;

        // --- Projectile ---
        [Tooltip("The projectile gameobject to instantiate each time the weapon is fired.")]
        public GameObject projectilePrefab;
        [Tooltip("Sometimes a mesh will want to be disabled on fire. For example: when a rocket is fired, we instantiate a new rocket, and disable" +
            " the visible rocket attached to the rocket launcher")]
        public GameObject projectileToDisableOnFire;

        // --- Timing ---
        [SerializeField] private float timeLastFired;

        // --- Recoil (Weapon) ---
        [Tooltip("Maximum recoil rotation on X axis (up/down) in degrees for weapon")]
        public float maxRecoilX = 5f;
        [Tooltip("Speed at which the weapon returns to original rotation and position")]
        public float recoilReturnSpeed = 10f;
        private Quaternion originalRotation;
        private Quaternion targetRotation;
        private bool isRecoiling = false;
        private float recoilAmount = 0f; // 올라간 반동량 저장

        // --- Ammo & Reload ---
        [Tooltip("최대 총알 개수")]
        public int maxAmmo = 30;
        [Tooltip("현재 총알 개수")]
        public int currentAmmo;
        [Tooltip("재장전 시간 (초)")]
        public float reloadTime = 1.5f;
        private bool isReloading = false;

        // --- UI ---
        [Tooltip("장탄을 표시할 UI Text (Legacy Text)")]
        public Text ammoText; // Legacy Text 참조

        // --- Animation ---
        private Animator animator; // 애니메이터 컴포넌트

        private void Start()
        {
            if (source == null)
            {
                Debug.LogError("AudioSource가 할당되지 않았습니다!");
            }
            else
            {
                source.clip = GunShotClip;
            }
            timeLastFired = 0;
            lastScopeState = scopeActive;

            originalRotation = transform.localRotation;
            targetRotation = originalRotation;
            currentAmmo = maxAmmo; // 시작 시 최대 총알로 초기화
            UpdateAmmoUI(); // UI 초기화

            // 애니메이터 초기화
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator 컴포넌트가 필요합니다!");
            }
        }

        private void Update()
        {
            // --- If rotate is set to true, rotate the weapon in scene ---
            if (rotate)
            {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y
                                                                        + rotationSpeed, transform.localEulerAngles.z);
            }

            // --- Fires the weapon if the delay time period has passed since the last shot and ammo exists ---
            if (autoFire && ((timeLastFired + shotDelay) <= Time.time) && !isReloading && currentAmmo > 0)
            {
                FireWeapon();
            }

            // --- Reload on 'R' key press ---
            if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo)
            {
                StartCoroutine(Reload());
            }

            // 디버깅: currentAmmo 모니터링
            if (currentAmmo < 0)
            {
                Debug.LogError("currentAmmo is negative! Correcting to 0.");
                currentAmmo = 0;
                UpdateAmmoUI();
            }

            // --- Return to original state only if recoiling ---
            if (isRecoiling)
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * recoilReturnSpeed);
                if (Mathf.Abs(Quaternion.Angle(transform.localRotation, targetRotation)) < 0.001f)
                {
                    transform.localRotation = targetRotation;
                    isRecoiling = false;
                }
            }
        }

        /// <summary>
        /// Creates an instance of the muzzle flash.
        /// Also creates an instance of the audioSource so that multiple shots are not overlapped on the same audio source.
        /// Insert projectile code in this function.
        /// </summary>
        public void FireWeapon()
        {
            if (currentAmmo <= 0)
            {
                Debug.Log("No ammo remaining! Current Ammo: " + currentAmmo);
                PlayNoAmmoSound(); // 총을 쏠 때마다 소리 재생
                return; // 총알이 없으면 다른 로직 중단
            }

            timeLastFired = Time.time;

            // --- Apply recoil only when ammo exists ---
            ApplyRecoil();

            // --- Spawn muzzle flash (한 번만 생성) ---
            if (muzzlePrefab != null)
            {
                GameObject flash = Instantiate(muzzlePrefab, muzzlePosition.transform.position, muzzlePosition.transform.rotation);
                Destroy(flash, 1f); // 1초 후 파괴
            }

            // --- Shoot Projectile Object ---
            if (projectilePrefab != null)
            {
                GameObject newProjectile = Instantiate(projectilePrefab, muzzlePosition.transform.position, muzzlePosition.transform.rotation);
                Rigidbody rb = newProjectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Z축 방향 보장 (muzzlePosition의 forward 사용)
                    rb.velocity = muzzlePosition.transform.forward * newProjectile.GetComponent<Bullet>().speed;
                    Debug.Log($"Projectile Spawned at: {muzzlePosition.transform.position}, Rotation: {muzzlePosition.transform.rotation.eulerAngles}, Velocity: {rb.velocity}");
                }
            }

            // --- Decrease ammo ---
            currentAmmo--;
            Debug.Log($"Ammo decreased to: {currentAmmo}");
            UpdateAmmoUI(); // 발사 후 UI 갱신

            // --- Disable any gameobjects, if needed ---
            if (projectileToDisableOnFire != null)
            {
                projectileToDisableOnFire.SetActive(false);
                Invoke("ReEnableDisabledProjectile", 3);
            }

            // --- Handle Audio ---
            if (source != null)
            {
                if (source.transform.IsChildOf(transform))
                {
                    source.clip = GunShotClip; // 명시적 클립 설정
                    source.Play();
                    Debug.Log("Played GunShotClip via source.Play()");
                }
                else
                {
                    AudioSource newAS = Instantiate(source);
                    if ((newAS = Instantiate(source)) != null && newAS.outputAudioMixerGroup != null && newAS.outputAudioMixerGroup.audioMixer != null)
                    {
                        newAS.outputAudioMixerGroup.audioMixer.SetFloat("Pitch", Random.Range(audioPitch.x, audioPitch.y));
                        newAS.pitch = Random.Range(audioPitch.x, audioPitch.y);
                        newAS.PlayOneShot(GunShotClip);
                        Destroy(newAS.gameObject, 4);
                        Debug.Log("Played GunShotClip via PlayOneShot()");
                    }
                }
            }

            
        }

        private void ApplyRecoil()
        {
            if (currentAmmo <= 0)
            {
                Debug.LogWarning("Attempted recoil with no ammo! Aborting. Current Ammo: " + currentAmmo);
                return; // 탄창이 없으면 반동 실행 안 함
            }
            Debug.Log("Applying recoil. Current Ammo: " + currentAmmo);
            // --- Weapon Recoil (Vertical only) ---
            recoilAmount = Random.Range(0f, maxRecoilX); // 위로 올라가는 반동량
            targetRotation = originalRotation * Quaternion.Euler(recoilAmount, 0f, 0f);
            transform.localRotation = targetRotation;
            isRecoiling = true;

            // Set target to half recoil for return after 0.1 seconds
            Invoke("SetHalfRecoilTarget", 0.1f);
        }

        private void SetHalfRecoilTarget()
        {
            if (isRecoiling)
            {
                targetRotation = originalRotation * Quaternion.Euler(recoilAmount * 0.5f, 0f, 0f); // 절반만큼 내려옴
            }
        }

        private void ReEnableDisabledProjectile()
        {
            if (projectileToDisableOnFire != null)
            {
                projectileToDisableOnFire.SetActive(true);
            }
        }

        private System.Collections.IEnumerator Reload()
        {
            isReloading = true;
            Debug.Log("Reloading...");
            if (reloadClip != null && source != null)
            {
                source.PlayOneShot(reloadClip);
                Debug.Log("Played reload sound.");
            }

            // --- Trigger Reload animation ---
            if (animator != null)
            {
                animator.SetTrigger("Reload"); // "Reload" 트리거 호출
                Debug.Log("Triggered Reload animation");
                yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length); // 애니메이션 길이 대기
            }
            else
            {
                yield return new WaitForSeconds(reloadTime); // 애니메이터 없으면 기본 대기
            }

            currentAmmo = maxAmmo; // 음수 상태면 maxAmmo로 강제 초기화
            if (source != null && source.transform.IsChildOf(transform))
            {
                source.clip = GunShotClip; // 재장전 후 기본 클립 복구
                Debug.Log("Reloaded, reset clip to GunShotClip");
            }
            isReloading = false;
            UpdateAmmoUI(); // 재장전 후 UI 갱신
            Debug.Log("Reload complete. Ammo: " + currentAmmo);
        }

        private void UpdateAmmoUI()
        {
            if (ammoText != null)
            {
                ammoText.text = $"Ammo: {Mathf.Max(0, currentAmmo)}/{maxAmmo}"; // 음수 방지
            }
        }

        private void PlayNoAmmoSound()
        {
            if (noAmmoClip == null)
            {
                Debug.LogError("noAmmoClip이 할당되지 않았습니다!");
                return;
            }
            if (source == null)
            {
                Debug.LogError("AudioSource가 없습니다!");
                return;
            }

            Debug.Log($"Playing no ammo sound. Source is child: {source.transform.IsChildOf(transform)}");

            if (source.transform.IsChildOf(transform))
            {
                source.clip = noAmmoClip;
                source.Play();
                Debug.Log("Played no ammo sound via source.Play()");
            }
            else
            {
                AudioSource newAS = Instantiate(source);
                if (newAS != null && newAS.outputAudioMixerGroup != null && newAS.outputAudioMixerGroup.audioMixer != null)
                {
                    newAS.outputAudioMixerGroup.audioMixer.SetFloat("Pitch", Random.Range(audioPitch.x, audioPitch.y));
                    newAS.pitch = Random.Range(audioPitch.x, audioPitch.y);
                    newAS.PlayOneShot(noAmmoClip);
                    Destroy(newAS.gameObject, 4);
                    Debug.Log("Played no ammo sound via PlayOneShot()");
                }
                else
                {
                    Debug.LogError("새로운 AudioSource 인스턴스 생성 또는 설정 실패!");
                }
            }
        }
    }
}