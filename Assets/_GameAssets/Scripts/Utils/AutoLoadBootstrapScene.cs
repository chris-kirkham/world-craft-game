using UnityEngine;
using UnityEngine.SceneManagement;

class AutoLoadBootstrapScene
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadBoostrapScene()
    {
        if(SceneManager.loadedSceneCount == 0)
        {
            SceneManager.LoadScene(0); //this should always be the boostrap scene
        }
    }
}
