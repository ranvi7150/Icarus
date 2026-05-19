using UnityEngine;
using Icarus.Core.SceneManagement;
using Icarus.Gameplay.Player;

namespace Icarus.Gameplay.World
{
    [RequireComponent(typeof(Collider2D))]
    public class Portal : MonoBehaviour
    {
        private const string SpawnPointName = "SpawnPoint";

        [SerializeField] private string portalId;
        [SerializeField] private string targetSceneName;
        [SerializeField] private string targetPortalId;
        [SerializeField] private StageTransitionController stageTransitionController;

        private Collider2D _portalCollider;
        private Transform _spawnPoint;

        public string PortalId => portalId;
        public Vector3 SpawnPosition => _spawnPoint.position;

        private void Awake()
        {
            _portalCollider = GetComponent<Collider2D>();
            _portalCollider.isTrigger = true;
            _spawnPoint = transform.Find(SpawnPointName);

            if (string.IsNullOrWhiteSpace(portalId))
            {
                Debug.LogError("Portal requires a Portal ID.", this);
                enabled = false;
                return;
            }

            if (_spawnPoint == null)
            {
                Debug.LogError("Portal requires a child SpawnPoint transform.", this);
                enabled = false;
                return;
            }

            if (HasSceneTransition() && stageTransitionController == null)
            {
                Debug.LogError("Portal requires a StageTransitionController reference for scene transitions.", this);
                enabled = false;
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!HasSceneTransition())
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

        private bool HasSceneTransition()
        {
            return !string.IsNullOrWhiteSpace(targetSceneName)
                && !string.IsNullOrWhiteSpace(targetPortalId);
        }
    }
}
