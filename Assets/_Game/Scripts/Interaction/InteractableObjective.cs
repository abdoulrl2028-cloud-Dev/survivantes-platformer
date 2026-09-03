using UnityEngine;
using BlackHorizon.Missions;

namespace BlackHorizon.Interaction
{
    /// <summary>
    /// An interactable prop tied to a mission Interact objective. When the
    /// player presses E on it, the linked MissionObjective advances.
    /// </summary>
    public class InteractableObjective : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Activate";
        [SerializeField] private MissionObjective linkedObjective;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private AudioSource audioSource;

        public string InteractionPrompt => prompt;
        public bool CanInteract => true;
        public float InteractionRange => interactionRange;

        public void Interact(GameObject interactor)
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
            if (linkedObjective != null)
            {
                linkedObjective.OnInteracted(interactor.transform);
            }
        }

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0.5f, interactionRange);
        }
    }
}
