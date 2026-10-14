using System;
using System.Collections;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Settings for flashing a Renderer's material color. </summary>
    [Serializable]
    public struct MaterialColorFlashSettings
    {
        public Color flashColor;
        [Min(0f)] public float duration;
        [Tooltip("Shader color property. URP lit: _BaseColor. Built-in: _Color.")]
        public string colorProperty;
        public bool returnToOriginal;
        public bool useUnscaledTime;

        /// <summary> Sensible default flash values for URP lit. </summary>
        public static MaterialColorFlashSettings Default => new MaterialColorFlashSettings
        {
            flashColor = Color.white,
            duration = 0.25f,
            colorProperty = "_BaseColor",
            returnToOriginal = true,
            useUnscaledTime = false
        };
    }

    /// <summary> Utility feedback that flashes a Renderer's color via a block. </summary>
    public sealed class MaterialColorFlasher : IFeedback<Renderer>
    {
        readonly MaterialColorFlashSettings settings;

        /// <summary> Creates a flasher with the given settings. </summary>
        public MaterialColorFlasher(MaterialColorFlashSettings settings) => this.settings = settings;

        /// <summary> Starts the flash coroutine on the runner. </summary>
        public void Play(Renderer target, MonoBehaviour runner)
        {
            if (target == null || runner == null) return;
            runner.StartCoroutine(Routine(target));
        }

        /// <summary> Animates the color property without editing shared materials. </summary>
        IEnumerator Routine(Renderer target)
        {
            string prop = string.IsNullOrEmpty(settings.colorProperty) ? "_BaseColor" : settings.colorProperty;
            int id = Shader.PropertyToID(prop);
            var block = new MaterialPropertyBlock();

            Color original = target.sharedMaterial != null && target.sharedMaterial.HasProperty(id)
                ? target.sharedMaterial.GetColor(id)
                : Color.white;
            float half = settings.returnToOriginal ? settings.duration * 0.5f : settings.duration;

            yield return Blend(target, block, id, original, settings.flashColor, half);
            Apply(target, block, id, settings.flashColor);
            if (!settings.returnToOriginal) yield break;

            yield return Blend(target, block, id, settings.flashColor, original, half);
            Apply(target, block, id, original);
        }

        /// <summary> Lerps the renderer color from one value to another. </summary>
        IEnumerator Blend(Renderer target, MaterialPropertyBlock block, int id, Color from, Color to, float time)
        {
            float elapsed = 0f;
            while (elapsed < time)
            {
                elapsed += settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                Apply(target, block, id, Color.Lerp(from, to, time > 0f ? elapsed / time : 1f));
                yield return null;
            }
        }

        /// <summary> Writes the color into the renderer's property block. </summary>
        static void Apply(Renderer target, MaterialPropertyBlock block, int id, Color color)
        {
            target.GetPropertyBlock(block);
            block.SetColor(id, color);
            target.SetPropertyBlock(block);
        }
    }
}
