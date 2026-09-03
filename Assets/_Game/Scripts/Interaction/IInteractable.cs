using UnityEngine;

namespace BlackHorizon.Interaction
{
    /// <summary>
    /// Generic interaction contract. Any object that can be interacted with by
    /// the player (doors, terminals, objectives, pickups) implements this.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short label shown in the interaction prompt, e.g. "Open door".</summary>
        string InteractionPrompt { get; }

        /// <summary>Whether the interaction is currently possible.</summary>
        bool CanInteract { get; }

        /// <summary>Distance at which the object can be interacted with.</summary>
        float InteractionRange { get; }

        /// <summary>Called when the player interacts with the object.</summary>
        void Interact(GameObject interactor);
    }
}
