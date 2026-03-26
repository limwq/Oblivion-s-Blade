using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("移動速度 (正數向左，負數向右)")]
    public float scrollSpeed = 5f;

    private float backgroundWidth;
    private Vector3 startPosition;

    void Start()
    {
        // 1. 取得圖片在世界座標中的寬度
        // 使用 bounds.size.x 可以精準取得縮放後的寬度
        backgroundWidth = GetComponent<SpriteRenderer>().bounds.size.x;

        // 2. 稍微縮小判定寬度 (例如 0.05)，消除浮點數產生的裂縫
        backgroundWidth -= 0.05f;

        // 紀錄初始位置
        startPosition = transform.position;
    }

    void Update()
    {
        // 3. 讓背景持續移動
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // 4. 關鍵邏輯：當圖片完全移出螢幕左側
        // 如果你的圖片中心點 (Pivot) 在正中央，判定條件如下：
        if (transform.position.x <= -backgroundWidth)
        {
            // 5. 瞬移到另一張圖片的後方
            // 這裡直接增加兩倍寬度，精準對齊到右側接班
            Vector3 resetPos = new Vector3(backgroundWidth * 2f, 0, 0);
            transform.position += resetPos;
        }
    }
}