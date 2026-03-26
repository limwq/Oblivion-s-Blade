using UnityEngine;

public class Bootstrapper : MonoBehaviour {
    [Header("Core State Managers")]
    public GameObject gameManagerPrefab;
    public GameObject saveManagerPrefab; // NEW: For our checkpoint system

    [Header("Audio & Scene Managers")]
    public GameObject audioManagerPrefab;
    public GameObject sceneManagerPrefab;

    [Header("Narrative Systems")]
    public GameObject noteSystemPrefab;
    public GameObject subtitleSystemPrefab;
    public GameObject dialogueSystemPrefab;

    void Awake() {
        // --- 1. CORE STATE ---
        if (GameManager.Instance == null && gameManagerPrefab != null) {
            Instantiate(gameManagerPrefab);
        }

        if (SaveManager.Instance == null && saveManagerPrefab != null) {
            Instantiate(saveManagerPrefab);
        }

        // --- 2. AUDIO & SCENES ---
        if (AudioManager.Instance == null && audioManagerPrefab != null) {
            Instantiate(audioManagerPrefab);
        }

        if (GameSceneManager.Instance == null && sceneManagerPrefab != null) {
            Instantiate(sceneManagerPrefab);
        }

        // --- 3. NARRATIVE & UI ---
        if (NoteSystem.Instance == null && noteSystemPrefab != null) {
            Instantiate(noteSystemPrefab);
        }

        if (SubtitleSystem.Instance == null && subtitleSystemPrefab != null) {
            Instantiate(subtitleSystemPrefab);
        }

        if (DialogueManager.Instance == null && dialogueSystemPrefab != null) {
            Instantiate(dialogueSystemPrefab);
        }

        Debug.Log("[Bootstrapper] All systems successfully booted.");
    }

    void Start() {
        // After booting, automatically go to the Main Menu
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene("MainMenu");
        }
    }
}