using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Settings for flashing a TMP text's color. </summary>
    [Serializable]
    public struct TMPColorFlashSettings
    {
        public Color flashColor;
        [Min(0f)] public float duration;
        public bool returnToOriginal;
        public bool useUnscaledTime;

        /// <summary> Sensible default flash values. </summary>
        public static TMPColorFlashSettings Default => new TMPColorFlashSettings
        {
            flashColor = Color.white,
            duration = 0.25f,
            returnToOriginal = true,
            useUnscaledTime = false
        };
    }

    /// <summary> Utility feedback that flashes a TMP text's color. </summary>
    public sealed class TMPColorFlasher : IFeedback<TMP_Text>
    {
        readonly TMPColorFlashSettings settings;

        /// <summary> Creates a flasher with the given settings. </summary>
        public TMPColorFlasher(TMPColorFlashSettings settings) => this.settings = settings;

        /// <summary> Starts the flash coroutine on the runner. </summary>
        public void Play(TMP_Text target, MonoBehaviour runner)
        {
            if (target == null || runner == null) return;
            runner.StartCoroutine(Routine(target));
        }

        /// <summary> Lerps to the flash color and optionally back again. </summary>
        IEnumerator Routine(TMP_Text target)
        {
            Color original = target.color;
            float half = settings.returnToOriginal ? settings.duration * 0.5f : settings.duration;

            yield return Blend(target, original, settings.flashColor, half);
            target.color = settings.flashColor;
            if (!settings.returnToOriginal) yield break;

            yield return Blend(target, settings.flashColor, original, half);
            target.color = original;
        }

        /// <summary> Lerps the text color from one value to another. </summary>
        IEnumerator Blend(TMP_Text target, Color from, Color to, float time)
        {
            float elapsed = 0f;
            while (elapsed < time)
            {
                elapsed += settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                target.color = Color.Lerp(from, to, time > 0f ? elapsed / time : 1f);
                yield return null;
            }
        }
    }
}
