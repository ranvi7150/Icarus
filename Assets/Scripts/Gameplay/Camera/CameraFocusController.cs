using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Icarus.Gameplay.Camera
{
    public class CameraFocusController : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private CinemachineCamera followCamera;
        [SerializeField] private CinemachineCamera focusCamera;

        private int _followPriority;
        private int _focusRestPriority;
        private Coroutine _focusRoutine;

        private void Awake()
        {
            if (followCamera == null)
            {
                Debug.LogError("CameraFocusController requires a Follow Camera reference.", this);
                enabled = false;
                return;
            }

            if (focusCamera == null)
            {
                Debug.LogError("CameraFocusController requires a Focus Camera reference.", this);
                enabled = false;
                return;
            }

            _followPriority = followCamera.Priority;
            _focusRestPriority = _followPriority - 1;
            RestoreFollowCamera();
        }

        private IEnumerator FocusRoutine(Transform target, float duration)
        {
            focusCamera.Follow = target;
            focusCamera.Priority = _followPriority + 1;

            yield return new WaitForSeconds(duration);

            RestoreFollowCamera();
            _focusRoutine = null;
        }

        private void RestoreFollowCamera()
        {
            followCamera.Priority = _followPriority;
            focusCamera.Priority = _focusRestPriority;
        }

        public void Focus(Transform target, float duration)
        {
            if (_focusRoutine != null)
            {
                StopCoroutine(_focusRoutine);
            }

            _focusRoutine = StartCoroutine(FocusRoutine(target, duration));
        }
    }
}
