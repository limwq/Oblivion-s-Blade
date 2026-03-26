using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))] // Good practice to ensure RB exists
public class PlayerCombat : MonoBehaviour {
    [Header("Melee Combat Settings")]
    public Transform meleeAttackPoint;
    public float attackRange = 0.5f;
    public int meleeDamage = 20;
    public LayerMask enemyLayers;

    public float comboWindow = 0.5f;
    public float postComboCooldown = 0.6f;

    // --- NEW: Attack Lunge Settings ---
    [Header("Attack Lunge Momentum")]
    public float attack1LungeForce = 4f; // Light step forward on first hit
    public float attack2LungeForce = 6f; // Deeper step on the combo finisher

    [Header("Camera Spring Settings")]
    public float whiffPunchForce = 2f;
    public float hitPunchForce = 8f;

    [Header("Juice & Game Feel")]
    public float hitStopDuration = 0.05f;
    public float knockbackForce = 10f;
    public float enemyStunTime = 0.8f;

    public GameObject hitVFXPrefab;

    private float comboTimer = 0f;
    private int comboStep = 0;
    private float attackLockoutTimer = 0f;

    [Header("Ranged Combat Settings")]
    public GameObject shurikenPrefab;
    public Transform throwPoint;
    public float shurikenCooldown = 1.5f;
    private float shurikenCooldownTimer = 0f;

    private Animator anim;
    private HealthSystem healthSys;
    private NinjaController2D controller;
    private Rigidbody2D rb; // --- NEW: To push the player ---

    private void Awake() {
        anim = GetComponent<Animator>();
        healthSys = GetComponent<HealthSystem>();
        controller = GetComponent<NinjaController2D>();
        rb = GetComponent<Rigidbody2D>(); // --- NEW: Grab the Rigidbody ---
    }

    private void Update() {
        if (healthSys != null && healthSys.IsDead()) return;
        if (controller != null && controller.IsClimbing) return;

        HandleTimers();
        HandleMeleeInput();
        HandleRangedInput();
    }

    private void HandleTimers() {
        if (attackLockoutTimer > 0) attackLockoutTimer -= Time.deltaTime;

        if (comboTimer > 0) {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0 && comboStep == 1) {
                comboStep = 0;
            }
        }

        if (shurikenCooldownTimer > 0) {
            shurikenCooldownTimer -= Time.deltaTime;
        }
    }

    private void HandleMeleeInput() {
        if (Input.GetMouseButtonDown(0)) {
            if (attackLockoutTimer > 0) return;

            float facingDir = Mathf.Sign(transform.localScale.x);

            if (comboStep == 0) {
                anim.SetTrigger("attack1");
                ApplyLunge(facingDir, attack1LungeForce); // --- NEW: Lunge Forward ---
                PerformMeleeHit(facingDir);

                comboStep = 1;
                comboTimer = comboWindow;
                attackLockoutTimer = 0.3f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("PlayerMelee1");

            } else if (comboStep == 1 && comboTimer > 0) {
                anim.SetTrigger("attack2");
                ApplyLunge(facingDir, attack2LungeForce); // --- NEW: Lunge Forward ---
                PerformMeleeHit(facingDir);

                comboStep = 0;
                comboTimer = 0;
                attackLockoutTimer = postComboCooldown;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("PlayerMelee2");
            }
        }
    }

    // --- UPDATED: The Lunge Trigger ---
    private void ApplyLunge(float dir, float force) {
        // Stop any previous lunges so they don't stack
        StopAllCoroutines();
        StartCoroutine(LungeRoutine(dir, force));
    }

    // --- NEW: The Lunge Override Coroutine ---
    private System.Collections.IEnumerator LungeRoutine(float dir, float force) {
        // 1. Turn OFF the movement controller so it stops forcing velocity to 0
        if (controller != null) controller.enabled = false;

        // 2. Set the velocity directly (more reliable than AddForce for this)
        if (rb != null) {
            rb.velocity = new Vector2(dir * force, rb.velocity.y);
        }

        // 3. Wait for the exact length of the forward slide (0.15 seconds feels snappy!)
        yield return new WaitForSeconds(0.15f);

        // 4. Stop the sliding momentum
        if (rb != null) {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // 5. Turn the movement controller back ON so the player can walk again
        if (controller != null) controller.enabled = true;
    }

    private void HandleRangedInput() {
        if (Input.GetMouseButtonDown(1)) {
            if (shurikenCooldownTimer <= 0 && attackLockoutTimer <= 0) {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("PlayerShuriken");

                anim.SetTrigger("shuriken");
                ThrowShuriken();
                shurikenCooldownTimer = shurikenCooldown;
                attackLockoutTimer = 0.3f;

                float facingDir = Mathf.Sign(transform.localScale.x);
                if (CameraSpring.Instance != null) {
                    CameraSpring.Instance.Punch(Vector2.left * facingDir, whiffPunchForce);
                }
            }
        }
    }

    private void PerformMeleeHit(float facingDir) {
        if (meleeAttackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(meleeAttackPoint.position, attackRange, enemyLayers);
        bool hitSomething = false;

        foreach (Collider2D enemy in hitEnemies) {
            HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
            if (enemyHealth != null) {
                enemyHealth.TakeDamage(meleeDamage);
                hitSomething = true;

                BaseEnemy baseEnemy = enemy.GetComponent<BaseEnemy>();
                if (baseEnemy != null) {
                    baseEnemy.ApplyKnockback(new Vector2(facingDir, 0), knockbackForce, enemyStunTime);
                }

                if (hitVFXPrefab != null) {
                    Destroy(Instantiate(hitVFXPrefab, enemy.transform.position, Quaternion.identity), 2f);
                }
            }
        }

        if (hitSomething) {
            if (CameraSpring.Instance != null)
                CameraSpring.Instance.Punch(Vector2.right * facingDir, hitPunchForce);

            StartCoroutine(HitStopRoutine(hitStopDuration));
        } else {
            if (CameraSpring.Instance != null)
                CameraSpring.Instance.Punch(Vector2.right * facingDir, whiffPunchForce);
        }
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration) {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private void ThrowShuriken() {
        if (shurikenPrefab == null || throwPoint == null) return;

        float facingDir = Mathf.Sign(transform.localScale.x);
        Quaternion spawnRotation = facingDir > 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        Instantiate(shurikenPrefab, throwPoint.position, spawnRotation);
    }

    private void OnDisable() {
        Time.timeScale = 1f;
    }
}