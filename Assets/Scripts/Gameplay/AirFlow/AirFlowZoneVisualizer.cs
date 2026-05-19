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
        private SpriteRenderer _visualRenderer;

        private void Awake()
        {
            CacheReferences();

            if (_visualRenderer == null)
            {
                Debug.LogError("AirFlowZoneVisualizer requires a child SpriteRenderer.", this);
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
            SyncToCollider();
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
        }
#endif

        public void SetActivated(bool isActivated)
        {
            _visualRenderer.gameObject.SetActive(isActivated);
        }

        public void SyncToCollider()
        {
            Vector2 colliderOffset = _airFlowCollider.offset;
            Vector3 visualPosition = _visualRenderer.transform.localPosition;
            _visualRenderer.transform.localPosition = new Vector3(
                colliderOffset.x,
                colliderOffset.y,
                visualPosition.z);

            _visualRenderer.drawMode = spriteDrawMode;
            _visualRenderer.size = _airFlowCollider.size;
        }

        private void CacheReferences()
        {
            _airFlowCollider = GetComponent<BoxCollider2D>();
            _visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

#if UNITY_EDITOR
        private void TrySyncToColliderInEditor()
        {
            if (_airFlowCollider == null || _visualRenderer == null)
            {
                return;
            }

            SyncToCollider();
        }
#endif
    }
}
