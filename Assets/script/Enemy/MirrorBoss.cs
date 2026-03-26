using UnityEngine;
using System.Collections;

public class MirrorBoss : BaseEnemy {
    [Header("Mirror Boss Settings")]
    public GameObject shadowPrefab;
    public GameObject shurikenPrefab;
    public Transform firePoint;

    [Header("VFX / Particles")]
    public GameObject shadowSmokePrefab;
    public GameObject timeStopRingPrefab;

    [Tooltip("Drag the VFX_BossAfterimage prefab here")]
    public GameObject afterimagePrefab;
    [Tooltip("How often to drop a ghost (e.g., every 0.05 seconds)")]
    public float afterimageRate = 0.05f;

    [Header("Shadow Settings")]
    public float shadowLifetime = 5f;
    private GameObject activeShadow;
    private Coroutine shadowTimerCoroutine;

    [Header("Ability Cooldowns & AI")]
    public float introWaitTime = 2.5f;
    public float timeStopCooldown = 12f;
    public float timeStopDuration = 5f;
    public float timeStopCastTime = 5f;
    public int timeStopInterruptDamage = 40;
    public float decisionInterval = 1.5f;
    public float hitResetTime = 2.5f;
    public float postEscapeImmunityTime = 2.0f;

    [Header("Arena Bounds (Wall Bumper)")]
    public LayerMask wallLayer;

    [Header("Karma Scaling (Punishment)")]
    public int healthPerKill = 15;
    public float speedBuffPerKill = 0.1f;
    public float cooldownReductionPerKill = 0.2f;

    // Internal AI Trackers
    private float timeStopTimer;
    private float immuneTimer = 0f;
    private bool isActing = false;
    private bool isAwake = false;
    private bool isCastingTimeStop = false;
    private SpriteRenderer sr; // Cached for performance

    // Hit Protection System
    private int recentHits = 0;
    private float lastHitTime = 0f;

    // Bulletproof Player Freeze Tracking
    private float playerFreezeTimer = 0f;
    private NinjaController2D frozenPlayerCtrl;
    private Rigidbody2D frozenPlayerRb;
    private float frozenPlayerGravity;

    protected override void Awake() {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        timeStopTimer = timeStopCooldown;
    }

    protected override void OnEnable() { }

    protected override void OnDisable() {
        base.OnDisable();

        // --- THE FIX: Clean up anything left behind when the boss dies ---
        StopAllCoroutines();
        ClearActiveShadow(); // <--- Destroys the shadow immediately

        // FAILSAFE: If the boss is killed/disabled while player is frozen, unfreeze them!
        UnfreezePlayer();
    }

    private void Start() {
        StartCoroutine(IntroWaitRoutine());

        if (GameManager.Instance != null) {
            int sins = GameManager.Instance.totalEnemiesKilled;
            if (sins > 0) {
                Debug.Log($"[Mirror Boss] Player killed {sins} enemies. Absorbing Karma...");
                if (healthSys != null) healthSys.AddMaxHealth(sins * healthPerKill);
                moveSpeed += (sins * speedBuffPerKill);
                timeStopCooldown = Mathf.Max(5f, timeStopCooldown - (sins * cooldownReductionPerKill));
            }
        }
    }

    private IEnumerator IntroWaitRoutine() {
        isAwake = false;
        yield return new WaitForSeconds(1f);
        FacePlayer();

        yield return new WaitForSeconds(introWaitTime);
        isAwake = true;
        Debug.Log("[Mirror Boss] FIGHT!");
    }

    protected override void Update() {
        base.Update();

        if (playerFreezeTimer > 0) {
            playerFreezeTimer -= Time.deltaTime;
            if (playerFreezeTimer <= 0) {
                UnfreezePlayer();
            }
        }

        if (healthSys != null && healthSys.IsDead()) return;
        if (currentState == EnemyState.Stunned) return;

        if (!isAwake) {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        if (immuneTimer > 0) immuneTimer -= Time.deltaTime;
        if (timeStopTimer > 0) timeStopTimer -= Time.deltaTime;

        if (Time.time - lastHitTime > hitResetTime) {
            recentHits = 0;
        }

        if (!isActing && targetPlayer != null) {
            if (timeStopTimer <= 0) {
                StartCoroutine(TimeStopRoutine());
            } else {
                StartCoroutine(ChooseActionRoutine());
            }
        }
    }

    protected override void HandlePatrol() { }
    protected override void HandleChase() { }
    protected override IEnumerator AttackRoutine() { yield break; }

    private void FacePlayer() {
        if (targetPlayer == null) return;
        float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);
        float facingDir = transform.localScale.x > 0 ? 1 : -1;

        if (dirToPlayer != facingDir) {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // --- CENTRALIZED SHADOW MANAGEMENT ---
    private void SpawnShadow(Vector3 spawnPos) {
        ClearActiveShadow(); // Clean up existing shadow safely

        if (shadowPrefab != null) {
            activeShadow = Instantiate(shadowPrefab, spawnPos, Quaternion.identity);

            if (shadowSmokePrefab != null) {
                Destroy(Instantiate(shadowSmokePrefab, spawnPos, Quaternion.identity), 2f);
            }

            if (shadowTimerCoroutine != null) StopCoroutine(shadowTimerCoroutine);
            shadowTimerCoroutine = StartCoroutine(ShadowTimerRoutine());
        }
    }

    private IEnumerator ShadowTimerRoutine() {
        yield return new WaitForSeconds(shadowLifetime);
        ClearActiveShadow(); // Disappears naturally with a poof
    }

    private void ClearActiveShadow() {
        if (activeShadow != null) {
            if (shadowSmokePrefab != null) {
                Destroy(Instantiate(shadowSmokePrefab, activeShadow.transform.position, Quaternion.identity), 2f);
            }
            Destroy(activeShadow);
            activeShadow = null;
        }
    }
    // -------------------------------------

    private void TryDealDamage() {
        if (targetPlayer != null) {
            if (Vector2.Distance(transform.position, targetPlayer.position) <= attackRange * 1.5f) {
                HealthSystem playerHealth = targetPlayer.GetComponent<HealthSystem>();
                if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    private IEnumerator ChooseActionRoutine() {
        isActing = true;
        if (anim != null) anim.SetBool("isRunning", false);
        rb.velocity = new Vector2(0, rb.velocity.y);

        FacePlayer();
        float distance = Vector2.Distance(transform.position, targetPlayer.position);

        if (distance > attackRange) {
            int choice = Random.Range(0, 4);
            if (choice == 0) yield return StartCoroutine(SlowWalkRoutine());
            else if (choice == 1) yield return StartCoroutine(SuddenDashRoutine());
            else if (choice == 2) yield return StartCoroutine(ShurikenRoutine());
            else yield return StartCoroutine(ShadowStepToPlayerRoutine());
        } else {
            int choice = Random.Range(0, 3);
            if (choice == 0) yield return StartCoroutine(MeleeSingleRoutine());
            else if (choice == 1) yield return StartCoroutine(MeleeComboRoutine());
            else yield return StartCoroutine(DashAwayRoutine());
        }

        yield return new WaitForSeconds(decisionInterval);
        isActing = false;
    }

    private IEnumerator SlowWalkRoutine() {
        if (anim != null) anim.SetBool("isRunning", true);

        float walkTime = 1.5f;
        float timer = 0f;

        while (timer < walkTime) {
            if (targetPlayer != null) {
                FacePlayer();
                float dir = transform.localScale.x > 0 ? 1f : -1f;

                if (IsBumperHittingPlayer(dir)) rb.velocity = new Vector2(0, rb.velocity.y);
                else rb.velocity = new Vector2(dir * (moveSpeed * 0.5f), rb.velocity.y);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (anim != null) anim.SetBool("isRunning", false);
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    private IEnumerator SuddenDashRoutine() {
        if (anim != null) anim.SetTrigger("charging");
        yield return new WaitForSeconds(1f);

        SpawnShadow(transform.position);
        if (anim != null) anim.SetTrigger("attack2");

        float dir = transform.localScale.x > 0 ? 1f : -1f;
        float dashTimer = 0f;
        float ghostTimer = 0f;
        Color dashColor = new Color(0.3f, 0f, 0f, 0.7f);
        bool hasDealtDamage = false;

        while (dashTimer < 0.4f) {
            // Drop afterimages
            if (sr != null && afterimagePrefab != null) {
                ghostTimer -= Time.deltaTime;
                if (ghostTimer <= 0) {
                    ghostTimer = afterimageRate;
                    GameObject newGhost = Instantiate(afterimagePrefab, transform.position, Quaternion.identity);
                    AfterimageFade fadeScript = newGhost.GetComponent<AfterimageFade>();
                    if (fadeScript != null) {
                        fadeScript.Initialize(sr.sprite, transform.position, transform.localScale, dashColor);
                    }
                }
            }

            Vector2 chestPos = new Vector2(GetComponent<Collider2D>().bounds.center.x, GetComponent<Collider2D>().bounds.center.y);
            RaycastHit2D wallHit = Physics2D.Raycast(chestPos, Vector2.right * dir, bumperDistance, wallLayer);

            if (IsBumperHittingPlayer(dir)) {
                rb.velocity = new Vector2(0, rb.velocity.y);
                if (!hasDealtDamage) {
                    TryDealDamage();
                    hasDealtDamage = true;
                }
            } else if (wallHit.collider != null) rb.velocity = new Vector2(0, rb.velocity.y);
            else rb.velocity = new Vector2(dir * (moveSpeed * 3.5f), 0);

            dashTimer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(1f);

        if (Random.value > 0.5f && activeShadow != null) {
            if (shadowSmokePrefab != null) Destroy(Instantiate(shadowSmokePrefab, transform.position, Quaternion.identity), 2f);
            transform.position = activeShadow.transform.position;
            ClearActiveShadow();
        }
    }

    private IEnumerator ShurikenRoutine() {
        for (int i = 0; i < 3; i++) {
            if (targetPlayer == null) break;

            FacePlayer();
            if (anim != null) anim.SetTrigger("attack");
            yield return new WaitForSeconds(0.2f);

            if (shurikenPrefab != null && firePoint != null) {
                Vector2 directionToPlayer = (targetPlayer.position - firePoint.position).normalized;
                float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                Instantiate(shurikenPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
            }

            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator MeleeSingleRoutine() {
        if (anim != null) anim.SetTrigger("attack1");
        yield return new WaitForSeconds(0.3f);
        TryDealDamage();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator MeleeComboRoutine() {
        if (anim != null) anim.SetTrigger("attack1");
        yield return new WaitForSeconds(0.3f);
        TryDealDamage();
        yield return new WaitForSeconds(0.1f);

        if (anim != null) anim.SetTrigger("attack2");
        yield return new WaitForSeconds(0.3f);
        TryDealDamage();
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator DashAwayRoutine() {
        if (targetPlayer == null) yield break;

        FacePlayer();
        if (anim != null) anim.SetTrigger("roll");
        yield return new WaitForSeconds(0.1f);

        float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);
        float escapeDir = -dirToPlayer;

        Vector3 scale = transform.localScale;
        scale.x = escapeDir > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        float dashTimer = 0f;
        while (dashTimer < 0.4f) {
            Vector2 chestPos = new Vector2(GetComponent<Collider2D>().bounds.center.x, GetComponent<Collider2D>().bounds.center.y);
            RaycastHit2D wallHit = Physics2D.Raycast(chestPos, Vector2.right * escapeDir, bumperDistance, wallLayer);

            if (wallHit.collider != null) {
                rb.velocity = Vector2.zero;
                break;
            } else {
                rb.velocity = new Vector2(escapeDir * (moveSpeed * 3.5f), rb.velocity.y);
            }

            dashTimer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = new Vector2(0, rb.velocity.y);
        FacePlayer();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ShadowStepToPlayerRoutine() {
        if (targetPlayer == null) yield break;

        if (anim != null) anim.SetTrigger("charging");
        yield return new WaitForSeconds(0.4f);

        float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);
        Vector2 ambushPos = new Vector2(targetPlayer.position.x - (dirToPlayer * 1f), targetPlayer.position.y + 0.2f);

        if (shadowSmokePrefab != null) {
            Destroy(Instantiate(shadowSmokePrefab, transform.position, Quaternion.identity), 2f);
        }
        SpawnShadow(ambushPos);

        yield return new WaitForSeconds(1f);

        transform.position = activeShadow != null ? activeShadow.transform.position : ambushPos;
        FacePlayer();

        if (shadowSmokePrefab != null) {
            Destroy(Instantiate(shadowSmokePrefab, transform.position, Quaternion.identity), 2f);
        }
        ClearActiveShadow();

        if (anim != null) anim.SetTrigger("attack1");
        yield return new WaitForSeconds(0.3f);
        TryDealDamage();

        yield return new WaitForSeconds(0.5f);
    }

    public override void ApplyKnockback(Vector2 direction, float force, float duration) {
        if (!isAwake) return;
        if (isCastingTimeStop) return;

        if (immuneTimer > 0) {
            return;
        }

        isActing = false;

        if (anim != null) {
            anim.ResetTrigger("attack");
            anim.ResetTrigger("attack1");
            anim.ResetTrigger("attack2");
            anim.ResetTrigger("charging");
            anim.ResetTrigger("jump");
            anim.ResetTrigger("roll");
            anim.SetBool("isRunning", false);
            anim.SetTrigger("hit");
        }

        if (activeShadow != null) {
            StopAllCoroutines();

            if (shadowSmokePrefab != null) Destroy(Instantiate(shadowSmokePrefab, transform.position, Quaternion.identity), 2f);

            transform.position = activeShadow.transform.position;
            ClearActiveShadow();

            rb.velocity = Vector2.zero;
            FacePlayer();

            immuneTimer = postEscapeImmunityTime;
            recentHits = 0;
            return;
        }

        recentHits++;
        lastHitTime = Time.time;

        if (recentHits >= 2) {
            recentHits = 0;
            StopAllCoroutines();
            StartCoroutine(HitEscapeRoutine());
        } else {
            base.ApplyKnockback(direction, force, duration);
        }
    }

    private IEnumerator HitEscapeRoutine() {
        isActing = true;
        if (anim != null) anim.SetBool("isRunning", false);
        rb.velocity = Vector2.zero;

        SpawnShadow(transform.position);

        if (anim != null) anim.SetTrigger("jump");
        rb.velocity = new Vector2(0, 12f);

        yield return new WaitForSeconds(0.2f);
        if (anim != null) anim.SetTrigger("roll");

        Vector2 chestPos = new Vector2(GetComponent<Collider2D>().bounds.center.x, GetComponent<Collider2D>().bounds.center.y);
        RaycastHit2D leftHit = Physics2D.Raycast(chestPos, Vector2.left, 50f, wallLayer);
        float leftSpace = leftHit.collider != null ? leftHit.distance : 50f;
        RaycastHit2D rightHit = Physics2D.Raycast(chestPos, Vector2.right, 50f, wallLayer);
        float rightSpace = rightHit.collider != null ? rightHit.distance : 50f;

        float escapeDir = (rightSpace > leftSpace) ? 1f : -1f;

        Vector3 scale = transform.localScale;
        scale.x = escapeDir > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        float defaultGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashTimer = 0f;
        while (dashTimer < 0.4f) {
            Vector2 currentChestPos = new Vector2(GetComponent<Collider2D>().bounds.center.x, GetComponent<Collider2D>().bounds.center.y);
            RaycastHit2D wallHit = Physics2D.Raycast(currentChestPos, Vector2.right * escapeDir, bumperDistance, wallLayer);

            if (wallHit.collider != null) rb.velocity = new Vector2(0, 0);
            else rb.velocity = new Vector2(escapeDir * (moveSpeed * 3.5f), 0);

            dashTimer += Time.deltaTime;
            yield return null;
        }

        rb.gravityScale = defaultGravity;
        rb.velocity = new Vector2(0, rb.velocity.y);
        FacePlayer();

        yield return new WaitForSeconds(0.5f);

        if (Random.value > 0.5f && activeShadow != null) {
            if (shadowSmokePrefab != null) Destroy(Instantiate(shadowSmokePrefab, transform.position, Quaternion.identity), 2f);

            transform.position = activeShadow.transform.position;
            ClearActiveShadow();

            if (anim != null) anim.SetTrigger("attack1");
            TryDealDamage();
        }

        immuneTimer = postEscapeImmunityTime;
        yield return new WaitForSeconds(decisionInterval);
        isActing = false;
    }

    private IEnumerator TimeStopRoutine() {
        isActing = true;
        isCastingTimeStop = true;
        timeStopTimer = timeStopCooldown;
        rb.velocity = Vector2.zero;

        if (anim != null) {
            anim.SetBool("isRunning", false);
            anim.SetTrigger("charging");
        }

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX("EnemyAlert");
        }

        Color originalColor = sr != null ? sr.color : Color.white;
        float blinkSpeed = 8f;
        int startingHealth = healthSys != null ? healthSys.currentHealth : 0;
        float castTimer = 0f;
        bool wasInterrupted = false;

        while (castTimer < timeStopCastTime) {
            if (sr != null) {
                float pulse = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                sr.color = Color.Lerp(originalColor, Color.red, pulse);
            }

            int damageTaken = startingHealth - healthSys.currentHealth;
            if (damageTaken >= timeStopInterruptDamage) {
                wasInterrupted = true;
                break;
            }

            castTimer += Time.deltaTime;
            yield return null;
        }

        if (sr != null) sr.color = originalColor;
        isCastingTimeStop = false;

        if (wasInterrupted) {
            Debug.Log("[Mirror Boss] TIME STOP INTERRUPTED!");

            if (CameraSpring.Instance != null) {
                CameraSpring.Instance.Punch(Vector2.up, 15f);
            }

            if (anim != null) anim.SetTrigger("hit");

            float pushDir = 1f;
            if (targetPlayer != null) {
                pushDir = Mathf.Sign(transform.position.x - targetPlayer.position.x);
            } else {
                pushDir = transform.localScale.x > 0 ? -1f : 1f;
            }

            rb.velocity = Vector2.zero;
            rb.AddForce(new Vector2(pushDir, 1.2f).normalized * 8f, ForceMode2D.Impulse);

            if (AudioManager.Instance != null) {
                AudioManager.Instance.PlaySFX("BossExplosion");
            }

            yield return new WaitForSeconds(1.5f);
            isActing = false;
            yield break;
        }

        if (timeStopRingPrefab != null) {
            Destroy(Instantiate(timeStopRingPrefab, transform.position, Quaternion.identity), 3f);
        }

        FreezePlayer(timeStopDuration);
        yield return new WaitForSeconds(0.8f);
        isActing = false;
    }

    private void FreezePlayer(float duration) {
        if (targetPlayer == null) return;

        frozenPlayerCtrl = targetPlayer.GetComponent<NinjaController2D>();
        frozenPlayerRb = targetPlayer.GetComponent<Rigidbody2D>();

        if (frozenPlayerCtrl != null && frozenPlayerRb != null) {
            frozenPlayerCtrl.canMove = false;

            frozenPlayerGravity = frozenPlayerRb.gravityScale;
            frozenPlayerRb.gravityScale = 0f;
            frozenPlayerRb.velocity = Vector2.zero;

            Animator playerAnim = targetPlayer.GetComponent<Animator>();
            if (playerAnim != null) playerAnim.speed = 0f;

            playerFreezeTimer = duration;
            GlobalEvents.TriggerTimeStopStarted();
        }
    }

    private void UnfreezePlayer() {
        if (frozenPlayerCtrl != null && frozenPlayerRb != null) {
            frozenPlayerCtrl.canMove = true;
            frozenPlayerRb.gravityScale = frozenPlayerGravity;

            if (targetPlayer != null) {
                Animator playerAnim = targetPlayer.GetComponent<Animator>();
                if (playerAnim != null) playerAnim.speed = 1f;
            }

            GlobalEvents.TriggerTimeStopEnded();
        }

        frozenPlayerCtrl = null;
        frozenPlayerRb = null;
    }
}