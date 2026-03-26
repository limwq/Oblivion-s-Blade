using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class LaserTrap : MonoBehaviour {
    [Header("Damage Settings")]
    public int damagePerTick = 15;
    public float damageTickRate = 0.5f;

    [Header("Targeting")]
    public LayerMask targetLayers;

    private float damageTimer = 0f; // Changed to a countdown timer
    private bool isFrozenInTime = false;

    private List<HealthSystem> targetsInside = new List<HealthSystem>();

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // --- NEW: Time Stop Event Listeners ---
    private void OnEnable() {
        GlobalEvents.OnTimeStopStarted += FreezeInTime;
        GlobalEvents.OnTimeStopEnded += UnfreezeInTime;
    }

    private void OnDisable() {
        GlobalEvents.OnTimeStopStarted -= FreezeInTime;
        GlobalEvents.OnTimeStopEnded -= UnfreezeInTime;
        targetsInside.Clear();
    }

    private void FreezeInTime() => isFrozenInTime = true;
    private void UnfreezeInTime() => isFrozenInTime = false;

    private void Update() {
        if (isFrozenInTime) return; // Laser stops ticking damage!

        if (targetsInside.Count == 0) return;

        targetsInside.RemoveAll(t => t == null || t.IsDead());

        if (damageTimer > 0) damageTimer -= Time.deltaTime;

        if (damageTimer <= 0 && targetsInside.Count > 0) {
            foreach (HealthSystem target in targetsInside) {
                target.TakeDamage(damagePerTick);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Lazer");
                Debug.Log($"Laser zapped {target.gameObject.name} for {damagePerTick} damage!");
            }

            damageTimer = damageTickRate; // Reset countdown
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if ((targetLayers.value & (1 << collision.gameObject.layer)) > 0) {
            HealthSystem targetHealth = collision.GetComponent<HealthSystem>();
            if (targetHealth != null && !targetsInside.Contains(targetHealth)) {
                targetsInside.Add(targetHealth);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if ((targetLayers.value & (1 << collision.gameObject.layer)) > 0) {
            HealthSystem targetHealth = collision.GetComponent<HealthSystem>();
            if (targetHealth != null && targetsInside.Contains(targetHealth)) {
                targetsInside.Remove(targetHealth);
            }
        }
    }
}