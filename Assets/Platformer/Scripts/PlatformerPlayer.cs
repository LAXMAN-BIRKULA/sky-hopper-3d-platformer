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
            // Drop any running speed so he hangs on the ladder instead of drifting into it.
            horizontalVelocity = Vector3.zero;
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

            bool spaceDown = (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) || MobileInput.JumpHeld;
            if (spaceDown && !spaceWasDown)
                jumpBufferTimer = jumpBufferTime;
            else
                jumpBufferTimer -= dt;
            spaceWasDown = spaceDown;

            // Walking into the ladder (or holding Space near it) grabs it, so simply
            // heading for the ladder starts the climb instead of walking through it.
            bool wantsLadder = spaceDown || (input.sqrMagnitude > 0.01f && input.y > -0.1f);
            if (!IsClimbing && wantsLadder && Time.time >= ladderCooldownUntil && dismountTimer <= 0f)
            {
                Ladder near = Ladder.FindFor(transform.position, IsGrounded);
                if (near != null) EnterLadder(near);
            }

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
            // Any movement key (other than backing down with S) keeps the climb going up.
            if (climb > -0.1f && input.sqrMagnitude > 0.04f) climb = Mathf.Max(climb, 1f);
            if (spaceHeld) climb = Mathf.Max(climb, 1f);

            Ladder ladder = ActiveLadder;
            if (ladder != null && !ladder.Contains(transform.position))
            {
                // Walked out of the ladder zone: let go and resume normal movement.
                ActiveLadder = null;
                UpdateAnimator(dt);
                return;
            }

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
                UpdateAnimator(dt);
                return;
            }

            verticalVelocity = climb * climbSpeed;

            // Climb straight along the ladder instead of drifting with the movement keys.
            // Pushing forward used to walk him under the floating platform, where he got
            // wedged and then fell through it.
            Vector3 climbPoint = ladder != null
                ? ladder.ClimbLine(transform.position, WorldRadius() + 0.05f)
                : transform.position;
            Vector3 lateral = climbPoint - transform.position;
            lateral.y = 0f;
            horizontalVelocity = Vector3.ClampMagnitude(lateral * 8f, 1.5f);

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            IsGrounded = controller.isGrounded;

            // Climbing down onto the floor: let go so he can walk away from the ladder.
            if (climb < -0.1f && IsGrounded)
            {
                ActiveLadder = null;
                verticalVelocity = -2f;
                UpdateAnimator(dt);
                return;
            }

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

            // Hanging on a ladder: stay in the idle pose instead of running in place.
            if (climbing)
            {
                animator.SetFloat("Speed", 0f, 0.1f, dt);
                // Grounded=true also blends out of the Jump clip if he grabbed the ladder mid-air.
                animator.SetBool("Grounded", true);
                animator.SetFloat("VerticalSpeed", 0f);
                animator.SetFloat("RunSpeed", 0f, 0.1f, dt);
                return;
            }

            Vector3 planarVelocity = controller.velocity;
            planarVelocity.y = 0f;
            float planarSpeed = planarVelocity.magnitude;

            animator.SetFloat("Speed", planarSpeed, 0.08f, dt);
            animator.SetBool("Grounded", IsGrounded);
            animator.SetFloat("VerticalSpeed", verticalVelocity);

            // Stride matching: play the Run cycle faster as the character
            // moves faster, so the planted foot stays put instead of sliding.
            float runRate = Mathf.Clamp(planarSpeed / Mathf.Max(0.01f, runAnimReferenceSpeed), 0.6f, runAnimMaxSpeed);
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

        /// <summary>Character radius in world units (the controller scales with the model).</summary>
        float WorldRadius()
        {
            Vector3 scale = transform.lossyScale;
            return controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        static Vector2 ReadMoveInput()
        {
            Vector2 input = MobileInput.Move;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            }

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
