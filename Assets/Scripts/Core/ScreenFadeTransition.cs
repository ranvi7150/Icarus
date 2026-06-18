using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Icarus.Core.SceneManagement
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFadeTransition : MonoBehaviour
    {
        private static ScreenFadeTransition _instance;

        [SerializeField] private float fadeDuration = 0.35f;

        private CanvasGroup _canvasGroup;
        private bool _isTransitioning;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public static void LoadScene(string targetSceneName)
        {
            ScreenFadeTransition transition = FindTransition();
            if (transition == null || !transition.enabled)
            {
                return;
            }

            transition.BeginLoadScene(targetSceneName);
        }

        private static ScreenFadeTransition FindTransition()
        {
            if (_instance != null)
            {
                return _instance;
            }

            ScreenFadeTransition existingTransition = FindFirstObjectByType<ScreenFadeTransition>();
            if (existingTransition != null)
            {
                return existingTransition;
            }

            Debug.LogError("ScreenFadeTransition requires a scene object with ScreenFadeTransition and CanvasGroup components.");
            return null;
        }

        private void BeginLoadScene(string targetSceneName)
        {
            if (_isTransitioning)
            {
                return;
            }

            StartCoroutine(LoadSceneWithFade(targetSceneName));
        }

        private IEnumerator LoadSceneWithFade(string targetSceneName)
        {
            _isTransitioning = true;
            _canvasGroup.blocksRaycasts = true;

            yield return FadeTo(1f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            if (operation == null)
            {
                Debug.LogError($"ScreenFadeTransition failed to start loading scene '{targetSceneName}'.", this);
                _isTransitioning = false;
                _canvasGroup.blocksRaycasts = false;
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return FadeTo(0f);

            _canvasGroup.blocksRaycasts = false;
            _isTransitioning = false;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float duration = Mathf.Max(0f, fadeDuration);

            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, normalizedTime));
                yield return null;
            }

            SetAlpha(targetAlpha);
        }

        private void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }
}
