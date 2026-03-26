using UnityEngine;
using System.Collections;

public class TimedPlatform : MonoBehaviour {
    public float visibleTime = 3f;

    private Coroutine timerCoroutine;
    private bool isTimeStopped = false; // Tracks the global time state

    // --- 1. SUBSCRIBE TO TIME STOP EVENTS ---
    private void OnEnable() {
        GlobalEvents.OnTimeStopStarted += FreezeInTime;
        GlobalEvents.OnTimeStopEnded += UnfreezeInTime;
    }

    private void OnDisable() {
        GlobalEvents.OnTimeStopStarted -= FreezeInTime;
        GlobalEvents.OnTimeStopEnded -= UnfreezeInTime;
    }

    private void FreezeInTime() {
        isTimeStopped = true;
    }

    private void UnfreezeInTime() {
        isTimeStopped = false;
    }

    // --- 2. ACTIVATION LOGIC ---
    public void ActivatePlatform() {
        gameObject.SetActive(true);

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(HideAfterTime());
    }

    // --- 3. THE PAUSABLE TIMER ---
    IEnumerator HideAfterTime() {
        float timeLeft = visibleTime;

        // Keep looping until the timer runs out
        while (timeLeft > 0f) {

            // Only count down if Time Stop is NOT active
            if (!isTimeStopped) {
                timeLeft -= Time.deltaTime;
            }

            // Wait for the exact next frame before checking again
            yield return null;
        }

        // Timer hit zero, hide the platform
        gameObject.SetActive(false);
    }
}