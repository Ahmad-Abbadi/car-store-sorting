using System;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Gate module: blocks the whole receiver until the cooldown ends. </summary>
    [Serializable]
    public sealed class CooldownModule : FeedbackModule
    {
        [Min(0f)] [SerializeField] float duration = 0.5f;
        [SerializeField] bool useUnscaledTime;

        [NonSerialized] float lastPlayTime = float.NegativeInfinity;

        float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

        /// <summary> False while the last play is still within the cooldown. </summary>
        public override bool CanPlay(FeedbackContext context) => Now >= lastPlayTime + duration;

        /// <summary> Starts the cooldown window. </summary>
        public override void Play(FeedbackContext context) => lastPlayTime = Now;
    }
}
