using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class TimelineX
{
    public static IEnumerator PlayTimelineAndWaitForFinish(PlayableDirector playableDirector, TimelineAsset timelineAsset)
    {
        if (!playableDirector || timelineAsset == null)
        {
            yield break;
        }

        playableDirector.Play(timelineAsset);

        yield return new WaitForSeconds((float)playableDirector.duration);
    }
}
