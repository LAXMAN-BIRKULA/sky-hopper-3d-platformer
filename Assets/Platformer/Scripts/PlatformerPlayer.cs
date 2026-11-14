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

        [Header("Ladder")]
        public float climbSpeed = 3.4f;

        [Header("Animation")]
        public Animator animator;
        public Transform model;
        /// <summary>
        /// World speed the Run clip covers at 1x playback, measured from the planted foot
        /// (toe contact) on the Man Animated Run clip at the player's final visual scale.
        /// </summary>
        public float runAnimReferenceSpeed = 6.4917f;
        /// <summary>Highest Run playback multiplier, to avoid an absurdly fast cycle.</summary>
        public float runAnimMaxSpeed = 1.6f;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        float coyoteTimer;
        float jumpBufferTimer;
        bool spaceWasDown;
        bool inputEnabled = true;

        // Short window after stepping off a ladder where the player keeps moving forward
        // onto the platform and cannot immediately re-grab the same ladder.
        float dismountTimer;
        Vector3 dismountVelocity;
        float ladderCooldownUntil;

        public bool IsGrounded { get; private set; }
        public Ladder ActiveLadder { get; private set; }
        public bool IsClimbing => ActiveLadder != null;
        public float VerticalVelocity => verticalVelocity;

        public void EnterLadder(Ladder ladder)
        {
            if (ladder == null) return;
            // Just stepped off the top: ignore the ladder until we are clear of it.
            if (Time.time < ladderCooldownUntil) return;
            ActiveLadder = ladder;
            // Grabbing a ladder cancels any buffered jump so Space becomes "climb".
            jumpBufferTimer = 0f;
            verticalVelocity = 0f;
            dismountTimer = 0f;
        }

        public void ExitLadder(Ladder ladder)
        {
            if (ActiveLadder == ladder) ActiveLadder = null;
        }

        /// <summary>Kick the player upward (bouncer pads).</summary>
        public void Launch(float speed)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, speed);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            IsGrounded = false;
            if (animator != null) animator.CrossFadeInFixedTime("Jump", 0.05f, 0, 0f);
            Sfx.Play(Sfx.JumpClip, transform.position, 0.4f);
        }

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

        bool deathAnimationPlayed;

        /// <summary>Plays the Death clip (fall death). Safe to call more than once.</summary>
        public void PlayDeathAnimation()
        {
            if (animator == null || deathAnimationPlayed) return;
            deathAnimationPlayed = true;

            // Direct state play is reliable here; the Any State trigger transition was not
            // firing for this avatar-less generic rig.
            animator.Play("Death", 0, 0f);
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

            if (IsClimbing)
            {
                UpdateClimb(dt, input, wishDirection, spaceDown);
                return;
            }

            IsGrounded = CheckGrounded();
            if (IsGrounded) coyoteTimer = coyoteTime;
            else coyoteTimer -= dt;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                IsGrounded = false;
                // Crossfade straight into the Jump state instead of using a trigger: a queued
                // trigger survives while already in Jump and then replays the clip after landing.
                if (animator != null) animator.CrossFadeInFixedTime("Jump", 0.05f, 0, 0f);
                Sfx.Play(Sfx.JumpClip, transform.position, 0.35f);
            }

            if (dismountTimer > 0f)
            {
                // Just stepped off a ladder: glide forward onto the platform.
                dismountTimer -= dt;
                horizontalVelocity = dismountVelocity;
            }
            else
            {
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
            }

            ApplyGravityAndMove(dt, false);
            UpdateAnimator(dt);
        }

        void UpdateClimb(float dt, Vector2 input, Vector3 wishDirection, bool spaceHeld)
        {
            // No gravity on a ladder. W / forward climbs up, S / back climbs down,
            // and holding Space climbs up too, so the jump key doubles as "grab & climb".
            jumpBufferTimer = 0f;
            float climb = input.y;
            if (spaceHeld) climb = Mathf.Max(climb, 1f);

            Ladder ladder = ActiveLadder;
            if (climb > 0.1f && ladder != null && ladder.exitHeight > 0f &&
                transform.position.y >= ladder.exitHeight)
            {
                // Top of the ladder: step off onto the platform instead of hanging there.
                Vector3 exitDirection = ladder.exitDirection;
                exitDirection.y = 0f;
                if (exitDirection.sqrMagnitude < 0.001f) exitDirection = transform.forward;
                exitDirection.Normalize();

                ActiveLadder = null;
                dismountVelocity = exitDirection * Mathf.Max(moveSpeed * 0.6f, 2.5f);
                horizontalVelocity = dismountVelocity;
                dismountTimer = 0.45f;
                ladderCooldownUntil = Time.time + 0.8f;
                verticalVelocity = 2f;
                IsGrounded = false;
                transform.rotation = Quaternion.LookRotation(exitDirection, Vector3.up);
                if (animator != null) animator.CrossFadeInFixedTime("Jump", 0.05f, 0, 0f);
                UpdateAnimator(dt, true);
                return;
            }

            verticalVelocity = climb * climbSpeed;

            Vector3 target = wishDirection * (moveSpeed * 0.3f);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * dt);
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            IsGrounded = controller.isGrounded;

            if (wishDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(wishDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    1f - Mathf.Exp(-turnSpeed * dt));
            }

            UpdateAnimator(dt, true);
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

        void UpdateAnimator(float dt, bool climbing = false)
        {
            if (animator == null) return;
            Vector3 planarVelocity = controller.velocity;
            planarVelocity.y = 0f;
            float planarSpeed = climbing ? 1.5f : planarVelocity.magnitude;

            animator.SetFloat("Speed", planarSpeed, 0.08f, dt);
            animator.SetBool("Grounded", climbing ? false : IsGrounded);
            animator.SetFloat("VerticalSpeed", verticalVelocity);

            // Stride matching: play the Run cycle faster as the character
            // moves faster, so the planted foot stays put instead of sliding.
            float runRate = climbing
                ? 0.9f
                : Mathf.Clamp(planarSpeed / Mathf.Max(0.01f, runAnimReferenceSpeed), 0.6f, runAnimMaxSpeed);
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
