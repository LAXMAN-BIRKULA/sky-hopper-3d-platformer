using System.Collections.Generic;
using UnityEngine;

namespace SkyHopper
{
    /// <summary>
    /// Climbable zone. Walking into it (or holding Space near it) grabs the ladder and
    /// climbs up with any movement key; S climbs down. When the player climbs past
    /// <see cref="exitHeight"/> he steps off in <see cref="exitDirection"/> onto the
    /// platform at the top.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Ladder : MonoBehaviour
    {
        [Tooltip("World Y where the climb ends and the player steps off onto the top platform. 0 disables the auto step-off.")]
        public float exitHeight = 0f;

        [Tooltip("Horizontal direction the player steps off in when he reaches exitHeight.")]
        public Vector3 exitDirection = Vector3.forward;

        static readonly List<Ladder> ladders = new List<Ladder>();
        Collider zone;

        void Awake()
        {
            zone = GetComponent<Collider>();
            if (zone != null) zone.isTrigger = true;
        }

        void OnEnable()
        {
            if (!ladders.Contains(this)) ladders.Add(this);
        }

        void OnDisable()
        {
            ladders.Remove(this);
        }

        /// <summary>True when this world position is inside the climb zone (with a little grace).</summary>
        public bool Contains(Vector3 worldPosition)
        {
            if (zone == null) return false;
            Bounds bounds = zone.bounds;
            bounds.Expand(0.3f);
            return bounds.Contains(worldPosition);
        }

        /// <summary>
        /// Horizontal point the player climbs along: the ladder's centre line, shifted to the
        /// approach side (away from <see cref="exitDirection"/>) so the body clears the
        /// platform the ladder is mounted on.
        /// </summary>
        public Vector3 ClimbLine(Vector3 from, float clearance)
        {
            if (zone == null) return from;

            Bounds bounds = zone.bounds;
            Vector3 away = exitDirection;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            away.Normalize();

            Vector3 point = bounds.center - away * Mathf.Max(0f, clearance);
            point.y = from.y;
            return point;
        }

        /// <summary>True when the player at this position can grab this ladder.</summary>
        public bool CanGrab(Vector3 worldPosition, bool grounded)
        {
            if (!Contains(worldPosition)) return false;
            // Standing on the platform at the top: don't grab the ladder again.
            if (exitHeight > 0f && grounded && worldPosition.y > exitHeight - 0.25f) return false;
            return true;
        }

        /// <summary>First ladder the player could grab at this position, or null.</summary>
        public static Ladder FindFor(Vector3 worldPosition, bool grounded)
        {
            for (int i = 0; i < ladders.Count; i++)
            {
                Ladder ladder = ladders[i];
                if (ladder != null && ladder.isActiveAndEnabled && ladder.CanGrab(worldPosition, grounded))
                    return ladder;
            }
            return null;
        }

        void OnTriggerEnter(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player == null) return;
            if (!CanGrab(player.transform.position, player.IsGrounded)) return;
            player.EnterLadder(this);
        }

        void OnTriggerExit(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player != null) player.ExitLadder(this);
        }
    }
}
