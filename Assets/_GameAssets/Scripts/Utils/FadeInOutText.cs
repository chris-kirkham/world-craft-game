using System.Collections;
using TMPro;
using UnityEngine;

public class FadeInOutText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float fadeInTime;
    [SerializeField] private float fadeOutTime;
    [SerializeField] private AnimationCurve fadeCurve;

    private Coroutine playRoutine;
    private float t;

    private void OnEnable()
    {
        EnableAndPlayInClip();
    }

    public void EnableAndPlayInClip()
    {
        Play(setActive: true);
    }

    public void PlayClipThenDisable()
    {
        Play(setActive: false);
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

    private IEnumerator PlayRoutine(bool setActive)
    {
        if(!text)
        {
            yield break;
        }

        var col = text.color;
        var time = setActive ? fadeInTime : fadeOutTime;
        var endT = setActive ? 1f : 0f;
        //var curve = setActive ? fadeInCurve : fadeOutCurve;
        var curve = fadeCurve;
        do
        {
            text.color = new Color(col.r, col.g, col.b, curve.Evaluate(t));
            var delta = Time.deltaTime / time;
            if(setActive)
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

        if(!setActive)
        {
            gameObject.SetActive(false);
        }
    }
}
