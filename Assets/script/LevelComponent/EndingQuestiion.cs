using UnityEngine;

public class EndingQuestiion : MonoBehaviour
{
    public GameObject worldCanvas; // 指向物件上方的 World Space Canvas

    void Start()
    {
        worldCanvas.SetActive(false); // 預設隱藏
    }

    void OnTriggerEnter2D(Collider2D other)
    {
		Debug.Log("Trigger Enter: " + other.name);
        if (other.CompareTag("Player"))
        {
            worldCanvas.SetActive(true); // 玩家進入 → 顯示
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            worldCanvas.SetActive(false); // 玩家離開 → 隱藏
        }
    }
}

