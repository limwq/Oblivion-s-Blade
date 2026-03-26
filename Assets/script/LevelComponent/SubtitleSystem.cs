using UnityEngine;
using TMPro; // Standard for Unity Text
using System.Collections;

public class SubtitleSystem : MonoBehaviour {
    public static SubtitleSystem Instance;

    [Header("UI References")]
    [Tooltip("Assign the TextMeshPro UI Object here.")]
    public TextMeshProUGUI subtitleText;
    [Tooltip("Assign the CanvasGroup on the text object (for fading).")]
    public CanvasGroup textCanvasGroup;

    [Header("Settings")]
    public float fadeSpeed = 2f;

    private Coroutine currentRoutine;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep between scenes
        } else {
            Destroy(gameObject);
        }

        // Hide text at start
        if (textCanvasGroup != null) {
            textCanvasGroup.alpha = 0f;
        }
    }

    public void ShowSubtitle(string text, float duration) {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SubtitleRoutine(text, duration));
    }

    private IEnumerator SubtitleRoutine(string text, float duration) {
        // 1. Set Text
        subtitleText.text = text;

        // 2. Fade In
        while (textCanvasGroup.alpha < 1f) {
            textCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // 3. Wait
        yield return new WaitForSeconds(duration);

        // 4. Fade Out
        while (textCanvasGroup.alpha > 0f) {
            textCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }
}