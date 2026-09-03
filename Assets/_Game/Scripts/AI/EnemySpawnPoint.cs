using UnityEngine;

namespace BlackHorizon.AI
{
    /// <summary>
    /// Simple marker describing where a group of enemies spawns and under
    /// what condition. Placed in the scene; SpawnManager reads them.
    /// </summary>
    public class EnemySpawnPoint : MonoBehaviour
    {
        public enum TriggerType { OnStart, OnEvent, OnDistance }

        [Tooltip("Prefab to spawn. Must have EnemyController + NavMeshAgent + Health.")]
        [SerializeField] private GameObject enemyPrefab;

        [Tooltip("How many of this enemy to spawn.")]
        [SerializeField] private int count = 1;

        [Tooltip("Delay (seconds) after trigger before spawning.")]
        [SerializeField] private float delay = 0f;

        [Tooltip("What triggers the spawn.")]
        [SerializeField] private TriggerType triggerType = TriggerType.OnStart;

        [Tooltip("If OnDistance: distance from which the spawn activates.")]
        [SerializeField] private float activationDistance = 40f;

        public GameObject EnemyPrefab => enemyPrefab;
        public int Count => count;
        public float Delay => delay;
        public TriggerType Trigger => triggerType;
        public float ActivationDistance => activationDistance;
        public bool HasSpawned { get; private set; }

        public void MarkSpawned() => HasSpawned = true;

        public void ResetSpawn() => HasSpawned = false;

        private void OnDrawGizmos()
        {
            Gizmos.color = HasSpawned ? Color.gray : Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 1f));
            if (triggerType == TriggerType.OnDistance)
            {
                Gizmos.DrawWireSphere(transform.position, activationDistance);
            }
        }
    }
}
