using UnityEngine;

public class AfterimageFade : MonoBehaviour {
    [Header("Fade Settings")]
    [Tooltip("How fast the ghost vanishes (Alpha reduction per second)")]
    public float fadeSpeed = 3f;
    [Tooltip("The initial color/opacity. Set it to a dark shadow color!")]
    public Color ghostColor = new Color(0.1f, 0.1f, 0.1f, 0.6f); // Dark, semi-transparent grey

    private SpriteRenderer sr;
    private float alpha;

    private void Awake() {
        sr = GetComponent<SpriteRenderer>();
        alpha = ghostColor.a;
    }

    // --- NEW: A helper method to initialize the ghost ---
    // It copies the look of the boss at the exact moment it spawns.
    public void Initialize(Sprite bossSprite, Vector3 position, Vector3 scale, Color colorOveride) {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        sr.sprite = bossSprite;
        transform.position = position;
        transform.localScale = scale;

        // Use the override if provided (e.g., if you want them red for Time Stop)
        // Otherwise, use the standard shadow color
        if (colorOveride != Color.clear) ghostColor = colorOveride;
        sr.color = ghostColor;
        alpha = ghostColor.a;
    }

    private void Update() {
        // Fade out over time
        alpha -= fadeSpeed * Time.deltaTime;

        if (sr != null) {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        // Destroy when fully invisible
        if (alpha <= 0) {
            Destroy(gameObject);
        }
    }
}