using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(HealthSystem))]
public abstract class BaseEnemy : MonoBehaviour {
    public enum EnemyState { Patrol, Chase, Attack, Stunned }

    [Header("Base Core Settings")]
    public EnemyState currentState = EnemyState.Patrol;
    public float detectionRadius = 6f;
    public float attackRange = 1.2f;
    public int attackDamage = 15;

    public float attackCooldown = 1.5f;
    public float preAttackDelay = 1f;
    public float attackWindup = 0.4f;

    public LayerMask playerLayer;

    [Header("Collision Bumpers")]
    public LayerMask playerBumperLayer;
    public float bumperDistance = 0.5f;

    [Header("Patrol Path (Offsets)")]
    public List<Vector2> patrolPoints = new List<Vector2>();
    public float patrolWaitTime = 1f;

    [Header("Movement")]
    public float moveSpeed;

    protected Rigidbody2D rb;
    protected HealthSystem healthSys;
    protected Animator anim;
    protected Transform targetPlayer;
    protected float lastAttackTime;

    protected bool isFrozenInTime;
    private Vector2 savedVelocity;
    private RigidbodyType2D originalBodyType;

    // State Tracking
    protected Vector2 startPosition;
    protected int currentPointIndex = 0;
    protected bool isWaiting;
    protected float waitTimer;

    // Combat Tracking
    protected float knockbackTimer;
    protected bool isAttacking;

    public AudioSource walkAudioSource;

    protected virtual void Awake() {
        rb = GetComponent<Rigidbody2D>();
        healthSys = GetComponent<HealthSystem>();
        anim = GetComponent<Animator>();
        originalBodyType = rb.bodyType;

        startPosition = transform.position;

        if (patrolPoints.Count == 0) {
            patrolPoints.Add(Vector2.zero);
        }
    }

    protected virtual void OnEnable() {
        GlobalEvents.OnTimeStopStarted += FreezeInTime;
        GlobalEvents.OnTimeStopEnded += UnfreezeInTime;
    }

    protected virtual void OnDisable() {
        GlobalEvents.OnTimeStopStarted -= FreezeInTime;
        GlobalEvents.OnTimeStopEnded -= UnfreezeInTime;
    }

    protected virtual void FreezeInTime() {
        isFrozenInTime = true;
        savedVelocity = rb.velocity;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (anim != null) anim.speed = 0;
    }

    protected virtual void UnfreezeInTime() {
        isFrozenInTime = false;
        rb.bodyType = originalBodyType;
        rb.velocity = savedVelocity;
        if (anim != null) anim.speed = 1;
    }

    protected virtual void Update() {
        if (healthSys.IsDead()) return;

        // --- CRUCIAL: Stop all logic processing if time is stopped ---
        if (isFrozenInTime) return;

        if (knockbackTimer > 0) {
            knockbackTimer -= Time.deltaTime;

            rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(0, rb.velocity.y), Time.deltaTime * 5f);

            if (knockbackTimer <= 0) {
                currentState = EnemyState.Patrol;
            }
            return;
        }

        DetectPlayer();
        StateMachine();

        if (walkAudioSource != null) {
            bool isMoving = Mathf.Abs(rb.velocity.x) > 0.1f;

            if (isMoving && !walkAudioSource.isPlaying) {
                walkAudioSource.Play();
            } else if ((!isMoving) && walkAudioSource.isPlaying) {
                walkAudioSource.Stop();
            }
        }
    }

    public virtual void ApplyKnockback(Vector2 direction, float force, float duration) {
        if (healthSys.IsDead() || isFrozenInTime) return;

        knockbackTimer = duration;

        StopAllCoroutines();
        isAttacking = false;
        isWaiting = false;

        currentState = EnemyState.Stunned;

        if (anim != null) {
            anim.ResetTrigger("attack");
            anim.SetTrigger("hit");
        }

        rb.velocity = Vector2.zero;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    protected virtual void DetectPlayer() {
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
        if (playerCol != null) {
            targetPlayer = playerCol.transform;
            isWaiting = false;
        } else {
            targetPlayer = null;
        }
    }

    protected virtual void StateMachine() {
        if (currentState == EnemyState.Stunned) return;
        if (isAttacking) return;

        if (targetPlayer == null) {
            currentState = EnemyState.Patrol;

            if (isWaiting) {
                HandleWait();
            } else {
                HandlePatrol();
            }
        } else {
            float dist = Vector2.Distance(transform.position, targetPlayer.position);
            if (dist <= attackRange) {
                currentState = EnemyState.Attack;
                HandleAttack();
            } else {
                currentState = EnemyState.Chase;
                HandleChase();
            }
        }
    }

    private void HandleWait() {
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null) anim.SetBool("isRunning", false);

        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0) {
            isWaiting = false;
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Count;
        }
    }

    protected void StartWaitTimer() {
        isWaiting = true;
        waitTimer = patrolWaitTime;
    }

    protected abstract void HandlePatrol();

    protected virtual void HandleChase() {
        if (targetPlayer != null) {
            float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);

            if (IsBumperHittingPlayer(dirToPlayer)) {
                rb.velocity = new Vector2(0, rb.velocity.y);
            } else {
                rb.velocity = new Vector2(dirToPlayer * moveSpeed, rb.velocity.y);
            }

            if (anim != null) anim.SetBool("isRunning", true);

            float facingDir = transform.localScale.x > 0 ? 1 : -1;
            if (dirToPlayer != facingDir) {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }
    }

    protected virtual void HandleAttack() {
        if (!isAttacking && Time.time >= lastAttackTime + attackCooldown) {
            StartCoroutine(AttackRoutine());
        }
    }

    protected virtual System.Collections.IEnumerator AttackRoutine() {
        isAttacking = true;

        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null) anim.SetBool("isRunning", false);

        if (targetPlayer != null) {
            float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);
            float facingDir = transform.localScale.x > 0 ? 1 : -1;

            if (dirToPlayer != facingDir && Mathf.Abs(targetPlayer.position.x - transform.position.x) > 0.1f) {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }

        // --- NEW: Use Custom Wait ---
        yield return StartCoroutine(EnemyWait(preAttackDelay));

        if (currentState == EnemyState.Stunned || healthSys.IsDead()) {
            isAttacking = false;
            yield break;
        }

        if (anim != null) anim.SetTrigger("attack");

        // --- NEW: Use Custom Wait ---
        yield return StartCoroutine(EnemyWait(attackWindup));

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("EnemyAttack");

        if (currentState != EnemyState.Stunned && !healthSys.IsDead() && targetPlayer != null) {
            float dist = Vector2.Distance(transform.position, targetPlayer.position);
            if (dist <= attackRange) {
                HealthSystem playerHealth = targetPlayer.GetComponent<HealthSystem>();
                if (playerHealth != null) {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    // --- NEW: The Time-Stop Aware Wait Coroutine ---
    protected System.Collections.IEnumerator EnemyWait(float duration) {
        float timer = 0f;
        while (timer < duration) {
            // Only count down if time is NOT stopped
            if (!isFrozenInTime) {
                timer += Time.deltaTime;
            }
            yield return null;
        }
    }

    protected bool IsBumperHittingPlayer(float moveDirection) {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        Vector2 chestPos = new Vector2(col.bounds.center.x, col.bounds.center.y);
        RaycastHit2D hit = Physics2D.Raycast(chestPos, Vector2.right * moveDirection, bumperDistance, playerBumperLayer);

        return hit.collider != null;
    }

    protected virtual void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (patrolPoints != null && patrolPoints.Count > 0) {
            Gizmos.color = Color.green;
            Vector2 basePos = Application.isPlaying ? startPosition : (Vector2)transform.position;
            for (int i = 0; i < patrolPoints.Count; i++) {
                Vector2 p1 = basePos + patrolPoints[i];
                Vector2 p2 = basePos + patrolPoints[(i + 1) % patrolPoints.Count];
                Gizmos.DrawSphere(p1, 0.2f);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}