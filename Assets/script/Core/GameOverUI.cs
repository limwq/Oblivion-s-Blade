using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour {
    [Header("UI Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Scene Settings")]
    [Tooltip("The exact name of your Main Menu scene")]
    public string mainMenuSceneName = "MainMenu";
    [Tooltip("Fallback scene just in case the save file is missing")]
    public string fallbackSceneName = "Level 1-1";

    private void Start() {
        // 1. Setup Restart Button
        if (restartButton != null) {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // 2. Setup Main Menu Button
        if (mainMenuButton != null) {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        // 3. Ensure the cursor is visible and unlocked so the player can click!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnRestartClicked() {
        // Reset time scale just in case the player died during a slow-mo effect!
        Time.timeScale = 1f;

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX("UIClick");
        }

        string sceneToLoad = fallbackSceneName;

        // Read the hard drive to find exactly where they died
        if (SaveManager.Instance != null) {
            SaveManager.Instance.LoadSaveData();

            if (SaveManager.Instance.currentSaveData != null &&
                !string.IsNullOrEmpty(SaveManager.Instance.currentSaveData.lastSceneName)) {

                sceneToLoad = SaveManager.Instance.currentSaveData.lastSceneName;
            } else {
                Debug.LogWarning("[GameOver] Save data is empty! Using fallback scene.");
            }
        }

        Debug.Log($"[GameOver] Respawning player in: {sceneToLoad}");

        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.LoadScene(sceneToLoad);
        } else {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void OnMainMenuClicked() {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX("UIClick");
        }

        Debug.Log("[GameOver] Returning to Main Menu.");

        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.LoadScene(mainMenuSceneName);
        } else {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}