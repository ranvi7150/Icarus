using UnityEngine;
using Icarus.Core.SceneManagement;
using Icarus.Gameplay.Player;

namespace Icarus.Gameplay.World
{
    [RequireComponent(typeof(Collider2D))]
    public class Portal : MonoBehaviour
    {
        [SerializeField] private string portalId;

        // Leave target fields empty to use this portal as a 'Spawn Point Only'.
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetPortalId;
        [SerializeField] private StageTransitionController stageTransitionController;

        private SpawnPoint _spawnPoint;
        private bool _hasSceneTransition;

        public string PortalId => portalId;
        public Vector3 SpawnPosition => _spawnPoint.Position;

        private void Awake()
        {
            _spawnPoint = GetComponentInChildren<SpawnPoint>();
            bool hasTargetScene = !string.IsNullOrWhiteSpace(targetSceneName);
            bool hasTargetPortal = !string.IsNullOrWhiteSpace(targetPortalId);
            _hasSceneTransition = hasTargetScene && hasTargetPortal;

            if (string.IsNullOrWhiteSpace(portalId))
            {
                Debug.LogError("Portal requires a Portal ID.", this);
                enabled = false;
                return;
            }

            if (_spawnPoint == null)
            {
                Debug.LogError("Portal requires a SpawnPoint component in children.", this);
                enabled = false;
                return;
            }

            if (hasTargetScene != hasTargetPortal)
            {
                Debug.LogError("Portal scene transitions require both Target Scene Name and Target Portal ID.", this);
                enabled = false;
                return;
            }

            if (_hasSceneTransition && stageTransitionController == null)
            {
                Debug.LogError("Portal requires a StageTransitionController reference for scene transitions.", this);
                enabled = false;
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_hasSceneTransition)
            {
                return;
            }

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || !rb.CompareTag("Player"))
            {
                return;
            }

            PlayerController player = rb.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            stageTransitionController.RequestSceneTransition(player, targetSceneName, targetPortalId);
        }
    }
}
