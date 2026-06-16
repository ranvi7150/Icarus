using UnityEngine;

namespace Icarus.Core.Settings
{
    public static class GameSettingsState
    {
        private static GameSettings _currentSettings;

        public static GameSettings CurrentSettings
        {
            get
            {
                EnsureInitialized();
                return _currentSettings;
            }
        }

        public static bool IsInitialized => _currentSettings != null;

        public static void Initialize(GameSettings settings)
        {
            _currentSettings = settings ?? GameSettings.CreateDefault();
            SanitizeCurrent();
        }

        private static void EnsureInitialized()
        {
            if (_currentSettings != null)
            {
                return;
            }

            Initialize(GameSettings.CreateDefault());
        }

        private static void SanitizeCurrent()
        {
            _currentSettings.masterVolume = Mathf.Clamp01(_currentSettings.masterVolume);
            _currentSettings.bgmVolume = Mathf.Clamp01(_currentSettings.bgmVolume);
            _currentSettings.sfxVolume = Mathf.Clamp01(_currentSettings.sfxVolume);
            _currentSettings.screenWidth = Mathf.Max(0, _currentSettings.screenWidth);
            _currentSettings.screenHeight = Mathf.Max(0, _currentSettings.screenHeight);
        }
    }
}
