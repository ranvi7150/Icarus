using UnityEngine;
using Icarus.Gameplay.Player;

namespace Icarus.Gameplay.World
{
    [RequireComponent(typeof(Collider2D))]
    public class DeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag("Player"))
            {
                return;
            }

            PlayerController player = rb.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            player.RequestDeath();
        }
    }
}
