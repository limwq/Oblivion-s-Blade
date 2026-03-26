using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;

    void OnEnable()
    {
        originalPos = transform.localPosition;
    }

    // 外部呼叫這個函數來啟動搖晃
    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines(); // 避免多次搖晃疊加衝突
        StartCoroutine(ProcessShake(duration, magnitude));
    }

    System.Collections.IEnumerator ProcessShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 在原位置附近隨機偏移
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 搖晃結束後回到原位
        transform.localPosition = originalPos;
    }
}