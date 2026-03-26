using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawner : MonoBehaviour {
    [Header("Spawning Settings")]
    [Tooltip("The enemy prefab you want to spawn.")]
    public GameObject enemyPrefab;
    [Tooltip("How many enemies to spawn in total.")]
    public int spawnCount = 3;
    [Tooltip("Delay between each enemy spawning.")]
    public float spawnDelay = 0.5f;

    [Header("Spawn Locations")]
    [Tooltip("Drag empty GameObjects here to act as exact spawn points. If empty, spawns at this trigger's location.")]
    public Transform[] spawnPoints;

    [Header("Visuals (Optional)")]
    [Tooltip("A smoke or magic puff particle effect to hide the instant spawning.")]
    public GameObject spawnEffectPrefab;

    // State lock so the player can't trigger this multiple times
    private bool hasTriggered = false;

    private void Awake() {
        // Ensure the collider is set to Trigger so the player can walk into it
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // 1. Check if it's the player AND we haven't spawned yet
        if (!hasTriggered && other.CompareTag("Player")) {
            hasTriggered = true;
            StartCoroutine(SpawnSequence());
        }
    }

    private IEnumerator SpawnSequence() {
        Debug.Log($"[EnemySpawner] Player entered ambush zone! Spawning {spawnCount} enemies.");

        for (int i = 0; i < spawnCount; i++) {
            // 2. Figure out WHERE to spawn this specific enemy
            Vector2 spawnPos = transform.position;

            if (spawnPoints != null && spawnPoints.Length > 0) {
                // Cycle through the spawn points (e.g., Point 1, Point 2, Point 3, then back to Point 1)
                int pointIndex = i % spawnPoints.Length;
                spawnPos = spawnPoints[pointIndex].position;
            }

            // 3. Play the spawn particle effect (Smoke bomb!)
            if (spawnEffectPrefab != null) {
                Destroy(Instantiate(spawnEffectPrefab, spawnPos, Quaternion.identity), 2f);
            }

            // Optional: Wait a tiny fraction of a second AFTER the smoke appears before the enemy pops in
            // yield return new WaitForSeconds(0.1f); 

            // 4. Actually spawn the enemy
            if (enemyPrefab != null) {
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            }

            // 5. Wait before spawning the next one
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void OnDrawGizmos() {
        // Draws a red outline in the Unity Editor so you can easily see the trigger zone
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        }

        // Draws lines connecting the trigger to the spawn points so you know what's linked
        if (spawnPoints != null) {
            Gizmos.color = Color.yellow;
            foreach (Transform point in spawnPoints) {
                if (point != null) {
                    Gizmos.DrawLine(transform.position, point.position);
                    Gizmos.DrawWireSphere(point.position, 0.3f);
                }
            }
        }
    }
}