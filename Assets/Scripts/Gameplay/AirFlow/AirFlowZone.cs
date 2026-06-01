using Icarus.Gameplay.Interaction;
using Icarus.Gameplay.Player;
using UnityEngine;

namespace Icarus.Gameplay.AirFlow
{
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(AirFlowZoneVisualizer))]
    public class AirFlowZone : MonoBehaviour, IActivatable
    {
        private enum ActivateMode
        {
            ToggleActivation,
            ReverseDirection
        }

        [Header("Air Flow")]
        [SerializeField] private float maxFlowSpeed = 10f;
        [SerializeField] private float flowAcceleration = 30f;
        [SerializeField] private bool startActivated = true;
        [SerializeField] private ActivateMode activateMode = ActivateMode.ToggleActivation;

        // Prefab default flow is local +X. Reverse mode flips the flow sign without rotating the zone.
        public Vector2 AirFlowDirection => ((Vector2)transform.right * _flowDirectionSign).normalized;

        private BoxCollider2D _airFlowCollider;
        private AirFlowZoneVisualizer _visualizer;
        private Vector2 _currentFlowVelocity;
        private bool _hasCurrentFlowVelocity;
        private bool _isActivated;
        private float _flowDirectionSign = 1f;

        private void Awake()
        {
            _airFlowCollider = GetComponent<BoxCollider2D>();
            _visualizer = GetComponent<AirFlowZoneVisualizer>();
        }

        private void Start()
        {
            // Force initial application to collider/visual state.
            _isActivated = !startActivated;
            SetActivated(startActivated);
        }

        private void OnDisable()
        {
            // TODO: Clear player airflow explicitly when deactivating this zone while occupied.
            ResetFlowState();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_isActivated)
            {
                return;
            }

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

        public void Activate()
        {
            if (activateMode == ActivateMode.ReverseDirection)
            {
                ReverseFlowDirection();
                return;
            }

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

            if (!_isActivated)
            {
                ResetFlowState();
            }
        }

        private void SetColliderState(bool isEnabled)
        {
            _airFlowCollider.enabled = isEnabled;
        }
        
        private void SetVisualState(bool isVisible)
        {
            _visualizer.SetActivated(isVisible);
        }

        private void ReverseFlowDirection()
        {
            _flowDirectionSign *= -1f;
            _visualizer.SetFlowDirection(_flowDirectionSign);

            if (_hasCurrentFlowVelocity)
            {
                _currentFlowVelocity = -_currentFlowVelocity;
            }
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
            BoxCollider2D gizmoCollider = _airFlowCollider != null
                ? _airFlowCollider
                : GetComponent<BoxCollider2D>();

            Matrix4x4 prevMatrix = Gizmos.matrix;

            Gizmos.color = Color.blue;
            Gizmos.matrix = gizmoCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(gizmoCollider.offset, gizmoCollider.size);

            Gizmos.matrix = prevMatrix;
        }
    }
}
