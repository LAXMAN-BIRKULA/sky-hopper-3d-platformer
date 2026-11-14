using UnityEngine;

namespace SkyHopper
{
    /// <summary>
    /// Climbable zone. While the player is inside the trigger he can move up and down
    /// with forward/back input; pressing jump detaches from the ladder.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Ladder : MonoBehaviour
    {
        void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player != null) player.EnterLadder(this);
        }

        void OnTriggerExit(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player != null) player.ExitLadder(this);
        }
    }
}
