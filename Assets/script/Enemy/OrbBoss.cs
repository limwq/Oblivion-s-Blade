using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HealthSystem), typeof(Animator))]
public class OrbBoss : MonoBehaviour {
    [Header("Orb Boss Settings")]
    public float spawnDuration = 2.5f;
    public float floatSpeed = 2f;
    public float floatAmplitude = 0.5f;

    [Header("Bullet Hell Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float timeBetweenPatterns = 5f;

    // --- NEW: Save Data Reset Variables ---
    [Header("Save Data Reset (End Game)")]
    [Tooltip("The unique save ID of the Phase 1 boss")]
    public string phase1SaveID = "Level3_OrbBoss_1";
    [Tooltip("The unique save ID of this Phase 2 boss")]
    public string phase2SaveID = "Level3_OrbBoss_2";
    // --------------------------------------

    private Vector3 startPos;
    private bool isSpawning = true;
    private bool isAttacking = false;
    private bool hasDied = false;

    // Component References 
    private HealthSystem healthSys;
    private Animator anim;
    private Transform targetPlayer;

    [Header("Ultimate: Energy Bombs")]
    public GameObject energyBombPrefab;
    public Transform[] bombSpawnPoints;
    public float ultimateCooldown = 60f;
    private float ultimateTimer;

    private SpriteRenderer bossSprite;
    [Header("Stagger Mechanic")]
    public float staggerDuration = 4.0f;
    public int staggerDamage = 150;
    public LayerMask floorLayer;

    private int bombsSpawned = 0;
    private int bombsDefused = 0;
    private bool ultimateFailed = false;
    private bool isStaggered = false;
    private int lastKnownHealth;

    private void Awake() {
        healthSys = GetComponent<HealthSystem>();
        anim = GetComponent<Animator>();
        bossSprite = GetComponentInChildren<SpriteRenderer>();
        startPos = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) targetPlayer = playerObj.transform;

        ultimateTimer = ultimateCooldown;
        if (healthSys != null) lastKnownHealth = healthSys.maxHealth;
    }

    private void Start() {
        if (anim != null) anim.SetTrigger("spawning");
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine() {
        isSpawning = true;
        yield return new WaitForSeconds(spawnDuration);

        if (anim != null) anim.SetTrigger("idle");
        isSpawning = false;
        Debug.Log("[Orb Boss] Spawn complete. Initiating Bullet Hell.");
    }

    private void Update() {
        if (healthSys != null && healthSys.IsDead()) {
            if (!hasDied) HandleDeath();
            return;
        }

        if (healthSys != null && healthSys.currentHealth < lastKnownHealth) {
            StartCoroutine(DamageFlashRoutine());
            lastKnownHealth = healthSys.currentHealth;
        }

        if (isSpawning || isStaggered) return;

        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        if (ultimateTimer > 0) ultimateTimer -= Time.deltaTime;

        if (!isAttacking && targetPlayer != null) {
            StartCoroutine(AttackCycleRoutine());
        }
    }

    private IEnumerator DamageFlashRoutine() {
        if (bossSprite != null) {
            bossSprite.color = Color.black;
            yield return new WaitForSeconds(0.1f);
            bossSprite.color = Color.white;
        }
    }

    private void HandleDeath() {
        hasDied = true;
        Debug.Log("[Orb Boss] Shattered!");

        StopAllCoroutines();
        isAttacking = false;

        if (anim != null) {
            anim.ResetTrigger("spawning");
            anim.ResetTrigger("idle");
            anim.SetTrigger("die");
        }

        // --- UPDATED: TALK TO THE SAVE MANAGER ---
        Debug.Log("[Game Complete] Resetting Boss Save Data for Replay!");
        if (SaveManager.Instance != null) {
            // Erase both phases from the JSON file!
            SaveManager.Instance.ReviveEnemyInSaveData(phase1SaveID);
            SaveManager.Instance.ReviveEnemyInSaveData(phase2SaveID);
        }
        // -----------------------------------------

        Destroy(gameObject, 2f);
    }

    private IEnumerator AttackCycleRoutine() {
        isAttacking = true;

        if (ultimateTimer <= 0 && bombSpawnPoints.Length > 0) {
            yield return StartCoroutine(UltimateBombRoutine());
        } else {
            int choice = Random.Range(0, 2);
            if (choice == 0) yield return StartCoroutine(SpiralPattern());
            else yield return StartCoroutine(BurstPattern());
        }

        yield return new WaitForSeconds(timeBetweenPatterns);
        isAttacking = false;
    }

    private IEnumerator UltimateBombRoutine() {
        Debug.Log("[Orb Boss] CHARGING ULTIMATE!");

        if (bossSprite != null) {
            Color originalColor = bossSprite.color;
            for (int i = 0; i < 5; i++) {
                bossSprite.color = Color.red;
                yield return new WaitForSeconds(0.5f);
                bossSprite.color = originalColor;
                yield return new WaitForSeconds(0.5f);
            }
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        bombsSpawned = bombSpawnPoints.Length;
        bombsDefused = 0;
        ultimateFailed = false;

        Debug.Log("[Orb Boss] Spawning Energy Bombs!");
        foreach (Transform spawnPoint in bombSpawnPoints) {
            if (energyBombPrefab != null && firePoint != null) {
                GameObject bomb = Instantiate(energyBombPrefab, firePoint.position, Quaternion.identity);

                EnergyBomb bombScript = bomb.GetComponent<EnergyBomb>();
                if (bombScript != null) bombScript.Initialize(this);

                StartCoroutine(ShootBombRoutine(bomb, spawnPoint.position));
            }
        }

        ultimateTimer = ultimateCooldown;
    }

    private IEnumerator ShootBombRoutine(GameObject bomb, Vector3 targetPos) {
        if (bomb == null) yield break;

        float flightDuration = 0.6f;
        float timer = 0f;

        Vector3 startPos = bomb.transform.position;
        Vector3 finalScale = bomb.transform.localScale;

        bomb.transform.localScale = Vector3.zero;

        while (timer < flightDuration) {
            if (bomb == null) yield break;

            timer += Time.deltaTime;
            float progress = timer / flightDuration;

            float ease = 1f - Mathf.Pow(1f - progress, 3f);

            bomb.transform.position = Vector3.Lerp(startPos, targetPos, ease);
            bomb.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, ease);

            yield return null;
        }

        if (bomb != null) {
            bomb.transform.position = targetPos;
            bomb.transform.localScale = finalScale;
        }
    }

    private IEnumerator SpiralPattern() {
        Debug.Log("[Orb Boss] Firing Spiral!");
        float currentAngle = 0f;

        for (int i = 0; i < 40; i++) {
            if (healthSys != null && healthSys.IsDead()) yield break;

            FireBullet(currentAngle);
            currentAngle += 20f;
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator BurstPattern() {
        Debug.Log("[Orb Boss] Firing Burst!");

        for (int wave = 0; wave < 3; wave++) {
            if (healthSys != null && healthSys.IsDead()) yield break;

            for (int i = 0; i < 360; i += 30) {
                FireBullet(i);
            }
            yield return new WaitForSeconds(0.6f);
        }
    }

    private void FireBullet(float angle) {
        if (bulletPrefab != null && firePoint != null) {
            Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        }
    }

    public void ReportBombDefused() {
        if (ultimateFailed || isStaggered) return;

        bombsDefused++;
        Debug.Log($"[Orb Boss] Bomb defused! {bombsDefused} / {bombsSpawned}");

        if (bombsDefused >= bombsSpawned) {
            StopAllCoroutines();
            StartCoroutine(StaggerRoutine());
        }
    }

    public void ReportBombExploded() {
        ultimateFailed = true;
    }

    private IEnumerator StaggerRoutine() {
        isStaggered = true;
        isAttacking = false;

        Debug.Log("[Orb Boss] STAGGERED! Dropping to the floor!");

        if (healthSys != null) healthSys.TakeDamage(staggerDamage);

        if (anim != null) anim.SetTrigger("idle");
        if (bossSprite != null) bossSprite.color = Color.gray;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) {
            rb.gravityScale = 8f;
        }

        if (CameraSpring.Instance != null) CameraSpring.Instance.Punch(Vector2.up, 100f);

        Debug.Log("[Orb Boss] Waiting on the ground for the player to attack!");

        yield return new WaitForSeconds(staggerDuration);

        Debug.Log("[Orb Boss] Rising back up!");

        if (bossSprite != null) bossSprite.color = Color.white;

        if (rb != null) {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        float dropTimer = 0f;
        Vector3 currentPos = transform.position;

        while (dropTimer < 1.0f) {
            dropTimer += Time.deltaTime;
            transform.position = Vector3.Lerp(currentPos, startPos, dropTimer / 1.0f);
            yield return null;
        }

        if (rb != null) {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        isStaggered = false;
        ultimateTimer = ultimateCooldown;
    }
}