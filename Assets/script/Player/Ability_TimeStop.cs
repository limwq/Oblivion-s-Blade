using UnityEngine;

public class Ability_TimeStop : MonoBehaviour {
    [Header("Time Stop Settings")]
    public float staminaCost = 40f;
    public float timeStopDuration = 5f;
    public KeyCode abilityKey = KeyCode.E;

    [Header("Visuals")]
    [Tooltip("The particle effect played when time is stopped")]
    public GameObject castParticlesPrefab; // --- NEW ---

    private StaminaSystem staminaSystem;
    private HealthSystem healthSys;
    private NinjaController2D controller;

    private bool isTimeStopped;
    private float timer;

    public bool IsActive => isTimeStopped;
    public float CurrentTimer => timer;

    private void Awake() {
        staminaSystem = GetComponent<StaminaSystem>();
        healthSys = GetComponent<HealthSystem>();
        controller = GetComponent<NinjaController2D>();
    }

    private void Update() {
        if (healthSys != null && healthSys.IsDead()) return;

        if (isTimeStopped) {
            timer -= Time.deltaTime;

            if (Input.GetKeyDown(abilityKey)) {
                Debug.Log("Time Stop cancelled early by player.");
                EndTimeStop();
                return;
            }

            if (timer <= 0) {
                EndTimeStop();
            }
        } else if (Input.GetKeyDown(abilityKey)) {
            if (controller != null && controller.abilitiesLocked) {
                Debug.Log("Powers are stripped! Cannot use Time Stop.");
                return;
            }
            TryStartTimeStop();
        }
    }

    private void TryStartTimeStop() {
        // It checks the stamina, deducts it, AND plays the error audio automatically if it fails!
        if (staminaSystem != null && staminaSystem.TryConsumeStamina(staminaCost)) {

            timer = timeStopDuration;
            isTimeStopped = true;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("time stop 2");

            if (castParticlesPrefab != null) {
                Destroy(Instantiate(castParticlesPrefab, transform.position, Quaternion.identity), 2f);
            }

            GlobalEvents.TriggerTimeStopStarted();
            Debug.Log("ZA WARUDO! Time Stopped.");
        }
        // Notice we don't even need an 'else' block anymore!
    }

    private void EndTimeStop() {
        isTimeStopped = false;
        timer = 0f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("time start 2");
        GlobalEvents.TriggerTimeStopEnded();
        Debug.Log("Time resumes.");
    }
}