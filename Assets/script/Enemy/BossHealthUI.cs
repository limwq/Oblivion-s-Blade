using UnityEngine;
using UnityEngine.UI;
using TMPro; // --- NEW: Required for TextMeshPro ---

public class BossHealthUI : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("The parent object holding the Boss UI so we can hide/show it.")]
    public GameObject bossHealthContainer;
    public Slider bossSlider;

    [Header("TextMeshPro References")]
    public TextMeshProUGUI bossNameText; // --- UPDATED ---
    public TextMeshProUGUI quoteText;    // --- UPDATED ---

    private HealthSystem currentBossHealth;

    private void Start() {
        // Make sure it is hidden when the level starts
        if (bossHealthContainer != null) {
            bossHealthContainer.SetActive(false);
        }
    }

    // --- ANY BOSS CAN CALL THIS TO ACTIVATE THE BAR ---
    public void ActivateBossUI(HealthSystem bossHealth, string bossName, string quote) {
        currentBossHealth = bossHealth;

        if (bossNameText != null) {
            bossNameText.text = bossName;
        }
        if (quoteText != null) {
            quoteText.text = $"\"{quote}\""; // Adds automatic quotation marks around the string!
        }

        if (bossSlider != null && currentBossHealth != null) {
            bossSlider.maxValue = currentBossHealth.maxHealth;
            bossSlider.value = currentBossHealth.currentHealth;
        }

        if (bossHealthContainer != null) {
            bossHealthContainer.SetActive(true);
        }

        Debug.Log($"[Boss UI] Health bar activated for {bossName}");
    }

    private void Update() {
        if (currentBossHealth == null || bossHealthContainer == null) return;

        if (bossSlider != null) {
            bossSlider.value = currentBossHealth.currentHealth;
        }

        if (currentBossHealth.IsDead()) {
            bossHealthContainer.SetActive(false);
            currentBossHealth = null;
        }
    }
}