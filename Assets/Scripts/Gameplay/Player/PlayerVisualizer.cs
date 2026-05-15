using UnityEngine;

namespace Icarus.Gameplay.Player
{
    public class PlayerVisualizer : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Wing wing;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer wingRenderer;

        private Vector3 _wingLocalPosition;
        private int _lastFacingDirection;
        private bool _lastIsWingOn;

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

            if (bodyRenderer == null)
            {
                Debug.LogError("PlayerVisualizer requires a Body SpriteRenderer reference.", this);
                enabled = false;
                return;
            }

            if (wingRenderer == null)
            {
                Debug.LogError("PlayerVisualizer requires a Wing SpriteRenderer reference.", this);
                enabled = false;
                return;
            }

            _wingLocalPosition = wingRenderer.transform.localPosition;
        }

        private void Start()
        {
            ApplyVisualState(playerController.FacingDirection, wing.IsWingOn);
        }

        private void Update()
        {
            int facingDirection = playerController.FacingDirection;
            bool isWingOn = wing.IsWingOn;
            if (_lastFacingDirection == facingDirection && _lastIsWingOn == isWingOn)
            {
                return;
            }

            ApplyVisualState(facingDirection, isWingOn);
        }

        private void ApplyVisualState(int facingDirection, bool isWingOn)
        {
            bool flipX = facingDirection > 0;

            bodyRenderer.flipX = flipX;
            wingRenderer.gameObject.SetActive(isWingOn);

            Vector3 wingPosition = _wingLocalPosition;
            wingPosition.x = Mathf.Abs(_wingLocalPosition.x) * (flipX ? -1f : 1f);
            wingRenderer.transform.localPosition = wingPosition;

            _lastFacingDirection = facingDirection;
            _lastIsWingOn = isWingOn;
        }
    }
}
