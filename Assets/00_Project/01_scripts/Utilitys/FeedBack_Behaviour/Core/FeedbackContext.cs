using UnityEngine;

namespace GameFeedback
{
    /// <summary> Runtime data passed to modules when a receiver plays. </summary>
    public readonly struct FeedbackContext
    {
        /// <summary> GameObject the feedback plays on (default: the receiver). </summary>
        public readonly GameObject Source;

        /// <summary> Behaviour used to start coroutines (the receiver). </summary>
        public readonly MonoBehaviour Runner;

        /// <summary> Creates a context for the given source and runner. </summary>
        public FeedbackContext(GameObject source, MonoBehaviour runner)
        {
            Source = source;
            Runner = runner;
        }
    }
}
