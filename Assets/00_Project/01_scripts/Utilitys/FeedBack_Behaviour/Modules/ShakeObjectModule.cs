using System;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Module that shakes a target Transform. </summary>
    [Serializable]
    public sealed class ShakeObjectModule : FeedbackModule
    {
        [Tooltip("Optional. Falls back to the receiver's transform.")]
        [SerializeField] Transform target;
        [SerializeField] ShakeSettings settings = ShakeSettings.Default;

        /// <summary> Shakes the resolved target using this module's settings. </summary>
        public override void Play(FeedbackContext context)
        {
            Transform t = target != null ? target : context.Source.transform;
            new TransformShaker(settings).Play(t, context.Runner);
        }
    }
}
