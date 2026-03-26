using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class ArenaTrap : MonoBehaviour {
    [Header("Arena Barriers")]
    [Tooltip("Drag the Laser GameObjects or Doors that will trap the player here.")]
    public GameObject[] barriers;

    [Header("Arena Enemies")]
    [Tooltip("Drag the HealthSystem components of the enemies inside this room here.")]
    public List<HealthSystem> enemiesToKill = new List<HealthSystem>();

    private bool isActivated = false;
    private bool isCleared = false;

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;

        // Ensure doors/lasers are turned off when the level starts
        SetBarriersState(false);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        // Only trigger if it hasn't been activated yet, and it's the Player
        if (!isActivated && !isCleared && collision.CompareTag("Player")) {
            ActivateArena();
        }
    }

    private void ActivateArena() {
        isActivated = true;
        SetBarriersState(true); // Lock the player in!
        Debug.Log("Arena Trap Activated! Survive!");
    }

    private void Update() {
        // Only monitor the enemies if the arena is currently active
        if (isActivated && !isCleared) {
            // Clean the list: Remove enemies that are destroyed OR marked as dead
            enemiesToKill.RemoveAll(enemy => enemy == null || enemy.IsDead());

            // If the list is completely empty, the player won!
            if (enemiesToKill.Count == 0) {
                ClearArena();
            }
        }
    }

    private void ClearArena() {
        isCleared = true;
        SetBarriersState(false); // Open the doors!
        Debug.Log("Arena Cleared!");
    }

    private void SetBarriersState(bool state) {
        foreach (GameObject barrier in barriers) {
            if (barrier != null) barrier.SetActive(state);
        }
    }
}