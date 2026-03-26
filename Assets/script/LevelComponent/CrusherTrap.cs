using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class CrusherTrap : MonoBehaviour {
    [Header("Movement Settings")]
    public float smashSpeed = 15f;
    public float retractSpeed = 3f;

    [Header("Timing Settings")]
    public float waitAtTopTime = 2f;
    public float waitAtBottomTime = 1f;

    [Header("Waypoint Settings")]
    public Vector2 smashOffset = new Vector2(0, -4f);

    [Header("Lethality & Audio")]
    public GameObject killZone;
    [Tooltip("Drag your heavy smash audio file here.")]
    public AudioClip smashSound;

    private Vector2 topPosition;
    private Vector2 bottomPosition;
    private Rigidbody2D rb;
    private AudioSource audioSource; // --- NEW: Local Audio Source ---
    private bool isFrozenInTime = false;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Automatically grab or add an AudioSource so you don't forget it in the Inspector
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        topPosition = transform.position;
        bottomPosition = topPosition + smashOffset;

        if (killZone != null) killZone.SetActive(false);
    }

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

    private void Start() {
        StartCoroutine(CrusherRoutine());
    }

    private IEnumerator CrusherRoutine() {
        while (true) {
            // 1. Wait at home
            yield return StartCoroutine(PauseableWait(waitAtTopTime));

            // 2. Smash to waypoint
            while (Vector2.Distance(rb.position, bottomPosition) > 0.05f) {
                if (!isFrozenInTime) {
                    Vector2 newPos = Vector2.MoveTowards(rb.position, bottomPosition, smashSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(newPos);
                }
                yield return new WaitForFixedUpdate();
            }

            if (!isFrozenInTime) rb.position = bottomPosition;

            // Activate KillZone
            if (killZone != null) killZone.SetActive(true);

            // --- NEW: Play the local spatial audio! ---
            if (audioSource != null && smashSound != null && !isFrozenInTime) {
                audioSource.PlayOneShot(smashSound);
            }

            // 3. Wait at destination
            yield return StartCoroutine(PauseableWait(waitAtBottomTime));

            // Disarm KillZone
            if (killZone != null) killZone.SetActive(false);

            // 4. Retract back to home
            while (Vector2.Distance(rb.position, topPosition) > 0.05f) {
                if (!isFrozenInTime) {
                    Vector2 newPos = Vector2.MoveTowards(rb.position, topPosition, retractSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(newPos);
                }
                yield return new WaitForFixedUpdate();
            }
            if (!isFrozenInTime) rb.position = topPosition;
        }
    }

    private IEnumerator PauseableWait(float duration) {
        float timer = 0f;
        while (timer < duration) {
            if (!isFrozenInTime) {
                timer += Time.deltaTime;
            }
            yield return null;
        }
    }

    private void OnDrawGizmos() {
        Vector2 startPos = Application.isPlaying ? topPosition : (Vector2)transform.position;
        Vector2 endPos = startPos + smashOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(startPos, endPos);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(endPos, GetComponent<Collider2D>() != null ? GetComponent<Collider2D>().bounds.size : Vector3.one);
    }
}