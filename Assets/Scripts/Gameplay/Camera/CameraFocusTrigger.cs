using Icarus.Gameplay.Interaction;
using UnityEngine;

namespace Icarus.Gameplay.Camera
{
    public class CameraFocusTrigger : MonoBehaviour, IActivatable
    {
        [Header("Focus")]
        [SerializeField] private CameraFocusController cameraFocusController;
        [SerializeField, Min(0f)] private float focusDuration = 2.5f;

        private void Awake()
        {
            if (cameraFocusController == null)
            {
                Debug.LogError("CameraFocusTrigger requires a Camera Focus Controller reference.", this);
                enabled = false;
                return;
            }
        }

        public void Activate()
        {
            if (!enabled)
            {
                return;
            }

            cameraFocusController.Focus(transform, focusDuration);
        }
    }
}
