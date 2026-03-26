using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraFocusTrigger : MonoBehaviour {
    [Header("Camera Settings")]
    public CinemachineVirtualCamera focusCamera;
    public int activePriority = 20;
    public int inactivePriority = 0; // Default Low Priority

    [Header("Timing")]
    public float focusTime = 3f;

    [Header("Player Control")]
    public bool lockPlayerControls = true;
    public bool triggerOnce = false;

    private bool hasTriggered = false;
    private NinjaController2D playerScript;

    void Start() {
        if (focusCamera != null) {
            // FIX: Do NOT disable the GameObject. Just lower priority.
            // This ensures Cinemachine 'knows' this camera exists from frame 1.
            focusCamera.Priority = inactivePriority;
            focusCamera.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            if (triggerOnce && hasTriggered) return;

            playerScript = other.GetComponent<NinjaController2D>();
            StartCoroutine(FocusRoutine());
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        // Only reset on exit if we are NOT using a timer
        if (other.CompareTag("Player") && focusTime <= 0) {
            StartCoroutine(ResetCameraRoutine());
        }
    }

    IEnumerator FocusRoutine() {
        hasTriggered = true;

        // 1. Lock Player
        if (lockPlayerControls && playerScript != null) {
            playerScript.canMove = false;
            playerScript.GetComponent<Rigidbody2D>().velocity = Vector2.zero;

            Animator anim = playerScript.GetComponent<Animator>();
            if (anim != null) {
                anim.SetBool("isRunning", false);
                anim.SetBool("isGrounded", true);
                anim.Play("Idle");
            }
        }

        // 2. Switch Camera
        if (focusCamera != null) {
            // FIX: Just boost priority.
            focusCamera.Priority = activePriority;

            // FIX: Force the Cinemachine Brain to notice the change immediately
            focusCamera.MoveToTopOfPrioritySubqueue();
        }

        // 3. Wait
        if (focusTime > 0) {
            yield return new WaitForSeconds(focusTime);
            StartCoroutine(ResetCameraRoutine());
        }
    }

    IEnumerator ResetCameraRoutine() {
        // 1. Drop Priority
        if (focusCamera != null) {
            focusCamera.Priority = inactivePriority;
        }

        // 2. Unlock Player
        if (lockPlayerControls && playerScript != null) {
            playerScript.canMove = true;
        }

        yield return null; // Logic is done, no need to wait for disable
    }
}