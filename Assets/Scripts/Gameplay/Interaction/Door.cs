using UnityEngine;

namespace Icarus.Gameplay.Interaction
{
    public class Door : MonoBehaviour, IActivatable
    {
        [Header("Door State")]
        [SerializeField] private bool startActivated = true;
        [SerializeField] private Collider2D controlledCollider;
        [SerializeField] private GameObject visualTarget;

        private bool _isActivated;

        private void Awake()
        {
            if (controlledCollider == null)
            {
                controlledCollider = GetComponent<Collider2D>();
            }

            // Force initial application to collider/visual state.
            _isActivated = !startActivated;
            SetActivated(startActivated);
        }

        public void Activate()
        {
            SetActivated(!_isActivated);
        }

        private void SetActivated(bool isActivated)
        {
            if (_isActivated == isActivated)
            {
                return;
            }

            _isActivated = isActivated;
            SetColliderState(_isActivated);
            SetVisualState(_isActivated);
        }

        private void SetColliderState(bool isEnabled)
        {
            if (controlledCollider == null)
            {
                return;
            }

            controlledCollider.enabled = isEnabled;
        }

        private void SetVisualState(bool isVisible)
        {
            if (visualTarget == null)
            {
                return;
            }

            visualTarget.SetActive(isVisible);
        }
    }
}
