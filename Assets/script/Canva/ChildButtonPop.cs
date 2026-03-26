using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChildButtonPop : MonoBehaviour {
    [Header("UI References")]
    public Button chapterButton;          // Main Button (Chapters)
    public RectTransform childPanel;      // The container for child buttons
    public CanvasGroup canvasGroup;       // To control fade/visibility

    // NEW: Reference to the RectTransform of the button we want to move/scale
    // Usually, this is the transform of the GameObject this script is attached to.
    private RectTransform mainButtonRect;

    [Header("Chapter Buttons")]
    public Button childButton1;
    public Button childButton2;

    [Header("Scene Configuration")]
    public string sceneName1 = "Level_1";
    public string sceneName2 = "Level_2";

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.4f;
    [SerializeField] private float targetHeight = 100f;

    [Header("Button Animation")]
    [Tooltip("How far left to move (Negative X)")]
    public float moveOffsetX = -50f;
    public Vector3 targetScale = new Vector3(1.75f, 1.75f, 1.75f);
    public Vector3 originalScale = new Vector3(2f, 2f, 2f);

    private bool isOpen = false;
    private Vector2 originalPosition; // To remember where we started

    void Start() {
        // Grab the RectTransform of this object (the button holder)
        mainButtonRect = GetComponent<RectTransform>();

        // Store the starting position so we can go back
        if (mainButtonRect != null) {
            originalPosition = mainButtonRect.anchoredPosition;
            mainButtonRect.localScale = originalScale; // Ensure start scale is set
        }

        // Initial Setup: Collapse the menu
        childPanel.sizeDelta = new Vector2(childPanel.sizeDelta.x, 0);
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        // Setup Buttons
        chapterButton.onClick.AddListener(ToggleChildPanel);
        childButton1.onClick.AddListener(() => LoadLevel(sceneName1));
        childButton2.onClick.AddListener(() => LoadLevel(sceneName2));
    }

    void ToggleChildPanel() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UIClick");

        // --- THE FIX: Deselect the button immediately ---
        // This forces the button back to "Normal" state so Hover works again instantly.
        EventSystem.current.SetSelectedGameObject(null);
        // -----------------------------------------------

        StopAllCoroutines();
        isOpen = !isOpen;

        // ... (The rest of your code remains the same) ...

        float endHeight = isOpen ? targetHeight : 0;
        float endAlpha = isOpen ? 1f : 0f;

        Vector3 endScale = isOpen ? targetScale : originalScale;
        Vector2 endPos = isOpen ? (originalPosition + new Vector2(moveOffsetX, 0)) : originalPosition;

        canvasGroup.blocksRaycasts = isOpen;

        StartCoroutine(AnimateUI(endHeight, endAlpha, endScale, endPos));
    }

    IEnumerator AnimateUI(float endHeight, float endAlpha, Vector3 endScale, Vector2 endPos) {
        // Start Values (Panel)
        float startHeight = childPanel.sizeDelta.y;
        float startAlpha = canvasGroup.alpha;

        // Start Values (Main Button)
        Vector3 startScale = mainButtonRect.localScale;
        Vector2 startPos = mainButtonRect.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < animationDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;

            // Smooth easing
            t = t * t * (3f - 2f * t);

            // --- ANIMATE PANEL ---
            float newHeight = Mathf.Lerp(startHeight, endHeight, t);
            childPanel.sizeDelta = new Vector2(childPanel.sizeDelta.x, newHeight);

            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
            canvasGroup.alpha = newAlpha;

            // --- ANIMATE MAIN BUTTON (Move & Scale) ---
            if (mainButtonRect != null) {
                mainButtonRect.localScale = Vector3.Lerp(startScale, endScale, t);
                mainButtonRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            }

            yield return null;
        }

        // Snap to final values to avoid floating point errors
        childPanel.sizeDelta = new Vector2(childPanel.sizeDelta.x, endHeight);
        canvasGroup.alpha = endAlpha;

        if (mainButtonRect != null) {
            mainButtonRect.localScale = endScale;
            mainButtonRect.anchoredPosition = endPos;
        }
    }

    void LoadLevel(string sceneName) {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("UIClick");

        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(sceneName);
        } else {
            SceneManager.LoadScene(sceneName);
        }
    }
}