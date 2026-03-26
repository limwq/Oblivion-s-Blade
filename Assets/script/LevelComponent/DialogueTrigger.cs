using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DialogueTrigger : MonoBehaviour {
    [Header("Dialogue Content")]
    public string npcName = "Guide";
    [TextArea(3, 10)]
    public string[] sentences;

    [Header("Per-Line Events (Optional)")]
    [Tooltip("Type the exact AudioManager SFX name. Must match the number of sentences! Leave elements blank to play nothing.")]
    public string[] sfxPerLine;
    [Tooltip("Type the exact Animator trigger name. Must match the number of sentences! Leave elements blank to play nothing.")]
    public string[] animTriggerPerLine;
    [Tooltip("The animator that will play the triggers (e.g., the Boss or NPC)")]
    public Animator targetAnimator;

    [Header("Scene Transition (After Dialogue)")]
    [Tooltip("If true, fades to a new scene after the dialogue panel closes.")]
    public bool transitionAfterDialogue = false;
    public float waitBeforeTransition = 1.0f;
    public string nextSceneName;

    [Header("Interaction Settings")]
    public bool autoTrigger = false;
    public bool triggerOnce = false;
    public GameObject interactPrompt;

    private bool isPlayerInRange;
    private bool hasTriggered = false;
    private GameObject playerObj;

    void Start() {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update() {
        if (isPlayerInRange && !autoTrigger && (!triggerOnce || !hasTriggered)) {
            if (Input.GetKeyDown(KeyCode.F)) {
                if (DialogueManager.Instance.dialoguePanel.activeSelf == false) {
                    TriggerDialogue();
                }
            }
        }
    }

    public void TriggerDialogue() {
        if (triggerOnce) {
            hasTriggered = true;
        }

        DialogueManager.Instance.StartDialogue(this, npcName, sentences);

        if (interactPrompt != null) interactPrompt.SetActive(false);

        StartCoroutine(LockPlayerRoutine());
    }

    public void PlayLineEvent(int lineIndex) {
        if (sfxPerLine != null && lineIndex < sfxPerLine.Length) {
            if (!string.IsNullOrEmpty(sfxPerLine[lineIndex]) && AudioManager.Instance != null) {
                AudioManager.Instance.PlaySFX(sfxPerLine[lineIndex]);
            }
        }

        if (animTriggerPerLine != null && lineIndex < animTriggerPerLine.Length) {
            if (!string.IsNullOrEmpty(animTriggerPerLine[lineIndex]) && targetAnimator != null) {
                targetAnimator.SetTrigger(animTriggerPerLine[lineIndex]);
            }
        }
    }

    private IEnumerator LockPlayerRoutine() {
        if (playerObj == null) playerObj = GameObject.FindGameObjectWithTag("Player");

        NinjaController2D controller = null;
        PlayerCombat combat = null;
        Ability_TimeStop timeStop = null;
        Ability_PastShadow pastShadow = null;

        if (playerObj != null) {
            controller = playerObj.GetComponent<NinjaController2D>();
            combat = playerObj.GetComponent<PlayerCombat>();
            timeStop = playerObj.GetComponent<Ability_TimeStop>();
            pastShadow = playerObj.GetComponent<Ability_PastShadow>();
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            Animator anim = playerObj.GetComponent<Animator>();

            if (controller != null) controller.canMove = false;
            if (combat != null) combat.enabled = false;
            if (timeStop != null) timeStop.enabled = false;
            if (pastShadow != null) pastShadow.enabled = false;

            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);

            // --- FIX: Force running off and grounded ON so the player stands still naturally ---
            if (anim != null) {
                anim.SetBool("isRunning", false);
                anim.SetBool("isGrounded", true);
            }
        }

        // Wait until the Dialogue Panel is closed
        yield return new WaitUntil(() => DialogueManager.Instance.dialoguePanel.activeSelf == false);

        // --- FIX: Use GameSceneManager to fade the scene out smoothly ---
        if (transitionAfterDialogue && !string.IsNullOrEmpty(nextSceneName)) {
            yield return new WaitForSeconds(waitBeforeTransition);

            if (GameManager.Instance != null) {
                GameManager.Instance.hasCheckpoint = false;
            }

            if (GameSceneManager.Instance != null) {
                GameSceneManager.Instance.FadeScene(nextSceneName);
            } else {
                // Fallback just in case GameSceneManager isn't in the scene
                SceneManager.LoadScene(nextSceneName);
            }
        } else {
            // Only unlock the player if we AREN'T changing scenes
            if (controller != null) controller.canMove = true;
            if (combat != null) combat.enabled = true;
            if (timeStop != null) timeStop.enabled = true;
            if (pastShadow != null) pastShadow.enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            isPlayerInRange = true;
            playerObj = other.gameObject;

            if (triggerOnce && hasTriggered) return;

            if (autoTrigger) {
                if (DialogueManager.Instance.dialoguePanel.activeSelf == false) {
                    TriggerDialogue();
                }
            } else {
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