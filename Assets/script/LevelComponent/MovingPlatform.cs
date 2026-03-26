using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour {
    [Header("Movement Settings")]
    public float speed = 3f;
    public float waitTimeAtPoint = 1f;

    [Header("Platform Path (Offsets)")]
    public List<Vector2> waypoints = new List<Vector2>();

    private Vector2 startPosition;
    private int currentWaypointIndex = 0;
    private float waitTimer;
    private bool isWaiting;

    private Rigidbody2D rb;
    private Vector2 currentVelocity;
    private bool isFrozenInTime;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        startPosition = transform.position;

        if (waypoints.Count == 0) {
            waypoints.Add(Vector2.zero);
        }
    }

    // --- NEW: Time Stop Event Listeners ---
    private void OnEnable() {
        GlobalEvents.OnTimeStopStarted += FreezeInTime;
        GlobalEvents.OnTimeStopEnded += UnfreezeInTime;
    }

    private void OnDisable() {
        GlobalEvents.OnTimeStopStarted -= FreezeInTime;
        GlobalEvents.OnTimeStopEnded -= UnfreezeInTime;
    }

    private void FreezeInTime() => isFrozenInTime = true;
    private void UnfreezeInTime() => isFrozenInTime = false;

    private void FixedUpdate() {
        // If time is stopped, do not move and do not pass velocity to the player
        if (isFrozenInTime) {
            currentVelocity = Vector2.zero;
            return;
        }

        if (waypoints.Count <= 1) return;

        if (isWaiting) {
            currentVelocity = Vector2.zero;

            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0) {
                isWaiting = false;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }
            return;
        }

        MovePlatform();
    }

    private void MovePlatform() {
        Vector2 targetPosition = startPosition + waypoints[currentWaypointIndex];
        Vector2 newPosition = Vector2.MoveTowards(rb.position, targetPosition, speed * Time.fixedDeltaTime);

        currentVelocity = (newPosition - rb.position) / Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        if (Vector2.Distance(rb.position, targetPosition) < 0.05f) {
            isWaiting = true;
            waitTimer = waitTimeAtPoint;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryPassVelocity(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryPassVelocity(collision);

    private void TryPassVelocity(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            NinjaController2D player = collision.gameObject.GetComponent<NinjaController2D>();
            if (player != null) {
                player.platformVelocity = currentVelocity;
                player.isOnMovingPlatform = true; // --- NEW: Lock rolling ---
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            NinjaController2D player = collision.gameObject.GetComponent<NinjaController2D>();
            if (player != null) {
                player.platformVelocity = Vector2.zero;
                player.isOnMovingPlatform = false; // --- NEW: Unlock rolling ---
            }
        }
    }

    private void OnDrawGizmos() {
        if (waypoints != null && waypoints.Count > 0) {
            Gizmos.color = Color.cyan;
            Vector2 basePos = Application.isPlaying ? startPosition : (Vector2)transform.position;

            for (int i = 0; i < waypoints.Count; i++) {
                Vector2 p1 = basePos + waypoints[i];
                Vector2 p2 = basePos + waypoints[(i + 1) % waypoints.Count];

                Gizmos.DrawSphere(p1, 0.2f);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}