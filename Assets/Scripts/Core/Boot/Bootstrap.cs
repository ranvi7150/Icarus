using Icarus.Core.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Icarus.Core.Boot
{    
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private int sceneToMainMenu = 1;
        
        private void Start()
        {
            SaveData saveData = SaveManager.Load();
            GameProgressState.Initialize(saveData);

            SceneManager.LoadScene(sceneToMainMenu);
        }
    }
}