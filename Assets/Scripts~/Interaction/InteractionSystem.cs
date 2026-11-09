using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    /// <summary>
    /// Finds the closest usable object, highlights it, shows its prompt and
    /// forwards the E key. Objects register themselves as they are created.
    /// </summary>
    public class InteractionSystem
    {
        readonly GameManager gm;
        readonly List<InteractableBase> all = new List<InteractableBase>();
        InteractableBase current;

        public float Range = 3.2f;

        public InteractionSystem(GameManager gameManager)
        {
            gm = gameManager;
        }

        public void Register(InteractableBase item)
        {
            if (item == null) return;
            if (!all.Contains(item)) all.Add(item);
        }

        public void Unregister(InteractableBase item)
        {
            if (item == null) return;
            all.Remove(item);
            if (current == item) current = null;
        }

        public void Update()
        {
            if (gm.Player == null || gm.UI == null) return;

            // Drop destroyed entries.
            for (int i = all.Count - 1; i >= 0; i--)
                if (all[i] == null) all.RemoveAt(i);

            if (!gm.CanPlayerAct)
            {
                SetCurrent(null);
                gm.UI.HidePrompt();
                return;
            }

            Vector3 playerPosition = gm.Player.transform.position;
            InteractableBase best = null;
            float bestDistance = Range;

            for (int i = 0; i < all.Count; i++)
            {
                var item = all[i];
                if (item == null || !item.CanInteract) continue;

                float distance = Vector3.Distance(playerPosition, item.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }

            SetCurrent(best);

            if (current == null)
            {
                gm.UI.HidePrompt();
                return;
            }

            gm.UI.ShowPrompt(current.Prompt);
            if (InputReader.InteractPressed)
                current.Interact(gm.Player);
        }

        void SetCurrent(InteractableBase next)
        {
            if (current == next) return;
            if (current != null) current.SetHighlighted(false);
            current = next;
            if (current != null) current.SetHighlighted(true);
        }
    }
}
