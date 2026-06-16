using Icarus.Core.Saving;
using Icarus.Core.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Icarus.Core.Boot
{    
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private int sceneToMainMenu = 1;
        
        private void Start()
        {
            GameSettings settings = SettingsManager.Load();
            GameSettingsState.Initialize(settings);
            SettingsManager.Apply(GameSettingsState.CurrentSettings);

            SaveData saveData = SaveManager.Load();
            GameProgressState.Initialize(saveData);

            SceneManager.LoadScene(sceneToMainMenu);
        }
    }
}
