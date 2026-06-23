using Icarus.Core.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Icarus.UI
{
    public class StagePauseMenu : MonoBehaviour
    {
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private OptionsPanel optionsPanel;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool _isPaused;
        private float _previousTimeScale = 1f;

        private void Awake()
        {
            if (pauseAction == null || pauseAction.action == null)
            {
                Debug.LogError("StagePauseMenu requires a Pause Action reference.", this);
                enabled = false;
                return;
            }

            if (pauseMenuRoot == null)
            {
                Debug.LogError("StagePauseMenu requires a Pause Menu Root reference.", this);
                enabled = false;
                return;
            }

            if (optionsPanel == null)
            {
                Debug.LogError("StagePauseMenu requires an Options Panel reference.", this);
                enabled = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogError("StagePauseMenu requires a Main Menu Scene Name.", this);
                enabled = false;
                return;
            }

            pauseMenuRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (pauseAction == null || pauseAction.action == null)
            {
                return;
            }

            pauseAction.action.performed += HandlePausePerformed;
            pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            if (pauseAction != null && pauseAction.action != null)
            {
                pauseAction.action.performed -= HandlePausePerformed;
                pauseAction.action.Disable();
            }

            if (!_isPaused)
            {
                return;
            }

            Time.timeScale = _previousTimeScale;
            _isPaused = false;
        }

        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            TogglePause();
        }

        public void Resume()
        {
            if (!_isPaused)
            {
                return;
            }

            optionsPanel.Close();
            pauseMenuRoot.SetActive(false);
            Time.timeScale = _previousTimeScale;
            _isPaused = false;
        }

        public void OpenOptions()
        {
            if (!_isPaused)
            {
                Pause();
            }

            optionsPanel.Open();
        }

        public void LoadMainMenu()
        {
            Resume();
            ScreenFadeTransition.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
            Resume();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void TogglePause()
        {
            if (_isPaused)
            {
                if (optionsPanel.gameObject.activeSelf)
                {
                    optionsPanel.Close();
                    return;
                }

                Resume();
                return;
            }

            Pause();
        }

        private void Pause()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pauseMenuRoot.SetActive(true);
            optionsPanel.Close();
            _isPaused = true;
        }
    }
}
