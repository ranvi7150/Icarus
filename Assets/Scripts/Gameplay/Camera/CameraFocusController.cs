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

        [Header("Focus")]
        [SerializeField] private int focusPriority = 20;
        [SerializeField] private int inactiveFocusPriority = -1;
        [SerializeField, Min(0f)] private float defaultFocusDuration = 2.5f;

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
            _focusRestPriority = Mathf.Min(inactiveFocusPriority, _followPriority - 1);
            RestoreFollowCamera();
        }

        private void OnDisable()
        {
            if (_focusRoutine != null)
            {
                StopCoroutine(_focusRoutine);
                _focusRoutine = null;
            }

            RestoreFollowCamera();
        }

        public void Focus(Transform target)
        {
            Focus(target, defaultFocusDuration);
        }

        public void Focus(Transform target, float duration)
        {
            if (!enabled)
            {
                return;
            }

            if (target == null)
            {
                Debug.LogError("CameraFocusController cannot focus a missing target.", this);
                return;
            }

            if (_focusRoutine != null)
            {
                StopCoroutine(_focusRoutine);
            }

            _focusRoutine = StartCoroutine(FocusRoutine(target, duration));
        }

        private IEnumerator FocusRoutine(Transform target, float duration)
        {
            focusCamera.Follow = target;
            focusCamera.Priority = Mathf.Max(focusPriority, _followPriority + 1);

            yield return new WaitForSeconds(duration > 0f ? duration : defaultFocusDuration);

            RestoreFollowCamera();
            _focusRoutine = null;
        }

        private void RestoreFollowCamera()
        {
            if (followCamera == null || focusCamera == null)
            {
                return;
            }

            followCamera.Priority = _followPriority;
            focusCamera.Priority = _focusRestPriority;
        }
    }
}
