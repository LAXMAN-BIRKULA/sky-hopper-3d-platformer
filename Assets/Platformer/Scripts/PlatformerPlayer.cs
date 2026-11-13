using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyHopper
{
    /// <summary>
    /// CharacterController-based third-person platformer movement.
    /// Camera-relative WASD, buffered jump with coyote time, and Animator hooks.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlatformerPlayer : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float acceleration = 32f;
        [Range(0f, 1f)] public float airControl = 0.55f;
        public float turnSpeed = 14f;

        [Header("Jump")]
        public float jumpHeight = 1.7f;
        public float gravity = -24f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float groundCheckDistance = 0.25f;
        public LayerMask groundMask = ~0;

        [Header("Animation")]
        public Animator animator;
        public Transform model;
        /// <summary>
        /// World speed the Run clip covers at 1x playback. Measured from the planted foot:
        /// it travels backwards at 5.92 m/s in the clip's own space (see Character.fbx Run).
        /// </summary>
        public float runAnimReferenceSpeed = 5.92f;
        /// <summary>Highest Run playback multiplier, to avoid an absurdly fast cycle.</summary>
        public float runAnimMaxSpeed = 1.6f;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        float coyoteTimer;
        float jumpBufferTimer;
        bool spaceWasDown;
        bool inputEnabled = true;

        public bool IsGrounded { get; private set; }
        public bool InputEnabled
        {
            get => inputEnabled;
            set
            {
                inputEnabled = value;
                if (!value)
                {
                    horizontalVelocity = Vector3.zero;
                    if (animator != null) animator.SetFloat("Speed", 0f);
                }
            }
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (model == null && animator != null) model = animator.transform;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (!inputEnabled)
            {
                ApplyGravityAndMove(dt, true);
                return;
            }

            Vector2 input = ReadMoveInput();
            Vector3 wishDirection = CameraRelative(input);

            bool spaceDown = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            if (spaceDown && !spaceWasDown)
                jumpBufferTimer = jumpBufferTime;
            else
                jumpBufferTimer -= dt;
            spaceWasDown = spaceDown;

            IsGrounded = CheckGrounded();
            if (IsGrounded) coyoteTimer = coyoteTime;
            else coyoteTimer -= dt;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                IsGrounded = false;
                if (animator != null) animator.SetTrigger("Jump");
                Sfx.Play(Sfx.JumpClip, transform.position, 0.35f);
            }

            float control = IsGrounded ? 1f : airControl;
            Vector3 targetVelocity = wishDirection * moveSpeed;
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity, targetVelocity, acceleration * control * dt);

            if (wishDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(wishDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    1f - Mathf.Exp(-turnSpeed * dt));
            }

            ApplyGravityAndMove(dt, false);
            UpdateAnimator(dt);
        }

        void ApplyGravityAndMove(float dt, bool frozen)
        {
            if (IsGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -35f);

            Vector3 planar = frozen ? Vector3.zero : horizontalVelocity;
            controller.Move((planar + Vector3.up * verticalVelocity) * dt);

            // Keep the falling state honest even when frozen.
            IsGrounded = controller.isGrounded;
        }

        void UpdateAnimator(float dt)
        {
            if (animator == null) return;
            Vector3 planarVelocity = controller.velocity;
            planarVelocity.y = 0f;
            float planarSpeed = planarVelocity.magnitude;

            animator.SetFloat("Speed", planarSpeed, 0.08f, dt);
            animator.SetBool("Grounded", IsGrounded);
            animator.SetFloat("VerticalSpeed", verticalVelocity);

            // Stride matching: play the Run cycle faster as the character
            // moves faster, so the planted foot stays put instead of sliding.
            float runRate = Mathf.Clamp(planarSpeed / Mathf.Max(0.01f, runAnimReferenceSpeed),
                0.6f, runAnimMaxSpeed);
            animator.SetFloat("RunSpeed", runRate, 0.08f, dt);
        }

        bool CheckGrounded()
        {
            if (controller.isGrounded) return true;

            Vector3 origin = transform.position + controller.center;
            float radius = Mathf.Max(0.01f, controller.radius - 0.05f);
            float distance = controller.height * 0.5f - radius + groundCheckDistance;
            return Physics.SphereCast(origin, radius, Vector3.down, out _, distance,
                groundMask, QueryTriggerInteraction.Ignore);
        }

        static Vector2 ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            Vector2 input = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            return Vector2.ClampMagnitude(input, 1f);
        }

        static Vector3 CameraRelative(Vector2 input)
        {
            Camera cam = Camera.main;
            if (cam == null) return new Vector3(input.x, 0f, input.y);

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = cam.transform.right;
            right.y = 0f;
            right.Normalize();

            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }
    }
}
