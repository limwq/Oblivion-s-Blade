using UnityEngine;
using System;

public class StaminaSystem : MonoBehaviour {
    [Header("Stamina Core Settings")]
    [Tooltip("The maximum amount of stamina the player can hold.")]
    public float maxStamina = 100f;

    [Tooltip("Amount of stamina rewarded per enemy kill.")]
    public float killRewardAmount = 20f;

    [Tooltip("Current available stamina.")]
    [SerializeField] private float currentStamina;
    public float GetCurrentStamina() { return currentStamina; }

    public event Action<float, float> OnStaminaChanged;

    // --- NEW: Reference to the controller for the Infinite Stamina check ---
    private NinjaController2D controller;

    private void Awake() {
        controller = GetComponent<NinjaController2D>();
    }

    private void Start() {
        currentStamina = maxStamina;
        NotifyStaminaChange();
    }

    private void OnEnable() {
        GlobalEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable() {
        GlobalEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled() {
        Debug.Log($"[StaminaSystem] Enemy death detected. Adding {killRewardAmount} stamina.");
        AddStamina(killRewardAmount);
    }

    public bool HasEnoughStamina(float amountRequired) {
        // --- THE FIX: If infinite stamina is on, they ALWAYS have enough! ---
        if (controller != null && controller.infiniteStamina) return true;

        return currentStamina >= amountRequired;
    }

    // --- NEW: The unified, centralized stamina consumer! ---
    // Use this in your abilities instead of checking HasEnoughStamina manually.
    public bool TryConsumeStamina(float amount) {
        if (HasEnoughStamina(amount)) {

            // Only deduct the math if infinite stamina is OFF
            if (controller == null || !controller.infiniteStamina) {
                currentStamina -= amount;
                NotifyStaminaChange();
                Debug.Log($"[StaminaSystem] Consumed {amount} stamina. Current: {currentStamina}/{maxStamina}");
            } else {
                Debug.Log("[StaminaSystem] Infinite Stamina active! No deduction.");
            }

            return true; // Success! The ability is allowed to fire.
        } else {
            // --- THE AUDIO FIX: Play the error sound centrally! ---
            if (AudioManager.Instance != null) {
                AudioManager.Instance.PlayPlayerSFX("AbilityError");
            }
            Debug.LogWarning("StaminaSystem: Attempted to consume stamina, but not enough available.");

            return false; // Failed! The ability should cancel.
        }
    }

    // (Keeping your old ConsumeStamina method just in case other scripts rely on it)
    public void ConsumeStamina(float amount) {
        TryConsumeStamina(amount);
    }

    public void AddStamina(float amount) {
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        Debug.Log($"[StaminaSystem] Stamina restored. Current: {currentStamina}/{maxStamina}");
        NotifyStaminaChange();
    }

    private void NotifyStaminaChange() {
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void RestoreFullStamina() {
        currentStamina = maxStamina;
        NotifyStaminaChange();
    }
}