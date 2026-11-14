using UnityEngine;

namespace SkyHopper
{
    /// <summary>
    /// Climbable zone. While the player is inside the trigger he can move up and down
    /// with forward/back input, or by holding Space (which climbs up).
    /// When he climbs past <see cref="exitHeight"/> he steps off in
    /// <see cref="exitDirection"/> onto the platform at the top.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Ladder : MonoBehaviour
    {
        [Tooltip("World Y where the climb ends and the player steps off onto the top platform. 0 disables the auto step-off.")]
        public float exitHeight = 0f;

        [Tooltip("Horizontal direction the player steps off in when he reaches exitHeight.")]
        public Vector3 exitDirection = Vector3.forward;

        void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player == null) return;
            // Standing on the platform at the top: don't re-grab the ladder through the trigger.
            if (exitHeight > 0f && player.IsGrounded &&
                player.transform.position.y > exitHeight - 0.2f) return;
            player.EnterLadder(this);
        }

        void OnTriggerExit(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player != null) player.ExitLadder(this);
        }
    }
}
