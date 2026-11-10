using UnityEngine;

namespace SkyHopper
{
    /// <summary>Spinning, bobbing collectible. Adds score when the player enters its trigger.</summary>
    public class Coin : MonoBehaviour
    {
        public int points = 10;
        public float spinSpeed = 140f;
        public float bobHeight = 0.16f;
        public float bobSpeed = 2.2f;

        Vector3 restPosition;
        float phase;

        void Start()
        {
            restPosition = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            Vector3 p = restPosition;
            p.y += Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
            transform.localPosition = p;
        }

        void OnTriggerEnter(Collider other)
        {
            PlatformerPlayer player = other.GetComponentInParent<PlatformerPlayer>();
            if (player == null) return;

            if (PlatformerGame.I != null) PlatformerGame.I.CollectCoin(points);
            Sfx.Play(Sfx.CoinClip, transform.position, 0.6f);
            Destroy(gameObject);
        }
    }
}
