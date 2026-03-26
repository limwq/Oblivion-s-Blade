using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    public GameObject bossPrefab; // 拖入你的 Boss Prefab
    public Transform spawnPoint;  // Boss 出現的初始高空位置

    private bool bossSpawned = false;

    private void Start() {
        bossSpawned = false;

        if (bossPrefab == null) {
            Debug.LogError("[BossTrigger] Boss Prefab is not assigned!");
        }
        if (spawnPoint == null) {
            Debug.LogError("[BossTrigger] Spawn Point is not assigned!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && bossSpawned == false)
        {
            bossSpawned = true;

            // 在上方生成 Boss
            Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);

            
        }
    }
}