using UnityEngine;
using System.Collections;

public class SimpleRockSpawner : MonoBehaviour {
    [Header("預製體")]
    public GameObject rockPrefab; 

    [Header("生成區域設定")]
    public float spawnWidth = 20f;  // 水平隨機範圍
    public float spawnInterval = 0.3f; // 生成間隔（秒）

    private bool isSpawning = false;

    private void Start() {
        StartSpawning();
    }

    public void StartSpawning() {
        isSpawning = true;
        StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning() {
        isSpawning = false;
    }

    IEnumerator SpawnRoutine() {
        while (isSpawning) {
            SpawnRock();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnRock() {
        // 在 spawnWidth 範圍內計算隨機 X 座標
        float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
        Vector3 spawnPos = transform.position + new Vector3(randomX, 0, 0);

        if (rockPrefab != null) {
            Instantiate(rockPrefab, spawnPos, Quaternion.identity);
        }
    }

    // 在 Scene 視窗畫出一條線，方便你對齊生成位置
    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position - new Vector3(spawnWidth / 2f, 0, 0), 
                        transform.position + new Vector3(spawnWidth / 2f, 0, 0));
    }
}