using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class WinningPoint : MonoBehaviour {
    [Header("Level Settings")]
    [Tooltip("The exact name of the next scene to load.")]
    public string nextSceneName;

    [Header("Interaction Settings")]
    [Tooltip("If true, the level ends immediately on touch. If false, player must press interact key.")]
    public bool autoTrigger = false;
    public KeyCode interactKey = KeyCode.F;
    [Tooltip("Drag your 'Press F' text object here.")]
    public GameObject interactPrompt;

    [Header("Audio Settings")]
    public string victorySound;
    [Tooltip("How long to wait before loading (usually length of sound).")]
    public float waitDuration = 3.0f;

    // Internal State
    private bool isPlayerInRange;
    private bool isSequenceStarted;
    private GameObject playerObj;

    void Start() {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update() {
        // Only check for key presses if autoTrigger is FALSE
        if (isPlayerInRange && !isSequenceStarted && !autoTrigger) {
            if (Input.GetKeyDown(interactKey)) {
                StartCoroutine(WinSequence());
            }
        }
    }

    IEnumerator WinSequence() {
        isSequenceStarted = true;

        // --- FIX 1: Normalize Time ---
        // Just in case the player won while Time Stop was active!
        Time.timeScale = 1f;

        if (interactPrompt != null) interactPrompt.SetActive(false);

        // 2. Disable Player (The "Ban" Logic)
        if (playerObj != null) {
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;

            NinjaController2D pc = playerObj.GetComponent<NinjaController2D>();
            if (pc != null) pc.enabled = false;

            PlayerCombat combat = playerObj.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;

            // --- FIX 2: Disable Abilities ---
            // Prevent the player from casting Time Stop or Shadows during the victory pose
            Ability_TimeStop timeStop = playerObj.GetComponent<Ability_TimeStop>();
            if (timeStop != null) timeStop.enabled = false;

            Ability_PastShadow pastShadow = playerObj.GetComponent<Ability_PastShadow>();
            if (pastShadow != null) pastShadow.enabled = false;
        }

        // 3. Play Sound
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(victorySound)) {
            AudioManager.Instance.PlaySFX(victorySound);
        }

        Debug.Log("Victory! Loading next level in " + waitDuration + " seconds...");

        // 4. Wait for audio to finish
        yield return new WaitForSeconds(waitDuration);

        // --- FIX 3: Reset Session Checkpoints ---
        // Wipe the temporary memory so the next level spawns you at the door (Checkpoint Zero)
        if (GameManager.Instance != null) {
            GameManager.Instance.hasCheckpoint = false;
        }

        // 5. Load Level
        LoadNextLevel();
    }

    void LoadNextLevel() {
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.LoadScene(nextSceneName);
        } else {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player") && !isSequenceStarted) {
            isPlayerInRange = true;
            playerObj = other.gameObject;

            // If auto trigger is on, start the sequence immediately!
            if (autoTrigger) {
                StartCoroutine(WinSequence());
            }
            // Otherwise, show the prompt so they know to press F
            else {
                if (interactPrompt != null) interactPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            isPlayerInRange = false;
            playerObj = null;
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }
}