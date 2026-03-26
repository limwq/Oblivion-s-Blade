using System;

public static class GlobalEvents {
    public static event Action OnEnemyKilled;
    public static event Action OnTimeStopStarted;
    public static event Action OnTimeStopEnded;

    // --- NEW: Player Death Event ---
    public static event Action OnPlayerDied;

    public static void TriggerEnemyKilled() => OnEnemyKilled?.Invoke();
    public static void TriggerTimeStopStarted() => OnTimeStopStarted?.Invoke();
    public static void TriggerTimeStopEnded() => OnTimeStopEnded?.Invoke();

    public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();
}