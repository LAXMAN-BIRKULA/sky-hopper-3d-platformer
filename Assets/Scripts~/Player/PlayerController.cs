using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Camera-relative third-person farmer movement with procedural animation.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public bool InputEnabled = true;

        [Header("Movement")]
        public float walkSpeed = 4.6f;
        public float sprintSpeed = 7.6f;
        public float jumpHeight = 1.05f;
        public float gravity = -22f;
        public float turnSpeed = 14f;

        CharacterController controller;
        Transform cameraTransform;
        Transform model;
        Transform legL, legR, armL, armR, head;
        float verticalVelocity;
        float bobPhase;
        float footTimer;
        Vector3 modelRestPosition;
        Quaternion modelRestRotation;

        public CharacterController Controller => controller;
        public Vector3 Velocity { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            model = transform.Find("Model");
            if (model != null)
            {
                modelRestPosition = model.localPosition;
                modelRestRotation = model.localRotation;
                legL = model.Find("LegL");
                legR = model.Find("LegR");
                armL = model.Find("ArmL");
                armR = model.Find("ArmR");
                head = model.Find("Head");
            }
        }

        public void SetCamera(Transform cam)
        {
            cameraTransform = cam;
        }

        void Update()
        {
            if (!InputEnabled)
            {
                Animate(Vector2.zero, 0f);
                return;
            }

            if (controller == null) return;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

            Vector2 input = InputReader.Move;
            Vector3 forward = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : Vector3.forward;
            Vector3 right = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized
                : Vector3.right;

            Vector3 direction = forward * input.y + right * input.x;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            float speed = InputReader.SprintHeld ? sprintSpeed : walkSpeed;
            if (controller.isGrounded && InputReader.JumpPressed)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = direction * speed + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);
            Velocity = direction * speed;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            Animate(input, direction.magnitude * speed);

            if (direction.sqrMagnitude > 0.01f && controller.isGrounded)
            {
                footTimer -= Time.deltaTime * (InputReader.SprintHeld ? 1.5f : 1f);
                if (footTimer <= 0f)
                {
                    footTimer = 0.42f;
                    GameManager.Instance?.Audio?.Play(Sfx.Step, 0.22f);
                }
            }
        }

        void Animate(Vector2 input, float speed)
        {
            if (model == null) return;

            float moving = Mathf.Clamp01(input.magnitude);
            bobPhase += Time.deltaTime * Mathf.Lerp(2f, 11f, moving);

            float bob = Mathf.Abs(Mathf.Sin(bobPhase)) * 0.055f * moving;
            model.localPosition = modelRestPosition + new Vector3(0f, bob, 0f);
            model.localRotation = modelRestRotation * Quaternion.Euler(0f, Mathf.Sin(bobPhase * 0.5f) * 2f * moving, 0f);

            float swing = Mathf.Sin(bobPhase) * 27f * moving;
            if (legL != null) legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (armL != null) armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, 8f);
            if (armR != null) armR.localRotation = Quaternion.Euler(swing * 0.8f, 0f, -8f);
            if (head != null) head.localRotation = Quaternion.Euler(0f, Mathf.Sin(bobPhase * 0.35f) * 4f * moving, 0f);
        }

        /// <summary>Teleport helper that respects the CharacterController.</summary>
        public void Teleport(Vector3 position)
        {
            if (controller != null) controller.enabled = false;
            transform.position = position;
            verticalVelocity = 0f;
            if (controller != null) controller.enabled = true;
        }
    }
}
