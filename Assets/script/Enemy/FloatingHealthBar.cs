using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : MonoBehaviour {
    [Header("UI References")]
    public Slider healthSlider;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Height above the enemy's head
    public bool hideWhenFull = true;

    private HealthSystem healthSys;
    private Transform enemyTransform;
    private Quaternion fixedRotation;

    private void Start() {
        // Find the health system on the parent enemy object
        enemyTransform = transform.parent;
        if (enemyTransform != null) {
            healthSys = enemyTransform.GetComponent<HealthSystem>();
        }

        if (healthSys != null && healthSlider != null) {
            healthSlider.maxValue = healthSys.maxHealth;
            healthSlider.value = healthSys.currentHealth;
        }

        // Lock the rotation so it never spins or flips
        fixedRotation = transform.rotation;
    }

    private void LateUpdate() {
        if (healthSys == null || healthSlider == null) return;

        // 1. Update the slider value
        healthSlider.value = healthSys.currentHealth;

        // 2. Hide if full health, show if damaged
        if (hideWhenFull) {
            healthSlider.gameObject.SetActive(healthSys.currentHealth < healthSys.maxHealth);
        }

        // 3. Prevent the 2D Flipping Bug!
        // We force the Canvas to stay upright and un-flipped, even if the parent enemy flips.
        transform.rotation = fixedRotation;

        // Ensure the scale stays positive so the bar doesn't draw backwards
        Vector3 parentScale = enemyTransform.localScale;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * Mathf.Sign(parentScale.x),
            transform.localScale.y,
            transform.localScale.z
        );
    }
}