using DG.Tweening;
using UnityEngine;

public static class Tweening
{
    /// <summary>
    /// Simultaneously move, rotate, and scale a Transform.
    /// </summary>
    public static Sequence DoTransform(Transform transform, Vector3 position_WS, Quaternion rotation_WS, Vector3 localScale, float time)
    {
        var sequence = DOTween.Sequence(transform);
        sequence.Append(transform.DOMove(position_WS, time));
        sequence.Join(transform.DORotate(rotation_WS.eulerAngles, time));
        sequence.Join(transform.DOScale(localScale, time));

        return sequence;
    }


    /// <summary>
    /// Simultaneously move and rotate a Transform.
    /// </summary>
    public static Sequence DoTransform(Transform transform, Vector3 position_WS, Quaternion rotation_WS, float time)
    {
        return DoTransform(transform, position_WS, rotation_WS, transform.localScale, time);
    }
}
