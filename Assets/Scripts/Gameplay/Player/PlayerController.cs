using UnityEngine;
using UnityEngine.InputSystem;
using Icarus.Gameplay.Interaction;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        private const float VelocityEpsilonSqr = 0.0001f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 11.5f;

        [Header("Ground Check")]
        [SerializeField] private Collider2D feetCollider;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundCheckCastDistance = 0.05f;

        [Header("Interaction")]
        [SerializeField] private Collider2D interactionCollider;

        [Header("Jump Detail")]
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Jump Multiplier")]
        [SerializeField] private float riseGravityMultiplier = 2.2f;
        [SerializeField] private float lowJumpMultiplier = 4.2f;
        [SerializeField] private float fallMultiplier = 3.4f;
        [SerializeField] private float maxFallSpeed = 18f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashDuration = 0.12f;
        [SerializeField] private float dashCooldown = 0.4f;

        [Header("Air Flow")]
        [SerializeField] private float airFlowCarryDecay = 18f;
        
        [Header("Wing")]
        [SerializeField] private Wing wing;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpHeld;

        private int _facingDirection = 1;
        private bool _dashRequested;
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private bool _hasAirDashed;

        private bool _isInAirFlow;
        private Vector2 _airFlowVelocity;
        private Vector2 _airFlowCarryVelocity;
        private Vector2 _motorVelocity;
        
        private IInteractable _currentInteractable;

        // _motorVelocity = Player Motor(Move, Jump, Dash, Gravity) Target Velocity
        // totalTargetVelocity = _motorVelocity + airFlowCarry
        // Delta V = totalTargetVelocity - _rb.linearVelocity

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>(); 
            _motorVelocity = _rb.linearVelocity;
            wing = GetComponentInChildren<Wing>();
        }

        private void OnEnable()
        {
            if (wing != null)
            {
                wing.WingStateChanged += HandleWingStateChanged;
            }
        }

        private void OnDisable()
        {
            if (wing != null)
            {
                wing.WingStateChanged -= HandleWingStateChanged;
            }

            if (_currentInteractable is IInteractionPromptTarget promptTarget)
            {
                promptTarget.SetInteractionPromptVisible(false);
            }

            _currentInteractable = null;
        }

        private void Update()
        {
            UpdateJumpTimers();
            UpdateDashTimers();
            ResetAirDash();
        }

        private void FixedUpdate()
        {
            if (_isInAirFlow && !CanUseAirFlow())
            {
                ForceExitAirFlow(clearCarry: true);
            }

            if (_isInAirFlow)
            {
                SyncAirFlowState();

                ApplyTargetVelocity(GetTotalTargetVelocity());
                return;
            }

            // During dash, skip normal move/jump/gravity logic,
            // Keep decaying residual AirFlow carry to prevent a pop when dash ends.
            if (TryDash())
            {
                ApplyTargetVelocity(GetTotalTargetVelocity());

                DecayAirFlowCarry();
                return;   
            }

            if (IsGrounded() && _motorVelocity.y < 0f)
            {
                _motorVelocity.y = 0f;
            }

            Move();
            Jump();
            ApplyGravity();

            ApplyTargetVelocity(GetTotalTargetVelocity());

            DecayAirFlowCarry();
        }

        private void UpdateJumpTimers()
        {
            // Coyote time: allow jump for a short time after leaving ground.
            if (IsGrounded())
            {
                _coyoteTimer = coyoteTime;
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }

            // Jump buffer: cache jump input briefly before landing.
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= Time.deltaTime;
            }
        }

        private void UpdateDashTimers()
        {
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
            }

            if (_dashTimer > 0f)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                }
            }
        }

        private void ResetAirDash()
        {
            if (IsGrounded())
            {
                _hasAirDashed = false;
            }
        }

        private bool TryDash()
        {
            if (!CanDash())
            {
                ResetDashState();
                return false;
            }

            // Start dash.
            if (_dashRequested && CanStartDash())
            {
                _dashRequested = false;
                _isDashing = true;

                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;

                // Prevent jump buffer from auto-firing right after dash.
                ResetJumpState(clearJumpHold: false, clearCoyoteTimer: false);

                if (!IsGrounded())
                {
                    _hasAirDashed = true;
                }

                SetDashVelocity();
                return true;
            }

            // Continue dash.
            if (_isDashing)
            {
                SetDashVelocity();
                return true;
            }

            return false;
        }

        private void SetDashVelocity()
        {
            _motorVelocity = new Vector2(_facingDirection * dashSpeed, 0f);
        }

        private void Move()
        {
            _motorVelocity.x = _moveInput.x * moveSpeed;
        }

        private void Jump()
        {
            if (_jumpBufferTimer <= 0f || _coyoteTimer <= 0f)
            {
                return;
            }

            ResetJumpState(clearJumpHold: false, clearCoyoteTimer: true);

            _motorVelocity.y = jumpForce;
        }

        private void ApplyGravity()
        {
            float gravityScale;

            // Separate upward gravity while holding jump to avoid floaty hang-time.
            if (_motorVelocity.y > 0f)
            {
                gravityScale = _jumpHeld ? riseGravityMultiplier : lowJumpMultiplier;
            }
            else
            {
                gravityScale = CanGlide() && !IsGrounded() ? wing.GlideFallGravityMultiplier : fallMultiplier;
            }

            _motorVelocity += Vector2.up * Physics2D.gravity.y * gravityScale * Time.fixedDeltaTime;
            float appliedMaxFallSpeed = CanGlide() && !IsGrounded() ? wing.GlideMaxFallSpeed : maxFallSpeed;
            _motorVelocity.y = Mathf.Max(_motorVelocity.y, -appliedMaxFallSpeed);
        }

        private void SyncAirFlowState()
        {
            _airFlowCarryVelocity = _airFlowVelocity;

            // Clear motor velocity so stale move/jump/dash values do not leak after zone exit.
            _motorVelocity = Vector2.zero;
        }

        private Vector2 GetTotalTargetVelocity()
        {
            if (_isInAirFlow)
            {
                return _airFlowVelocity;
            }

            if (_airFlowCarryVelocity.sqrMagnitude <= VelocityEpsilonSqr)
            {
                return _motorVelocity;
            }

            return _motorVelocity + _airFlowCarryVelocity;
        }

        private void DecayAirFlowCarry()
        {
            // Decayed carry is applied starting from the next physics step.
            _airFlowCarryVelocity = Vector2.MoveTowards(
                _airFlowCarryVelocity,
                Vector2.zero,
                airFlowCarryDecay * Time.fixedDeltaTime);
        }

        private void ApplyVelocityDeltaAsImpulse(Vector2 deltaVelocity)
        {
            if (deltaVelocity.sqrMagnitude <= VelocityEpsilonSqr)
            {
                return;
            }

            _rb.AddForce(deltaVelocity * _rb.mass, ForceMode2D.Impulse);
        }

        private void ApplyTargetVelocity(Vector2 targetVelocity)
        {
            ApplyVelocityDeltaAsImpulse(targetVelocity - _rb.linearVelocity);
        }

        private bool IsGrounded()
        {
            if (feetCollider == null)
            {
                return false;
            }

            // Check ground layer by casting a feet-sized box downward.
            Bounds feetBounds = feetCollider.bounds;
            Vector2 castOrigin = feetBounds.center;
            Vector2 castSize = feetBounds.size;
            RaycastHit2D hit = Physics2D.BoxCast(castOrigin, castSize,
                                                0f, Vector2.down,
                                                groundCheckCastDistance, groundLayers);

            return hit.collider != null;
        }

        private void UpdateFacingDirection()
        {
            // Update facing direction from horizontal input.
            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                _facingDirection = _moveInput.x > 0f ? 1 : -1;
            }
        }

        private bool CanStartDash()
        {
            return CanDash() && !_isDashing && _dashCooldownTimer <= 0f && (IsGrounded() || !_hasAirDashed);
        }

        private void ResetDashState()
        {
            _isDashing = false;
            _dashRequested = false;
        }

        private void ResetJumpState(bool clearJumpHold, bool clearCoyoteTimer)
        {
            _jumpBufferTimer = 0f;

            if (clearJumpHold)
            {
                _jumpHeld = false;
            }

            if (clearCoyoteTimer)
            {
                _coyoteTimer = 0f;
            }
        }


        public void SetAirFlowVelocity(Vector2 velocity)
        {
            if (!CanUseAirFlow())
            {
                return;
            }

            _isInAirFlow = true;
            _airFlowVelocity = velocity;
            _airFlowCarryVelocity = velocity;
            ResetDashState();
            ResetJumpState(clearJumpHold: true, clearCoyoteTimer: true);
        }

        public void ClearAirFlowVelocity()
        {
            if (!_isInAirFlow)
            {
                return;
            }

            _isInAirFlow = false;
            _airFlowVelocity = Vector2.zero;
            _motorVelocity = _rb.linearVelocity;
        }

        private bool CanDash()
        {
            return wing == null || wing.CanDash;
        }

        private bool CanUseAirFlow()
        {
            return wing == null || wing.CanUseAirFlow;
        }

        private bool CanGlide()
        {
            return wing != null && wing.CanGlide;
        }

        private void HandleWingStateChanged(bool isWingOn)
        {
            if (isWingOn)
            {
                ResetDashState();
                return;
            }

            ResetDashState();
            ForceExitAirFlow(clearCarry: true);
        }

        private void ForceExitAirFlow(bool clearCarry)
        {
            bool wasInAirFlow = _isInAirFlow;

            _isInAirFlow = false;
            _airFlowVelocity = Vector2.zero;

            if (clearCarry)
            {
                _airFlowCarryVelocity = Vector2.zero;
            }

            if (wasInAirFlow)
            {
                _motorVelocity = _rb.linearVelocity;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsInteractionTouching(other))
            {
                return;
            }

            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            _currentInteractable = interactable;

            if (interactable is IInteractionPromptTarget promptTarget)
            {
                promptTarget.SetInteractionPromptVisible(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            if (_currentInteractable == interactable)
            {
                // Ignore exits from non-interaction colliders while the interaction collider is still touching.
                if (IsInteractionTouching(other))
                {
                    return;
                }

                if (interactable is IInteractionPromptTarget promptTarget)
                {
                    promptTarget.SetInteractionPromptVisible(false);
                }

                _currentInteractable = null;
            }
        }

        private bool IsInteractionTouching(Collider2D other)
        {
            if (interactionCollider == null)
            {
                return false;
            }

            return interactionCollider.IsTouching(other);
        }


        /* Player Control Event */

        public void OnMove(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
            UpdateFacingDirection();
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.started || ctx.performed)
            {
                _jumpHeld = true;
                _jumpBufferTimer = jumpBufferTime;
            }

            if (ctx.canceled)
            {
                _jumpHeld = false;
            }
        }

        public void OnDash(InputAction.CallbackContext ctx)
        {
            if ((ctx.started || ctx.performed) && CanDash())
            {
                _dashRequested = true;
            }
        }

        public void OnWingToggle(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && wing != null)
            {
                wing.ToggleWing();
            }
        }

        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
            {
                return;
            }

            if (_currentInteractable == null)
            {
                return;
            }

            _currentInteractable.Interact();
        }



        /* Gizmos */

        private void OnDrawGizmos()
        {
            if (feetCollider != null) DrawFeetColliderGizmo();
        }

        private void DrawFeetColliderGizmo()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                feetCollider.bounds.center + Vector3.down * groundCheckCastDistance,
                feetCollider.bounds.size
            );
        }

    }
}
