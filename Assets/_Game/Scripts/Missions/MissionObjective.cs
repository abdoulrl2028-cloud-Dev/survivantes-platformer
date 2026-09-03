using UnityEngine;
using BlackHorizon.Interaction;
using BlackHorizon.Systems;

namespace BlackHorizon.Missions
{
    /// <summary>
    /// A single step in a mission. Tracks its own completion; the manager
    /// advances the flow when this reports Complete().
    /// </summary>
    public class MissionObjective : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private ObjectiveType type = ObjectiveType.ReachLocation;
        [SerializeField] private string title = "Objective";
        [SerializeField] private string description = "";

        [Header("Reach / Extract")]
        [SerializeField] private float requiredDistance = 6f;

        [Header("Eliminate / Secure / Collect / Investigate")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private int requiredCount = 1;

        [Header("Interact")]
        [SerializeField] private MonoBehaviour interactableTarget;

        public ObjectiveType Type => type;
        public string Title => title;
        public string Description => description;
        public ObjectiveState State { get; private set; } = ObjectiveState.Inactive;
        public int Progress { get; private set; }

        public event System.Action<MissionObjective> OnCompleted;

        /// <summary>Reference to the player, set by the manager.</summary>
        public Transform PlayerTransform { get; set; }

        public void Activate()
        {
            State = ObjectiveState.Active;
            Progress = 0;
            EventBus.FireObjective(BuildText());
        }

        public void Deactivate()
        {
            State = ObjectiveState.Inactive;
        }

        public void Complete()
        {
            if (State == ObjectiveState.Completed) return;
            State = ObjectiveState.Completed;
            OnCompleted?.Invoke(this);
        }

        public void ProgressTo(int increment = 1)
        {
            if (State != ObjectiveState.Active) return;
            Progress += increment;
            EventBus.FireObjective(BuildText());
        }

        /// <summary>Called every frame by the manager for alive objectives.</summary>
        public void Tick(float deltaTime)
        {
            if (State != ObjectiveState.Active) return;

            switch (type)
            {
                case ObjectiveType.ReachLocation:
                case ObjectiveType.ReachExtraction:
                    TickReach();
                    break;
            }
        }

        private void TickReach()
        {
            if (PlayerTransform == null) return;
            if (Vector3.Distance(PlayerTransform.position, transform.position) <= requiredDistance)
            {
                Complete();
            }
        }

        /// <summary>Called when an interactable target is used.</summary>
        public void OnInteracted(Transform interactor)
        {
            if (State != ObjectiveState.Active) return;
            if (type == ObjectiveType.Interact && interactableTarget != null)
            {
                ComputeInteractMatch(interactor);
            }
        }

        private void ComputeInteractMatch(Transform interactor)
        {
            // TODO: verify the interacted object == interactableTarget
            ProgressTo(1);
            if (Progress >= requiredCount) Complete();
        }

        private string BuildText()
        {
            switch (type)
            {
                case ObjectiveType.ReachLocation:
                    return $"{title}: Reach the marked location.";
                case ObjectiveType.ReachExtraction:
                    return $"{title}: Reach the extraction point.";
                case ObjectiveType.EliminateEnemies:
                    return $"{title}: Eliminate hostiles ({Progress}/{requiredCount}).";
                case ObjectiveType.Interact:
                    return $"{title}: Interact with {description}.";
                case ObjectiveType.SecureArea:
                    return $"{title}: Secure the area.";
                case ObjectiveType.Investigate:
                    return $"{title}: Investigate {description}.";
                case ObjectiveType.Collect:
                    return $"{title}: Collect {description} ({Progress}/{requiredCount}).";
                default:
                    return title;
            }
        }
    }
}
