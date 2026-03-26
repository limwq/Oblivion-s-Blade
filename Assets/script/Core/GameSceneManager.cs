using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour {
    public static GameSceneManager Instance;

    [Header("UI References")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private Slider loadingBar;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float minLoadTime = 2.0f;

    [System.Serializable]
    public struct SceneMusic {
        public string sceneName;
        public string bgmName;
    }

    [Header("Music Playlist")]
    [Tooltip("Define which BGM plays for which Scene")]
    public List<SceneMusic> musicPlaylist;

    private void Awake() {
        if (Instance != null && Instance != this) {
            if (loadingCanvasGroup != null) loadingCanvasGroup.alpha = 0;
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize() {
        loadingCanvasGroup.alpha = 0;
        loadingCanvasGroup.blocksRaycasts = false;

        if (loadingBar != null) {
            loadingBar.gameObject.SetActive(false);
            loadingBar.value = 0;
        }
    }

    // =========================================================
    // OPTION 1: LOAD SCENE (With Bar)
    // =========================================================
    public void LoadScene(string sceneName) {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName) {
        yield return StartCoroutine(Fade(1f));

        // --- THE AUDIO NUKE ---
        // Instantly kill ALL music and SFX the exact moment the screen goes black
        if (AudioManager.Instance != null) {
            AudioManager.Instance.StopAllAudio();
        }

        if (loadingBar != null) {
            loadingBar.gameObject.SetActive(true);
            loadingBar.value = 0;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;
        while (timer < minLoadTime || operation.progress < 0.9f) {
            timer += Time.unscaledDeltaTime;
            float timeProgress = Mathf.Clamp01(timer / minLoadTime);
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);

            if (loadingBar != null) {
                loadingBar.value = Mathf.Min(timeProgress, realProgress);
            }
            yield return null;
        }

        if (loadingBar != null) loadingBar.value = 1f;
        yield return new WaitForSecondsRealtime(0.2f);

        if (loadingBar != null) loadingBar.gameObject.SetActive(false);

        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        yield return null;

        PlaySceneMusic(sceneName);

        yield return StartCoroutine(Fade(0f));
    }

    // =========================================================
    // OPTION 2: FADE SCENE (No Bar)
    // =========================================================
    public void FadeScene(string sceneName) {
        StartCoroutine(FadeSceneRoutine(sceneName));
    }

    private IEnumerator FadeSceneRoutine(string sceneName) {
        yield return StartCoroutine(Fade(1f));

        // --- THE AUDIO NUKE ---
        // Instantly kill ALL music and SFX the exact moment the screen goes black
        if (AudioManager.Instance != null) {
            AudioManager.Instance.StopAllAudio();
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f) {
            yield return null;
        }

        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        yield return null;

        PlaySceneMusic(sceneName);

        yield return StartCoroutine(Fade(0f));
    }

    // =========================================================
    // HELPER: Music Logic
    // =========================================================
    private void PlaySceneMusic(string sceneName) {
        foreach (SceneMusic track in musicPlaylist) {
            if (track.sceneName == sceneName) {
                if (AudioManager.Instance != null) {
                    AudioManager.Instance.PlayMusic(track.bgmName);
                }
                return;
            }
        }

        if (AudioManager.Instance != null) {
            AudioManager.Instance.StopMusic();
        }
    }

    // =========================================================
    // HELPER: Fader
    // =========================================================
    private IEnumerator Fade(float targetAlpha) {
        loadingCanvasGroup.blocksRaycasts = (targetAlpha > 0.1f);
        float startAlpha = loadingCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration) {
            timer += Time.unscaledDeltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        loadingCanvasGroup.alpha = targetAlpha;
    }
}