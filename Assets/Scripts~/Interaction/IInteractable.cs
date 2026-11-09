using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Anything the player can walk up to and use with E.</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        Transform Transform { get; }
        void Interact(PlayerController player);
        void SetHighlighted(bool highlighted);
    }
}
