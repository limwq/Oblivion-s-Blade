using UnityEngine;
using System.Collections;

public class BossController1 : MonoBehaviour {
    public enum AttackPattern { Single, Fan, Circle }

    [Header("Components")]
    private HealthSystem health;
    private Animator anim;
    private CameraShake camShake;

    // --- NEW: Save Data Reset Variables ---
    [Header("Save Data Reset (End Game)")]
    [Tooltip("The unique save ID of Phase 1")]
    public string phase1SaveID = "Level1_Boss_1";
    [Tooltip("The unique save ID of this Phase 2 boss")]
    public string phase2SaveID = "Level1_Boss_2";
    // --------------------------------------

    [Header("Settings")]
    public string playerTag = "Player";
    private Transform playerTransform;
    [Range(0f, 1f)] public float enrageThreshold = 0.3f; 

    public bool isEnraged = false;
    private bool isDead = false;

    [Header("Movement (Phase 1)")]
    public float minX = -5f; public float maxX = 5f;
    public float minY = 2f; public float maxY = 5f;
    public float groundY = -2f;
    public float moveSpeed = 5f;
    public float landingCooldown = 2.5f; 
    public float deathFlySpeed = 8f;

    [Header("Attack Settings")]
    public float attackDurationLimit = 15f; 
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1.0f; 
    public float bulletSpeed = 10f;

    [Header("Looping Audio")]
    public AudioSource engineAudioSource;

    private Coroutine currentCycle;

    void Start() {
        health = GetComponent<HealthSystem>();
        anim = GetComponent<Animator>();

        BossSceneDirector director = FindFirstObjectByType<BossSceneDirector>();
        if (director != null) {
            if (health != null) {
                health.currentHealth = health.maxHealth; 
                director.RegisterPhase2Boss(health);

                BossHealthUI bossUI = FindFirstObjectByType<BossHealthUI>();
                if (bossUI != null) {
                    bossUI.ActivateBossUI(health, "The Jet Ship", "So many missiles... all for you! Now, BE THE FIREWORKS!");
                }
            }
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null) playerTransform = player.transform;

        if (Camera.main != null) camShake = Camera.main.GetComponent<CameraShake>();

        if (engineAudioSource != null && !engineAudioSource.isPlaying) {
            engineAudioSource.Play(); 
        }

        currentCycle = StartCoroutine(BossCycleRoutine());
    }

    void Update() {
        if (isDead || health == null) return;

        if (!isEnraged && health.currentHealth <= health.maxHealth * enrageThreshold) {
            TriggerEnrage();
        }

        if (health.IsDead() && !isDead) {
            isDead = true;
            StopAllCoroutines();

            // --- NEW: WIPE THE BOSS DEATHS FROM SAVE DATA ---
            Debug.Log("[Level 1 Complete] Resetting Boss Save Data for Replay!");
            if (SaveManager.Instance != null) {
                SaveManager.Instance.ReviveEnemyInSaveData(phase1SaveID);
                SaveManager.Instance.ReviveEnemyInSaveData(phase2SaveID);
            }
            // ------------------------------------------------

            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator BossCycleRoutine() {
        yield return new WaitForSeconds(1f);

        while (!isDead) {
            float timer = 0f;
            Coroutine moveTask = null; 

            while (timer < attackDurationLimit && !isDead) {
                if (moveTask != null) StopCoroutine(moveTask);

                Vector2 randomSpot = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                moveTask = StartCoroutine(MoveToPosition(randomSpot, moveSpeed));

                if (!isEnraged) {
                    ExecuteRandomPattern();
                } else {
                    ExecuteRandomPattern();
                    ExecuteRandomPattern();
                }

                float waitTime = isEnraged ? fireRate * 0.7f : fireRate;
                yield return new WaitForSeconds(waitTime);
                
                timer += waitTime;
            }

            if (moveTask != null) StopCoroutine(moveTask);

            Vector2 landingSpot = new Vector2(transform.position.x, groundY);
            yield return StartCoroutine(MoveToPosition(landingSpot, moveSpeed * 1.5f)); 

            yield return new WaitForSeconds(landingCooldown);
        }
    }

    // ==========================================
    // 修正版：使用 Rotation 進行轉向
    // ==========================================
    IEnumerator MoveToPosition(Vector2 targetPos, float speed) {
        // 因為你的 Sprite 預設看左：
        // 往右走 (target.x > current.x) -> 繞 Y 軸轉 180 度
        // 往左走 -> 轉回 0 度
        if (targetPos.x > transform.position.x) {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        } else {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        while (Vector2.Distance(transform.position, targetPos) > 0.1f) {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
    }

    private void TriggerEnrage() {
        isEnraged = true;
        if (currentCycle != null) StopCoroutine(currentCycle);

        if (anim != null) anim.SetBool("isEnraged", true);
        if (CameraSpring.Instance != null) CameraSpring.Instance.Punch(Vector2.down, 10f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BossEnrage");

        StartCoroutine(EnrageSpamSequence());
    }

    IEnumerator EnrageSpamSequence() {
        Vector2 centerPoint = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        yield return StartCoroutine(MoveToPosition(centerPoint, moveSpeed * 2f));

        for (int i = 0; i < 5; i++) {
            FireCirclePattern();
            yield return new WaitForSeconds(0.3f); 
        }

        currentCycle = StartCoroutine(BossCycleRoutine());
    }

    private void ExecuteRandomPattern() {
        AttackPattern randomPattern = (AttackPattern)Random.Range(0, 3);
        switch (randomPattern) {
            case AttackPattern.Single: FireSinglePattern(); break;
            case AttackPattern.Fan: FireFanPattern(); break;
            case AttackPattern.Circle: FireCirclePattern(); break;
        }
    }

    private void FireSinglePattern() {
        if (playerTransform == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BossShoot");
        Vector2 dir = (playerTransform.position - firePoint.position).normalized;
        SpawnBullet(dir);
    }

    private void FireFanPattern() {
        if (playerTransform == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BossShoot");
        Vector2 baseDir = (playerTransform.position - firePoint.position).normalized;
        float spreadAngle = 20f;
        SpawnBullet(Quaternion.Euler(0, 0, -spreadAngle) * baseDir);
        SpawnBullet(baseDir);
        SpawnBullet(Quaternion.Euler(0, 0, spreadAngle) * baseDir);
    }

    private void FireCirclePattern() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BossShoot");
        int bulletCount = 8;
        float angleStep = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++) {
            float angle = i * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnBullet(dir);
        }
    }

    private void SpawnBullet(Vector2 direction) {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        BossProjectile proj = bullet.GetComponent<BossProjectile>();
        if (proj != null) {
            proj.SetBossVelocity(direction * bulletSpeed);
        }

        // 注意：這裡的子彈旋轉不受 Boss 旋轉影響，因為是 Quaternion.identity 生成
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    IEnumerator DeathSequence() {
        Vector2 centerPoint = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        yield return StartCoroutine(MoveToPosition(centerPoint, deathFlySpeed)); 
        if (anim != null) anim.SetTrigger("Die");
        if (camShake != null) camShake.Shake(1.0f, 0.5f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BossExplosion");
    }

    public void FinishDeath() {
        if (engineAudioSource != null) engineAudioSource.Stop(); 
        Destroy(gameObject);
    }
}