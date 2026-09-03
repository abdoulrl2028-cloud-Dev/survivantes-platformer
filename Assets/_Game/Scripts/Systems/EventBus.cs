using System;
using UnityEngine;

namespace BlackHorizon.Systems
{
    /// <summary>
    /// Simple static event aggregator. Keeps systems decoupled without heavy
    /// manager references. All events are fired on the main thread.
    /// </summary>
    public static class EventBus
    {
        public static event Action<bool> GamePaused;
        public static event Action PlayerDied;
        public static event Action<Vector3> OnShotFired;          // world position of a gunshot (for AI hearing)
        public static event Action<Vector3> OnFootstep;           // world position of a footstep
        public static event Action<string> ObjectiveUpdated;      // formatted objective text
        public static event Action MissionCompleted;
        public static event Action MissionFailed;

        public static void PauseGame(bool paused) => GamePaused?.Invoke(paused);
        public static void FirePlayerDied() => PlayerDied?.Invoke();
        public static void FireShotFired(Vector3 worldPos) => OnShotFired?.Invoke(worldPos);
        public static void FireFootstep(Vector3 worldPos) => OnFootstep?.Invoke(worldPos);
        public static void FireObjective(string text) => ObjectiveUpdated?.Invoke(text);
        public static void FireMissionCompleted() => MissionCompleted?.Invoke();
        public static void FireMissionFailed() => MissionFailed?.Invoke();
    }
}
