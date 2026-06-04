using System;
using UnityEngine;
using Icarus.Gameplay.Interaction;

namespace Icarus.Gameplay.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private Collider2D interactionCollider;

        private PlayerController _playerController;
        private IInteractable _currentInteractable;

        public event Action Interacted;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();

            if (_playerController == null)
            {
                Debug.LogError("PlayerInteractor requires a PlayerController component.", this);
                enabled = false;
                return;
            }

            if (interactionCollider == null)
            {
                Debug.LogError("PlayerInteractor requires an Interaction Collider reference.", this);
                enabled = false;
                return;
            }
        }

        private void OnDisable()
        {
            ClearTarget();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_playerController.IsAlive)
            {
                return;
            }

            if (!IsInteractionTouching(other))
            {
                return;
            }

            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            _currentInteractable = interactable;

            if (interactable is IInteractionPromptTarget promptTarget)
            {
                promptTarget.SetInteractionPromptVisible(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_playerController.IsAlive)
            {
                return;
            }

            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            if (_currentInteractable != interactable)
            {
                return;
            }

            // Ignore exits from non-interaction colliders while the interaction collider is still touching.
            if (IsInteractionTouching(other))
            {
                return;
            }

            ClearTarget();
        }

        public void TryInteract()
        {
            if (_currentInteractable == null)
            {
                return;
            }

            _currentInteractable.Interact();
            Interacted?.Invoke();
        }

        public void ClearTarget()
        {
            if (_currentInteractable is IInteractionPromptTarget promptTarget)
            {
                promptTarget.SetInteractionPromptVisible(false);
            }

            _currentInteractable = null;
        }

        private bool IsInteractionTouching(Collider2D other)
        {
            return interactionCollider.IsTouching(other);
        }
    }
}
