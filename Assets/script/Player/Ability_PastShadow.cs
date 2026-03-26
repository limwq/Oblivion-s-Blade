using UnityEngine;

public class Ability_PastShadow : MonoBehaviour {
    [Header("Ability Settings")]
    public float staminaCost = 30f;
    public float shadowDuration = 5f;
    public KeyCode abilityKey = KeyCode.Q;

    [Header("Camera Integration")]
    public float teleportPunchForce = 12f;

    [Header("Visuals")]
    [Tooltip("The static ghost/shadow prefab left behind")]
    public GameObject shadowPrefab;
    [Tooltip("The particle effect played when casting or teleporting")]
    public GameObject castParticlesPrefab;

    private StaminaSystem staminaSystem;
    private HealthSystem healthSys;

    private GameObject currentShadow;
    private Vector3 savedPosition;

    private float timer;
    private bool isShadowActive;

    public bool IsActive => isShadowActive;
    public float CurrentTimer => timer;

    private void Awake() {
        staminaSystem = GetComponent<StaminaSystem>();
        healthSys = GetComponent<HealthSystem>();
    }

    private void Update() {
        if (healthSys != null && healthSys.IsDead()) return;

        HandleTimer();
        HandleInput();
    }

    private void HandleTimer() {
        if (isShadowActive) {
            timer -= Time.deltaTime;
            if (timer <= 0) {
                ClearShadow();
            }
        }
    }

    private void HandleInput() {
        if (Input.GetKeyDown(abilityKey)) {
            if (GetComponent<NinjaController2D>().abilitiesLocked) {
                // If abilities are locked, play the error sound
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("AbilityError");
                Debug.Log("Powers are stripped! Cannot use Shadow.");
                return;
            }

            if (!isShadowActive) {
                TryPlaceShadow();
            } else {
                TeleportToShadow();
            }
        }
    }

    private void TryPlaceShadow() {
        // --- THE FIX: Use the new unified stamina consumer! ---
        if (staminaSystem != null && staminaSystem.TryConsumeStamina(staminaCost)) {
            savedPosition = transform.position;

            if (shadowPrefab != null) {
                currentShadow = Instantiate(shadowPrefab, savedPosition, transform.rotation);
            }

            if (castParticlesPrefab != null) {
                Destroy(Instantiate(castParticlesPrefab, transform.position, Quaternion.identity), 2f);
            }

            isShadowActive = true;
            timer = shadowDuration;

            // --- AUDIO FIX: Use the Player Channel ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("Shadow_Place");

            Debug.Log("Shadow Placed!");
        }
    }

    private void TeleportToShadow() {
        transform.position = savedPosition;

        if (CameraSpring.Instance != null) {
            CameraSpring.Instance.Punch(Random.insideUnitCircle.normalized, teleportPunchForce);
        }

        // --- AUDIO FIX: Use the Player Channel ---
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("Shadow_Teleport");

        Debug.Log("Teleported to Shadow!");
        ClearShadow();
    }

    private void ClearShadow() {
        isShadowActive = false;
        if (castParticlesPrefab != null) {
            Destroy(Instantiate(castParticlesPrefab, savedPosition, Quaternion.identity), 2f);
        }
        if (currentShadow != null) {
            Destroy(currentShadow);
        }
    }
}