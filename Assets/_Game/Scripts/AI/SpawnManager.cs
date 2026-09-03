using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlackHorizon.Systems;

namespace BlackHorizon.AI
{
    /// <summary>
    /// Spawns enemies from EnemySpawnPoint definitions, respecting delays and
    /// trigger conditions. Scene singleton; enemy prefabs are instantiated and
    /// registered with any nearby EnemyGroup.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [SerializeField] private EnemySpawnPoint[] spawnPoints;
        [SerializeField] private Transform player;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly Queue<IEnumerator> _pending = new Queue<IEnumerator>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
        }

        private void Update()
        {
            if (spawnPoints == null) return;
            foreach (var sp in spawnPoints)
            {
                if (sp == null || sp.HasSpawned) continue;

                bool trigger = false;
                switch (sp.Trigger)
                {
                    case EnemySpawnPoint.TriggerType.OnStart:
                        trigger = true;
                        break;
                    case EnemySpawnPoint.TriggerType.OnDistance:
                        trigger = player != null && Vector3.Distance(player.position, sp.transform.position) <= sp.ActivationDistance;
                        break;
                    case EnemySpawnPoint.TriggerType.OnEvent:
                        // Fired by events via FireEvent trigger.
                        trigger = false;
                        break;
                }

                if (trigger)
                {
                    sp.MarkSpawned();
                    StartCoroutine(SpawnWithDelay(sp));
                }
            }
        }

        private IEnumerator SpawnWithDelay(EnemySpawnPoint sp)
        {
            if (sp.Delay > 0f) yield return new WaitForSeconds(sp.Delay);
            SpawnPoint(sp);
        }

        private void SpawnPoint(EnemySpawnPoint sp)
        {
            for (int i = 0; i < sp.Count; i++)
            {
                var enemy = Instantiate(sp.EnemyPrefab, sp.transform.position + Vector3.up, sp.transform.rotation);
                _spawned.Add(enemy);
                var group = GetComponentInParent<EnemyGroup>();
                var controller = enemy.GetComponent<EnemyController>();
                if (controller != null && group != null) group.AddMember(controller);
            }
        }

        /// <summary>Trigger all OnEvent spawn points (used by mission events).</summary>
        public void FireEventTrigger()
        {
            if (spawnPoints == null) return;
            foreach (var sp in spawnPoints)
            {
                if (sp != null && !sp.HasSpawned && sp.Trigger == EnemySpawnPoint.TriggerType.OnEvent)
                {
                    sp.MarkSpawned();
                    StartCoroutine(SpawnWithDelay(sp));
                }
            }
        }

        public int AliveEnemies()
        {
            _spawned.RemoveAll(e => e == null);
            return _spawned.Count;
        }
    }
}
