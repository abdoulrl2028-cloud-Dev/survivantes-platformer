using UnityEngine;
using BlackHorizon.Core;
using BlackHorizon.Missions;
using BlackHorizon.Systems;
using BlackHorizon.Weapons;

namespace BlackHorizon.Player
{
    /// <summary>
    /// Player health/damage and death handling. Respawns at the last activated
    /// checkpoint via the MissionManager on death.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(FirstPersonController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 1.5f;

        private Health _health;
        public Health HealthComponent => _health;

        public event System.Action<float, float> OnHealthUIChanged;
        public event System.Action OnPlayerDeath;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;
        }

        private void HandleHealthChanged(float current, float max)
        {
            OnHealthUIChanged?.Invoke(current, max);
        }

        private void HandleDamaged(Health.DamageInfo info)
        {
            // Small screen shake feedback.
            var cam = GetComponent<PlayerCamera>();
            if (cam != null) cam.AddImpact(0.4f);
        }

        private void HandleDied(GameObject source)
        {
            OnPlayerDeath?.Invoke();
            EventBus.FirePlayerDied();
            // Disable controls.
            GetComponent<FirstPersonController>().enabled = false;
            GetComponent<WeaponManager>().SetActive(false);
            Invoke(nameof(RespawnAtCheckpoint), respawnDelay);
        }

        private void RespawnAtCheckpoint()
        {
            var mission = MissionManager.Instance;
            if (mission != null)
            {
                mission.RespawnPlayer(gameObject);
            }
            // Restore health.
            GetComponent<Health>().Heal(GetComponent<Health>().MaxHealth);
            GetComponent<FirstPersonController>().enabled = true;
        }
    }
}
