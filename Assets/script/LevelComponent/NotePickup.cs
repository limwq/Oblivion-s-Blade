using UnityEngine;

public class NotePickup : MonoBehaviour {
    [Header("Content")]
    public Sprite noteSprite;
    public KeyCode interactKey = KeyCode.F;

    [Header("Optional Visuals")]
    public GameObject interactPrompt;

    [Header("Audio")]
    public string pickupSound = "PaperRustle";

    private bool isPlayerInRange;

    void Start() {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Update() {
        if (isPlayerInRange) {
            if (Input.GetKeyDown(interactKey)) {

                // --- THE SYNC FIX: Ask the NoteSystem exactly what state it is in ---
                if (NoteSystem.Instance != null) {
                    if (!NoteSystem.Instance.isNoteOpen) {
                        OpenNote();
                    } else {
                        CloseNote();
                    }
                }

            }
        }
    }

    void OpenNote() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(pickupSound);

        if (NoteSystem.Instance != null) {
            NoteSystem.Instance.ShowNote(noteSprite);
        }

        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void CloseNote() {
        if (NoteSystem.Instance != null) {
            NoteSystem.Instance.CloseNote();
        }

        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            isPlayerInRange = true;

            // Only show prompt if a note isn't already taking up the screen
            if (NoteSystem.Instance != null && !NoteSystem.Instance.isNoteOpen) {
                if (interactPrompt != null) interactPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            isPlayerInRange = false;

            // Auto-close if they walk away
            if (NoteSystem.Instance != null && NoteSystem.Instance.isNoteOpen) {
                CloseNote();
            }

            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }
}