using UnityEngine;

using Icarus.Gameplay.Player;

namespace Icarus.Gameplay.Item
{
    [RequireComponent(typeof(Collider2D))]
    public class FeatherPickup : MonoBehaviour
    {
        private bool _isPickedUp;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPickedUp)
            {
                return;
            }

            if (!TryGetPlayerStats(other, out PlayerStats playerStats))
            {
                return;
            }

            if (!playerStats.TryCollectFeather())
            {
                return;
            }

            _isPickedUp = true;
            Destroy(gameObject);
        }

        private static bool TryGetPlayerStats(Collider2D other, out PlayerStats playerStats)
        {
            playerStats = null;

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag("Player"))
            {
                return false;
            }

            playerStats = rb.GetComponent<PlayerStats>();
            return playerStats != null;
        }

        private void OnValidate()
        {
            Collider2D pickupCollider = GetComponent<Collider2D>();
            if (pickupCollider != null && !pickupCollider.isTrigger)
            {
                Debug.LogWarning("FeatherPickup requires a Trigger Collider2D.", this);
            }
        }
    }
}
