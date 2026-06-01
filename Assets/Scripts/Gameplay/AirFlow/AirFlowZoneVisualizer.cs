using UnityEngine;

namespace Icarus.Gameplay.AirFlow
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider2D))]
    public class AirFlowZoneVisualizer : MonoBehaviour
    {
        [Header("Sprite")]
        [SerializeField] private SpriteDrawMode spriteDrawMode = SpriteDrawMode.Sliced;

        private BoxCollider2D _airFlowCollider;
        private Transform _visualRoot;
        private SpriteRenderer _areaRenderer;

        private GameObject _inactiveOrb;
        private GameObject _activeOrb;

        private AirFlowParticleEffect _particleEffect;

        private bool _hasSyncedToCollider;
        private float _flowDirectionSign = 1f;

        private void Awake()
        {
            _hasSyncedToCollider = false;
            CacheReferences();

            if (_visualRoot == null)
            {
                Debug.LogError("AirFlowZoneVisualizer requires a VisualRoot Transform.", this);
                enabled = false;
                return;
            }

            if (_areaRenderer == null)
            {
                Debug.LogError("AirFlowZoneVisualizer requires an AreaVisual SpriteRenderer.", this);
                enabled = false;
                return;
            }

            if (_inactiveOrb == null)
            {
                Debug.LogError("AirFlowZoneVisualizer requires an inactive orb GameObject.", this);
                enabled = false;
                return;
            }

            if (_activeOrb == null)
            {
                Debug.LogError("AirFlowZoneVisualizer requires an active orb GameObject.", this);
                enabled = false;
                return;
            }

            if (_particleEffect == null)
            {
                Debug.LogError("AirFlowZoneVisualizer requires an AirFlowParticleEffect.", this);
                enabled = false;
                return;
            }
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void Start()
        {
            EnsureSyncedToCollider();
            SyncFlowDirection();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.isPlaying)
            {
                return;
            }

            CacheReferences();
            TrySyncToColliderInEditor();
            SyncFlowDirection();
        }
#endif

        private void CacheReferences()
        {
            _airFlowCollider = GetComponent<BoxCollider2D>();

            if (_visualRoot == null)
            {
                _visualRoot = FindChild("VisualRoot");
            }

            if (_areaRenderer == null)
            {
                Transform areaVisual = FindChild("AreaVisual");
                _areaRenderer = areaVisual != null ? areaVisual.GetComponent<SpriteRenderer>() : null;
            }

            if (_inactiveOrb == null)
            {
                Transform orbRed = FindChild("orb_red");
                _inactiveOrb = orbRed != null ? orbRed.gameObject : null;
            }

            if (_activeOrb == null)
            {
                Transform orbGreen = FindChild("orb_green");
                _activeOrb = orbGreen != null ? orbGreen.gameObject : null;
            }

            if (_particleEffect == null)
            {
                _particleEffect = GetComponentInChildren<AirFlowParticleEffect>(true);
            }
        }

        private void SyncFlowDirection()
        {
            _visualRoot.localRotation = _flowDirectionSign < 0f
                ? Quaternion.Euler(0f, 0f, 180f)
                : Quaternion.identity;
        }

        private void EnsureSyncedToCollider()
        {
            if (_hasSyncedToCollider)
            {
                return;
            }

            SyncToCollider();
        }

#if UNITY_EDITOR
        private void TrySyncToColliderInEditor()
        {
            if (_airFlowCollider == null || _visualRoot == null || _areaRenderer == null || _particleEffect == null)
            {
                return;
            }

            SyncToCollider();
        }
#endif

        private Transform FindChild(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }

        public void SetActivated(bool isActivated)
        {
            EnsureSyncedToCollider();
            SyncFlowDirection();

            _areaRenderer.gameObject.SetActive(isActivated);

            _inactiveOrb.SetActive(!isActivated);
            _activeOrb.SetActive(isActivated);

            _particleEffect.SetActivated(isActivated);
        }

        public void SyncToCollider()
        {
            Vector2 colliderOffset = _airFlowCollider.offset;
            Vector3 visualRootPosition = _visualRoot.localPosition;
            _visualRoot.localPosition = new Vector3(
                colliderOffset.x,
                colliderOffset.y,
                visualRootPosition.z);

            _areaRenderer.drawMode = spriteDrawMode;
            _areaRenderer.size = _airFlowCollider.size;

            _particleEffect.SyncToCollider(_airFlowCollider.size);
            
            _hasSyncedToCollider = true;
        }

        public void SetFlowDirection(float flowDirectionSign)
        {
            _flowDirectionSign = flowDirectionSign < 0f ? -1f : 1f;
            SyncFlowDirection();
        }
    }
}
