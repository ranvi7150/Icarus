using UnityEngine;
using UnityEngine.SceneManagement;

using Icarus.Core.Saving;

namespace Icarus.Gameplay.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Door : MonoBehaviour, IActivatable
    {
        [Header("Door State")]
        [SerializeField] private string doorId;

        private Collider2D _controlledCollider;
        private SpriteRenderer _spriteRenderer;
        private string _saveId;
        private bool _isActivated;

        private void Awake()
        {
            _controlledCollider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _saveId = BuildSaveId();

            // Force initial application to collider/visual state.
            _isActivated = false;
            SetActivated(true, updateProgress: false);
        }

        private void Start()
        {
            if (GameProgressState.HasOpenedDoor(_saveId))
            {
                SetActivated(false, updateProgress: false);
            }
        }

        public void Activate()
        {
            SetActivated(!_isActivated, updateProgress: true);
        }

        private void SetActivated(bool isActivated, bool updateProgress)
        {
            if (_isActivated == isActivated)
            {
                return;
            }

            _isActivated = isActivated;
            SetColliderState(_isActivated);
            SetVisualState(_isActivated);

            if (updateProgress)
            {
                GameProgressState.SetDoorOpen(_saveId, !_isActivated);
            }
        }

        private void SetColliderState(bool isEnabled)
        {
            _controlledCollider.enabled = isEnabled;
        }

        private void SetVisualState(bool isVisible)
        {
            _spriteRenderer.enabled = isVisible;
        }

        private string BuildSaveId()
        {
            string localId = string.IsNullOrWhiteSpace(doorId)
                ? gameObject.name
                : doorId;

            return $"{SceneManager.GetActiveScene().name}:{localId}";
        }
    }
}
