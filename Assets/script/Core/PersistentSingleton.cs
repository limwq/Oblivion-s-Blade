using UnityEngine;

// T allows any script to inherit from this (e.g., class AudioManager : PersistentSingleton<AudioManager>)
public class PersistentSingleton<T> : MonoBehaviour where T : Component {
    public static T Instance { get; private set; }

    protected virtual void Awake() {
        if (Instance == null) {
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
            Initialize();
        } else {
            // If a duplicate exists (like when reloading the menu), destroy this one.
            Destroy(gameObject);
        }
    }

    protected virtual void Initialize() { }
}