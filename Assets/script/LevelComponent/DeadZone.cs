using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DeadZone : MonoBehaviour {
    [Tooltip("Amount of damage to deal to anything that falls in here.")]
    public int damageAmount = 9999;

    [Tooltip("Check this TRUE for the bottom of the map to delete falling bullets. Leave FALSE for Crushers!")]
    public bool destroyNonHealthObjects = false;

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        HealthSystem health = collision.GetComponent<HealthSystem>();

        if (health != null && !health.IsDead()) {
            health.TakeDamage(damageAmount);
            Debug.Log($"[DeadZone] Destroyed {collision.gameObject.name}.");
        } else if (destroyNonHealthObjects) {
            // SAFETY CHECK: Never destroy static environment objects (like Tilemaps)
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Static) return;

            Destroy(collision.gameObject);
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}