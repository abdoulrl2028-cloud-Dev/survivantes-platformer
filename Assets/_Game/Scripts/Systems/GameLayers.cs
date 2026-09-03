using UnityEngine;

namespace BlackHorizon.Core
{
    /// <summary>
    /// Centralized layer names and masks for the whole game.
    /// Keep layer indices in sync with ProjectSettings/TagManager.asset.
    /// </summary>
    public static class GameLayers
    {
        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";
        public const string EnvironmentName = "Environment";
        public const string InteractableName = "Interactable";
        public const string ProjectileName = "Projectile";
        public const string WeaponName = "Weapon";

        public const int Player = 6;
        public const int Enemy = 7;
        public const int Environment = 8;
        public const int Interactable = 9;
        public const int Projectile = 10;
        public const int Weapon = 11;

        public static readonly int PlayerMask = 1 << Player;
        public static readonly int EnemyMask = 1 << Enemy;
        public static readonly int EnvironmentMask = 1 << Environment;
        public static readonly int InteractableMask = 1 << Interactable;
        public static readonly int ProjectileMask = 1 << Projectile;
        public static readonly int WeaponMask = 1 << Weapon;

        /// <summary>Anything the player's gun hits (damageable + world).</summary>
        public static readonly int ShootMask = EnemyMask | EnvironmentMask | InteractableMask;

        /// <summary>Anything that blocks line of sight / vision.</summary>
        public static readonly int LineOfSightMask = EnvironmentMask | PlayerMask | InteractableMask;
    }
}
