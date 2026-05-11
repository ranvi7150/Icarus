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

        private PlayerStats _playerStats;
        private bool isWingOn;
        private float _remainingGlideDuration;

        public bool CanDash => !isWingOn;
        public bool CanGlide => isWingOn && _remainingGlideDuration > 0f;
        public bool CanUseAirFlow => CanGlide;
        public float GlideFallGravityMultiplier => glideFallGravityMultiplier;
        public float GlideMaxFallSpeed => glideMaxFallSpeed;

        public event Action<bool> WingStateChanged;

        private void Awake()
        {
            _playerStats = GetComponentInParent<PlayerStats>();
            if (_playerStats == null)
            {
                Debug.LogError("Wing requires a PlayerStats component in parent hierarchy.", this);
                enabled = false;
                return;
            }

            isWingOn = false;

            if (wingVisual != null)
            {
                wingVisual.SetActive(false);
            }
        }

        private void Start()
        {
            _remainingGlideDuration = GetMaxGlideDurationSeconds();
        }

        public void ToggleWing()
        {
            if (!CanToggleWing())
            {
                return;
            }

            if (!isWingOn && _remainingGlideDuration <= 0f)
            {
                return;
            }

            SetWingState(!isWingOn);
        }

        private bool CanToggleWing()
        {
            return _playerStats.CanWingToggle;
        }

        private void SetWingState(bool isOn)
        {
            if (isWingOn == isOn)
            {
                return;
            }

            isWingOn = isOn;

            if (wingVisual != null)
            {
                wingVisual.SetActive(isWingOn);
            }

            WingStateChanged?.Invoke(isWingOn);
        }

        public void TickGlideDuration(bool isGrounded, float deltaTime)
        {
            if (!CanToggleWing())
            {
                return;
            }

            if (isGrounded)
            {
                _remainingGlideDuration = GetMaxGlideDurationSeconds();
                return;
            }

            if (!isWingOn)
            {
                return;
            }

            _remainingGlideDuration = Mathf.Max(0f, _remainingGlideDuration - Mathf.Max(0f, deltaTime));
            if (_remainingGlideDuration <= 0f)
            {
                SetWingState(false);
            }
        }
        private float GetMaxGlideDurationSeconds()
        {
            return Mathf.Max(0.1f, _playerStats.GlideDurationSeconds);
        }
    }
}
