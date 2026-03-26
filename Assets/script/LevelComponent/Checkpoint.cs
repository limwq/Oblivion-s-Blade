using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour {
    [Header("Checkpoint Visuals")]
    [Tooltip("Drag the child GameObject containing your Particle System (sparks) here.")]
    public GameObject sparkEffectObject; // --- RENAMED for clarity ---

    private bool isActivated = false;

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
        // Ensure the sparks are OFF at the very start
        if (sparkEffectObject != null) sparkEffectObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!isActivated && collision.CompareTag("Player")) {
            isActivated = true;

            // 1. Session Save (GameManager)
            if (GameManager.Instance != null) {
                GameManager.Instance.lastCheckpointPos = transform.position;
                GameManager.Instance.hasCheckpoint = true;
            }

            // 2. Hard Drive Save (SaveManager)
            if (SaveManager.Instance != null) {
                string currentScene = SceneManager.GetActiveScene().name;
                SaveManager.Instance.SaveCheckpoint(currentScene, transform.position);
            }

            // 3. Fully Heal the Player
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null) {
                playerHealth.currentHealth = playerHealth.maxHealth;
            }

            // 4. Fully Restore Stamina
            StaminaSystem playerStamina = collision.GetComponent<StaminaSystem>();
            if (playerStamina != null) {
                playerStamina.RestoreFullStamina();
            }

            // --- AUDIO TRIGGER ---
            if (AudioManager.Instance != null) {
                AudioManager.Instance.PlaySFX("CheckpointReached");
            }

            // --- VISUAL TRIGGER: Turn on the sparks! ---
            if (sparkEffectObject != null) sparkEffectObject.SetActive(true);

            Debug.Log($"Checkpoint Reached! Saved position: {transform.position}");
        }
    }
}