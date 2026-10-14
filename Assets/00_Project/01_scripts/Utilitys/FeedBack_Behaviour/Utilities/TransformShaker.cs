using System;
using System.Collections;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Settings for shaking a Transform's position. </summary>
    [Serializable]
    public struct ShakeSettings
    {
        [Min(0f)] public float duration;
        public float strength;
        public bool useLocalSpace;
        public bool fadeOut;
        public bool useUnscaledTime;

        /// <summary> Sensible default shake values. </summary>
        public static ShakeSettings Default => new ShakeSettings
        {
            duration = 0.4f,
            strength = 0.15f,
            useLocalSpace = true,
            fadeOut = true,
            useUnscaledTime = false
        };
    }

    /// <summary> Utility feedback that shakes a Transform over time. </summary>
    public sealed class TransformShaker : IFeedback<Transform>
    {
        readonly ShakeSettings settings;

        /// <summary> Creates a shaker with the given settings. </summary>
        public TransformShaker(ShakeSettings settings) => this.settings = settings;

        /// <summary> Starts the shake coroutine on the runner. </summary>
        public void Play(Transform target, MonoBehaviour runner)
        {
            if (target == null || runner == null) return;
            runner.StartCoroutine(Routine(target));
        }

        /// <summary> Offsets the target randomly each frame, then restores it. </summary>
        IEnumerator Routine(Transform target)
        {
            Vector3 origin = settings.useLocalSpace ? target.localPosition : target.position;
            float elapsed = 0f;
            while (elapsed < settings.duration)
            {
                elapsed += settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float damper = settings.fadeOut ? Mathf.Clamp01(1f - elapsed / settings.duration) : 1f;
                Vector3 offset = (Vector3)UnityEngine.Random.insideUnitCircle * (settings.strength * damper);
                if (settings.useLocalSpace) target.localPosition = origin + offset;
                else target.position = origin + offset;
                yield return null;
            }
            if (settings.useLocalSpace) target.localPosition = origin;
            else target.position = origin;
        }
    }
}
