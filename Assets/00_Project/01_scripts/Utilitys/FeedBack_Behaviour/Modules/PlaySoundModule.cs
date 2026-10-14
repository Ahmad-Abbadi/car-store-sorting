using System;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Module that plays a gameplay library sound via SimpleAudioManager. </summary>
    [Serializable]
    public sealed class PlaySoundModule : FeedbackModule
    {
        [Tooltip("Sound picked from the GameplaySounds library.")]
        [SerializeField] GameplaySoundKey sound = GameplaySoundKey.Wrong;
        [Tooltip("Random ±10% pitch on top of the sound's own.")]
        [SerializeField] bool juicy = true;
        [Tooltip("Plays at the camera instead of the source position.")]
        [SerializeField] bool global = true;

        /// <summary> Plays the library sound globally or at the source position. </summary>
        public override void Play(FeedbackContext context)
        {
            SimpleAudioManager audio = SimpleAudioManager.instance;
            if (audio == null || audio.GameplaySounds == null) return;

            GameSound clip = audio.GameplaySounds.Get(sound);
            if (!clip.IsValid) return;

            if (global)
            {
                if (juicy) audio.PlayJuicyGlobalClip(clip);
                else audio.PlayGlobal(clip);
                return;
            }

            Vector3 position = context.Source.transform.position;
            if (juicy) audio.PlayJuicyClip(clip, position);
            else audio.PlayClipAt(clip, position);
        }
    }
}
