using UnityEngine;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WingVisual : MonoBehaviour
    {
        private SpriteRenderer _wingRenderer;
        private Vector3 _initialLocalPosition;

        private void Awake()
        {
            _wingRenderer = GetComponent<SpriteRenderer>();
            _initialLocalPosition = transform.localPosition;
        }

        public void ApplyVisualState(bool flipX, bool isVisible)
        {
            _wingRenderer.enabled = isVisible;

            Vector3 wingPosition = _initialLocalPosition;
            wingPosition.x = Mathf.Abs(_initialLocalPosition.x) * (flipX ? -1f : 1f);
            transform.localPosition = wingPosition;
        }
    }
}
