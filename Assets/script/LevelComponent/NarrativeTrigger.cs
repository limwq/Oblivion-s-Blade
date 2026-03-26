using UnityEngine;
using System.Collections;

public class NarrativeTrigger : MonoBehaviour {
    [Header("1. Narrative")]
    [TextArea] public string[] sentences;
    [Tooltip("How long EACH sentence stays on screen.")]
    public float durationPerSentence = 4f;

    [Header("2. Audio")]
    public string soundName;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("3. Visuals (Sprite)")]
    public Sprite spriteToRender;
    public Transform spriteSpawnPoint;
    public bool destroySpriteAfterTime = true;

    // Optional: Add a field for sorting order if you need it on top of the player
    [Tooltip("Higher numbers are drawn on top of lower numbers within the same layer")]
    public int sortingOrder = 0;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other) {
        if (!hasTriggered && other.CompareTag("Player")) {
            hasTriggered = true;
            PlaySequence();
        }
    }

    void PlaySequence() {
        // --- 1. Play Text Sequence (Coroutine) ---
        if (SubtitleSystem.Instance != null && sentences.Length > 0) {
            StartCoroutine(PlaySubtitleRoutine());
        }

        // --- 2. Play Sound (Once at start) ---
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName)) {
            AudioManager.Instance.PlaySFX(soundName);
        }

        // --- 3. Render Sprite ---
        if (spriteToRender != null && spriteSpawnPoint != null) {
            SpawnSprite();
        }
    }

    IEnumerator PlaySubtitleRoutine() {
        foreach (string line in sentences) {
            SubtitleSystem.Instance.ShowSubtitle(line, durationPerSentence);
            yield return new WaitForSeconds(durationPerSentence);
        }
    }

    void SpawnSprite() {
        GameObject visual = new GameObject("NarrativeSprite");
        visual.transform.position = spriteSpawnPoint.position;
        visual.transform.localScale = spriteSpawnPoint.localScale;

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = spriteToRender;

        // --- NEW: Set Sorting Layer to "Player" ---
        sr.sortingLayerName = "Players";
        sr.sortingOrder = sortingOrder; // Defaults to 0, or whatever you set in inspector
        // ------------------------------------------

        if (destroySpriteAfterTime) {
            float totalDuration = sentences.Length * durationPerSentence;
            Destroy(visual, totalDuration + 2f);
        }
    }
}