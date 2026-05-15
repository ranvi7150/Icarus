using UnityEngine;
using System;
using UnityEngine.InputSystem;
using Icarus.Gameplay.Interaction;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        private const float VelocityEpsilonSqr = 0.0001f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 11.5f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
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
        
        private Rigidbody2D _rb;
        private PlayerStats _playerStats;
        private Wing _wing;
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
        private bool _isInitialized;

        public event Action Interacted;

        public Vector2 MoveInput => _moveInput;
        public Vector2 Velocity => _rb.linearVelocity;
        public int FacingDirection => _facingDirection;
        public bool IsGroundedForAnimation => IsGrounded();

        // _motorVelocity = Player Motor(Move, Jump, Dash, Gravity) Target Velocity
        // totalTargetVelocity = _motorVelocity + airFlowCarry
        // Delta V = totalTargetVelocity - _rb.linearVelocity

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _motorVelocity = _rb.linearVelocity;
            _playerStats = GetComponent<PlayerStats>();
            _wing = GetComponentInChildren<Wing>();

            if (groundCheckOrigin == null)
            {
                Debug.LogError("PlayerController requires a Ground Check Origin reference.", this);
                enabled = false;
                return;
            }

            if (interactionCollider == null)
            {
                Debug.LogError("PlayerController requires an Interaction Collider reference.", this);
                enabled = false;
                return;
            }

            if (_wing == null)
            {
                Debug.LogError("PlayerController requires a Wing component in children.", this);
                enabled = false;
                return;
            }

            _isInitialized = true;
        }

        private void OnEnable()
        {
            _wing.WingStateChanged += HandleWingStateChanged;
        }

        private void OnDisable()
        {
            if (!_isInitialized)
            {
                return;
            }

            _wing.WingStateChanged -= HandleWingStateChanged;

            if (_currentInteractable is IInteractionPromptTarget promptTarget)
            {
                promptTarget.SetInteractionPromptVisible(false);
            }
            _currentInteractable = null;
        }

        private void Update()
        {
            bool isGrounded = IsGrounded();

            UpdateJumpTimers(isGrounded);
            UpdateDashTimers();
            ResetAirDash(isGrounded);
        }

        private void FixedUpdate()
        {
            bool isGrounded = IsGrounded();
            UpdateWingGlideState(isGrounded);

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
            if (TryDash(isGrounded))
            {
                ApplyTargetVelocity(GetTotalTargetVelocity());

                DecayAirFlowCarry();
                return;   
            }

            if (isGrounded && _motorVelocity.y < 0f)
            {
                _motorVelocity.y = 0f;
            }

            Move();
            Jump();
            ApplyGravity(isGrounded);

            ApplyTargetVelocity(GetTotalTargetVelocity());

            DecayAirFlowCarry();
        }

        private void UpdateJumpTimers(bool isGrounded)
        {
            // Coyote time: allow jump for a short time after leaving ground.
            if (isGrounded)
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

        private void ResetAirDash(bool isGrounded)
        {
            if (isGrounded)
            {
                _hasAirDashed = false;
            }
        }

        private bool TryDash(bool isGrounded)
        {
            if (!CanDash())
            {
                ResetDashState();
                return false;
            }

            // Start dash.
            if (_dashRequested && CanStartDash(isGrounded))
            {
                _dashRequested = false;
                _isDashing = true;

                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;

                // Prevent jump buffer from auto-firing right after dash.
                ResetJumpState(clearJumpHold: false, clearCoyoteTimer: false);

                if (!isGrounded)
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

        private void ApplyGravity(bool isGrounded)
        {
            float gravityScale;
            bool canGlideInAir = CanGlide() && !isGrounded;

            // Separate upward gravity while holding jump to avoid floaty hang-time.
            if (_motorVelocity.y > 0f)
            {
                gravityScale = _jumpHeld ? riseGravityMultiplier : lowJumpMultiplier;
            }
            else
            {
                gravityScale = canGlideInAir ? _wing.GlideFallGravityMultiplier : fallMultiplier;
            }

            _motorVelocity += Vector2.up * Physics2D.gravity.y * gravityScale * Time.fixedDeltaTime;
            float appliedMaxFallSpeed = canGlideInAir ? _wing.GlideMaxFallSpeed : maxFallSpeed;
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
            // Check ground layer by casting a sensor-sized box downward.
            Vector2 castOrigin = groundCheckOrigin.position;
            Vector2 castSize = GetWorldGroundCheckSize();
            RaycastHit2D hit = Physics2D.BoxCast(castOrigin, castSize,
                                                0f, Vector2.down,
                                                groundCheckCastDistance, groundLayers);

            return hit.collider != null;
        }

        private Vector2 GetWorldGroundCheckSize()
        {
            Vector3 scale = groundCheckOrigin.lossyScale;
            return new Vector2(
                Mathf.Abs(groundCheckSize.x * scale.x),
                Mathf.Abs(groundCheckSize.y * scale.y));
        }

        private void UpdateFacingDirection()
        {
            // Update facing direction from horizontal input.
            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                SetFacingDirection(_moveInput.x > 0f ? 1 : -1);
            }
        }

        private void SetFacingDirection(int direction)
        {
            _facingDirection = direction;
        }

        private bool CanStartDash(bool isGrounded)
        {
            return !_isDashing && _dashCooldownTimer <= 0f && (isGrounded || !_hasAirDashed);
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
            return _playerStats.CanDash && _wing.CanDash;
        }

        private bool CanUseAirFlow()
        {
            return _wing.CanUseAirFlow;
        }

        private bool CanGlide()
        {
            return _wing.CanGlide;
        }

        private void UpdateWingGlideState(bool isGrounded)
        {
            _wing.TickGlideDuration(isGrounded, Time.fixedDeltaTime);
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
            if (ctx.started || ctx.performed)
            {
                _dashRequested = true;
            }
        }

        public void OnWingToggle(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                _wing.ToggleWing();
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
            Interacted?.Invoke();
        }



        /* Gizmos */

        private void OnDrawGizmos()
        {
            if (groundCheckOrigin != null) DrawGroundCheckGizmo();
        }

        private void DrawGroundCheckGizmo()
        {
            Vector2 castSize = GetWorldGroundCheckSize();
            Vector2 startCenter = groundCheckOrigin.position;
            Vector2 endCenter = startCenter + Vector2.down * Mathf.Abs(groundCheckCastDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(startCenter, castSize);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(endCenter, castSize);
        }

    }
}
