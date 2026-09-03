using System.Collections;
using UnityEngine;
using BlackHorizon.Core;
using BlackHorizon.Systems;

namespace BlackHorizon.Weapons
{
    /// <summary>
    /// Runtime behaviour of a single equipped weapon. Reads parameters from a
    /// WeaponData ScriptableObject and performs raycast hitscan firing.
    /// Media-free: spawns VFX at impact with a pooled object when available,
    /// otherwise falls back to simple prefab instantiation (pooled later).
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData data;

        public WeaponData Data => data;
        public bool IsReloading { get; private set; }
        public int CurrentMag { get; private set; }
        public int CurrentReserve { get; private set; }

        private float _nextFireTime;
        private Transform _camera;
        private bool _aiming;

        public event System.Action OnFire;
        public event System.Action OnReloadStarted;
        public event System.Action OnReloadFinished;

        private void Awake()
        {
            CurrentMag = data ? data.magazineSize : 0;
            CurrentReserve = data ? data.reserveAmmo : 0;
        }

        public void Initialize(WeaponData weaponData, Transform fireCam)
        {
            data = weaponData;
            _camera = fireCam;
            CurrentMag = data.magazineSize;
            CurrentReserve = data.reserveAmmo;
        }

        private void Update()
        {
            if (_camera == null) _camera = Camera.main ? Camera.main.transform : transform;
        }

        public bool CanFire()
        {
            return !IsReloading && CurrentMag > 0 && Time.time >= _nextFireTime;
        }

        /// <summary>Attempt to fire. Returns true if a shot was spent.</summary>
        public bool TryFire()
        {
            if (!CanFire()) return false;

            _nextFireTime = Time.time + (1f / Mathf.Max(0.1f, data.fireRate));
            CurrentMag--;

            for (int i = 0; i < data.pellets; i++)
            {
                FireSingleShot();
            }

            OnFire?.Invoke();
            EventBus.FireShotFired(transform.position);
            return true;
        }

        private void FireSingleShot()
        {
            if (_camera == null) return;

            float spread = data.spread * (_aiming ? data.spreadAimMultiplier : 1f);
            spread *= MoveSpreadMultiplier();

            Vector3 dir = _camera.forward;
            dir += new Vector3(Random.Range(-spread, spread) * 0.01f, Random.Range(-spread, spread) * 0.01f, Random.Range(-spread, spread) * 0.01f);
            dir.Normalize();

            Ray ray = new Ray(_camera.position, dir);
            RaycastHit hit;
            float dist = data.range;

            // We want to hit damageables (Projectile/Enemy) plus environment.
            if (Physics.Raycast(ray, out hit, data.range, data.hitMask, QueryTriggerInteraction.Ignore))
            {
                dist = hit.distance;
                ResolveHit(hit.collider, ray.GetPoint(dist), hit.normal, hit.point, hit.normal);
            }
        }

        private float MoveSpreadMultiplier()
        {
            // Increase spread when the player is moving fast (run).
            var player = GetComponentInParent<BlackHorizon.Player.FirstPersonController>();
            if (player != null && player.IsRunning) return data.moveSpreadMultiplier;
            return 1f;
        }

        private void ResolveHit(Collider collider, Vector3 hitPoint, Vector3 hitNormal, Vector3 decalPoint, Vector3 decalNormal)
        {
            if (collider == null) return;

            var damageable = collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(data.damage, hitPoint, hitNormal, gameObject);
            }

            // Surface impact VFX + decal.
            ObjectPooler.Instance?.SpawnImpact(hitPoint, decalNormal, data.impactPrefab);
        }

        public void StartReload()
        {
            if (IsReloading || CurrentMag >= data.magazineSize || CurrentReserve <= 0) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            OnReloadStarted?.Invoke();
            yield return new WaitForSeconds(data.reloadTime);

            int needed = data.magazineSize - CurrentMag;
            int take = Mathf.Min(needed, CurrentReserve);
            CurrentMag += take;
            CurrentReserve -= take;
            IsReloading = false;
            OnReloadFinished?.Invoke();
        }

        public void SetAiming(bool aiming)
        {
            _aiming = aiming;
        }
    }
}
