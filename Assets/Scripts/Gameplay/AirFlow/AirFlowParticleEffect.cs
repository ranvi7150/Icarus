using UnityEngine;

namespace Icarus.Gameplay.AirFlow
{
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public class AirFlowParticleEffect : MonoBehaviour
    {
        [SerializeField] private float referenceFlowLength = 10f;
        [SerializeField] private float referenceFlowDuration = 0.8f;

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

        private void SyncTimingToCollider(float flowLength)
        {
            float lengthScale = flowLength / Mathf.Max(referenceFlowLength, 0.01f);
            float flowDuration = Mathf.Max(
                referenceFlowDuration * lengthScale,
                MinimumParticleDuration);

            if (_particles.isPlaying)
            {
                return;
            }

            ParticleSystem.MainModule main = _particles.main;
            main.duration = flowDuration;
            main.startLifetime = flowDuration;

            ParticleSystem.EmissionModule emission = _particles.emission;
            ParticleSystem.Burst[] bursts =
            {
                new ParticleSystem.Burst(0f, 1),
                new ParticleSystem.Burst(flowDuration * 0.5f, 1)
            };

            emission.SetBursts(bursts);
        }

        public void SyncToCollider(Vector2 colliderSize)
        {
            float flowLength = colliderSize.x;
            float flowWidth = colliderSize.y;
            Vector3 particlePosition = transform.localPosition;
            transform.localPosition = new Vector3(
                -flowLength * 0.5f,
                0f,
                particlePosition.z);

            ParticleSystem.MainModule main = _particles.main;
            main.startSize3D = true;
            main.startSizeY = flowWidth;
            main.startSizeZ = 1f;

            SyncTimingToCollider(flowLength);
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
