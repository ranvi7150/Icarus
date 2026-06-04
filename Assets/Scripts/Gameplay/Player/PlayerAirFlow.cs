using UnityEngine;

namespace Icarus.Gameplay.Player
{
    public class PlayerAirFlow : MonoBehaviour
    {
        [Header("Air Flow")]
        [SerializeField] private float airFlowCarryDecay = 18f;

        private bool _isInAirFlow;
        private Vector2 _airFlowVelocity;
        private Vector2 _airFlowCarryVelocity;

        public bool IsInAirFlow => _isInAirFlow;
        public Vector2 CurrentVelocity => _airFlowVelocity;
        public Vector2 CarryVelocity => _airFlowCarryVelocity;

        public void SetVelocity(Vector2 velocity)
        {
            _isInAirFlow = true;
            _airFlowVelocity = velocity;
            _airFlowCarryVelocity = velocity;
        }

        public bool ClearVelocity()
        {
            if (!_isInAirFlow)
            {
                return false;
            }

            _isInAirFlow = false;
            _airFlowVelocity = Vector2.zero;
            return true;
        }

        public bool ForceExit(bool clearCarry)
        {
            bool wasInAirFlow = _isInAirFlow;

            _isInAirFlow = false;
            _airFlowVelocity = Vector2.zero;

            if (clearCarry)
            {
                _airFlowCarryVelocity = Vector2.zero;
            }

            return wasInAirFlow;
        }

        public void DecayCarry()
        {
            // Decayed carry is applied starting from the next physics step.
            _airFlowCarryVelocity = Vector2.MoveTowards(
                _airFlowCarryVelocity,
                Vector2.zero,
                airFlowCarryDecay * Time.fixedDeltaTime);
        }

        public void Sync()
        {
            _airFlowCarryVelocity = _airFlowVelocity;
        }

        public void ResetState()
        {
            _isInAirFlow = false;
            _airFlowVelocity = Vector2.zero;
            _airFlowCarryVelocity = Vector2.zero;
        }
    }
}
