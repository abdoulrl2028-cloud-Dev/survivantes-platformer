using UnityEngine;

namespace BlackHorizon.AI
{
    /// <summary>
    /// Perception data: vision cone, hearing radius and LOS blocking layers.
    /// Used by the enemy to detect the player through a shared function.
    /// </summary>
    [CreateAssetMenu(fileName = "PerceptionConfig", menuName = "Black Horizon/AI/Perception Config")]
    public class PerceptionConfig : ScriptableObject
    {
        [Header("Vision")]
        public float sightRange = 30f;
        [Range(0f, 180f)] public float fieldOfView = 90f;
        public LayerMask lineOfSightMask;
        public float detectSpeed = 8f;          // how fast awareness fills while visible

        [Header("Hearing")]
        public float hearingRange = 20f;        // gunshot / loud noise hearing radius
        public float hearingDelay = 0.6f;

        [Header("Search")]
        public float searchDuration = 6f;
        public float searchRadius = 10f;

        [Header("Combat")]
        public float attackRange = 30f;
        public float attackCooldown = 0.9f;
        public float loseSightTime = 3.5f;      // seconds before enemy gives up combat -> search

        [Header("Movement")]
        public float patrolSpeed = 2.5f;
        public float combatSpeed = 5.5f;
        public float turnSpeed = 540f;
    }
}
