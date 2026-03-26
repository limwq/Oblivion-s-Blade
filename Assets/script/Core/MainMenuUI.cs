using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    [Header("Buttons")]
    [SerializeField] private Button continueButton; // --- NEW ---
    [SerializeField] private Button playButton;     // (This will now act as 'New Game')
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("External Systems")]
    [SerializeField] private SettingsMenu settingsMenu;

    private void Start() {
        // --- NEW: Setup Continue Button ---
        if (continueButton != null) {
            // Check if the button actually has a graphic assigned to it
            var buttonImage = continueButton.targetGraphic;

            // 1. A Save File Exists (Active & White)
            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData()) {

                // Allow input
                continueButton.interactable = true;

                // Set visual color to white (Active)
                if (buttonImage != null) {
                    buttonImage.color = Color.white;
                }

                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            // 2. No Save File Exists (Disabled & Grey)
            else {

                // Block input
                continueButton.interactable = false;

                // Set visual color to grey (Disabled)
                if (buttonImage != null) {
                    // This tints the entire background sprite grey!
                    buttonImage.color = Color.grey;
                }
            }
        }

        // 1. Setup Play (New Game) Button
        if (playButton != null) {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        // 2. Setup Options Button
        if (optionsButton != null) {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnOptionsClicked);
        }

        // 3. Setup Quit Button
        if (quitButton != null) {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    // --- NEW: Continue Game Logic ---
    private void OnContinueClicked() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UIClick");

        Debug.Log("[MainMenu] Continue button clicked! Attempting to read save...");

        if (SaveManager.Instance != null && GameSceneManager.Instance != null) {

            // 1. Force a reload of the data
            SaveManager.Instance.LoadSaveData();

            // 2. CRITICAL FIX: Ensure the save data didn't corrupt and return null!
            if (SaveManager.Instance.currentSaveData == null) {
                Debug.LogError("[CRITICAL] Save Data is completely null! The JSON file might be corrupted.");
                return; // Stop here before it crashes the game!
            }

            // 3. Read the scene string
            string savedScene = SaveManager.Instance.currentSaveData.lastSceneName;
            Debug.Log($"[MainMenu] Save file says the last scene was: '{savedScene}'");

            // 4. Clean up any weird blank spaces or empty strings
            if (string.IsNullOrWhiteSpace(savedScene)) {
                Debug.LogWarning("[MainMenu] The saved scene name was completely blank! Defaulting to 'Start'.");
                savedScene = "Start";
            }

            // 5. Finally, load the scene
            Debug.Log($"[MainMenu] Handing '{savedScene}' to the GameSceneManager...");
            GameSceneManager.Instance.LoadScene(savedScene);

        } else {
            Debug.LogError("[MainMenu] ERROR: SaveManager or GameSceneManager is missing from the scene!");
        }
    }

    private void OnPlayClicked() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UIClick");

        // --- NEW: Wipe old data for a fresh run ---
        if (SaveManager.Instance != null) {
            SaveManager.Instance.DeleteSaveData();
        }

        if (GameSceneManager.Instance != null) GameSceneManager.Instance.LoadScene("Level 1-1");
    }

    private void OnOptionsClicked() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UIClick");

        if (settingsMenu != null) {
            settingsMenu.OpenOptions();
        } else {
            Debug.LogWarning("SettingsMenu reference is missing in MainMenuUI!");
        }
    }

    private void OnQuitClicked() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UIClick");
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}