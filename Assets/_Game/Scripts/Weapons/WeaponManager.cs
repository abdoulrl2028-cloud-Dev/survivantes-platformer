using UnityEngine;
using BlackHorizon.Core;
using BlackHorizon.Player;
using BlackHorizon.Systems;

namespace BlackHorizon.Weapons
{
    /// <summary>
    /// Player weapon-handling controller. Reads fire / aim / reload / switch
    /// input and drives the equipped Weapon + camera recoil + HUD events.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController))]
    public class WeaponManager : MonoBehaviour
    {
        [Header("Starting Loadout")]
        [SerializeField] private WeaponData[] startingLoadout;
        [SerializeField] private int startIndex = 0;

        private FirstPersonController _controller;
        private PlayerCamera _cameraRig;
        private Weapon[] _weapons = new Weapon[0];
        private int _currentIndex = -1;

        public Weapon CurrentWeapon => (_currentIndex >= 0 && _currentIndex < _weapons.Length) ? _weapons[_currentIndex] : null;
        public int CurrentIndex => _currentIndex;

        public event System.Action<int> OnWeaponSwitched;
        public event System.Action OnReloadStarted;
        public event System.Action OnReloadFinished;
        public event System.Action OnAmmoChanged;

        private void Awake()
        {
            _controller = GetComponent<FirstPersonController>();
            _cameraRig = GetComponent<PlayerCamera>();
        }

        private void Start()
        {
            BuildWeapons();
        }

        private void BuildWeapons()
        {
            _weapons = new Weapon[startingLoadout.Length];
            for (int i = 0; i < startingLoadout.Length; i++)
            {
                if (startingLoadout[i] == null) continue;
                var holder = new GameObject("Weapon_" + startingLoadout[i].weaponName);
                holder.transform.SetParent(transform, false);
                var weapon = holder.AddComponent<Weapon>();
                weapon.Initialize(startingLoadout[i], _cameraRig ? _cameraRig.Camera.transform : Camera.main.transform);
                if (startingLoadout[i].viewModelPrefab != null)
                {
                    Instantiate(startingLoadout[i].viewModelPrefab, holder.transform);
                }
                _weapons[i] = weapon;
            }
            if (_weapons.Length > 0)
            {
                SwitchTo(startIndex);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && (GameManager.Instance.IsPaused || !GameManager.Instance.IsGameplayActive))
                return;

            if (CurrentWeapon == null) return;

            HandleSwitching();
            HandleAim();
            HandleFire();
            HandleReload();
        }

        private void HandleSwitching()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTo(2);

            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll > 0.05f) SwitchTo((_currentIndex + 1) % _weapons.Length);
            else if (scroll < -0.05f) SwitchTo((_currentIndex - 1 + _weapons.Length) % _weapons.Length);
        }

        private void SwitchTo(int index)
        {
            if (index < 0 || index >= _weapons.Length) return;
            _currentIndex = index;
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] != null)
                    _weapons[i].gameObject.SetActive(i == index);
            }
            OnWeaponSwitched?.Invoke(index);
            OnAmmoChanged?.Invoke();
        }

        private void HandleAim()
        {
            bool aiming = Input.GetMouseButton(1);
            CurrentWeapon.SetAiming(aiming);
            if (_cameraRig != null) _cameraRig.SetAiming(aiming);
        }

        private void HandleFire()
        {
            if (Input.GetButton("Fire1"))
            {
                bool automatic = CurrentWeapon.Data && CurrentWeapon.Data.automatic;
                if (automatic || Input.GetButtonDown("Fire1"))
                {
                    if (CurrentWeapon.TryFire())
                    {
                        ApplyRecoilAndMuzzle();
                    }
                }
            }
        }

        private void ApplyRecoilAndMuzzle()
        {
            if (_cameraRig != null)
            {
                _cameraRig.ApplyRecoil();
                _cameraRig.AddImpact(0.2f);
            }
            // Aim-pressed recoil extra handled by camera rig.
            OnAmmoChanged?.Invoke();
        }

        private void HandleReload()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                var w = CurrentWeapon;
                w.OnReloadStarted += () => OnReloadStarted?.Invoke();
                w.OnReloadFinished += () =>
                {
                    OnReloadFinished?.Invoke();
                    OnAmmoChanged?.Invoke();
                };
                w.StartReload();
            }
        }

        /// <summary>Enable the player to fire/aim. Called by the mission or boot flow.</summary>
        public void SetActive(bool active) => enabled = active;
    }
}
