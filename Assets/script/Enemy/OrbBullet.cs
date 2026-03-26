using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] // Ensures we don't forget the Rigidbody!
public class OrbBullet : MonoBehaviour {
    public float speed = 8f;
    public int damage = 15;
    public float lifetime = 2f;

    private Rigidbody2D rb;

    void Start() {
        rb = GetComponent<Rigidbody2D>();

        // Shoot forward immediately based on the rotation the boss spawned it at
        rb.velocity = transform.up * speed;

        Destroy(gameObject, lifetime);
    }

    // Notice we removed Update() entirely! The Rigidbody handles the movement now.

    private void OnTriggerEnter2D(Collider2D collision) {
        // Damage the player
        if (collision.CompareTag("Player")) {
            HealthSystem hp = collision.GetComponent<HealthSystem>();
            if (hp != null) hp.TakeDamage(damage);

            Destroy(gameObject);
        }

        // Destroy the bullet if it hits a wall/floor
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("LevelComponent")) {
            Destroy(gameObject);
        }
    }
}