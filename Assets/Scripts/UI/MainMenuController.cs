using Icarus.Core.Saving;
using Icarus.Core.SceneManagement;
using UnityEngine;

namespace Icarus.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string startStageName = "Stage_01";
        [SerializeField] private OptionsPanel optionsPanel;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(startStageName))
            {
                Debug.LogError("MainMenuController requires a Start Stage Name.", this);
                enabled = false;
                return;
            }

            if (optionsPanel == null)
            {
                Debug.LogError("MainMenuController requires an Options Panel reference.", this);
                enabled = false;
                return;
            }
        }

        public void StartNewGame()
        {
            GameProgressState.Initialize(new SaveData());

            GameProgressState.SetCurrentStage(startStageName);
            SaveManager.Save(GameProgressState.CurrentSaveData);
            ScreenFadeTransition.LoadScene(startStageName);
        }

        public void LoadGame()
        {
            SaveManager.EnsureLoaded();

            string currentStage = GameProgressState.CurrentSaveData.currentStage;
            if (string.IsNullOrWhiteSpace(currentStage))
            {
                Debug.LogWarning("No saved stage found to load.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(currentStage))
            {
                Debug.LogError($"Saved stage '{currentStage}' is not in Build Settings.", this);
                return;
            }

            ScreenFadeTransition.LoadScene(currentStage);
        }

        public void OpenOptions()
        {
            optionsPanel.Open();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

    }
}
