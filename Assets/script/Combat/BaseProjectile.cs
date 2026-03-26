using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class BaseProjectile : MonoBehaviour {
    [Header("Base Projectile Settings")]
    public float speed = 15f;
    public float lifetime = 3f;
    public int damage = 10;

    [Tooltip("If true, Time Stop will NOT freeze this.")]
    public bool isPlayerProjectile = true;

    public LayerMask collisionLayers;

    protected Rigidbody2D rb;

    // Time stop tracking
    private bool isFrozenInTime;
    private Vector2 savedVelocity;
    private RigidbodyType2D originalBodyType;
    private float currentLifetime;

    protected virtual void Awake() {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<Collider2D>().isTrigger = true;
        originalBodyType = rb.bodyType;
    }

    protected virtual void OnEnable() {
        GlobalEvents.OnTimeStopStarted += FreezeInTime;
        GlobalEvents.OnTimeStopEnded += UnfreezeInTime;
    }

    protected virtual void OnDisable() {
        GlobalEvents.OnTimeStopStarted -= FreezeInTime;
        GlobalEvents.OnTimeStopEnded -= UnfreezeInTime;
    }

    private void FreezeInTime() {
        if (isPlayerProjectile) return; // Don't freeze our own shurikens!

        isFrozenInTime = true;
        savedVelocity = rb.velocity;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void UnfreezeInTime() {
        if (isPlayerProjectile) return;

        isFrozenInTime = false;
        rb.bodyType = originalBodyType;
        rb.velocity = savedVelocity;
    }

    protected virtual void Start() {
        currentLifetime = lifetime;
        rb.velocity = transform.right * speed;
    }

    protected virtual void Update() {
        if (isFrozenInTime) return; // Do not age or move while time is stopped

        // Manual lifetime tracking
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0) {
            Destroy(gameObject);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D hitInfo) {
        if ((collisionLayers.value & (1 << hitInfo.gameObject.layer)) > 0) {
            OnHitTarget(hitInfo);
        }
    }

    protected abstract void OnHitTarget(Collider2D hitInfo);
}