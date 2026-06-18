using System.Collections;
using Icarus.Core.Saving;
using UnityEngine;
using Icarus.Gameplay.Player;

namespace Icarus.Core.SceneManagement
{
    public class StageTransitionController : MonoBehaviour
    {
        private static string _arrivalPortalId;

        private bool _isTransitioning;

        public static bool TryGetArrivalPortalId(out string portalId)
        {
            portalId = _arrivalPortalId;
            _arrivalPortalId = null;
            return !string.IsNullOrWhiteSpace(portalId);
        }

        public void RequestSceneTransition(PlayerController player, string targetSceneName, string targetPortalId)
        {
            if (_isTransitioning || !player.IsAlive)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogError("StageTransitionController requires a target scene name.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(targetPortalId))
            {
                Debug.LogError("StageTransitionController requires a target portal ID.", this);
                return;
            }

            _isTransitioning = true;
            _arrivalPortalId = targetPortalId;
            player.BeginSceneTransition();
            StartCoroutine(SaveThenLoadScene(targetSceneName));
        }

        private IEnumerator SaveThenLoadScene(string targetSceneName)
        {
            GameProgressState.SetCurrentStage(targetSceneName);
            yield return SaveManager.SaveRoutine(GameProgressState.CurrentSaveData);
            ScreenFadeTransition.LoadScene(targetSceneName);
        }
    }
}
