using System;
using UnityEngine;

namespace Icarus.Gameplay.Player
{
    public class Wing : MonoBehaviour
    {
        [Header("Wing State")]
        [SerializeField] private GameObject wingVisual;

        [Header("Glide")]
        [SerializeField] private float glideFallGravityMultiplier = 1.1f;
        [SerializeField] private float glideMaxFallSpeed = 6f;

        private bool isWingOn;

        public bool CanDash => !isWingOn;
        public bool CanUseAirFlow => isWingOn;
        public bool CanGlide => isWingOn;
        public float GlideFallGravityMultiplier => glideFallGravityMultiplier;
        public float GlideMaxFallSpeed => glideMaxFallSpeed;

        public event Action<bool> WingStateChanged;

        private void Awake()
        {
            isWingOn = false;

            if (wingVisual != null)
            {
                wingVisual.SetActive(false);
            }
        }

        public void ToggleWing()
        {
            SetWingState(!isWingOn);
        }

        private void SetWingState(bool isOn)
        {
            isWingOn = isOn;

            if (wingVisual != null)
            {
                wingVisual.SetActive(isWingOn);
            }

            WingStateChanged?.Invoke(isWingOn);
        }
    }
}
