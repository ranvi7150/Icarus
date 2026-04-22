using UnityEngine;
using Icarus.Gameplay.Player;

namespace Icarus.Gameplay.AirFlow
{
    [RequireComponent(typeof(Collider2D))]
    public class AirFlowZone : MonoBehaviour
    {
        [Header("Air Flow")]
        [SerializeField] private Vector2 windDirection = Vector2.up;
        [SerializeField] private float flowSpeed = 6f;

        [Header("Gizmo")]
        [SerializeField] private BoxCollider2D windCollider;


        private void OnTriggerStay2D(Collider2D other)
        {
            if (!TryGetPlayerController(other, out PlayerController player)) return;
            if (!TryGetForcedVelocity(out Vector2 forcedVelocity)) return;

            player.SetAirFlowVelocity(forcedVelocity);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryGetPlayerController(other, out PlayerController player)) return;

            player.ClearAirFlowVelocity();
        }

        private bool TryGetPlayerController(Collider2D other, out PlayerController player)
        {
            // Ensure out parameter is initialized.
            player = null;

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag("Player")) return false;

            player = rb.GetComponent<PlayerController>();
            if (player == null) return false;

            return true;
        }

        private bool TryGetForcedVelocity(out Vector2 forcedVelocity)
        {
            if (windDirection == Vector2.zero)
            {
                forcedVelocity = Vector2.zero;
                return false;
            }

            // Convert local airflow direction into world space, so Prefab rotation affects movement.
            Vector2 worldDirection = transform.TransformDirection(windDirection.normalized).normalized;
            forcedVelocity = worldDirection * flowSpeed;
            return true;
        }



        /* Gizmos */

        private void OnDrawGizmos()
        {
            if (windCollider != null) DrawWindColliderGizmo();
        }

        private void DrawWindColliderGizmo()
        {
            Gizmos.color = Color.blue;
            Gizmos.matrix = windCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(windCollider.offset, windCollider.size);
        }
    }
}
