using UnityEngine;

namespace Icarus.Gameplay.Player
{
    [CreateAssetMenu(fileName = "Progression", menuName = "Scriptable Objects/Progression")]
    public class Progression : ScriptableObject
    {
        [Header("Unlock Feathers")]
        [SerializeField, Min(1)] private int dashUnlockFeathers = 1;
        [SerializeField, Min(1)] private int wingToggleUnlockFeathers = 3;
        [SerializeField, Min(1)] private int glideDurationUpgradeFeathers = 5;
        [SerializeField, Min(1)] private int doubleJumpUnlockFeathers = 7;

        [Header("Glide Duration")]
        [SerializeField, Min(0.1f)] private float baseGlideDurationSeconds = 1f;
        [SerializeField, Min(0.1f)] private float upgradedGlideDurationSeconds = 3f;

        public bool IsDashUnlocked(int featherCount)
        {
            return featherCount >= dashUnlockFeathers;
        }

        public bool IsWingToggleUnlocked(int featherCount)
        {
            return featherCount >= wingToggleUnlockFeathers;
        }

        public bool HasGlideDurationUpgrade(int featherCount)
        {
            return featherCount >= glideDurationUpgradeFeathers;
        }

        public bool IsDoubleJumpUnlocked(int featherCount)
        {
            return featherCount >= doubleJumpUnlockFeathers;
        }

        public float GetGlideDurationSeconds(int featherCount)
        {
            return HasGlideDurationUpgrade(featherCount)
                ? upgradedGlideDurationSeconds
                : baseGlideDurationSeconds;
        }

        public int GetReachedFeatherIndex(int featherCount)
        {
            if (featherCount < dashUnlockFeathers)
            {
                return -1;
            }

            if (featherCount < wingToggleUnlockFeathers)
            {
                return 0;
            }

            if (featherCount < glideDurationUpgradeFeathers)
            {
                return 1;
            }

            if (featherCount < doubleJumpUnlockFeathers)
            {
                return 2;
            }

            return 3;
        }

        public string GetFeatherIndexDebugMessage(int featherIndex)
        {
            switch (featherIndex)
            {
                case 0:
                    return $"Reached {dashUnlockFeathers} feather(s): Dash unlocked.";
                case 1:
                    return $"Reached {wingToggleUnlockFeathers} feather(s): Wing toggle unlocked.";
                case 2:
                    return $"Reached {glideDurationUpgradeFeathers} feather(s): Glide duration increased.";
                case 3:
                    return $"Reached {doubleJumpUnlockFeathers} feather(s): Double jump unlocked.";
                default:
                    return string.Empty;
            }
        }

        
        private void OnValidate()
        {
            wingToggleUnlockFeathers = Mathf.Max(wingToggleUnlockFeathers, dashUnlockFeathers + 1);
            glideDurationUpgradeFeathers = Mathf.Max(glideDurationUpgradeFeathers, wingToggleUnlockFeathers + 1);
            doubleJumpUnlockFeathers = Mathf.Max(doubleJumpUnlockFeathers, glideDurationUpgradeFeathers + 1);
            upgradedGlideDurationSeconds = Mathf.Max(upgradedGlideDurationSeconds, baseGlideDurationSeconds);
        }
    }
}
