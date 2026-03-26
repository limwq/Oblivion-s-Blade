using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Cinemachine;
using UnityEngine.UIElements;

public class PhaseTransitionManager : MonoBehaviour {
    [Header("Phase 1 References")]
    public HealthSystem phase1BossHealth;
    public GameObject phase1BossObject;
    public GameObject phase1Environment;

    [Header("Phase 2 References")]
    public GameObject phase2Environment;
    public GameObject phase2BossPrefab;
    public Transform phase2SpawnPoint;

    [Header("Cinemachine Cameras")]
    public CinemachineVirtualCamera vcamPhase1;
    public CinemachineVirtualCamera vcamPhase2;
    // --- NEW: A dedicated Focus Cam variable ---
    public CinemachineVirtualCamera focusCamPhase1;

    [Header("UI & VFX")]
    public CanvasGroup whiteFadeCanvas;
    public GameObject massiveExplosionPrefab;
    public float whiteFadeInSpeed = 0.2f;    // Very fast flash in
    public float whiteFadeOutSpeed = 1.5f;   // Slower reveal
    public float deathSlowMoDuration = 2.0f; // How long the boss stays in slow-mo

    private bool isTransitioning = false;
    private bool playerIsSynced = false;

    private void Start() {
        // --- THE FIX: Disable Focus Cam instead of destroying the main vcam ---
        if (phase1BossObject == null) {
            if (focusCamPhase1 != null) focusCamPhase1.gameObject.SetActive(false);
            SpawnPhase2Immediately();
            return;
        }

        if (SaveManager.Instance != null) {

            // 1. Calculate the exact same Auto-ID that the HealthSystem uses!
            int startX = Mathf.RoundToInt(phase1BossObject.transform.position.x);
            int startY = Mathf.RoundToInt(phase1BossObject.transform.position.y);
            string phase1ID = $"{phase1BossObject.scene.name}_{phase1BossObject.name}_{startX}_{startY}";

            // 2. Ask the SaveManager if this boss is already permanently dead
            SaveManager.EnemySaveData savedState = SaveManager.Instance.GetEnemyState(phase1ID);

            if (savedState != null && savedState.isDead) {
                Debug.Log("[PhaseTransition] Phase 1 Boss is already defeated! Skipping to Phase 2.");

                // 3. Destroy the Phase 1 Boss so it doesn't accidentally run its own Start() logic
                Destroy(phase1BossObject);

                // --- NEW: Disable the Phase 1 Focus Cam ---
                if (focusCamPhase1 != null) focusCamPhase1.gameObject.SetActive(false);

                // 4. Immediately spawn the Phase 2 Boss and skip the waiting!
                SpawnPhase2Immediately();

                return; // Stop the rest of the Start() method so the normal transition doesn't run
            }
        }
    }

    private void SpawnPhase2Immediately() {
        if (phase2BossPrefab != null && phase2SpawnPoint != null) {
            StartCoroutine(TransitionRoutine());
        }
    }

    void Update() {
        if (!playerIsSynced) SyncPlayerOnSpawn();

        if (playerIsSynced && !isTransitioning && phase1BossHealth != null && phase1BossHealth.IsDead()) {
            StartCoroutine(TransitionRoutine());
        }
    }

    private void SyncPlayerOnSpawn() {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            NinjaController2D controller = playerObj.GetComponent<NinjaController2D>();
            if (controller != null) {
                controller.abilitiesLocked = true;
                if (vcamPhase1 != null) { vcamPhase1.Follow = playerObj.transform; }
                playerIsSynced = true;

                BossHealthUI bossUI = FindFirstObjectByType<BossHealthUI>();
                if (bossUI != null) {
                    bossUI.ActivateBossUI(phase1BossHealth, "SiFu?", "My student... kill me... KILL YOU... KILL HIM! GGGHHRAAA!!");
                }
            }
        }
    }

    private IEnumerator TransitionRoutine() {
        isTransitioning = true;

        // 1. COMMENCE SLOW-MO DEATH
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        NinjaController2D controller = playerObj.GetComponent<NinjaController2D>();
        if (controller != null) controller.canMove = false;

        Time.timeScale = 0.5f;
        Debug.Log("[Phase Manager] Boss dying... Slow-mo active.");

        // 2. WAIT FOR SLOW-MO TO FINISH
        yield return new WaitForSecondsRealtime(deathSlowMoDuration);

        // 3. RESTORE TIME & TRIGGER FLASH + EXPLOSION
        Time.timeScale = 1.0f;
        Debug.Log("[Phase Manager] Time restored. Triggering Flashbang!");

        StartCoroutine(FadeWhite(1f, whiteFadeInSpeed));

        if (massiveExplosionPrefab != null && phase1BossObject != null) {
            Destroy(Instantiate(massiveExplosionPrefab, phase1BossObject.transform.position, Quaternion.identity), 2f);
        }

        if (CameraSpring.Instance != null) {
            CameraSpring.Instance.Punch(Vector2.down, 15f);
        }

        // 4. WAIT A FRACTION FOR THE WHITE TO FULLY COVER THE SCREEN
        yield return new WaitForSeconds(whiteFadeInSpeed);

        // --- EVERYTHING BELOW IS HIDDEN BY FULL WHITE ---

        // 5. SWAP ENVIRONMENT & CAMERAS
        if (phase1Environment != null) phase1Environment.SetActive(false);
        if (phase2Environment != null) phase2Environment.SetActive(true);
        if (phase1BossObject != null) Destroy(phase1BossObject);

        if (vcamPhase1 != null) vcamPhase1.Priority = 0;
        if (vcamPhase2 != null) { vcamPhase2.Follow = playerObj.transform; }
        if (vcamPhase2 != null) vcamPhase2.Priority = 10;

        // Also ensure the focus cam is turned off during the transition just in case
        if (focusCamPhase1 != null) focusCamPhase1.gameObject.SetActive(false);

        // 6. SPAWN PHASE 2 BOSS
        GameObject newlySpawnedBoss = null;
        if (phase2BossPrefab != null && phase2SpawnPoint != null) {
            newlySpawnedBoss = Instantiate(phase2BossPrefab, phase2SpawnPoint.position, Quaternion.identity);

            // --- AUDIO TRIGGER ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("OrbBoss_Spawn");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("3-2 2");
        }

        BossSceneDirector director = FindFirstObjectByType<BossSceneDirector>();
        if (director != null && newlySpawnedBoss != null) {
            HealthSystem bossHealth = newlySpawnedBoss.GetComponent<HealthSystem>();
            if (bossHealth != null) {
                director.RegisterPhase2Boss(bossHealth);

                BossHealthUI bossUI = FindFirstObjectByType<BossHealthUI>();
                if (bossUI != null) {
                    bossUI.ActivateBossUI(bossHealth, "The Orb", "I CREATE. I DESTROY. I CONTROL, I GIVE , I TAKE BACK.");
                }
            }
        }

        // Small buffer to let Cinemachine finish the snap while the screen is white
        yield return new WaitForSeconds(0.2f);

        // 7. FADE OUT TO REVEAL PHASE 2
        yield return StartCoroutine(FadeWhite(0f, whiteFadeOutSpeed));

        // 8. UNLOCK ABILITIES & GRANT "ASCENDED" POWERS
        if (controller != null) {
            controller.canMove = true;
            controller.abilitiesLocked = false;

            Debug.Log("[Phase Manager] Player has Ascended! Infinite Stamina & Extended Time Stop unlocked!");

            controller.infiniteStamina = true;

            Ability_PastShadow ability1 = playerObj.GetComponent<Ability_PastShadow>();
            if (ability1 != null) {
                ability1.shadowDuration = 15f;
            }
            Ability_TimeStop ability2 = playerObj.GetComponent<Ability_TimeStop>();
            if (ability2 != null) {
                ability2.timeStopDuration = 15f;
            }

            HealthSystem playerHealth = controller.GetComponent<HealthSystem>();
            if (playerHealth != null) {
                playerHealth.currentHealth = playerHealth.maxHealth;
            }
        }
    }

    private IEnumerator FadeWhite(float targetAlpha, float duration) {
        if (whiteFadeCanvas == null) yield break;

        float startAlpha = whiteFadeCanvas.alpha;
        float timer = 0f;

        while (timer < duration) {
            timer += Time.unscaledDeltaTime;
            whiteFadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        whiteFadeCanvas.alpha = targetAlpha;
    }
}