using UnityEngine;
using Icarus.Gameplay.Player;
using TMPro;

namespace Icarus.UI.HUD
{
    public class FeatherCounterUI : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private TMP_Text featherCountText;

        private void Awake()
        {
            if (playerStats == null)
            {
                Debug.LogError("FeatherCounterUI requires a PlayerStats reference.", this);
                enabled = false;
                return;
            }

            if (featherCountText == null)
            {
                Debug.LogError("FeatherCounterUI requires a UI Text reference.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            playerStats.FeatherCountChanged += HandleFeatherCountChanged;
            UpdateCountText(playerStats.FeatherCount);
        }

        private void OnDisable()
        {
            playerStats.FeatherCountChanged -= HandleFeatherCountChanged;
        }

        private void HandleFeatherCountChanged(int featherCount)
        {
            UpdateCountText(featherCount);
        }

        private void UpdateCountText(int featherCount)
        {
            featherCountText.text = $" x {featherCount}";
        }
    }
}