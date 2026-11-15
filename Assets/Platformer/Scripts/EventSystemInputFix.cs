using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyHopper
{
    /// <summary>
    /// Re-registers the input modules after a scene change.
    /// The event system only scans its modules while the scene is being enabled, and on a
    /// scene-to-scene load that scan can still target the outgoing event system, leaving the
    /// new one with no active module (no clicks). Running the scan again once the new scene
    /// is awake fixes that.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public class EventSystemInputFix : MonoBehaviour
    {
        void Start()
        {
            EventSystem system = GetComponent<EventSystem>();
            if (system != null) system.UpdateModules();
        }
    }
}
