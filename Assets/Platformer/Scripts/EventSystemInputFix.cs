using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SkyHopper
{
    /// <summary>
    /// Re-registers the input modules after a scene change and repairs missing UI actions.
    /// The event system only scans its modules while the scene is being enabled, and on a
    /// scene-to-scene load that scan can still target the outgoing event system, leaving the
    /// new one with no active module (no clicks). Some scenes also reference an actions asset
    /// that is no longer in the project, so the UI module falls back to no actions at all and
    /// every menu button appears dead. Assigning the default actions at runtime fixes both.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public class EventSystemInputFix : MonoBehaviour
    {
        void Start()
        {
            EventSystem system = GetComponent<EventSystem>();
            if (system != null) system.UpdateModules();

            InputSystemUIInputModule ui = FindAnyObjectByType<InputSystemUIInputModule>();
            if (ui == null) return;

            bool missing = ui.actionsAsset == null || ui.point == null || ui.leftClick == null ||
                           ui.submit == null || ui.cancel == null || ui.move == null;
            if (missing) ui.AssignDefaultActions();
        }
    }
}
