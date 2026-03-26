using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HealthSystem), typeof(Animator))]
public class EnergyBomb : MonoBehaviour {
    [Header("Bomb Settings")]
    public float fuseTime = 5f;
    public float fastBlinkThreshold = 2f;  // When the fuse hits 3 seconds, switch to FAST animation!
    public float explosionRadius = 5f;
    public int explosionDamage = 40;

    [Header("VFX")]
    public GameObject explosionParticlePrefab; // Drag your particle system prefab here

    private HealthSystem healthSys;
    private Animator anim;

    private bool hasTriggered = false;
    private bool isFastBlinking = false;

    private OrbBoss bossRef;

    private void Awake() {
        healthSys = GetComponent<HealthSystem>();
        anim = GetComponent<Animator>();
    }

    private void Start() {
        // Start the slow pulsing animation immediately
        if (anim != null) anim.SetTrigger("slow");
    }

    public void Initialize(OrbBoss boss) {
        bossRef = boss;
    }

    private void Update() {
        if (hasTriggered) return;

        if (healthSys.IsDead()) {
            DefuseBomb();
            return;
        }

        fuseTime -= Time.deltaTime;

        if (fuseTime <= fastBlinkThreshold && !isFastBlinking) {
            isFastBlinking = true;
            if (anim != null) anim.SetTrigger("fast");
        }

        if (fuseTime <= 0) {
            StartCoroutine(ExplodeRoutine());
        }
    }

    private void DefuseBomb() {
        hasTriggered = true;
        Debug.Log("[Energy Bomb] Defused!");

        // NEW: Tell the boss a bomb was cleared!
        if (bossRef != null) bossRef.ReportBombDefused();

        Destroy(gameObject);
    }

    private IEnumerator ExplodeRoutine() {
        hasTriggered = true;

        // 1. Play the Animator Explode state
        if (anim != null) anim.SetTrigger("explode");

        // 2. Spawn the Particle Effect independent of the bomb
        if (explosionParticlePrefab != null) {
            Destroy(Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity),2f);
        }
        yield return new WaitForSeconds(0.5f);

        // 3. Deal Area of Effect (AoE) Damage
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hitObjects) {
            if (hit.CompareTag("Player")) {
                HealthSystem playerHealth = hit.GetComponent<HealthSystem>();
                if (playerHealth != null) {
                    playerHealth.TakeDamage(explosionDamage);
                }
            }
        }

        // 4. Massive Camera Shake!
        if (CameraSpring.Instance != null) {
            CameraSpring.Instance.Punch(Vector2.down, 15f);
        }

        // 5. Wait for the animation to finish before destroying the bomb object
        // NOTE: Adjust this "0.5f" to match the exact length of your 'explode' animation clip!
        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }

    // Draws a red circle in the Unity Editor so you can visually see the blast radius
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}