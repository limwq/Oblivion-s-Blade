using UnityEngine;
using UnityEngine.UI;

public class NoteSystem : MonoBehaviour {
    public static NoteSystem Instance;

    [Header("UI References")]
    public GameObject notePanel;
    public Image noteImageDisplay;
    public GameObject blurBackground;

    [Header("Settings")]
    public bool pauseGameOnInspect = true;

    // --- FIX 1: Make this public so NotePickup can check the real status! ---
    [HideInInspector] public bool isNoteOpen = false;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return; // Added return to prevent double-execution
        }

        // Just hide the panel safely on start
        if (notePanel != null) notePanel.SetActive(false);
        isNoteOpen = false;
    }

    void Update() {
        if (isNoteOpen) {
            // --- FIX 2: Removed KeyCode.F to prevent the flickering bug! ---
            // NotePickup handles 'F' now. This only handles alternate close buttons.
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) {
                CloseNote();
            }
        }
    }

    public void ShowNote(Sprite noteContent) {
        isNoteOpen = true;

        if (noteImageDisplay != null) {
            noteImageDisplay.sprite = noteContent;
            noteImageDisplay.preserveAspect = true;
        }

        if (notePanel != null) notePanel.SetActive(true);

        if (pauseGameOnInspect) {
            Time.timeScale = 0f;
            AudioListener.pause = true; // --- PAUSE FIX: Freeze world audio while reading! ---
        }
    }

    public void CloseNote() {
        isNoteOpen = false;

        if (notePanel != null) notePanel.SetActive(false);

        if (pauseGameOnInspect) {
            Time.timeScale = 1f;
            AudioListener.pause = false; // --- PAUSE FIX: Unfreeze world audio! ---
        }
    }
}