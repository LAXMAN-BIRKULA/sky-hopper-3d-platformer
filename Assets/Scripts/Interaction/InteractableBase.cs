using UnityEngine;
using UnityEngine.Rendering;

namespace WheatAndCo
{
    /// <summary>
    /// Shared behaviour for world objects: self-registration with the
    /// InteractionSystem and the soft glowing ring shown on the nearest target.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        GameObject highlightRing;

        public Transform Transform => transform;
        public virtual bool CanInteract => isActiveAndEnabled;
        public abstract string Prompt { get; }
        public abstract void Interact(PlayerController player);

        /// <summary>Ring size in metres.</summary>
        public virtual float HighlightRadius => 1.15f;

        /// <summary>Height of the highlight ring above the object origin.</summary>
        protected virtual float RingY => 0.04f;

        protected virtual void Start()
        {
            GameManager.Instance?.Interaction?.Register(this);
        }

        protected virtual void OnDestroy()
        {
            GameManager.Instance?.Interaction?.Unregister(this);
        }

        public virtual void SetHighlighted(bool highlighted)
        {
            if (highlighted) EnsureRing();
            if (highlightRing != null) highlightRing.SetActive(highlighted);
        }

        void EnsureRing()
        {
            if (highlightRing != null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Materials == null) return;

            highlightRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            highlightRing.name = "HighlightRing";
            highlightRing.transform.SetParent(transform, false);
            highlightRing.transform.localPosition = new Vector3(0f, RingY, 0f);
            highlightRing.transform.localScale = new Vector3(HighlightRadius * 2f, 0.012f, HighlightRadius * 2f);
            var collider = highlightRing.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            gm.Materials.Apply(highlightRing, "HighlightRing", false, false);
            highlightRing.SetActive(false);
        }
    }
}
