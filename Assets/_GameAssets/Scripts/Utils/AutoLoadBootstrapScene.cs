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
            SceneManager.UnloadScene(activeSceneIdx);
            Debug.Log($"Loading boostrap scene...");
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            SceneManager.LoadScene(activeSceneIdx, LoadSceneMode.Additive);
        }
    }
}
