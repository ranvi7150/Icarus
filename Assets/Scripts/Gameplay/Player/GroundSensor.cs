using UnityEngine;

namespace Icarus.Gameplay.Player
{
    public class GroundSensor : MonoBehaviour
    {
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundCheckCastDistance = 0.05f;

        private void Awake()
        {
            if (groundCheckOrigin == null)
            {
                Debug.LogError("GroundSensor requires a Ground Check Origin reference.", this);
                enabled = false;
                return;
            }
        }

        public bool IsGrounded()
        {
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

        private void OnDrawGizmos()
        {
            if (groundCheckOrigin == null)
            {
                return;
            }

            DrawGroundCheckGizmo();
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
