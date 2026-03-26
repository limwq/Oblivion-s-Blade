using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour {
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Invincibility Settings")]
    public bool isInvincible = false;

    [Header("Enemy Save System")]
    public string uniqueEnemyID;

    private bool isDead = false;
    private float defaultGravity;


    private void Awake() {
        currentHealth = maxHealth;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) defaultGravity = rb.gravityScale;
    }

    private void Start() {
        // --- AUTO-ID SYSTEM (Now supports Bosses!) ---
        if (gameObject.CompareTag("Enemy") || gameObject.CompareTag("Boss")) {

            int startX = Mathf.RoundToInt(transform.position.x);
            int startY = Mathf.RoundToInt(transform.position.y);
            uniqueEnemyID = $"{gameObject.scene.name}_{gameObject.name}_{startX}_{startY}";

            if (SaveManager.Instance != null) {
                SaveManager.EnemySaveData savedState = SaveManager.Instance.GetEnemyState(uniqueEnemyID);

                if (savedState != null) {
                    if (savedState.isDead) {
                        Destroy(gameObject); // We died in a previous session. Poof!
                        return;
                    }

                    // --- THE FIX: Only load mid-fight health if it's a normal Enemy! ---
                    // Bosses completely ignore this block so they start fresh.
                    if (gameObject.CompareTag("Enemy")) {
                        currentHealth = savedState.currentHealth;
                    }
                }
            }
        }
    }

    public void AddMaxHealth(int amount) {
        maxHealth += amount;
        currentHealth += amount;
    }

    public void TakeDamage(int amount) {
        if (isInvincible || IsDead()) return;

        currentHealth -= amount;

        if (AudioManager.Instance != null) {
            if (gameObject.CompareTag("Player")) {
                AudioManager.Instance.PlayPlayerSFX("PlayerHurt");
            } else {
                AudioManager.Instance.PlaySFX("EnemyHit");
            }
        }

        Animator anim = GetComponent<Animator>();
        if (!gameObject.CompareTag("Boss")) {
            if (anim != null) anim.SetTrigger("hit");
        }

        if (gameObject.CompareTag("Player")) {
            NinjaController2D controller = GetComponent<NinjaController2D>();
            if (controller != null) controller.ApplyStun(0.4f);
        }

        // --- THE FIX: Save health state ONLY for normal enemies! ---
        if (gameObject.CompareTag("Enemy") && !string.IsNullOrEmpty(uniqueEnemyID) && SaveManager.Instance != null) {
            SaveManager.Instance.UpdateEnemyState(uniqueEnemyID, currentHealth, false);
        }

        if (currentHealth <= 0) {
            Die();
        }
    }



    public IEnumerator TriggerInvincibility(float duration) {
        isInvincible = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            sr.color = new Color(1f, 1f, 1f, 0.5f);
        }

        yield return new WaitForSeconds(duration);

        isInvincible = false;

        if (sr != null) {
            sr.color = Color.white;
        }
    }

    public bool IsDead() {
        return isDead;
    }

    public void Die() {
        if (isDead) return;
        isDead = true;

        if (AudioManager.Instance != null) {
            if (gameObject.CompareTag("Player")) {
                AudioManager.Instance.PlayPlayerSFX("PlayerDeath");
            } else {
                AudioManager.Instance.PlaySFX("EnemyDie");
            }
        }

        // --- PERMANENT DEATH LOGIC ---
        // Both Enemies AND Bosses save their permanent death and add to your Kill Count!
        if (gameObject.CompareTag("Enemy") || gameObject.CompareTag("Boss")) {
            if (SaveManager.Instance != null && !string.IsNullOrEmpty(uniqueEnemyID)) {
                SaveManager.Instance.UpdateEnemyState(uniqueEnemyID, 0, true);
                SaveManager.Instance.AddKill();
            }
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetBool("die", true);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (gameObject.CompareTag("Player")) {
            GlobalEvents.TriggerPlayerDied();
            NinjaController2D controller = GetComponent<NinjaController2D>();
            if (controller != null) controller.isDead = true;

            if (rb != null) {
                rb.gravityScale = 0f;
                rb.velocity = new Vector2(0f, 1.5f);
            }
        } else {
            GlobalEvents.TriggerEnemyKilled();
            if (rb != null) {
                rb.gravityScale = 0;
                rb.velocity = Vector2.zero;
            }

            if (GetComponent<BossController1>() == null) {
                Destroy(gameObject, 1.2f);
            }
        }
    }

    public void RespawnPlayer(Vector2 respawnPoint) {
        isDead = false;
        currentHealth = maxHealth;
        transform.position = respawnPoint;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) {
            rb.gravityScale = defaultGravity;
            rb.velocity = Vector2.zero;
        }

        // --- NEW: Reset Stamina ---
        StaminaSystem stamina = GetComponent<StaminaSystem>();
        if (stamina != null) {
            // Note: If you named your variables differently in NinjaController2D 
            // (like 'stamina' instead of 'currentStamina'), just change the names below!
            stamina.RestoreFullStamina();
        }

        StartCoroutine(BornSequenceRoutine());
    }

    public void TriggerBornSequence() {
        StartCoroutine(BornSequenceRoutine());
    }

    private IEnumerator BornSequenceRoutine() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerSFX("PlayerBorn");
        NinjaController2D controller = GetComponent<NinjaController2D>();
        Animator anim = GetComponent<Animator>();

        if (controller != null) controller.isDead = true;

        if (anim != null) {
            anim.SetBool("die", false);
            anim.Play("Born");
            yield return null;
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(animLength);
        }

        if (controller != null) controller.isDead = false;
    }
}