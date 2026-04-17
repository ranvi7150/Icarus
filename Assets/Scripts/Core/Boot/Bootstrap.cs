using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Icarus.Core.Boot
{    
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] int sceneToMainMenu = 1;
        
        void Start()
        {
            SceneManager.LoadScene(sceneToMainMenu);
        }
    }
}