using Icarus.Core.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Icarus.UI
{
    public class OptionsPanel : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Value Labels")]
        [SerializeField] private TMP_Text masterVolumeValueText;
        [SerializeField] private TMP_Text bgmVolumeValueText;
        [SerializeField] private TMP_Text sfxVolumeValueText;

        private void Awake()
        {
            if (masterVolumeSlider == null)
            {
                Debug.LogError("OptionsPanel requires a Master Volume Slider reference.", this);
                enabled = false;
                return;
            }

            if (bgmVolumeSlider == null)
            {
                Debug.LogError("OptionsPanel requires a BGM Volume Slider reference.", this);
                enabled = false;
                return;
            }

            if (sfxVolumeSlider == null)
            {
                Debug.LogError("OptionsPanel requires an SFX Volume Slider reference.", this);
                enabled = false;
                return;
            }

            if (fullscreenToggle == null)
            {
                Debug.LogError("OptionsPanel requires a Fullscreen Toggle reference.", this);
                enabled = false;
                return;
            }

            ConfigureSlider(masterVolumeSlider);
            ConfigureSlider(bgmVolumeSlider);
            ConfigureSlider(sfxVolumeSlider);

            masterVolumeSlider.onValueChanged.AddListener(_ => RefreshValueLabels());
            bgmVolumeSlider.onValueChanged.AddListener(_ => RefreshValueLabels());
            sfxVolumeSlider.onValueChanged.AddListener(_ => RefreshValueLabels());
        }

        public void Open()
        {
            gameObject.SetActive(true);

            if (!enabled)
            {
                return;
            }

            SettingsManager.EnsureLoaded();
            Bind(GameSettingsState.CurrentSettings);
        }

        public void Apply()
        {
            GameSettings settings = ReadFromControls();
            GameSettingsState.Initialize(settings);
            SettingsManager.Apply(GameSettingsState.CurrentSettings);
            SettingsManager.Save(GameSettingsState.CurrentSettings);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private static void ConfigureSlider(Slider slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private void Bind(GameSettings settings)
        {
            masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
            bgmVolumeSlider.SetValueWithoutNotify(settings.bgmVolume);
            sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);
            fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);

            RefreshValueLabels();
        }

        private GameSettings ReadFromControls()
        {
            SettingsManager.EnsureLoaded();
            GameSettings settings = GameSettingsState.CurrentSettings.Clone();
            settings.masterVolume = masterVolumeSlider.value;
            settings.bgmVolume = bgmVolumeSlider.value;
            settings.sfxVolume = sfxVolumeSlider.value;
            settings.fullscreen = fullscreenToggle.isOn;
            return settings;
        }

        private void RefreshValueLabels()
        {
            SetVolumeLabel(masterVolumeValueText, masterVolumeSlider.value);
            SetVolumeLabel(bgmVolumeValueText, bgmVolumeSlider.value);
            SetVolumeLabel(sfxVolumeValueText, sfxVolumeSlider.value);
        }

        private static void SetVolumeLabel(TMP_Text label, float value)
        {
            if (label == null)
            {
                return;
            }

            label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }

    }
}
