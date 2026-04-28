using Icarus.Gameplay.Player;
using UnityEngine;

namespace Icarus.Gameplay.AirFlow
{
    [RequireComponent(typeof(Collider2D))]
    public class AirFlowZone : MonoBehaviour
    {
        [Header("Air Flow")]
        [SerializeField] private float maxFlowSpeed = 10f;
        [SerializeField] private float flowAcceleration = 30f;

        [Header("Gizmo")]
        [SerializeField] private BoxCollider2D airFlowCollider;

        // Prefab default flow is +X. Actual world flow comes from zone rotation.
        public Vector2 AirFlowDirection => ((Vector2)transform.right).normalized;

        private Vector2 _currentFlowVelocity;
        private bool _hasCurrentFlowVelocity;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!TryGetPlayerController(other, out PlayerController player))
            {
                return;
            }

            Vector2 targetVelocity = AirFlowDirection * maxFlowSpeed;
            EnsureFlowInitialized();

            // Acceleration * Time = Change in Velocity (Delta V).
            _currentFlowVelocity = Vector2.MoveTowards(
                _currentFlowVelocity,
                targetVelocity,
                flowAcceleration * Time.fixedDeltaTime);

            player.SetAirFlowVelocity(_currentFlowVelocity);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryGetPlayerController(other, out PlayerController player))
            {
                return;
            }

            ResetFlowState();

            player.ClearAirFlowVelocity();
        }

        private void EnsureFlowInitialized()
        {
            if (_hasCurrentFlowVelocity)
            {
                return;
            }

            _currentFlowVelocity = Vector2.zero;
            _hasCurrentFlowVelocity = true;
        }

        private void ResetFlowState()
        {
            _currentFlowVelocity = Vector2.zero;
            _hasCurrentFlowVelocity = false;
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
