using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Smooth follow/orbit camera. Hold right mouse to look, scroll to zoom.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public bool InputEnabled = true;
        public Transform Target;

        public float distance = 6.8f;
        public float minDistance = 3.2f;
        public float maxDistance = 13f;
        public float yaw;
        public float pitch = 17f;
        public float followSmooth = 0.075f;
        public float lookSpeed = 0.22f;

        Vector3 followVelocity;
        bool snapped;

        public void Initialize(Transform target)
        {
            Target = target;
            Snap();
        }

        public void Snap()
        {
            if (Target == null) return;
            Vector3 pivot = Target.position + Vector3.up * 1.45f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = pivot - rotation * Vector3.forward * distance;
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
            followVelocity = Vector3.zero;
            snapped = true;
        }

        void LateUpdate()
        {
            if (Target == null) return;

            if (InputEnabled)
            {
                if (InputReader.LookHeld)
                {
                    Vector2 look = InputReader.LookDelta;
                    yaw += look.x * lookSpeed;
                    pitch = Mathf.Clamp(pitch - look.y * lookSpeed, 4f, 58f);
                }

                float scroll = InputReader.Scroll;
                if (Mathf.Abs(scroll) > 0.01f)
                    distance = Mathf.Clamp(distance - scroll * 0.8f, minDistance, maxDistance);
            }

            Vector3 pivot = Target.position + Vector3.up * 1.45f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = -(rotation * Vector3.forward);
            Vector3 desired = pivot + direction * distance;

            // Keep the camera out of buildings and trees.
            var hits = Physics.SphereCastAll(pivot, 0.28f, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            float closest = distance;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                if (hit.collider.transform == Target || hit.collider.transform.IsChildOf(Target)) continue;
                if (hit.distance < closest) closest = hit.distance;
            }
            if (closest < distance) desired = pivot + direction * Mathf.Max(1.3f, closest - 0.22f);

            if (!snapped)
            {
                transform.position = desired;
                snapped = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, followSmooth);
            }

            Vector3 lookTarget = pivot;
            Quaternion lookRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 14f * Time.unscaledDeltaTime);
        }
    }
}
