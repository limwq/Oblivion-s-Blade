using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour {
    [Header("Targeting")]
    [Tooltip("Layers heavy enough to press the button (e.g., Player, Shadow)")]
    public LayerMask triggerLayers;

    [Header("Events")]
    public UnityEvent onPlatePressed;
    public UnityEvent onPlateReleased;

    [Header("Visuals (Optional)")]
    public Transform buttonTop;
    public float pressedYOffset = -0.1f;

    private List<Collider2D> objectsOnPlate = new List<Collider2D>();
    private Vector3 originalTopPos;

    // --- NEW: Time and State Tracking ---
    private bool isFrozenInTime = false;
    private bool isCurrentlyPressed = false;

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
        if (buttonTop != null) originalTopPos = buttonTop.localPosition;
    }

    // --- NEW: Time Stop Event Listeners ---
    private void OnEnable() {
        GlobalEvents.OnTimeStopStarted += FreezeInTime;
        GlobalEvents.OnTimeStopEnded += UnfreezeInTime;
    }

    private void OnDisable() {
        GlobalEvents.OnTimeStopStarted -= FreezeInTime;
        GlobalEvents.OnTimeStopEnded -= UnfreezeInTime;
    }

    private void FreezeInTime() => isFrozenInTime = true;

    private void UnfreezeInTime() {
        isFrozenInTime = false;
        // The exact frame time unfreezes, check if we need to snap up or down!
        EvaluatePlateState();
    }

    private void Update() {
        if (objectsOnPlate.Count > 0) {
            // Clean up destroyed objects (like expired Shadows)
            bool removedAny = objectsOnPlate.RemoveAll(col => col == null || !col.gameObject.activeInHierarchy) > 0;

            if (removedAny) {
                EvaluatePlateState();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if ((triggerLayers.value & (1 << collision.gameObject.layer)) > 0) {
            if (!objectsOnPlate.Contains(collision)) {
                objectsOnPlate.Add(collision);
                EvaluatePlateState();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (objectsOnPlate.Contains(collision)) {
            objectsOnPlate.Remove(collision);
            EvaluatePlateState();
        }
    }

    // --- NEW: Master State Evaluator ---
    private void EvaluatePlateState() {
        // If time is stopped, the plate is physically locked. Do nothing.
        if (isFrozenInTime) return;

        bool shouldBePressed = objectsOnPlate.Count > 0;

        // If it should be pressed, but currently isn't, push it down
        if (shouldBePressed && !isCurrentlyPressed) {
            isCurrentlyPressed = true;
            if (buttonTop != null) buttonTop.localPosition = originalTopPos + new Vector3(0, pressedYOffset, 0);
            onPlatePressed?.Invoke();
        }
        // If it should be released, but currently is pushed down, pop it up
        else if (!shouldBePressed && isCurrentlyPressed) {
            isCurrentlyPressed = false;
            if (buttonTop != null) buttonTop.localPosition = originalTopPos;
            onPlateReleased?.Invoke();
        }
    }
}