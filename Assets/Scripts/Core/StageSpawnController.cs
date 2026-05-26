using System.Collections;
using UnityEngine;
using Icarus.Gameplay.Player;
using Icarus.Gameplay.World;

namespace Icarus.Core.SceneManagement
{
    public class StageSpawnController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerController player;

        [Header("Spawn")]
        [SerializeField] private Portal defaultSpawnPortal;
        [SerializeField] private Portal[] stagePortals;
        [SerializeField] private float deathRespawnDelay = 1f;
        [SerializeField] private bool spawnOnStart = true;

        private Coroutine _respawnRoutine;
        private bool _isInitialized;

        private void Awake()
        {
            if (player == null)
            {
                Debug.LogError("StageSpawnController requires a PlayerController reference.", this);
                enabled = false;
                return;
            }

            if (defaultSpawnPortal == null)
            {
                Debug.LogError("StageSpawnController requires a Default Spawn Portal reference.", this);
                enabled = false;
                return;
            }

            if (stagePortals == null || stagePortals.Length == 0)
            {
                Debug.LogError("StageSpawnController requires Stage Portal references.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < stagePortals.Length; i++)
            {
                if (stagePortals[i] == null)
                {
                    Debug.LogError("StageSpawnController has an empty Stage Portal reference.", this);
                    enabled = false;
                    return;
                }
            }

            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                return;
            }

            player.Died += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (!_isInitialized)
            {
                return;
            }

            player.Died -= HandlePlayerDied;
        }

        private void Start()
        {
            if (!spawnOnStart)
            {
                return;
            }

            SpawnAtInitialPortal();
        }

        private void HandlePlayerDied()
        {
            if (_respawnRoutine != null)
            {
                return;
            }

            _respawnRoutine = StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, deathRespawnDelay));

            SpawnAtPortal(defaultSpawnPortal);
            _respawnRoutine = null;
        }

        private void SpawnAtInitialPortal()
        {
            if (StageTransitionController.TryGetArrivalPortalId(out string portalId))
            {
                if (!TryGetPortal(portalId, out Portal transitionPortal))
                {
                    Debug.LogError($"StageSpawnController could not find a Portal with ID '{portalId}'.", this);
                    enabled = false;
                    return;
                }

                SpawnAtPortal(transitionPortal);
                return;
            }

            SpawnAtPortal(defaultSpawnPortal);
        }

        private void SpawnAtPortal(Portal portal)
        {
            player.RespawnAt(portal.SpawnPosition);
        }

        private bool TryGetPortal(string portalId, out Portal portal)
        {
            for (int i = 0; i < stagePortals.Length; i++)
            {
                if (stagePortals[i].PortalId == portalId)
                {
                    portal = stagePortals[i];
                    return true;
                }
            }

            portal = null;
            return false;
        }
    }
}
