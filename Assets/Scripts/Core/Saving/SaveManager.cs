using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Icarus.Core.Saving
{
    public static class SaveManager
    {
        private const string SaveFileName = "save";
        private const string SaveExtension = ".json";

        public static SaveData Load()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                return new SaveData();
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);
                return saveData ?? new SaveData();
            }
            catch (JsonException exception)
            {
                Debug.LogError($"Failed to parse save file at '{path}'. {exception.Message}");
                return new SaveData();
            }
            catch (IOException exception)
            {
                Debug.LogError($"Failed to read save file at '{path}'. {exception.Message}");
                return new SaveData();
            }
        }

        public static bool Save(SaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("SaveManager requires SaveData to save.");
                return false;
            }

            string path = GetSavePath();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(path, json);
                return true;
            }
            catch (JsonException exception)
            {
                Debug.LogError($"Failed to serialize save data. {exception.Message}");
                return false;
            }
            catch (IOException exception)
            {
                Debug.LogError($"Failed to write save file at '{path}'. {exception.Message}");
                return false;
            }
        }

        public static IEnumerator SaveRoutine(SaveData saveData)
        {
            Save(saveData);
            yield break;
        }

        public static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName + SaveExtension);
        }
    }
}
