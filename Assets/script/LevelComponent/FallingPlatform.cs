using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class FallingPlatform : MonoBehaviour {
    [Header("Falling Debris Settings")]
    public float fallSpeed = 5f;
    public float initialDelay = 0f;    // Use this to offset multiple platforms so they don't all fall at exactly the same time

    [Header("Respawn Triggers")]
    public LayerMask groundLayer;      // The layer that causes it to shatter/respawn
    public float mapBottomY = -20f;    // The abyss threshold

    private Rigidbody2D rb;
    private Vector3 startPos;
    private bool isFrozen = false;
    private Vector2 standardVelocity;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        standardVelocity = new Vector2(0, -fallSpeed);
    }

    private void OnEnable() {
        GlobalEvents.OnTimeStopStarted += FreezeInPlace;
        GlobalEvents.OnTimeStopEnded += Unfreeze;
    }

    private void OnDisable() {
        GlobalEvents.OnTimeStopStarted -= FreezeInPlace;
        GlobalEvents.OnTimeStopEnded -= Unfreeze;
    }

    private void Start() {
        // Start the endless loop of falling
        if (initialDelay > 0) {
            StartCoroutine(DelayedStartRoutine());
        } else {
            rb.velocity = standardVelocity;
        }
    }

    private IEnumerator DelayedStartRoutine() {
        // Keeps it hovering at the top invisibly until its delay is up
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(initialDelay);

        if (!isFrozen) {
            rb.velocity = standardVelocity;
        }
    }

    private void Update() {
        // FAILSAFE: If it falls into the abyss, respawn it instantly
        if (transform.position.y < mapBottomY) {
            ResetPlatform();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        // IMPACT: If it hits the ground, respawn it instantly
        if (((1 << collision.gameObject.layer) & groundLayer) != 0) {
            Debug.Log("[FallingPlatform] Hit the ground! Respawning...");
            // Optional: You could spawn a small dust/shatter particle effect here before it teleports back up!
            ResetPlatform();
        }
    }

    // --- TIME STOP LOGIC ---
    private void FreezeInPlace() {
        isFrozen = true;
        rb.velocity = Vector2.zero;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.5f, 0.8f, 1f); // Turn icy blue
    }

    private void Unfreeze() {
        isFrozen = false;
        rb.velocity = standardVelocity; // Resume falling

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white; // Restore color
    }

    private void ResetPlatform() {
        // Teleport back to the ceiling
        transform.position = startPos;

        // If Time Stop is currently active when it respawns, it should stay frozen at the top.
        // Otherwise, it immediately continues falling.
        if (!isFrozen) {
            rb.velocity = standardVelocity;
        } else {
            rb.velocity = Vector2.zero;
        }
    }
}