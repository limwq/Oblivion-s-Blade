using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel; // The overlay panel
    [SerializeField] private Button resumeButton;   // Button inside the Pause Menu
    [SerializeField] private Button menuButton;     // Button inside the Pause Menu
    [SerializeField] private Button restartButton;  // Button inside the Pause Menu

    [Header("HUD References")]
    [SerializeField] private Button hudPauseButton; // NEW: The button on the game screen

    private bool isPaused = false;

    private void Awake() {
        var systems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);

        if (systems.Length > 1) {
            var myEventSystem = GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();

            // If this prefab has its own EventSystem, but another one exists globally,
            // kill the local one to prevent conflict.
            if (myEventSystem != null) {
                Destroy(myEventSystem.gameObject);
            }
        }
    }

    private void Start() {
        // --- Setup Pause Menu Buttons ---
        if (resumeButton != null) {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (menuButton != null) {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoToMenu);
        }

        if (restartButton != null) {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        // --- Setup HUD Button (New) ---
        if (hudPauseButton != null) {
            hudPauseButton.onClick.RemoveAllListeners();
            // Clicking this does the same thing as pressing Escape
            hudPauseButton.onClick.AddListener(PauseGame);
        }

        // Ensure the menu is hidden when the level starts
        if (pausePanel != null) pausePanel.SetActive(false);

        // Ensure the HUD button is visible when playing
        if (hudPauseButton != null) hudPauseButton.gameObject.SetActive(true);
    }

    private void Update() {
        // Toggle pause with the Escape key
        if (Input.GetKeyDown(KeyCode.Escape) || (Input.GetKeyDown(KeyCode.P))) {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame() {
        isPaused = true;
        Time.timeScale = 0f; // Freeze time

        // Show the Pause Menu
        if (pausePanel != null) pausePanel.SetActive(true);

        // Hide the HUD button (optional, cleaner look)
        if (hudPauseButton != null) hudPauseButton.gameObject.SetActive(false);

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX("UIClick");
            AudioListener.pause = true;
        }
    }

    public void ResumeGame() {
        isPaused = false;
        Time.timeScale = 1f; // Resume time

        // Hide the Pause Menu
        if (pausePanel != null) pausePanel.SetActive(false);

        // Show the HUD button again
        if (hudPauseButton != null) hudPauseButton.gameObject.SetActive(true);

        if (AudioManager.Instance != null) {
            AudioListener.pause = false;
            AudioManager.Instance.PlaySFX("UIClick");
        }
    }

    private void GoToMenu() {
        Time.timeScale = 1f; // Always unfreeze before leaving

        if (AudioManager.Instance != null) {
            AudioListener.pause = false;
            AudioManager.Instance.PlaySFX("UIClick");
        }

        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.FadeScene("MainMenu");
    }

    private void RestartLevel() {
        Time.timeScale = 1f; // Unfreeze

        if (AudioManager.Instance != null) {
            AudioListener.pause = false;
            AudioManager.Instance.PlaySFX("UIClick"); 
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadScene(currentSceneName);
    }
}