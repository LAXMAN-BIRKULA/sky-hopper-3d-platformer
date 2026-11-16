using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyHopper
{
    /// <summary>Third-person orbit/follow camera with mouse look, zoom and wall avoidance.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 6.5f;
        public float minDistance = 2.5f;
        public float maxDistance = 11f;
        public float pivotHeight = 1.45f;
        public float yaw = 0f;
        public float pitch = 24f;
        public float minPitch = 4f;
        public float maxPitch = 68f;
        public float sensitivity = 0.12f;
        public float followSharpness = 14f;
        public float collisionRadius = 0.28f;
        public bool lockCursorOnStart = true;

        Vector3 smoothedPivot;

        void Start()
        {
            if (target != null) smoothedPivot = target.position + Vector3.up * pivotHeight;
            if (lockCursorOnStart && !MobileInput.Active) Cursor.lockState = CursorLockMode.Locked;
        }

        void LateUpdate()
        {
            if (target == null) return;

            // On-screen controls replace the mouse: dragging the right side of the screen orbits.
            if (MobileInput.Active)
            {
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;

                Vector2 touchLook = MobileInput.LookDelta;
                MobileInput.LookDelta = Vector2.zero;
                if (touchLook.sqrMagnitude > 0f)
                {
                    yaw += touchLook.x * sensitivity;
                    pitch = Mathf.Clamp(pitch - touchLook.y * sensitivity, minPitch, maxPitch);
                }

                UpdateTransform();
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * sensitivity;
                    pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    distance = Mathf.Clamp(distance - scroll * 0.01f, minDistance, maxDistance);

                if (mouse.leftButton.wasPressedThisFrame &&
                    Cursor.lockState != CursorLockMode.Locked && !PlatformerGame.IsDead && !PlatformerGame.IsWon)
                    Cursor.lockState = CursorLockMode.Locked;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;

            UpdateTransform();
        }

        /// <summary>Move the camera to the orbit position, pulling in when geometry blocks the view.</summary>
        void UpdateTransform()
        {
            Vector3 targetPivot = target.position + Vector3.up * pivotHeight;
            smoothedPivot = Vector3.Lerp(smoothedPivot, targetPivot,
                1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            float allowedDistance = distance;

            // Pull the camera in when level geometry is between the pivot and the desired position.
            RaycastHit[] hits = Physics.SphereCastAll(smoothedPivot, collisionRadius, direction,
                distance, ~0, QueryTriggerInteraction.Ignore);
            float closest = distance;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(target)) continue;
                if (hit.distance <= 0.01f) continue;
                if (hit.distance < closest) closest = hit.distance;
            }
            allowedDistance = Mathf.Clamp(closest - 0.1f, minDistance * 0.5f, distance);

            transform.position = smoothedPivot + direction * allowedDistance;
            transform.rotation = rotation;
        }
    }
}
