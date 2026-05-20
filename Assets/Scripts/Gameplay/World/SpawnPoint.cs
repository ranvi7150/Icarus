using UnityEngine;

namespace Icarus.Gameplay.World
{
    public class SpawnPoint : MonoBehaviour
    {
        private const float GizmoRadius = 0.25f;

        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, GizmoRadius);
        }
    }
}
