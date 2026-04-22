using UnityEngine;
using UnityEngine.InputSystem;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 8.5f;

        [Header("Ground Check")]
        [SerializeField] private Collider2D feetCollider;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundCheckCastDistance = 0.05f;

        [Header("Jump Detail")]
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Jump Multiplier")]
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashDuration = 0.12f;
        [SerializeField] private float dashCooldown = 0.4f;

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

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            UpdateJumpTimers();
            UpdateDashTimers();
            ResetAirDash();
        }

        private void FixedUpdate()
        {
            if (_isInAirFlow)
            {
                _rb.linearVelocity = _airFlowVelocity;
                return;
            }

            if(TryDash()) return;   //NO Move, Jump, Gravity during Dash
            Move();
            Jump();
            ApplyGravity();
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
            //Start Dash
            if (_dashRequested && CanStartDash())
            {
                _dashRequested = false;
                _isDashing = true;

                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;

                //Prevent Jump after Dash
                ClearJumpStatus(clearJumpHold: false, clearCoyoteTimer: false);

                if (!IsGrounded())
                {
                    _hasAirDashed = true;
                }

                _rb.linearVelocity = new Vector2(_facingDirection * dashSpeed, 0f);
                return true;
            }

            //Dashing
            if(_isDashing)
            {
                _rb.linearVelocity = new Vector2(_facingDirection * dashSpeed, 0f);
                return true;
            }

            return false;
        }

        private void Move()
        {
            _rb.linearVelocity = new Vector2(_moveInput.x * moveSpeed, _rb.linearVelocity.y);
        }

        private void Jump()
        {
            if (_jumpBufferTimer <= 0f || _coyoteTimer <= 0f)
            {
                return;
            }

            ClearJumpStatus(clearJumpHold: false, clearCoyoteTimer: true);

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        }

        private void ApplyGravity()
        {
            //Apply additional gravity when ascending, if there is no jump key down x (Low Jump)
            if (_rb.linearVelocity.y > 0f && !_jumpHeld)
            {
                _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
            }

            //Apply additional gravity when descenting
            if (_rb.linearVelocity.y < 0f)
            {
                _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
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
            // Modify facingDirection to Left or Right
            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                _facingDirection = _moveInput.x > 0f ? 1 : -1;
            }
        }

        private bool CanStartDash()
        {
            return !_isDashing && _dashCooldownTimer <= 0f && (IsGrounded() || !_hasAirDashed);
        }

        private void ClearDashStatus()
        {
            _isDashing = false;
            _dashRequested = false;
        }

        private void ClearJumpStatus(bool clearJumpHold, bool clearCoyoteTimer)
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
            _isInAirFlow = true;
            _airFlowVelocity = velocity;
            ClearDashStatus();
            ClearJumpStatus(clearJumpHold: true, clearCoyoteTimer: true);
        }

        public void ClearAirFlowVelocity()
        {
            _isInAirFlow = false;
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
