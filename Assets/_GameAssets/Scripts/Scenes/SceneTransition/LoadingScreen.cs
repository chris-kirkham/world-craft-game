using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : SingletonMonoBehaviour<LoadingScreen>
{
    [SerializeField] private GameObject loadingObjectRoot;
    [SerializeField] private Image progressBar;

    //progress should be between 0 and 1
    public void SetProgressBar(float progress)
    {
        progressBar.fillAmount = Mathf.Clamp01(progress);
    }

    public IEnumerator FadeInRoutine()
    {
        loadingObjectRoot.SetActive(true);
        yield return new WaitForSeconds(1f);
    }

    public IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(1f);
        loadingObjectRoot.SetActive(false);
    }
}
