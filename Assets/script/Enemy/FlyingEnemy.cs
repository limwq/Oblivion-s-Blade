using UnityEngine;

public class FlyingEnemy : BaseEnemy {
    [Header("Flying Settings")]
    public float flightSpeed = 3f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    protected override void Awake() {
        base.Awake();
        rb.gravityScale = 0f;
    }

    protected override void HandlePatrol() {
        // --- GUARD CLAUSE: Don't move if stunned or frozen ---
        if (currentState == EnemyState.Stunned || isFrozenInTime) {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 worldTarget = startPosition + patrolPoints[currentPointIndex];

        Vector2 newPos = Vector2.MoveTowards(rb.position, worldTarget, flightSpeed * Time.deltaTime);
        Vector2 moveDir = (newPos - rb.position).normalized;
        rb.velocity = moveDir * flightSpeed;

        float facingDir = transform.localScale.x > 0 ? 1 : -1;
        if (moveDir.x != 0 && Mathf.Sign(moveDir.x) != facingDir) {
            Flip();
        }

        if (Vector2.Distance(transform.position, worldTarget) < 0.1f) {
            StartWaitTimer();
        }
    }

    protected override void HandleChase() {
        // --- GUARD CLAUSE: Don't move if stunned or frozen ---
        if (currentState == EnemyState.Stunned || isFrozenInTime) {
            rb.velocity = Vector2.zero;
            return;
        }

        if (targetPlayer == null) return;

        float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);
        float desiredDistance = 4f;
        Vector2 hoverTarget = new Vector2(targetPlayer.position.x - (dirToPlayer * desiredDistance), targetPlayer.position.y);

        Vector2 moveDir = (hoverTarget - rb.position).normalized;

        if (Vector2.Distance(rb.position, hoverTarget) > 0.2f) {
            rb.velocity = moveDir * flightSpeed;
        } else {
            rb.velocity = Vector2.zero;
        }

        float facingDir = transform.localScale.x > 0 ? 1 : -1;
        if (dirToPlayer != facingDir) {
            Flip();
        }
    }

    private void Flip() {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    protected override System.Collections.IEnumerator AttackRoutine() {
        isAttacking = true;
        rb.velocity = Vector2.zero;

        // --- NEW: Use Custom Wait ---
        yield return StartCoroutine(EnemyWait(preAttackDelay));

        if (currentState == EnemyState.Stunned || healthSys.IsDead() || isFrozenInTime) {
            isAttacking = false;
            yield break;
        }

        if (anim != null) anim.SetTrigger("attack");

        // --- NEW: Use Custom Wait ---
        yield return StartCoroutine(EnemyWait(attackWindup));

        if (currentState == EnemyState.Stunned || healthSys.IsDead() || isFrozenInTime) {
            isAttacking = false;
            yield break;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("EnemyShoot");

        if (targetPlayer != null) {
            if (projectilePrefab != null && firePoint != null) {
                Vector2 dirToPlayer = (targetPlayer.position - firePoint.position).normalized;
                float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                Quaternion spawnRot = Quaternion.Euler(0, 0, angle);

                Instantiate(projectilePrefab, firePoint.position, spawnRot);
            }
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }
}