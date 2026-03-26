using UnityEngine;

public class ShurikenProjectile : BaseProjectile {
    [Header("Shuriken Specifics")]
    [Tooltip("The metal sparks effect for missing and hitting the environment")]
    public GameObject hitWallParticles;
    [Tooltip("The flesh/energy burst for hitting a target")]
    public GameObject hitEnemyParticles;

    public float knockbackForce = 3f;   // Light force to interrupt
    public float stunDuration = 0.4f;   // Length of the stun state

    protected override void OnHitTarget(Collider2D hitInfo) {

        // 1. DID WE HIT AN ENEMY OR BOSS?
        if (hitInfo.CompareTag("Enemy") || hitInfo.CompareTag("Boss") || hitInfo.CompareTag("Player")) {

            // Deal Damage
            HealthSystem targetHealth = hitInfo.GetComponent<HealthSystem>();
            if (targetHealth != null) {
                targetHealth.TakeDamage(damage);
            }

            // Interrupt AI & Apply Knockback
            BaseEnemy enemyAI = hitInfo.GetComponent<BaseEnemy>();
            if (enemyAI != null) {
                float pushDir = Mathf.Sign(transform.right.x);
                enemyAI.ApplyKnockback(new Vector2(pushDir, 0), knockbackForce, stunDuration);
            }

            // Enemy Hit Audio & Visuals
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("EnemyHit");

            if (hitEnemyParticles != null) {
                // Instantiate the burst and clean it up after 1 second
                Destroy(Instantiate(hitEnemyParticles, transform.position, Quaternion.identity), 1f);
            }
        }
        // 2. DID WE HIT THE ENVIRONMENT? (Assuming it's not the player)
        else if (!hitInfo.CompareTag("Player")) {

            // Wall Hit Audio & Visuals
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ShurikenHItWall");

            if (hitWallParticles != null) {
                // Instantiate the metal bouncing sparks and clean them up after 1 second
                Destroy(Instantiate(hitWallParticles, transform.position, Quaternion.identity), 1f);
            }
        }

        // 3. DESTROY PROJECTILE UPON IMPACT
        Destroy(gameObject);
    }
}