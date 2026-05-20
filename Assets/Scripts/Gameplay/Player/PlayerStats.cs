using System;
using UnityEngine;
using Icarus.Core;

namespace Icarus.Gameplay.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Feather")]
        [SerializeField] private Progression progression;
        [SerializeField, Min(0)] private int startFeatherCount;

        private int _featherCount;
        private int _currentFeatherIndex = -1;

        public int FeatherCount => _featherCount;
        public int CurrentFeatherIndex => _currentFeatherIndex;

        public bool CanDash => progression.IsDashUnlocked(_featherCount);
        public bool CanWingToggle => progression.IsWingToggleUnlocked(_featherCount);
        public bool HasGlideDurationUpgrade => progression.HasGlideDurationUpgrade(_featherCount);
        public float GlideDurationSeconds => progression.GetGlideDurationSeconds(_featherCount);

        public event Action<int> FeatherCountChanged;
        public event Action<int> FeatherIndexReached;

        private void Awake()
        {
            if (progression == null)
            {
                Debug.LogError("PlayerStats requires a Progression asset reference.", this);
                enabled = false;
                return;
            }

            _featherCount = Mathf.Max(startFeatherCount, PlayerProgressState.FeatherCount);
            PlayerProgressState.SetFeatherCount(_featherCount);
            RecalculateProgress(logFeatherIndexChanges: false);
        }

        public bool TryCollectFeather()
        {
            PlayerProgressState.AddFeather();
            _featherCount = PlayerProgressState.FeatherCount;

            Debug.Log($"Feather picked up: +1 (current: {_featherCount})", this);

            RecalculateProgress(logFeatherIndexChanges: true);
            FeatherCountChanged?.Invoke(_featherCount);
            return true;
        }

        private void RecalculateProgress(bool logFeatherIndexChanges)
        {
            int nextFeatherIndex = progression.GetReachedFeatherIndex(_featherCount);

            if (logFeatherIndexChanges && nextFeatherIndex > _currentFeatherIndex)
            {
                string debugMessage = progression.GetFeatherIndexDebugMessage(nextFeatherIndex);

                if (!string.IsNullOrWhiteSpace(debugMessage))
                {
                    Debug.Log(debugMessage, this);
                }

                FeatherIndexReached?.Invoke(nextFeatherIndex);
            }

            _currentFeatherIndex = nextFeatherIndex;
        }
    }
}
