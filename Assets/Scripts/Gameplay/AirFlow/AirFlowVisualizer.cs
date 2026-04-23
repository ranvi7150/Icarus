using UnityEngine;

namespace Icarus.Gameplay.AirFlow
{
    [ExecuteAlways]
    public class AirFlowVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AirFlowZone airFlowZone;

        private void Reset()
        {
            if (airFlowZone == null)
            {
                airFlowZone = GetComponentInParent<AirFlowZone>();
            }
        }

        private void OnValidate()
        {
            UpdateArrowRotation();
        }

        private void LateUpdate()
        {
            UpdateArrowRotation();
        }

        private void UpdateArrowRotation()
        {
            if (airFlowZone == null)
            {
                return;
            }

            Vector2 direction = airFlowZone.AirFlowDirection;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
