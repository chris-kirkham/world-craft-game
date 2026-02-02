using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : SingletonMonoBehaviour<SceneTransitionManager>
{
    [SerializeField] private int initialSceneBuildIdx;
    [SerializeField] private int loadingSceneBuildIdx;

    private void Start()
    {
        LoadInitialScene();
    }

    [ContextMenu("Load initial scene")]
    private void LoadInitialScene()
    {
        SceneManager.LoadScene(initialSceneBuildIdx, LoadSceneMode.Single);
    }

    public void LoadSceneWithLoadingScreen(int sceneBuildIdx)
    {
        StartCoroutine(LoadSceneWithLoadingScreenRoutine(sceneBuildIdx));
    }

    private IEnumerator LoadSceneWithLoadingScreenRoutine(int sceneBuildIdx)
    {
        AsyncOperation async = null;

        if(sceneBuildIdx < 0 || sceneBuildIdx >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogErrorFormat($"Given scene build index {0} is invalid! Cannot load scene.", sceneBuildIdx);
            yield break;
        }

        //get scene about to be unloaded
        var prevScene = SceneManager.GetActiveScene();

        //load loading scene additively before unloading active scene
        async = SceneManager.LoadSceneAsync(loadingSceneBuildIdx, LoadSceneMode.Additive);
        yield return LoadingScreen.Inst.FadeInRoutine();
        yield return async;
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(loadingSceneBuildIdx));

        //unload previous scene, if any
        if (prevScene.IsValid())
        {
            async = SceneManager.UnloadSceneAsync(prevScene);
            yield return async;
        }

        //load target scene async
        async = SceneManager.LoadSceneAsync(sceneBuildIdx, LoadSceneMode.Additive); 
        while(!async.isDone)
        {
            if(LoadingScreen.InstExists())
            {
                LoadingScreen.Inst.SetProgressBar(async.progress);
            }

            yield return null;
        }

        //unload loading scene async
        async = SceneManager.UnloadSceneAsync(loadingSceneBuildIdx);
        yield return LoadingScreen.Inst.FadeOutRoutine();
        yield return async;
    }
}
