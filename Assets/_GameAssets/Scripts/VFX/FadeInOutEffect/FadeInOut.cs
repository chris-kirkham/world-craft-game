using System.Collections;
using TMPro;
using UnityEngine;

public abstract class FadeInOut : MonoBehaviour
{
    private enum State
    {
        Idle,
        FadingIn,
        FadingOut
    }

    [SerializeField] private float fadeInTime;
    [SerializeField] private float fadeOutTime;
    [SerializeField] private AnimationCurve fadeCurve;

    private Coroutine playRoutine;
    private float t;

    private void OnEnable()
    {
        FadeIn();
    }

    private void OnDisable()
    {
        SetFadeAmount(0f); //set faded out on disable (should this be optional?)
    }

    public void FadeIn()
    {
        Play(setActive: true);
    }

    public void FadeOut(bool autoSetGameObjectInactive = true)
    {
        Play(setActive: !autoSetGameObjectInactive);
    }

    private void Play(bool setActive)
    {
        gameObject.SetActive(true);

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine(setActive));
    }

    /// <summary>
    /// Amount should be in range [0, 1]
    /// </summary>
    /// <param name="amount"></param>
    protected abstract void SetFadeAmount(float amount);

    private IEnumerator PlayRoutine(bool setActive)
    {
        var time = setActive ? fadeInTime : fadeOutTime;
        var endT = setActive ? 1f : 0f;
        var curve = fadeCurve;
        do
        {
            SetFadeAmount(curve.Evaluate(t));

            var delta = Time.deltaTime / time;
            if (setActive)
            {
                t = Mathf.Clamp01(t + delta);
            }
            else
            {
                t = Mathf.Clamp01(t - delta);
            }

            yield return null;
        }
        while (t != endT);

        if (!setActive)
        {
            gameObject.SetActive(false);
        }
    }
}
