using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource), typeof(Collider2D))]
public class BoxSoundController : MonoBehaviour {
    [Header("Settings")]
    [Tooltip("How fast it must move sideways to make drag noise.")]
    public float minSpeed = 0.1f;
    [Tooltip("Layers that count as 'Ground' (so it doesn't play while falling).")]
    public LayerMask groundLayer;

    [Header("Landing Logic")] // --- NEW SECTION ---
    [Tooltip("How fast it must hit the ground to play the thud.")]
    public float minFallVelocity = 2f;
    public AudioClip landingClip;      // Drag your 'BoxLand' sound here
    [Range(0f, 1f)] public float landingVolume = 1f;

    [Header("Audio Tuning (Drag)")]
    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Collider2D col;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        col = GetComponent<Collider2D>();

        // Force correct settings
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.Stop();
    }

    void Update() {
        // 1. Check if moving sideways
        bool isMovingHorizontally = Mathf.Abs(rb.velocity.x) > minSpeed;

        // 2. Check if grounded (Raycast down from center)
        float distToEdge = col.bounds.extents.y;
        bool isGrounded = Physics2D.Raycast(transform.position, Vector2.down, distToEdge + 0.1f, groundLayer);

        // 3. Dragging Sound Logic
        if (isMovingHorizontally && isGrounded) {
            if (!audioSource.isPlaying) {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.Play();
            }
        } else {
            if (audioSource.isPlaying) {
                audioSource.Stop();
            }
        }
    }

    // --- NEW: Detect Impact for Landing Sound ---
    private void OnCollisionEnter2D(Collision2D collision) {
        // 1. Check if we hit the Ground Layer
        // (Bitwise check is the fastest way to check LayerMasks)
        if ((groundLayer.value & (1 << collision.gameObject.layer)) > 0) {

            // 2. Check if the impact was hard enough
            // relativeVelocity gives us the speed of the collision impact
            if (collision.relativeVelocity.magnitude >= minFallVelocity) {

                // 3. Optional: Check if we hit the Floor (not a wall)
                // If the normal.y is positive, we hit something below us.
                if (collision.contacts[0].normal.y > 0.5f) {
                    PlayLandingSound();
                }
            }
        }
    }

    void PlayLandingSound() {
        if (landingClip != null) {
            AudioSource.PlayClipAtPoint(landingClip, transform.position, landingVolume);

            Debug.Log("Box Landed!");
        }
    }
}