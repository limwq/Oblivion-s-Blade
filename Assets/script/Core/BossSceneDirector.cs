using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // --- NEW: Required to check the active scene name ---

public class BossSceneDirector : MonoBehaviour {
    [Header("Scene Routing")]
    public string gameOverSceneName = "GameOver";
    [Tooltip("The scene to load when the registered boss dies (e.g., 'Ending' or 'Level 2').")]
    public string victorySceneName = "Ending";

    [Header("Encounter Settings")]
    [Tooltip("The exact name of the scene where Phase 1 deaths trigger a Game Over.")]
    public string deadlyPhase1Scene = "Level 3-2";

    [Header("Cinematic Delays")]
    public float gameOverDelay = 2.0f;
    public float victoryDelay = 4.0f;

    private HealthSystem playerHealth;
    private HealthSystem orbBossHealth;

    private bool sequenceTriggered = false;
    private bool playerIsSynced = false;


    private void SyncPlayerOnSpawn() {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            playerHealth = playerObj.GetComponent<HealthSystem>();
            if (playerHealth != null) {
                playerIsSynced = true;
            }
        }
    }

    public void RegisterPhase2Boss(HealthSystem bossHealth) {
        orbBossHealth = bossHealth;
        Debug.Log("[Boss Director] Main Target Boss successfully registered for tracking!");
    }

    private void Update() {
        if (sequenceTriggered) return;
        if (!playerIsSynced) SyncPlayerOnSpawn();

        // Condition 1: Player dies
        if (playerHealth != null && playerHealth.IsDead()) {

            // Check if we are in Phase 1 (the Phase 2 boss hasn't been registered yet)
            bool isPhase1 = (orbBossHealth == null);
            string currentScene = SceneManager.GetActiveScene().name;

            // ONLY trigger Game Over if they are in Phase 1 AND in the deadly Level 3-2 scene
            if (isPhase1 && currentScene == deadlyPhase1Scene) {
                StartCoroutine(GameOverSequence());
            }
        }

        // Condition 2: The Registered Boss dies (Victory!)
        if (orbBossHealth != null && orbBossHealth.IsDead()) {
            StartCoroutine(VictorySequence());
        }
    }

    private IEnumerator GameOverSequence() {
        sequenceTriggered = true;
        Debug.Log("[Boss Director] Player defeated in Phase 1. Initiating Game Over sequence...");

        Time.timeScale = 0.4f;
        yield return new WaitForSecondsRealtime(gameOverDelay);
        Time.timeScale = 1f;

        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(gameOverSceneName);
        } else {
            SceneManager.LoadScene(gameOverSceneName);
        }
    }

    private IEnumerator VictorySequence() {
        sequenceTriggered = true;
        Debug.Log("[Boss Director] Boss defeated! Initiating Victory sequence...");

        yield return new WaitForSeconds(victoryDelay);

        if (GameManager.Instance != null) {
            GameManager.Instance.hasCheckpoint = false;
        }

        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(victorySceneName);
        } else {
            SceneManager.LoadScene(victorySceneName);
        }
    }
}