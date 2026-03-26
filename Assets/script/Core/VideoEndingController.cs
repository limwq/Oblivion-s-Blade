using UnityEngine;
using UnityEngine.Video; // Required for VideoPlayer
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VideoEndingController : MonoBehaviour {
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer endingVideo; // Drag your Video Player here
    [SerializeField] private bool autoPlay = true;

    [Header("UI References")]
    [SerializeField] private Button skipButton; // Optional: A button to skip the video

    [Header("Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // --- NEW: Toggle to wipe the save file ---
    [Tooltip("If true, the player's save file will be deleted when the video ends.")]
    [SerializeField] private bool wipeSaveData = true;

    private bool hasTriggeredNextScene = false;

    private void Awake() {
        // Unlock cursor so player can click Skip if they want
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;
    }

    private void Start() {
        // 1. Setup Video
        if (endingVideo != null) {
            // Important: Video must NOT loop, otherwise the 'End' event never fires
            endingVideo.isLooping = false;

            // Subscribe to the event that fires when video finishes
            endingVideo.loopPointReached += OnVideoFinished;

            if (autoPlay) {
                endingVideo.Play();
            }
        } else {
            Debug.LogWarning("VideoEndingController: No Video Player assigned! Loading menu immediately.");
            GoToMenu();
        }

        // 2. Setup Skip Button
        if (skipButton != null) {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(GoToMenu);
        }
    }

    // Event called automatically by Unity when video ends
    private void OnVideoFinished(VideoPlayer vp) {
        GoToMenu();
    }

    public void GoToMenu() {
        // Prevent double-loading (e.g. if video ends exactly when player clicks skip)
        if (hasTriggeredNextScene) return;
        hasTriggeredNextScene = true;

        // Play Click Sound (Optional, only if clicking button)
        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX("UIClick");
        }

        // --- NEW: WIPE SAVE DATA LOGIC ---
        if (wipeSaveData) {
            if (SaveManager.Instance != null) {
                // IMPORTANT: Change "DeleteSave" to whatever your actual delete method is called 
                // inside your SaveManager script (e.g., ClearData(), ResetSave(), DeleteAll()).
                SaveManager.Instance.DeleteSaveData();
                Debug.Log("Save data wiped after ending!");
            }

            // Also wipe the temporary GameManager session state just to be completely clean
            if (GameManager.Instance != null) {
                GameManager.Instance.hasCheckpoint = false;
                GameManager.Instance.totalEnemiesKilled = 0;
            }
        }
        // ---------------------------------

        Debug.Log("Video finished/Skipped. Loading Main Menu...");

        // Load Main Menu
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(mainMenuSceneName);
        } else {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // Good practice: Unsubscribe from event if object is destroyed
    private void OnDestroy() {
        if (endingVideo != null) {
            endingVideo.loopPointReached -= OnVideoFinished;
        }
    }
}