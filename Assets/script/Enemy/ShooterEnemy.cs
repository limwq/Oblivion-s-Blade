using UnityEngine;
using System.Collections;

public class ShooterEnemy : BaseEnemy {
    [Header("Shooter Combat Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Laser Sight Settings")]
    public LineRenderer laserSight;
    public LayerMask obstacleLayers;

    public Color aimColor = Color.red;
    public float aimWidth = 0.02f;

    public Color shootColor = Color.white;
    public float shootWidth = 0.1f;

    // Tracks the exact mathematical angle to shoot at
    private float lockedAimAngle;

    protected override void Awake() {
        base.Awake();
        // Mass is back to normal so knockback and hit animations work perfectly!
        if (laserSight != null) laserSight.enabled = false;
    }

    protected override void HandlePatrol() {
        // Shooters stand guard
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null) anim.SetBool("isRunning", false);
        if (laserSight != null) laserSight.enabled = false;
    }

    protected override void HandleChase() {
        rb.velocity = new Vector2(0, rb.velocity.y);
        if (anim != null) anim.SetBool("isRunning", false);

        if (targetPlayer != null) {
            FacePlayer();
            // We don't turn on the laser until they are winding up an attack
            if (laserSight != null) laserSight.enabled = false;
        }
    }

    private void FacePlayer() {
        float dirToPlayer = Mathf.Sign(targetPlayer.position.x - transform.position.x);
        float facingDir = transform.localScale.x > 0 ? 1 : -1;

        if (dirToPlayer != facingDir && Mathf.Abs(targetPlayer.position.x - transform.position.x) > 0.1f) {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // A helper method to draw the laser based on an exact angle
    private void DrawLaser(Color color, float width, float angle) {
        if (laserSight == null || firePoint == null) return;

        laserSight.enabled = true;
        laserSight.startColor = color;
        laserSight.endColor = color;
        laserSight.startWidth = width;
        laserSight.endWidth = width;

        laserSight.SetPosition(0, firePoint.position);

        // Convert the angle back into a directional vector for the Raycast
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dir, attackRange, obstacleLayers);

        if (hit.collider != null) {
            laserSight.SetPosition(1, hit.point);
        } else {
            laserSight.SetPosition(1, firePoint.position + (Vector3)dir * attackRange);
        }
    }

    // --- OVERRIDE: Laser Aim and Shoot ---
    protected override IEnumerator AttackRoutine() {
        isAttacking = true;


        rb.velocity = new Vector2(0, rb.velocity.y);

        // 1. AIMING PHASE (Track player perfectly, Red Laser)
        float timer = 0f;
        while (timer < preAttackDelay) {
            if (!isFrozenInTime) {
                if (targetPlayer != null) {
                    FacePlayer();

                    // Constantly update the angle to the player's current position
                    Vector2 direction = (targetPlayer.position - firePoint.position).normalized;
                    lockedAimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                    DrawLaser(aimColor, aimWidth, lockedAimAngle);
                }
                timer += Time.deltaTime;
            }
            yield return null;
        }

        // If stunned or killed during windup, abort!
        if (currentState == EnemyState.Stunned || healthSys.IsDead()) {
            isAttacking = false;
            if (laserSight != null) laserSight.enabled = false;
            yield break;
        }

        if (anim != null) anim.SetTrigger("attack");

        // 2. WINDUP PHASE (Lock the angle, White Laser)
        DrawLaser(shootColor, shootWidth, lockedAimAngle);

        timer = 0f;
        while (timer < attackWindup) {
            if (!isFrozenInTime) {
                timer += Time.deltaTime;
            }
            yield return null;
        }

        // 3. FIRE PROJECTILE
        if (currentState != EnemyState.Stunned && !healthSys.IsDead() && targetPlayer != null) {
            if (projectilePrefab != null && firePoint != null) {
                // Instantiate the bullet rotated to the exact locked angle
                Quaternion spawnRot = Quaternion.Euler(0, 0, lockedAimAngle);
                Instantiate(projectilePrefab, firePoint.position, spawnRot);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("EnemyShoot");
        }

        if (laserSight != null) laserSight.enabled = false;

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    public override void ApplyKnockback(Vector2 direction, float force, float duration) {
        // Instantly shut off the laser before the knockback halts the coroutine
        if (laserSight != null) laserSight.enabled = false;

        // Let the BaseEnemy handle the actual physics and stun states
        base.ApplyKnockback(direction, force, duration);
    }

    // --- THE FIX: Catch the death interruption ---
    protected override void Update() {
        base.Update(); // Run normal BaseEnemy logic

        // If the health system registers death, ensure the laser is dead too
        if (healthSys != null && healthSys.IsDead()) {
            if (laserSight != null) laserSight.enabled = false;
        }
    }
}