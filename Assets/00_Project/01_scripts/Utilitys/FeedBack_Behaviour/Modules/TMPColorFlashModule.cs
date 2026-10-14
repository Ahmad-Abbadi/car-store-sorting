using System;
using TMPro;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Module that flashes a TMP text's color. </summary>
    [Serializable]
    public sealed class TMPColorFlashModule : FeedbackModule
    {
        [Tooltip("Optional. Falls back to a TMP_Text under the receiver.")]
        [SerializeField] TMP_Text target;
        [SerializeField] TMPColorFlashSettings settings = TMPColorFlashSettings.Default;

        /// <summary> Flashes the resolved text using this module's settings. </summary>
        public override void Play(FeedbackContext context)
        {
            TMP_Text text = target != null ? target : context.Source.GetComponentInChildren<TMP_Text>();
            new TMPColorFlasher(settings).Play(text, context.Runner);
        }
    }
}
