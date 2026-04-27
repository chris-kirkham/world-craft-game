using UnityEngine;
using UnityEngine.SceneManagement;

class AutoLoadBootstrapScene
{
    private const int BoostrapSceneIndex = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadBoostrapScene()
    {
        var activeSceneIdx = SceneManager.GetActiveScene().buildIndex;
        if (activeSceneIdx != BoostrapSceneIndex) 
        {
            SceneManager.UnloadScene(activeSceneIdx); //TODO: I don't want this to be async!
            Debug.Log($"Loading boostrap scene...");
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            SceneManager.LoadScene(activeSceneIdx, LoadSceneMode.Additive);
            Debug.Log($"Bootstrap scene loaded!");
        }
    }
}
