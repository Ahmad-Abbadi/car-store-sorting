using System;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Module that flashes a Renderer's material color. </summary>
    [Serializable]
    public sealed class MaterialColorFlashModule : FeedbackModule
    {
        [Tooltip("Optional. Falls back to a Renderer under the receiver.")]
        [SerializeField] Renderer target;
        [SerializeField] MaterialColorFlashSettings settings = MaterialColorFlashSettings.Default;

        /// <summary> Flashes the resolved renderer using this module's settings. </summary>
        public override void Play(FeedbackContext context)
        {
            Renderer r = target != null ? target : context.Source.GetComponentInChildren<Renderer>();
            new MaterialColorFlasher(settings).Play(r, context.Runner);
        }
    }
}
