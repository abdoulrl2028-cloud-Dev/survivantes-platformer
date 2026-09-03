using UnityEngine;

namespace BlackHorizon.Core
{
    /// <summary>
    /// Anything that can take damage and react to it (player, enemies, props).
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject source);
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
    }
}
