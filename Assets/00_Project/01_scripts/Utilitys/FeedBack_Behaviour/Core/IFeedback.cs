using UnityEngine;

namespace GameFeedback
{
    /// <summary> Base contract: play a feedback effect on a typed target. </summary>
    public interface IFeedback<in T>
    {
        /// <summary> Runs the effect on target; runner hosts any coroutine. </summary>
        void Play(T target, MonoBehaviour runner);
    }
}
