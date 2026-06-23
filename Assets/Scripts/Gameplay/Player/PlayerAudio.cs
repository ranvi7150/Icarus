using Icarus.Core.Audio;
using UnityEngine;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip jumpClip;
        [SerializeField, Range(0f, 1f)] private float jumpVolumeScale = 1f;

        private PlayerMotor _playerMotor;
        private SoundManager _soundManager;

        private void Awake()
        {
            _playerMotor = GetComponent<PlayerMotor>();
            
            _soundManager = FindFirstObjectByType<SoundManager>();
            if (jumpClip != null && _soundManager == null)
            {
                Debug.LogError("PlayerAudio requires a SoundManager in the scene when Jump Clip is assigned.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (_playerMotor != null)
            {
                _playerMotor.Jumped += PlayJump;
            }
        }

        private void OnDisable()
        {
            if (_playerMotor != null)
            {
                _playerMotor.Jumped -= PlayJump;
            }
        }

        private void PlayJump()
        {
            if (jumpClip == null)
            {
                return;
            }

            _soundManager.PlaySfx(jumpClip, jumpVolumeScale);
        }
    }
}
