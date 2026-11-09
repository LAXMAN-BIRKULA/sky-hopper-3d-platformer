using UnityEngine;

namespace WheatAndCo
{
    /// <summary>The farm shop stall. Interacting opens the stock and selling panel.</summary>
    public class MarketStall : InteractableBase
    {
        GameManager gm;

        public void Initialize(GameManager gameManager)
        {
            gm = gameManager;
        }

        public override string Prompt => "Open the Farm Shop";
        public override bool CanInteract => base.CanInteract && gm != null && gm.UI != null;
        public override float HighlightRadius => 1.9f;
        protected override float RingY => 0.12f;

        public override void Interact(PlayerController player)
        {
            gm.Audio.Play(Sfx.Click);
            gm.UI.OpenShop();
        }
    }
}
