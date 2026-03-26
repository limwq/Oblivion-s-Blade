using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SaveManager : MonoBehaviour {
    public static SaveManager Instance { get; private set; }

    // --- NEW: ENEMY DATA STRUCTURE ---
    [System.Serializable]
    public class EnemySaveData {
        public string enemyID;
        public int currentHealth;
        public bool isDead;
    }

    // --- THE MAIN SAVE DATA ---
    [System.Serializable]
    public class SaveData {
        public string lastSceneName;
        public float playerPosX;
        public float playerPosY;
        public int totalKills; // Your Karma / Kill Count!

        // A list of every enemy the player has interacted with
        public List<EnemySaveData> savedEnemies = new List<EnemySaveData>();
    }

    public SaveData currentSaveData = new SaveData();
    private string saveFilePath;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        saveFilePath = Application.persistentDataPath + "/echoes_save.json";

        LoadSaveData(); // Auto-load data when the game boots up
    }

    // --- CHECKPOINT SAVING ---
    public void SaveCheckpoint(string sceneName, Vector2 playerPosition) {
        currentSaveData.lastSceneName = sceneName;
        currentSaveData.playerPosX = playerPosition.x;
        currentSaveData.playerPosY = playerPosition.y;

        // We DO NOT save player health here, because you want them to heal at checkpoints!
        WriteToFile();
        Debug.Log($"[SaveManager] Checkpoint Reached! Game Saved.");
    }

    public void AddKill() {
        currentSaveData.totalKills++;
        WriteToFile(); // Save immediately so they can't "undo" a kill by quitting
    }

    // --- NEW: ENEMY STATE MANAGEMENT ---
    public void UpdateEnemyState(string id, int health, bool dead) {
        // 1. Look to see if we already have this enemy in our save file
        EnemySaveData enemyData = currentSaveData.savedEnemies.Find(e => e.enemyID == id);

        if (enemyData != null) {
            // 2. Update existing enemy
            enemyData.currentHealth = health;
            enemyData.isDead = dead;
        } else {
            // 3. Add brand new enemy to the save file
            currentSaveData.savedEnemies.Add(new EnemySaveData {
                enemyID = id,
                currentHealth = health,
                isDead = dead
            });
        }

        WriteToFile();
    }

    public EnemySaveData GetEnemyState(string id) {
        return currentSaveData.savedEnemies.Find(e => e.enemyID == id);
    }

    // --- SAVED DATA AMNESIA: For reviving bosses so the player can replay the finale ---
    public void ReviveEnemyInSaveData(string id) {
        // 1. Find the specific boss in your custom save data list
        EnemySaveData enemyData = currentSaveData.savedEnemies.Find(e => e.enemyID == id);

        if (enemyData != null) {

            // 2. If this enemy was counted as a kill, subtract it from the total!
            // (We ensure totalKills doesn't accidentally drop below 0 just to be safe)
            if (enemyData.isDead && currentSaveData.totalKills > 0) {
                currentSaveData.totalKills--;
            }

            // 3. Remove it from the list completely
            currentSaveData.savedEnemies.Remove(enemyData);
            Debug.Log($"[SaveManager] Erased {id} from the save file and refunded 1 Karma/Kill.");

            // 4. Rewrite the JSON file immediately so the amnesia is permanent!
            WriteToFile();
        }
    }

    // --- FILE I/O ---
    private void WriteToFile() {
        string jsonText = JsonUtility.ToJson(currentSaveData, true);
        File.WriteAllText(saveFilePath, jsonText);
    }

    public bool HasSaveData() { return File.Exists(saveFilePath); }

    public void LoadSaveData() {
        if (HasSaveData()) {
            try {
                string jsonText = File.ReadAllText(saveFilePath);
                SaveData loadedData = JsonUtility.FromJson<SaveData>(jsonText);

                // Only overwrite if we successfully pulled data!
                if (loadedData != null) {
                    currentSaveData = loadedData;
                    Debug.Log("[SaveManager] Save Data successfully loaded from hard drive.");
                } else {
                    Debug.LogWarning("[SaveManager] File exists, but no data was found inside it.");
                }
            } catch (System.Exception e) {
                Debug.LogError($"[SaveManager] Failed to read JSON file! Error: {e.Message}");
            }
        }
    }

    public void DeleteSaveData() {
        if (HasSaveData()) File.Delete(saveFilePath);
        currentSaveData = new SaveData();
    }
}