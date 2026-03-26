using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using System.Collections.Generic; // --- NEW: Required for Lists ---

public class PlayerSpawner : MonoBehaviour {
    [Header("Settings")]
    public GameObject playerPrefab;

    [Header("Story Progression")]
    [Tooltip("Type the exact names of scenes where the player starts with NO abilities.")]
    public List<string> scenesWithoutAbilities = new List<string> { "Level 1-1", "Level 1-2" };

    [Header("Camera Connection")]
    public CinemachineVirtualCamera virtualCamera;

    void Start() {
        if (playerPrefab != null) {
            Vector2 spawnPos = transform.position;
            string currentScene = SceneManager.GetActiveScene().name;

            // --- 1. PRIORITY CHECK: Is there a Hard Drive save for this level? ---
            if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData.lastSceneName == currentScene) {
                spawnPos = new Vector2(
                    SaveManager.Instance.currentSaveData.playerPosX,
                    SaveManager.Instance.currentSaveData.playerPosY
                );
                Debug.Log("[PlayerSpawner] Spawning at Hard Drive Checkpoint!");
            }
            // --- 2. FALLBACK CHECK: Is there a Session save? ---
            else if (GameManager.Instance != null && GameManager.Instance.hasCheckpoint) {
                spawnPos = GameManager.Instance.lastCheckpointPos;
                Debug.Log("[PlayerSpawner] Spawning at Session Checkpoint!");
            }
            // --- 3. NEW LEVEL: Spawn at spawner position and create Checkpoint Zero ---
            else {
                Debug.Log("[PlayerSpawner] Spawning at Level Start.");

                if (SaveManager.Instance != null) {
                    SaveManager.Instance.SaveCheckpoint(currentScene, spawnPos);
                    Debug.Log("[PlayerSpawner] Initial level save (Checkpoint Zero) created!");

                    // --- AUDIO TRIGGER ---
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("CheckpointReached");
                }
            }

            // Sync the GameManager so the session state matches the hard drive state
            if (GameManager.Instance != null) {
                GameManager.Instance.lastCheckpointPos = spawnPos;
                GameManager.Instance.hasCheckpoint = true;
            }

            // --- INSTANTIATE & SETUP ---
            GameObject newPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

            // --- NEW: ABILITY LOCK LOGIC ---
            NinjaController2D controller = newPlayer.GetComponent<NinjaController2D>();
            if (controller != null) {
                // Check if the current scene is in our restricted list
                if (scenesWithoutAbilities.Contains(currentScene)) {
                    controller.abilitiesLocked = true;
                    Debug.Log($"[PlayerSpawner] Story Lock Active! Abilities disabled for {currentScene}.");
                } else {
                    controller.abilitiesLocked = false;
                    Debug.Log($"[PlayerSpawner] Player has their powers in {currentScene}.");
                }
            }
            // --------------------------------

            // Play the exact Born sequence on Level Load!
            HealthSystem playerHealth = newPlayer.GetComponent<HealthSystem>();
            if (playerHealth != null) {
                playerHealth.TriggerBornSequence();
            }

            // Connect the Camera
            if (virtualCamera != null) {
                virtualCamera.Follow = newPlayer.transform;
            } else {
                var cam = FindFirstObjectByType<CinemachineVirtualCamera>();
                if (cam != null) cam.Follow = newPlayer.transform;
            }
        } else {
            Debug.LogError("PlayerSpawner: No Player Prefab assigned!");
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}