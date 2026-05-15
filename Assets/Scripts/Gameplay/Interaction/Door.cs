using UnityEngine;

namespace Icarus.Gameplay.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour, IActivatable
    {
        [Header("Door State")]
        [SerializeField] private bool startActivated = true;
        [SerializeField] private GameObject visualTarget;

        private Collider2D _controlledCollider;
        private bool _isActivated;

        private void Awake()
        {
            _controlledCollider = GetComponent<Collider2D>();

            if (visualTarget == null)
            {
                Debug.LogError("Door requires a Visual Target reference.", this);
                enabled = false;
                return;
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
            _controlledCollider.enabled = isEnabled;
        }

        private void SetVisualState(bool isVisible)
        {
            visualTarget.SetActive(isVisible);
        }
    }
}
