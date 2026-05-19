using UnityEngine;
using UnityEngine.SceneManagement;
using Icarus.Gameplay.Player;

namespace Icarus.Core.SceneManagement
{
    public class StageTransitionController : MonoBehaviour
    {
        private static string _pendingSpawnPortalId;

        private bool _isTransitioning;

        public static bool TryConsumePendingSpawnPortalId(out string portalId)
        {
            portalId = _pendingSpawnPortalId;
            _pendingSpawnPortalId = null;
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
            _pendingSpawnPortalId = targetPortalId;
            player.BeginSceneTransition();
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
