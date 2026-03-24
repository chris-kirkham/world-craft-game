using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Timeline.Samples
{
    // Runtime representation of a Tween clip.
    public class TweenBehaviour : PlayableBehaviour
    {
        public Transform startLocation;
        public Transform endLocation;
        public Vector3 startLocalScale;
        public Vector3 endLocalScale;

        public bool shouldTweenPosition;
        public bool shouldTweenRotation;
        public bool shouldTweenScale;

        public AnimationCurve curve;
    }
}
