using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // Required for Audio Mixer

public class SettingsMenu : MonoBehaviour {
    [Header("Audio References")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("UI References")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backButton;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start() {
        // Setup Back Button
        if (backButton != null) {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseOptions);
        }

        // Setup Sliders with Listeners
        // We use a delegate to pass the float value dynamically
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Optional: Load saved values (Basic PlayerPrefs)
        masterSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);
    }

    public void OpenOptions() {
        optionsPanel.SetActive(true);
    }

    public void CloseOptions() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("UIClick");

        optionsPanel.SetActive(false);

        // Save preferences when closing
        PlayerPrefs.Save();
    }

    // --- Volume Logic ---
    // Note: Mixers work in Decibels (-80 to 0). Sliders are 0 to 1.
    // Formula: Mathf.Log10(sliderValue) * 20

    public void SetMasterVolume(float value) {
        mainMixer.SetFloat(masterParam, Mathf.Log10(value) * 80);
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetMusicVolume(float value) {
        mainMixer.SetFloat(musicParam, Mathf.Log10(value) * 80);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value) {
        mainMixer.SetFloat(sfxParam, Mathf.Log10(value) * 80);
        PlayerPrefs.SetFloat("SFXVol", value);
    }
}