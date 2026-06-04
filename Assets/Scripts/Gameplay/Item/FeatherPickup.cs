using UnityEngine;
using UnityEngine.SceneManagement;

using Icarus.Core.Saving;
using Icarus.Gameplay.Player;

namespace Icarus.Gameplay.Item
{
    [RequireComponent(typeof(Collider2D))]
    public class FeatherPickup : MonoBehaviour
    {
        [SerializeField] private string featherId;

        private string _saveId;
        private bool _isPickedUp;

        private void Awake()
        {
            _saveId = BuildSaveId();
        }

        private void Start()
        {
            if (GameProgressState.HasCollectedFeather(_saveId))
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPickedUp)
            {
                return;
            }

            if (!TryGetPlayerController(other, out PlayerController player))
            {
                return;
            }

            if (!player.TryCollectFeather(_saveId))
            {
                return;
            }

            _isPickedUp = true;
            Destroy(gameObject);
        }

        private static bool TryGetPlayerController(Collider2D other, out PlayerController player)
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

        private string BuildSaveId()
        {
            string localId = string.IsNullOrWhiteSpace(featherId)
                ? gameObject.name
                : featherId;

            return $"{SceneManager.GetActiveScene().name}:{localId}";
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
