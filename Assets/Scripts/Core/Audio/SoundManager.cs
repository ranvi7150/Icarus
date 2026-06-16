using Icarus.Core.Settings;
using UnityEngine;

namespace Icarus.Core.Audio
{
    public class SoundManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("BGM")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

        [Header("Common SFX")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private void Awake()
        {
            if (bgmSource == null)
            {
                Debug.LogError("SoundManager requires a BGM AudioSource reference.", this);
                enabled = false;
                return;
            }

            if (sfxSource == null)
            {
                Debug.LogError("SoundManager requires an SFX AudioSource reference.", this);
                enabled = false;
                return;
            }

            bgmSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            SettingsManager.SettingsApplied += ApplySettings;

            if (GameSettingsState.IsInitialized)
            {
                ApplySettings(GameSettingsState.CurrentSettings);
            }
        }

        private void OnDisable()
        {
            SettingsManager.SettingsApplied -= ApplySettings;
        }

        private void Start()
        {
            PlayBgm();
        }

        public void PlayBgm()
        {
            if (bgmClip == null)
            {
                return;
            }

            if (bgmSource.clip == bgmClip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            bgmSource.Stop();
        }

        public void PlayButtonClick()
        {
            PlaySfx(buttonClickClip);
        }

        public void PlaySfx(AudioClip clip)
        {
            PlaySfx(clip, 1f);
        }

        public void PlaySfx(AudioClip clip, float volumeScale)
        {
            if (clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
        }

        private void ApplySettings(GameSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            SetBgmVolume(settings.bgmVolume);
            SetSfxVolume(settings.sfxVolume);
        }
        
        public void SetBgmVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            bgmSource.volume = bgmVolume;
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
    }
}
