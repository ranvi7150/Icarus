using Icarus.Core.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Icarus.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string startStageName = "Stage_01";

        public void StartNewGame()
        {
            if (string.IsNullOrWhiteSpace(startStageName))
            {
                Debug.LogError("MainMenuController requires a Start Stage Name.", this);
                return;
            }

            GameProgressState.Initialize(new SaveData());

            GameProgressState.SetCurrentStage(startStageName);
            SaveManager.Save(GameProgressState.CurrentSaveData);
            SceneManager.LoadScene(startStageName);
        }

        public void LoadGame()
        {
            EnsureProgressInitialized();

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

            SceneManager.LoadScene(currentStage);
        }

        private static void EnsureProgressInitialized()
        {
            // Allows direct main menu play in the editor without entering through Boot.
            if (!GameProgressState.IsInitialized)
            {
                GameProgressState.Initialize(SaveManager.Load());
            }
        }
    }
}
