using System.Collections.Generic;
using UnityEngine;

namespace GameFeedback
{
    /// <summary> Holds feedback modules and plays them on demand. </summary>
    public class FeedbackReceiver : MonoBehaviour
    {
        [SerializeReference] List<FeedbackModule> modules = new List<FeedbackModule>();

        /// <summary> The modules currently attached to this receiver. </summary>
        public IReadOnlyList<FeedbackModule> Modules => modules;

        /// <summary> Plays every enabled module on this receiver's GameObject. </summary>
        public void Play() => Play(gameObject);

        /// <summary> Plays every enabled module against the given source object. </summary>
        public void Play(GameObject source) => Play(source, true);

        /// <summary> Allows an action owner to provide its own sound while retaining visual feedback. </summary>
        public void Play(GameObject source, bool playSounds)
        {
            if (source == null) source = gameObject;
            var context = new FeedbackContext(source, this);
            if (!CanPlay(context)) return;

            for (int i = 0; i < modules.Count; i++)
            {
                FeedbackModule module = modules[i];
                if (module != null && module.Enabled && (playSounds || !(module is PlaySoundModule))) module.Play(context);
            }
        }

        /// <summary> True when every enabled module's gate allows playing. </summary>
        bool CanPlay(FeedbackContext context)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                FeedbackModule module = modules[i];
                if (module != null && module.Enabled && !module.CanPlay(context)) return false;
            }
            return true;
        }
    }
}
