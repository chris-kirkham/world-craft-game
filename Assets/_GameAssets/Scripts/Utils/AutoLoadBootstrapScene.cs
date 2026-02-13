using UnityEngine;
using UnityEngine.SceneManagement;

class AutoLoadBootstrapScene
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void LoadBoostrapScene()
    {
        if(SceneManager.GetActiveScene().buildIndex != 0) //build index 0 should always be the boostrap scene (TODO: add validation)
        {
            Debug.Log($"Loading boostrap scene...");
            SceneManager.LoadScene(0, LoadSceneMode.Additive); 
        }
    }
}
