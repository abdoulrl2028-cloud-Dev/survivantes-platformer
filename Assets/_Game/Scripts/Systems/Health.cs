using System;
using UnityEngine;

namespace BlackHorizon.Core
{
    /// <summary>
    /// A health value attached to a damageable object. Emits events for health
    /// changes, damage taken and death so UI / AI / audio can react.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Tooltip("Maximum hit points.")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Automatically destroy the GameObject on death.")]
        [SerializeField] private bool destroyOnDeath = true;

        [Tooltip("Seconds to wait before destroying the corpse on death.")]
        [SerializeField] private float destroyDelay = 0f;

        private float _current;

        public event Action<float, float> OnHealthChanged;   // (current, max)
        public event Action<DamageInfo> OnDamaged;
        public event Action<GameObject> OnDied;              // (killer)

        public float CurrentHealth => _current;
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }

        public struct DamageInfo
        {
            public float Amount;
            public Vector3 HitPoint;
            public Vector3 HitNormal;
            public GameObject Source;
        }

        private void Awake()
        {
            _current = maxHealth;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject source)
        {
            if (IsDead || amount <= 0f) return;

            _current = Mathf.Max(0f, _current - amount);
            OnHealthChanged?.Invoke(_current, maxHealth);
            OnDamaged?.Invoke(new DamageInfo { Amount = amount, HitPoint = hitPoint, HitNormal = hitNormal, Source = source });

            if (_current <= 0f)
            {
                Die(source);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _current = Mathf.Min(maxHealth, _current + amount);
            OnHealthChanged?.Invoke(_current, maxHealth);
        }

        /// <summary>Editor/build-time setter so tools can configure health.</summary>
        public void SetForInspector(float max)
        {
            maxHealth = max;
            _current = max;
        }

        public void Kill(GameObject source)
        {
            TakeDamage(_current + 1f, transform.position, Vector3.up, source);
        }

        private void Die(GameObject source)
        {
            IsDead = true;
            OnDied?.Invoke(source);
            if (destroyOnDeath)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _current = maxHealth;
        }
#endif
    }
}
