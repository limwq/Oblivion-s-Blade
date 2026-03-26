using UnityEngine;

public class DoorController : MonoBehaviour {
    [Header("Door Settings")]
    public Transform doorVisual;
    public Vector3 openOffset = new Vector3(0, 3, 0); // Slides up
    public float speed = 5f;

    [Header("Audio")]
    public string doorMoveSound = "DoorSlide";

    // Internal State
    private Vector3 closedPos;
    private Vector3 targetPos;

    private Vector3 closedScale; // Store original size
    private Vector3 targetScale;

    private bool isOpen = false;

    void Start() {
        if (doorVisual == null) doorVisual = transform;

        // Remember where we started (Position & Scale)
        closedPos = doorVisual.localPosition;
        closedScale = doorVisual.localScale;

        // Start closed
        targetPos = closedPos;
        targetScale = closedScale;
    }

    void Update() {
        // 1. Animate Position (Slide)
        doorVisual.localPosition = Vector3.Lerp(doorVisual.localPosition, targetPos, Time.deltaTime * speed);

        // 2. Animate Scale (Shrink/Grow)
        doorVisual.localScale = Vector3.Lerp(doorVisual.localScale, targetScale, Time.deltaTime * speed);
    }

    // --- PUBLIC METHODS ---

    public void OpenDoor() {
        if (!isOpen) { // Check to prevent spamming sound
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(doorMoveSound);
            Debug.Log("Door Opening Sound Played");
        }
        isOpen = true;
        targetPos = closedPos + openOffset;
        targetScale = Vector3.zero; // Shrink to nothing
    }

    public void CloseDoor() {
        if (isOpen) {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(doorMoveSound);
            Debug.Log("Door Closing Sound Played");
        }
        isOpen = false;
        targetPos = closedPos;
        targetScale = closedScale; // Grow back to normal
    }

    public void ToggleDoor() {
        if (isOpen) CloseDoor();
        else OpenDoor();
    }
}