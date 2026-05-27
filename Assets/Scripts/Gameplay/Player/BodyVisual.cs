using UnityEngine;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BodyVisual : MonoBehaviour
    {
        private SpriteRenderer _bodyRenderer;

        private void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
        }

        public void ApplyFacing(bool flipX)
        {
            _bodyRenderer.flipX = flipX;
        }
    }
}
