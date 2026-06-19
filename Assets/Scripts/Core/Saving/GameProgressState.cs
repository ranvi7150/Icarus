using UnityEngine;

namespace Icarus.Core.Saving
{
    public static class GameProgressState
    {
        private static SaveData _currentSaveData;

        public static SaveData CurrentSaveData
        {
            get
            {
                // Allows direct stage play in the editor without entering through Boot.
                EnsureInitialized();
                
                return _currentSaveData;
            }
        }

        public static bool IsInitialized => _currentSaveData != null;

        public static int FeatherCount => CurrentSaveData.featherCount;


        public static void Initialize(SaveData saveData)
        {
            _currentSaveData = saveData;
            SanitizeCurrent();
        }

        public static void SetCurrentStage(string stageName)
        {
            CurrentSaveData.currentStage = stageName ?? string.Empty;
        }

        public static void SetFeatherCount(int featherCount)
        {
            CurrentSaveData.featherCount = Mathf.Max(0, featherCount);
        }

        public static void SetDoorOpen(string doorId, bool isOpen)
        {
            if (isOpen)
            {
                if (!HasOpenedDoor(doorId))
                {
                    CurrentSaveData.openedDoorIds.Add(doorId);
                }

                return;
            }

            CurrentSaveData.openedDoorIds.Remove(doorId);
        }

        public static bool HasCollectedFeather(string featherId)
        {
            if (string.IsNullOrWhiteSpace(featherId))
            {
                return false;
            }

            return CurrentSaveData.collectedFeatherIds.Contains(featherId);
        }

        public static bool TryCollectFeather(string featherId)
        {
            if (string.IsNullOrWhiteSpace(featherId) || HasCollectedFeather(featherId))
            {
                return false;
            }

            CurrentSaveData.collectedFeatherIds.Add(featherId);
            CurrentSaveData.featherCount += 1;
            return true;
        }

        public static bool HasOpenedDoor(string doorId)
        {
            if (string.IsNullOrWhiteSpace(doorId))
            {
                return false;
            }

            return CurrentSaveData.openedDoorIds.Contains(doorId);
        }

        private static void EnsureInitialized()
        {
            if (_currentSaveData != null)
            {
                return;
            }

            Initialize(new SaveData());
        }

        private static void SanitizeCurrent()
        {
            _currentSaveData.currentStage ??= string.Empty;
            _currentSaveData.featherCount = Mathf.Max(0, _currentSaveData.featherCount);
            _currentSaveData.collectedFeatherIds ??= new System.Collections.Generic.List<string>();
            _currentSaveData.openedDoorIds ??= new System.Collections.Generic.List<string>();
        }
    }
}
