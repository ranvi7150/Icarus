using System;
using UnityEngine;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(GroundSensor))]
    [RequireComponent(typeof(PlayerAirFlow))]
    public class PlayerMotor : MonoBehaviour
    {
        private const float VelocityEpsilonSqr = 0.0001f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 11.5f;

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

        private Rigidbody2D _rb;
        private PlayerStats _playerStats;
        private GroundSensor _groundSensor;
        private PlayerAirFlow _airFlow;
        private Wing _wing;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpHeld;
        private bool _jumpFeedbackPlayedForCurrentRequest;

        private Vector2 _moveInput;
        private int _facingDirection = 1;
        private bool _dashRequested;
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private bool _hasAirDashed;

        private Vector2 _motorVelocity;

        private bool _isInitialized;

        public Vector2 MoveInput => _moveInput;
        public Vector2 Velocity => _rb.linearVelocity;
        public int FacingDirection => _facingDirection;
        public bool IsGrounded => _groundSensor.IsGrounded();

        public event Action JumpStarted;

        // _motorVelocity = Player Motor(Move, Jump, Dash, Gravity) Target Velocity
        // totalTargetVelocity = _motorVelocity + airFlow
        // Delta V = totalTargetVelocity - _rb.linearVelocity

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _motorVelocity = _rb.linearVelocity;

            _playerStats = GetComponent<PlayerStats>();

            _groundSensor = GetComponent<GroundSensor>();
            _wing = GetComponentInChildren<Wing>();
            _airFlow = GetComponent<PlayerAirFlow>();

            if (_groundSensor == null)
            {
                Debug.LogError("PlayerMotor requires a GroundSensor component.", this);
                enabled = false;
                return;
            }

            if (_wing == null)
            {
                Debug.LogError("PlayerMotor requires a Wing component in children.", this);
                enabled = false;
                return;
            }

            if (_airFlow == null)
            {
                Debug.LogError("PlayerMotor requires a PlayerAirFlow component.", this);
                enabled = false;
                return;
            }

            _isInitialized = true;
        }

        private void OnEnable()
        {
            _wing.WingStateChanged += ResetMovementForWingState;
        }

        private void OnDisable()
        {
            if (!_isInitialized)
            {
                return;
            }

            _wing.WingStateChanged -= ResetMovementForWingState;
        }

        public void Tick()
        {
            bool isGrounded = IsGrounded;

            UpdateJumpTimers(isGrounded);
            UpdateDashTimers();
            ResetAirDash(isGrounded);
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
                if (_jumpBufferTimer <= 0f)
                {
                    _jumpFeedbackPlayedForCurrentRequest = false;
                }
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

        public void FixedTick()
        {
            bool isGrounded = IsGrounded;

            _wing.TickGlideDuration(isGrounded, Time.fixedDeltaTime);

            if (_airFlow.IsInAirFlow && !CanUseAirFlow())
            {
                if (_airFlow.ForceExit(clearCarry: true))
                {
                    _motorVelocity = _rb.linearVelocity;
                }
            }

            if (_airFlow.IsInAirFlow)
            {
                _airFlow.Sync();
                _motorVelocity = Vector2.zero;

                ApplyTargetVelocity(GetTotalTargetVelocity());
                return;
            }

            // During dash, skip normal move/jump/gravity logic,
            // Keep decaying residual AirFlow carry to prevent a pop when dash ends.
            if (TryDash(isGrounded))
            {
                ApplyTargetVelocity(GetTotalTargetVelocity());
                _airFlow.DecayCarry();
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
            _airFlow.DecayCarry();
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

            bool jumpFeedbackAlreadyPlayed = _jumpFeedbackPlayedForCurrentRequest;
            ResetJumpState(clearJumpHold: false, clearCoyoteTimer: true);

            _motorVelocity.y = jumpForce;
            if (!jumpFeedbackAlreadyPlayed)
            {
                JumpStarted?.Invoke();
            }
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

        private bool CanStartDash(bool isGrounded)
        {
            return !_isDashing && _dashCooldownTimer <= 0f && (isGrounded || !_hasAirDashed);
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

        private Vector2 GetTotalTargetVelocity()
        {
            if (_airFlow.IsInAirFlow)
            {
                return _airFlow.CurrentVelocity;
            }

            Vector2 carryVelocity = _airFlow.CarryVelocity;
            if (carryVelocity.sqrMagnitude <= VelocityEpsilonSqr)
            {
                return _motorVelocity;
            }

            return _motorVelocity + carryVelocity;
        }

        private void ApplyTargetVelocity(Vector2 targetVelocity)
        {
            ApplyVelocityDeltaAsImpulse(targetVelocity - _rb.linearVelocity);
        }

        private void ApplyVelocityDeltaAsImpulse(Vector2 deltaVelocity)
        {
            if (deltaVelocity.sqrMagnitude <= VelocityEpsilonSqr)
            {
                return;
            }

            _rb.AddForce(deltaVelocity * _rb.mass, ForceMode2D.Impulse);
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

        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
            UpdateFacingDirection();
        }

        public void RequestDash()
        {
            _dashRequested = true;
        }

        public void PressJump()
        {
            _jumpHeld = true;
            _jumpBufferTimer = jumpBufferTime;

            if (CanPlayJumpFeedbackImmediately())
            {
                PlayJumpFeedbackIfNeeded();
            }
        }

        public void ReleaseJump()
        {
            _jumpHeld = false;
        }

        public void SetAirFlowVelocity(Vector2 velocity)
        {
            if (!CanUseAirFlow())
            {
                return;
            }

            _airFlow.SetVelocity(velocity);
            ResetDashState();
            ResetJumpState(clearJumpHold: true, clearCoyoteTimer: true);
        }

        public void ClearAirFlowVelocity()
        {
            if (_airFlow.ClearVelocity())
            {
                _motorVelocity = _rb.linearVelocity;
            }
        }

        public void ResetMovementState()
        {
            _moveInput = Vector2.zero;
            _motorVelocity = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;

            ResetDashState();
            ResetJumpState(clearJumpHold: true, clearCoyoteTimer: true);

            _airFlow.ResetState();
        }

        private void ResetDashState()
        {
            _isDashing = false;
            _dashRequested = false;
        }

        private void ResetJumpState(bool clearJumpHold, bool clearCoyoteTimer)
        {
            _jumpBufferTimer = 0f;
            _jumpFeedbackPlayedForCurrentRequest = false;

            if (clearJumpHold)
            {
                _jumpHeld = false;
            }

            if (clearCoyoteTimer)
            {
                _coyoteTimer = 0f;
            }
        }

        private void ResetMovementForWingState(bool isWingOn)
        {
            if (isWingOn)
            {
                ResetDashState();
                return;
            }

            ResetDashState();
            if (_airFlow.ForceExit(clearCarry: true))
            {
                _motorVelocity = _rb.linearVelocity;
            }
        }

        private bool CanPlayJumpFeedbackImmediately()
        {
            return _jumpBufferTimer > 0f
                   && _coyoteTimer > 0f
                   && !_isDashing
                   && !_dashRequested
                   && !_airFlow.IsInAirFlow;
        }

        private void PlayJumpFeedbackIfNeeded()
        {
            if (_jumpFeedbackPlayedForCurrentRequest)
            {
                return;
            }

            _jumpFeedbackPlayedForCurrentRequest = true;
            JumpStarted?.Invoke();
        }
    }
}
