using UnityEngine;

namespace BlackHorizon.AI
{
    /// <summary>
    /// A named spot in the world an AI can take cover behind. Designed to be
    /// placed by hand near walls, vehicles, crates etc.
    /// </summary>
    public class CoverPoint : MonoBehaviour
    {
        public enum CoverType { Wall, Vehicle, Crate, Structure }

        [Tooltip("Which way cover faces (away from the threat).")]
        [SerializeField] private Vector3 coverDirection = Vector3.forward;

        [SerializeField] private CoverType type = CoverType.Wall;
        [SerializeField] private float coverHeight = 1.8f;
        [SerializeField] private float standOffset = 0.7f;

        public bool IsOccupied { get; private set; }
        public CoverType Type => type;
        public float Height => coverHeight;

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, coverHeight, 0.6f));

            Vector3 dir = (Quaternion.LookRotation(coverDirection.normalized) * Vector3.forward).normalized;
            dir.y = 0f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, dir * 1.2f);
        }

        public bool TryClaim()
        {
            if (IsOccupied) return false;
            IsOccupied = true;
            return true;
        }

        public void Release()
        {
            IsOccupied = false;
        }

        /// <summary>World position the AI should stand while in cover.</summary>
        public Vector3 GetStandPosition()
        {
            Vector3 dir = coverDirection.normalized;
            dir.y = 0f;
            return transform.position - dir * standOffset;
        }
    }
}
