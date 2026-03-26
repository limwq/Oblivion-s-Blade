using UnityEngine;

public class ShadowPulse : MonoBehaviour {
    [Header("Pulse Settings")]
    public float pulseSpeed = 4f;
    public float minAlpha = 0.4f;
    public float maxAlpha = 0.9f;

    [Header("Hover Settings")]
    public float hoverSpeed = 2f;
    public float hoverHeight = 0.15f; // How high it floats up and down

    private SpriteRenderer sr;
    private Color baseColor;
    private Vector3 startPos;

    private void Start() {
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;

        if (sr != null) {
            baseColor = sr.color;
        }
    }

    private void Update() {
        // 1. PULSE THE OPACITY (Fades in and out rhythmically)
        if (sr != null) {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, currentAlpha);
        }

        // 2. HOVER UP AND DOWN (Makes it feel like a floating spirit)
        float newY = startPos.y + (Mathf.Sin(Time.time * hoverSpeed) * hoverHeight);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}