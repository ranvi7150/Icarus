using UnityEngine;
using UnityEngine.InputSystem;

namespace Icarus.Gameplay.PlayerController
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 5f;

        [Header("Ground Check")]
        [SerializeField] private Collider2D feetCollider;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private bool _jumpQueued;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            //MOVE
            _rb.linearVelocity = new Vector2(_moveInput.x * moveSpeed, _rb.linearVelocity.y);

            //JUMP
            if (_jumpQueued)
            {
                _jumpQueued = false;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            }
        }

        private bool IsGrounded()
        {
            return feetCollider.IsTouchingLayers(groundLayers);
        }


        /* Player Control Event */

        public void Move(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
        }

        public void Jump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && IsGrounded())
            {
                _jumpQueued = true;
            }
        }

    }
}
