using UnityEngine;
using Icarus.Gameplay.Player;

namespace Icarus.Gameplay.AirFlow
{
    [RequireComponent(typeof(Collider2D))]
    public class AirFlowZone : MonoBehaviour
    {
        [Header("Air Flow")]
        [SerializeField] private float flowSpeed = 6f;

        [Header("Gizmo")]
        [SerializeField] private BoxCollider2D airFlowCollider;

        // Prefab default flow is +X. Actual world flow comes from zone rotation.
        public Vector2 AirFlowDirection => ((Vector2)transform.right).normalized;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!TryGetPlayerController(other, out PlayerController player))
            {
                return;
            }

            Vector2 forcedVelocity = AirFlowDirection * flowSpeed;
            player.SetAirFlowVelocity(forcedVelocity);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryGetPlayerController(other, out PlayerController player))
            {
                return;
            }

            player.ClearAirFlowVelocity();
        }

        private bool TryGetPlayerController(Collider2D other, out PlayerController player)
        {
            player = null;

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag("Player"))
            {
                return false;
            }

            player = rb.GetComponent<PlayerController>();
            return player != null;
        }

        /* Gizmos */

        private void OnDrawGizmos()
        {
            if (airFlowCollider == null)
            {
                return;
            }

            Matrix4x4 prevMatrix = Gizmos.matrix;

            Gizmos.color = Color.blue;
            Gizmos.matrix = airFlowCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(airFlowCollider.offset, airFlowCollider.size);

            Gizmos.matrix = prevMatrix;
        }
    }
}
