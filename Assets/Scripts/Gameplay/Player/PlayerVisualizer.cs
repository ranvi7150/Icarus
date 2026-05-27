using UnityEngine;

namespace Icarus.Gameplay.Player
{
    public class PlayerVisualizer : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Wing wing;

        private BodyVisual _bodyVisual;
        private WingVisual _wingVisual;
        private GlideBar _glideBar;
        private int _lastFacingDirection;
        private bool _lastIsWingOn;
        private float _lastGlideFillAmount = -1f;

        private void Awake()
        {
            if (playerController == null)
            {
                Debug.LogError("PlayerVisualizer requires a PlayerController component in parent hierarchy.", this);
                enabled = false;
                return;
            }

            if (wing == null)
            {
                Debug.LogError("PlayerVisualizer requires a Wing reference.", this);
                enabled = false;
                return;
            }

            _bodyVisual = GetComponentInChildren<BodyVisual>(true);
            if (_bodyVisual == null)
            {
                Debug.LogError("PlayerVisualizer requires a BodyVisual component in children.", this);
                enabled = false;
                return;
            }

            _wingVisual = wing.GetComponentInChildren<WingVisual>(true);
            if (_wingVisual == null)
            {
                Debug.LogError("PlayerVisualizer requires a WingVisual component under Wing.", this);
                enabled = false;
                return;
            }

            _glideBar = wing.GetComponentInChildren<GlideBar>(true);
            if (_glideBar == null)
            {
                Debug.LogError("PlayerVisualizer requires a GlideBar component under Wing.", this);
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            ApplyVisualState(playerController.FacingDirection, wing.IsWingOn, wing.GlideDurationNormalized);
        }

        private void Update()
        {
            int facingDirection = playerController.FacingDirection;
            bool isWingOn = wing.IsWingOn;
            float glideFillAmount = wing.GlideDurationNormalized;
            
            if (_lastFacingDirection == facingDirection
                && _lastIsWingOn == isWingOn
                && Mathf.Approximately(_lastGlideFillAmount, glideFillAmount))
            {
                return;
            }

            ApplyVisualState(facingDirection, isWingOn, glideFillAmount);
        }

        private void ApplyVisualState(int facingDirection, bool isWingOn, float glideFillAmount)
        {
            bool flipX = facingDirection > 0;

            _bodyVisual.ApplyFacing(flipX);
            _wingVisual.ApplyVisualState(flipX, isWingOn);
            _glideBar.ApplyVisualState(isWingOn, glideFillAmount);

            _lastFacingDirection = facingDirection;
            _lastIsWingOn = isWingOn;
            _lastGlideFillAmount = glideFillAmount;
        }
    }
}
