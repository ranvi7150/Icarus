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

        [Header("Jump Detail")]
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Jump Multiplier")]
        [SerializeField] private float jumpSpeedMultiplier = 1.3f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpHeld;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            UpdateJumpTimers();
        }

        private void FixedUpdate()
        {
            Move();
            Jump();
            ApplyGravity();
        }
        private void UpdateJumpTimers()
        {
            //CoyoteTime : 착지시 초기화 -> 지면에서 벗어나더라도 coyoteTime동안은 점프가능
            //JumpBuffer : 점프버튼 다운시 초기화 -> 착지하기 전이라도 JumpBufferTime동안은 점프 선입력

            if (IsGrounded())
            {
                _coyoteTimer = coyoteTime;
            }
            else
            {
                _coyoteTimer -= Time.fixedDeltaTime;
            }

            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= Time.fixedDeltaTime;
            }
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

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce * jumpSpeedMultiplier);
        }

        private void ApplyGravity()
        {
            //점프 높이 유지를 위한 중력가속도 조정 h = v^2 / 2g --> h = (v * k)^2 / 2g * k^2 
            //k = jumpSpeedMultiplier
            float speedGravityMultiplier = jumpSpeedMultiplier * jumpSpeedMultiplier;

            if (_rb.linearVelocity.y > 0f)
            {
                // 낮은 점프시(탭 점프) 중력가속도 수정
                if (!_jumpHeld)
                {
                    float effectiveLowJumpMultiplier = lowJumpMultiplier * speedGravityMultiplier;
                    _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (effectiveLowJumpMultiplier - 1f) * Time.fixedDeltaTime;
                }
                // 긴 점프시(점프 버튼 유지) 중력가속도 수정
                else
                {
                    _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (speedGravityMultiplier - 1f) * Time.fixedDeltaTime;
                }
            }

            if (_rb.linearVelocity.y < 0f)
            {
                float effectiveFallMultiplier = fallMultiplier * speedGravityMultiplier;
                _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (effectiveFallMultiplier - 1f) * Time.fixedDeltaTime;
                return;
            }
        }

        private bool IsGrounded()
        {
            return feetCollider != null && feetCollider.IsTouchingLayers(groundLayers);
        }



        /* Player Control Event */

        public void OnMove(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
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


        /* Gizmos */

        private void OnDrawGizmos()
        {
            if (feetCollider != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(feetCollider.bounds.center, feetCollider.bounds.size);
            }
        }

    }
}

