using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SaveManager;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Global Game Data")]
    public int totalEnemiesKilled = 0;

    [Header("Checkpoint Data")]
    public bool hasCheckpoint = false;
    public Vector2 lastCheckpointPos;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable() {
        GlobalEvents.OnEnemyKilled += HandleEnemyKilled;
        GlobalEvents.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable() {
        GlobalEvents.OnEnemyKilled -= HandleEnemyKilled;
        GlobalEvents.OnPlayerDied -= HandlePlayerDeath;
    }

    private void HandleEnemyKilled() {
        totalEnemiesKilled++;
    }

    private void HandlePlayerDeath() {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine() {
        // Wait 2 seconds so the player can see their stasis float
        yield return new WaitForSeconds(2f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            HealthSystem playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null) {
                // Trigger the rewind! (The HealthSystem will handle its own animation lock now)
                playerHealth.RespawnPlayer(lastCheckpointPos);
                Debug.Log("Time Rewound. Player Respawned seamlessly!");
            }
        }
    }
}