using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Icarus.Core.Settings
{
    public static class SettingsManager
    {
        private const string SettingsFileName = "settings";
        private const string SettingsExtension = ".json";

        public static event Action<GameSettings> SettingsApplied;

        public static GameSettings Load()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                return GameSettings.CreateDefault();
            }

            try
            {
                string json = File.ReadAllText(path);
                GameSettings settings = JsonConvert.DeserializeObject<GameSettings>(json);
                return settings ?? GameSettings.CreateDefault();
            }
            catch (JsonException exception)
            {
                Debug.LogError($"Failed to parse settings file at '{path}'. {exception.Message}");
                return GameSettings.CreateDefault();
            }
            catch (IOException exception)
            {
                Debug.LogError($"Failed to read settings file at '{path}'. {exception.Message}");
                return GameSettings.CreateDefault();
            }
        }

        public static bool Save(GameSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("SettingsManager requires GameSettings to save.");
                return false;
            }

            string path = GetSavePath();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(path, json);
                return true;
            }
            catch (JsonException exception)
            {
                Debug.LogError($"Failed to serialize settings. {exception.Message}");
                return false;
            }
            catch (IOException exception)
            {
                Debug.LogError($"Failed to write settings file at '{path}'. {exception.Message}");
                return false;
            }
        }

        public static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SettingsFileName + SettingsExtension);
        }

        public static void EnsureLoaded()
        {
            // Allows direct main menu play in the editor without entering through Boot.
            if (GameSettingsState.IsInitialized)
            {
                return;
            }

            GameSettingsState.Initialize(Load());
        }

        public static void Apply(GameSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("SettingsManager requires GameSettings to apply.");
                return;
            }

            GameSettings currentSettings = settings;

            AudioListener.volume = currentSettings.masterVolume;

            if (currentSettings.screenWidth > 0 && currentSettings.screenHeight > 0)
            {
                Screen.SetResolution(currentSettings.screenWidth, currentSettings.screenHeight, currentSettings.fullscreen);
            }
            else
            {
                Screen.fullScreen = currentSettings.fullscreen;
            }

            SettingsApplied?.Invoke(currentSettings);
        }
    }
}
