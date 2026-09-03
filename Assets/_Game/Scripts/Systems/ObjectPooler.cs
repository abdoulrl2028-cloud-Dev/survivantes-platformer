using System.Collections.Generic;
using UnityEngine;

namespace BlackHorizon.Systems
{
    /// <summary>
    /// Minimal cheap object pooler for recurring short-lived effects (impacts,
    /// muzzle flashes, decals). Reduces GC and instantiation cost at runtime.
    /// A single pool per prefab is cached lazily.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        private readonly Dictionary<GameObject, Stack<GameObject>> _pools = new Dictionary<GameObject, Stack<GameObject>>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            GameObject go = GetFromPool(prefab);
            if (go == null)
            {
                go = Instantiate(prefab, position, rotation);
            }
            else
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
                go.SetActive(true);
            }
            return go;
        }

        public void Despawn(GameObject go, GameObject prefab)
        {
            if (go == null) return;
            go.SetActive(false);
            if (prefab != null && _pools.TryGetValue(prefab, out var stack)) stack.Push(go);
            else Destroy(go);
        }

        public void SpawnImpact(Vector3 position, Vector3 normal, GameObject impactPrefab)
        {
            if (impactPrefab == null) return;
            Spawn(impactPrefab, position + normal * 0.01f, Quaternion.LookRotation(normal));
        }

        private GameObject GetFromPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
            return null;
        }

        public void PreWarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            if (!_pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[prefab] = stack;
            }
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(prefab);
                go.SetActive(false);
                stack.Push(go);
            }
        }
    }
}
