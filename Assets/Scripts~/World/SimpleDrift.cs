using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Slowly spins windmill blades around their local forward axis.</summary>
    public class SimpleDrift : MonoBehaviour
    {
        public float speed = 16f;
        public Vector3 axis = Vector3.forward;

        void Update()
        {
            transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
        }
    }
}
