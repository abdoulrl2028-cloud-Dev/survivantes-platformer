using UnityEngine;

namespace BlackHorizon.Weapons
{
    /// <summary>
    /// Static weapon parameters kept as a ScriptableObject so designers can
    /// create many weapon variants without code changes. All values are
    /// original/fictional and tuned for a military sandbox feel.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Black Horizon/Weapons/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Assault Rifle";
        [TextArea] public string description = "";

        [Header("Damage")]
        public float damage = 34f;
        public float range = 100f;

        [Header("Fire")]
        public float fireRate = 9f;                 // shots per second
        public bool automatic = true;
        public float spread = 0.5f;                 // base spread in degrees
        public float spreadAimMultiplier = 0.4f;
        public int pellets = 1;

        [Header("Ammo")]
        public int magazineSize = 30;
        public int reserveAmmo = 120;
        public float reloadTime = 2.2f;

        [Header("Recoil")]
        public float recoilForce = 0.6f;            // camera kick
        public Vector2 recoilKick = new Vector2(0.25f, 0.8f); // horizontal, vertical
        public float recoilRecovery = 6f;

        [Header("Movement")]
        public float moveSpreadMultiplier = 1.6f;
        public float aimSpeed = 12f;

        [Header("Aiming")]
        public float hipToAimFov = 15f;

        [Header("Visual")]
        public GameObject viewModelPrefab;
        public Vector3 aimPosition = new Vector3(0f, -0.08f, 0.25f);
        public Vector3 hipPosition = new Vector3(0.18f, -0.18f, 0.35f);

        [Header("Muzzle")]
        public Transform muzzlePoint;
        public ParticleSystem muzzleFlashPrefab;
        public GameObject impactPrefab;
        public GameObject decalPrefab;

        [Header("Layers")]
        public LayerMask hitMask = ~0;
        public float bulletForce = 10f;
    }
}
