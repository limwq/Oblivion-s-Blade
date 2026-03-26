using UnityEngine;

public class BackgroundFallingRock : MonoBehaviour {
    [Header("掉落設定")]
    public float fallSpeed = 5f;        // 下掉速度
    public float rotationSpeed = 100f;  // 旋轉速度

    private void Start() {
        // 隨機初始旋轉角度，讓每顆石頭掉下來的姿勢不一樣
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
        
        // 隨機旋轉方向（左旋或右旋）
        if (Random.value > 0.5f) rotationSpeed *= -1f;
        
        // 隨機縮放，增加多樣性
        float randomScale = Random.Range(0.5f, 1.2f);
        transform.localScale = new Vector3(randomScale, randomScale, 1f);
    }

    private void Update() {
        // 1. 向下移動 (不使用物理系統以節省效能)
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        // 2. 自轉
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }

    // 當石頭完全離開攝影機畫面時自動銷毀
    private void OnBecameInvisible() {
        Destroy(gameObject);
    }
}