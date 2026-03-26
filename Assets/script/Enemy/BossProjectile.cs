using UnityEngine;

public class BossProjectile : BaseProjectile {
    private Animator anim;
    private bool hasHit = false;

    // --- NEW: Store the 360-degree velocity given by the boss ---
    private Vector2 bossVelocity;

    protected override void Awake() {
        base.Awake(); // CRITICAL: Lets BaseProjectile grab the Rigidbody2D!
        anim = GetComponent<Animator>();
    }

    // --- NEW: The Boss script calls this when it spawns the bullet ---
    public void SetBossVelocity(Vector2 velocity) {
        bossVelocity = velocity;
    }

    protected override void Start() {
        base.Start(); // Runs the lifetime setup from your BaseProjectile

        // Immediately overwrite the base class's straight-line movement 
        // with the boss's crazy 360-degree patterns!
        if (bossVelocity != Vector2.zero) {
            rb.velocity = bossVelocity;
        }
    }

    protected override void OnHitTarget(Collider2D hitInfo) {
        if (hasHit) return;

        // Prevent friendly fire (don't hit the boss itself)
        if (hitInfo.CompareTag("Enemy")) {
            return;
        }

        hasHit = true;

        HealthSystem targetHealth = hitInfo.GetComponent<HealthSystem>();
        if (targetHealth != null && hitInfo.CompareTag("Player")) {
            targetHealth.TakeDamage(damage);
        }

        if (rb != null) {
            rb.velocity = Vector2.zero;
            rb.simulated = false; // Stop physics
        }

        if (anim != null) {
            anim.SetTrigger("Die");
        } else {
            Destroy(gameObject);
        }
    }

    public void FinishDeath() {
        Destroy(gameObject);
    }
}