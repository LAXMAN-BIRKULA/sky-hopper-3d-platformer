using UnityEngine;

namespace SkyHopper
{
    /// <summary>Springy pad that launches the player upward when he lands on it.</summary>
    [RequireComponent(typeof(Collider))]
    public class Bouncer : MonoBehaviour
    {
        [Header("Bounce")]
        public float launchSpeed = 16f;
        public float squashAmount = 0.35f;
        public float squashRecovery = 5f;

        Vector3 baseScale;
        float squash;

        void Awake()
        {
            baseScale = transform.localScale;
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void Update()
        {
            squash = Mathf.Max(0f, squash - Time.deltaTime * squashRecovery);
            float s = Mathf.Sin(squash * Mathf.PI * 0.5f) * squashAmount;
            transform.localScale = new Vector3(
                baseScale.x * (1f + s * 0.5f),
                baseScale.y * (1f - s),
                baseScale.z * (1f + s * 0.5f));
        }

        void OnTriggerEnter(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player == null) return;

            // Only bounce on the way down, so you do not get stuck in a loop of bounces.
            if (player.VerticalVelocity > 0.5f) return;

            player.Launch(launchSpeed);
            squash = 1f;
        }
    }
}
