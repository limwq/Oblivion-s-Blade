using UnityEngine;
using UnityEngine.UI;

public class PlayerFloatingHUD : MonoBehaviour {
    [Header("Core Systems")]
    public HealthSystem healthSys;
    public StaminaSystem staminaSys;
    // (Removed NinjaController2D since we don't need it to check actions anymore!)

    [Header("Ability Systems")]
    public Ability_TimeStop timeStopSys;
    public Ability_PastShadow shadowSys;

    [Header("UI Elements")]
    [Tooltip("Drag the Canvas itself here (make sure it has a CanvasGroup component!)")]
    public CanvasGroup hudCanvasGroup;

    [Header("Bars & Icons")]
    public Slider healthSlider;
    public Slider staminaSlider;
    public Image timeStopIcon;
    public Image shadowIcon;

    [Header("HUD Visibility Settings")]
    [Tooltip("How long the HUD stays visible after stopping an action")]
    public float hideDelay = 3f;
    [Tooltip("How fast the HUD fades in and out")]
    public float fadeInSpeed = 5f;
    public float fadeOutSpeed = 1f;

    [Header("Input Keys for HUD Wakeup")]
    [Tooltip("The HUD will appear when any of these are pressed")]
    public KeyCode rollKey = KeyCode.LeftShift;
    // Note: Assuming Left Click (Mouse 0) is Attack. We will hardcode that in the Update loop.

    private Transform playerTransform;
    private Quaternion fixedRotation;

    private float visibilityTimer = 0f;
    private float lastKnownHealth;
    private float lastKnownStamina;

    private void Start() {
        playerTransform = transform.parent;
        fixedRotation = transform.rotation;

        if (healthSys != null && healthSlider != null) {
            healthSlider.maxValue = healthSys.maxHealth;
            lastKnownHealth = healthSys.currentHealth;
        }

        if (staminaSys != null && staminaSlider != null) {
            staminaSlider.maxValue = staminaSys.maxStamina;
            lastKnownStamina = staminaSys.GetCurrentStamina();
        }

        // Start the HUD completely invisible
        if (hudCanvasGroup != null) hudCanvasGroup.alpha = 0f;
    }

    private void LateUpdate() {
        if (playerTransform == null) return;

        bool shouldShowHUD = false;

        // 1. UPDATE BARS & CHECK FOR STAT CHANGES (Damage or Stamina Use)
        if (healthSys != null && healthSlider != null) {
            healthSlider.value = healthSys.currentHealth;
            if (healthSys.currentHealth != lastKnownHealth) {
                shouldShowHUD = true;
                lastKnownHealth = healthSys.currentHealth;
            }
        }

        if (staminaSys != null && staminaSlider != null) {
            float currentStamina = staminaSys.GetCurrentStamina();
            staminaSlider.value = currentStamina;
            if (currentStamina != lastKnownStamina) {
                shouldShowHUD = true;
                lastKnownStamina = currentStamina;
            }
        }

        // 2. CHECK ABILITY STATES & UPDATE ICONS
        if (timeStopSys != null && timeStopIcon != null) {
            if (timeStopSys.IsActive) {
                shouldShowHUD = true;
                timeStopIcon.fillAmount = 1f - (timeStopSys.CurrentTimer / timeStopSys.timeStopDuration);
            } else {
                timeStopIcon.fillAmount = 1f;
            }
        }

        if (shadowSys != null && shadowIcon != null) {
            if (shadowSys.IsActive) {
                shouldShowHUD = true;
                shadowIcon.fillAmount = 1f - (shadowSys.CurrentTimer / shadowSys.shadowDuration);
            } else {
                shadowIcon.fillAmount = 1f;
            }
        }

        // --- THE FIX: 3. CHECK PLAYER INPUTS DIRECTLY ---
        // If the player presses Attack (Left Click) or the Roll Key, wake up the HUD!
        if (Input.GetMouseButton(0) || Input.GetKeyDown(rollKey)) {
            shouldShowHUD = true;
        }

        // 4. THE VISIBILITY TIMER LOGIC
        if (shouldShowHUD) {
            visibilityTimer = hideDelay; // Reset the 1-second countdown
        } else if (visibilityTimer > 0) {
            visibilityTimer -= Time.deltaTime; // Count down
        }

        // 5. FADE THE ENTIRE CANVAS
        if (hudCanvasGroup != null) {
            if (visibilityTimer > 0) {
                hudCanvasGroup.alpha = Mathf.MoveTowards(hudCanvasGroup.alpha, 1f, fadeInSpeed * Time.deltaTime);
            } else {
                hudCanvasGroup.alpha = Mathf.MoveTowards(hudCanvasGroup.alpha, 0f, fadeOutSpeed * Time.deltaTime);
            }
        }

        // 6. PREVENT THE FLIPPING BUG
        transform.rotation = fixedRotation;
        Vector3 parentScale = playerTransform.localScale;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * Mathf.Sign(parentScale.x),
            transform.localScale.y,
            transform.localScale.z
        );
    }
}