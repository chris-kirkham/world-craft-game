using UnityEngine;
using UnityEngine.SceneManagement;

class AutoLoadBootstrapScene
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadBoostrapScene()
    {
        Debug.Log($"Current scene: " + SceneManager.GetActiveScene());
        if(SceneManager.loadedSceneCount == 0)
        {
            SceneManager.LoadScene(0); //this should always be the boostrap scene
        }
    }
}
