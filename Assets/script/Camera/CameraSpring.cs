using UnityEngine;
using Cinemachine;

public class CameraSpring : MonoBehaviour {
    public static CameraSpring Instance; // 1. Static Reference

    [Header("Settings")]
    public float stiffness = 250f; // Increased stiffness for snappier punch
    public float damping = 15f;
    public float mass = 1f;

    private CinemachineVirtualCamera vcam;
    private CinemachineFramingTransposer transposer;
    private Vector3 velocity;
    private Vector3 currentOffset;
    private Vector3 baseOffset; // To remember your original adjustments

    void Awake() {
        Instance = this; // 2. Assign Singleton
        vcam = GetComponent<CinemachineVirtualCamera>();
        transposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();

        // Save whatever offset you set in the Inspector manually
        if (transposer != null) baseOffset = transposer.m_TrackedObjectOffset;
    }

    void LateUpdate() {
        if (transposer == null) return;

        // Physics Calculation (Hooke's Law)
        Vector3 force = -stiffness * currentOffset;
        Vector3 acceleration = (force - damping * velocity) / mass;

        velocity += acceleration * Time.deltaTime;
        currentOffset += velocity * Time.deltaTime;

        // Apply to Cinemachine
        // We add the Spring Offset ON TOP of your Base Offset
        transposer.m_TrackedObjectOffset = baseOffset + currentOffset;
    }

    // Call this from anywhere!
    public void Punch(Vector2 direction, float force) {
        velocity += (Vector3)direction * force;
    }
}