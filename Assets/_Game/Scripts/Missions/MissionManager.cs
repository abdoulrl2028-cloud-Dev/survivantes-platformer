using System.Collections.Generic;
using UnityEngine;
using BlackHorizon.Systems;

namespace BlackHorizon.Missions
{
    /// <summary>
    /// Orchestrates the mission flow: activates objectives in order, tracks
    /// checkpoints, and respawns the player. Add one to the mission scene.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Flow")]
        [SerializeField] private MissionObjective[] objectives;
        [SerializeField] private bool autoStart = true;

        [Header("Player")]
        [SerializeField] private Transform playerTransform;

        [Header("Checkpoints")]
        [SerializeField] private List<Transform> _checkpoints = new List<Transform>();

        private int _currentObjective = -1;
        private int _checkpointIndex = -1;

        public int CurrentObjectiveIndex => _currentObjective;
        public bool IsMissionComplete { get; private set; }

        public event System.Action<int> OnObjectiveStarted;
        public event System.Action OnMissionComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (objectives == null) return;
            AssignPlayerRefs();
            if (autoStart) StartMission();
        }

        private void Update()
        {
            if (IsMissionComplete || _currentObjective < 0 || _currentObjective >= objectives.Length) return;

            var current = objectives[_currentObjective];
            if (current == null) return;
            current.Tick(Time.deltaTime);

            if (current.State == ObjectiveState.Completed)
            {
                AdvanceObjective();
            }
        }

        private void AssignPlayerRefs()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            foreach (var obj in objectives)
            {
                if (obj != null) obj.PlayerTransform = playerTransform;
            }
        }

        public void StartMission()
        {
            if (objectives == null || objectives.Length == 0) return;
            _currentObjective = -1;
            IsMissionComplete = false;
            foreach (var obj in objectives)
            {
                if (obj != null) obj.Deactivate();
            }
            AdvanceObjective();
        }

        private void AdvanceObjective()
        {
            _currentObjective++;
            if (_currentObjective >= objectives.Length)
            {
                IsMissionComplete = true;
                OnMissionComplete?.Invoke();
                EventBus.FireMissionCompleted();
                return;
            }

            var next = objectives[_currentObjective];
            next.Activate();
            OnObjectiveStarted?.Invoke(_currentObjective);
        }

        // ---- Checkpoints ----

        public void RegisterCheckpoint(Transform checkpoint)
        {
            if (!_checkpoints.Contains(checkpoint)) _checkpoints.Add(checkpoint);
        }

        public void TriggerCheckpoint(Transform checkpoint)
        {
            int index = _checkpoints.IndexOf(checkpoint);
            if (index > _checkpointIndex)
            {
                _checkpointIndex = index;
            }
        }

        public void RespawnPlayer(GameObject player)
        {
            if (player == null) return;

            var controller = player.GetComponent<BlackHorizon.Player.FirstPersonController>();
            if (_checkpointIndex >= 0 && _checkpointIndex < _checkpoints.Count)
            {
                var cp = _checkpoints[_checkpointIndex];
                if (controller != null) controller.Teleport(cp.position);
                else player.transform.position = cp.position;
            }
            else if (playerTransform != null && controller != null)
            {
                // Fallback: restart near the active objective.
                var target = objectives.Length > 0 ? objectives[0].transform : playerTransform;
                if (target != null) controller.Teleport(target.position + Vector3.back * 5f);
            }
        }

        /// <summary>Advance the flow when an objective is externally reported (e.g. enemies cleared).</summary>
        public void CompleteCurrentObjective()
        {
            if (_currentObjective >= 0 && _currentObjective < objectives.Length)
            {
                objectives[_currentObjective].Complete();
            }
        }
    }
}
