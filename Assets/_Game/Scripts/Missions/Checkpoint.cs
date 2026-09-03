using UnityEngine;
using BlackHorizon.Missions;

namespace BlackHorizon.Missions
{
    /// <summary>
    /// A checkpoint the player activates by walking through. On death the
    /// player respawns at the most recent checkpoint.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointName = "Checkpoint";
        [SerializeField] private bool isActiveByDefault = true;

        public string CheckpointName => checkpointName;

        private void Start()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.RegisterCheckpoint(transform);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.TriggerCheckpoint(transform);
            }
        }
    }
}
