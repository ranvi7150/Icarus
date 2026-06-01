using UnityEngine;

namespace Icarus.Gameplay.AirFlow
{
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public class AirFlowParticleEffect : MonoBehaviour
    {
        [SerializeField] private float referenceColliderWidth = 10f;
        [SerializeField] private float referenceParticleDuration = 0.8f;

        private const float MinimumParticleDuration = 0.01f;
        private ParticleSystem _particles;

        private void Awake()
        {
            CacheReferences();

            if (_particles == null)
            {
                Debug.LogError("AirFlowParticleEffect requires a ParticleSystem.", this);
                enabled = false;
                return;
            }
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void CacheReferences()
        {
            if (_particles == null)
            {
                _particles = GetComponent<ParticleSystem>();
            }
        }

        private void SyncTimingToCollider(float colliderWidth)
        {
            float widthScale = colliderWidth / Mathf.Max(referenceColliderWidth, 0.01f);
            float particleDuration = Mathf.Max(
                referenceParticleDuration * widthScale,
                MinimumParticleDuration);
                
            bool shouldRestart = Application.isPlaying
                && gameObject.activeInHierarchy
                && _particles.isPlaying;

            if (!Application.isPlaying && _particles.isPlaying)
            {
                return;
            }

            if (shouldRestart)
            {
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ParticleSystem.MainModule main = _particles.main;
            main.duration = particleDuration;
            main.startLifetime = particleDuration;

            ParticleSystem.EmissionModule emission = _particles.emission;
            ParticleSystem.Burst[] bursts =
            {
                new ParticleSystem.Burst(0f, 1),
                new ParticleSystem.Burst(particleDuration * 0.5f, 1)
            };

            emission.SetBursts(bursts);

            if (shouldRestart)
            {
                _particles.Play(true);
            }
        }

        public void SyncToCollider(Vector2 colliderSize)
        {
            Vector3 particlePosition = transform.localPosition;
            transform.localPosition = new Vector3(
                -colliderSize.x * 0.5f,
                0f,
                particlePosition.z);

            ParticleSystem.MainModule main = _particles.main;
            main.startSize3D = true;
            main.startSizeY = colliderSize.y;
            main.startSizeZ = 1f;

            SyncTimingToCollider(colliderSize.x);
        }

        public void SetActivated(bool isActivated)
        {
            if (isActivated)
            {
                gameObject.SetActive(true);
                _particles.Play(true);
                return;
            }

            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }
    }
}
