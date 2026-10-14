using System;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Base for a serialized feedback module stored on a receiver. </summary>
    [Serializable]
    public abstract class FeedbackModule
    {
        [SerializeField] bool enabled = true;

        /// <summary> Whether this module runs when the receiver plays. </summary>
        public bool Enabled => enabled;

        /// <summary> Label shown for this module in the inspector. </summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary> Gate checked before playing; false blocks the whole receiver. </summary>
        public virtual bool CanPlay(FeedbackContext context) => true;

        /// <summary> Runs this module's effect using the given context. </summary>
        public abstract void Play(FeedbackContext context);
    }
}
