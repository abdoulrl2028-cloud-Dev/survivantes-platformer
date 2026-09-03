using System.Collections.Generic;
using UnityEngine;

namespace BlackHorizon.AI
{
    /// <summary>
    /// A squad of enemies placed together. Shares a lieutenant/leader reference
    /// and can coordinate behaviours (e.g. patrolling flowing points). Kept
    /// minimal for the MVP but structured for future group tactics.
    /// </summary>
    public class EnemyGroup : MonoBehaviour
    {
        [SerializeField] private string groupName = "Squad";
        [SerializeField] private Transform[] patrolRoute;
        [SerializeField] private bool _usesCover = false;

        private readonly List<EnemyController> _members = new List<EnemyController>();

        public string GroupName => groupName;

        public void AddMember(EnemyController enemy)
        {
            if (enemy != null && !_members.Contains(enemy)) _members.Add(enemy);
        }

        public void RemoveMember(EnemyController enemy)
        {
            _members.Remove(enemy);
        }

        public IReadOnlyList<EnemyController> Members => _members;

        public Transform[] PatrolRoute => patrolRoute;

        /// <summary>Assign the shared patrol route to all members.</summary>
        public void AssignRoutes()
        {
            foreach (var m in _members)
            {
                if (m != null) m.AssignPatrolPoints(patrolRoute);
            }
        }
    }
}
