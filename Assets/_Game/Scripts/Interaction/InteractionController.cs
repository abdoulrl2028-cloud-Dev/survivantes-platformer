using UnityEngine;
using BlackHorizon.Core;

namespace BlackHorizon.Interaction
{
    /// <summary>
    /// Player-facing interaction. Raycasts from the camera for IInteractable
    /// objects and fires Interact() when the use key is pressed. Raises events
    /// the HUD listens to for showing the prompt.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactableMask;
        [SerializeField] private Transform cameraTransform;

        private IInteractable _current;

        public event System.Action<string> OnPromptChanged;
        public event System.Action OnPromptHide;

        private void Awake()
        {
            if (interactableMask == 0) interactableMask = GameLayers.InteractableMask;
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main ? Camera.main.transform : transform;
            }
        }

        private void Update()
        {
            UpdateCurrentTarget();

            if (Input.GetKeyDown(KeyCode.E) && _current != null && _current.CanInteract)
            {
                _current.Interact(gameObject);
            }
        }

        private void UpdateCurrentTarget()
        {
            if (cameraTransform == null) return;

            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, interactionRange, interactableMask, QueryTriggerInteraction.Collide))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    if (_current != interactable)
                    {
                        _current = interactable;
                        OnPromptChanged?.Invoke(interactable.InteractionPrompt);
                    }
                    return;
                }
            }

            if (_current != null)
            {
                _current = null;
                OnPromptHide?.Invoke();
            }
        }
    }
}
