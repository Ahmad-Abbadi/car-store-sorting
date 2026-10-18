using System;
using UnityEngine;

namespace TinyRiders
{
    /// <summary>Presentation only. Collection completion and persistence belong to the existing sorting flow.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GoldenRewardCeremony : MonoBehaviour
    {
        public static GoldenRewardCeremony Active { get; private set; }
        public Bounds protectedLooseBounds;
        public static bool ContainsLoose(Bounds bounds, Vector3 position) =>
            position.x >= bounds.min.x - .25f && position.x <= bounds.max.x + .25f &&
            position.z >= bounds.min.z - .25f && position.z <= bounds.max.z + .25f;
        public enum Phase { Empty, Awakening, Gathering, Manifesting, Ascending, Settling, Finale, Earned }
        [Serializable] public sealed class EffectCue
        {
            public GameObject effect;
            public float start, end;
            public bool earnedAmbient;
            [NonSerialized] public bool active;
        }
        [Serializable] public sealed class CameraShot
        {
            public Transform viewpoint;
            public float time;
            [Range(15, 90)] public float fieldOfView = 43;
        }
        [Serializable] public sealed class SoundCue
        {
            public AudioClip clip;
            public float time;
            [Range(0, 1)] public float volume = .3f;
            [NonSerialized] public bool played;
        }

        public Transform presentationPivot;
        public Light[] lights = Array.Empty<Light>();
        public EffectCue[] effects = Array.Empty<EffectCue>();
        public CameraShot[] shots = Array.Empty<CameraShot>();
        public SoundCue[] sounds = Array.Empty<SoundCue>();
        public AudioSource audioSource;
        public float riseHeight = .32f;
        public Phase CurrentPhase { get; private set; }
        public float CeremonySeconds { get; private set; }
        Vector3 restPosition;
        Quaternion restRotation;
        bool initialized;

        void Awake() { Active = this; Initialize(); }
        void OnDestroy() { if (Active == this) Active = null; }
        void OnEnable() => SimpleAudioManager.OnMuteSettingsChanged += ApplyMute;
        void OnDisable()
        {
            SimpleAudioManager.OnMuteSettingsChanged -= ApplyMute;
            if (initialized) SetRest(false);
        }
        void ApplyMute() { if (audioSource != null) audioSource.mute = SimpleAudioManager.SfxMuted; }
        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (presentationPivot != null)
            { restPosition = presentationPivot.localPosition; restRotation = presentationPivot.localRotation; }
            ApplyMute();
        }
        public void Begin()
        {
            Initialize();
            SetRest(false);
            foreach (var sound in sounds) sound.played = false;
        }
        static float Ease(float start, float end, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, t));

        public void Sample(float seconds)
        {
            Initialize();
            CeremonySeconds = Mathf.Clamp(seconds, 0, 90);
            CurrentPhase = seconds < 12 ? Phase.Awakening : seconds < 28 ? Phase.Gathering :
                seconds < 45 ? Phase.Manifesting : seconds < 62 ? Phase.Ascending :
                seconds < 78 ? Phase.Settling : Phase.Finale;
            float lift = Ease(32, 45, seconds) * (1 - Ease(62, 76, seconds));
            float rotation = 360 * Ease(45, 62, seconds);
            if (presentationPivot != null)
            {
                // Pivot uses world-sized units and never changes the authored car or anchor transforms.
                presentationPivot.localPosition = restPosition + presentationPivot.parent.InverseTransformVector(Vector3.up * (riseHeight * lift));
                presentationPivot.localRotation = restRotation * Quaternion.Euler(0, rotation, 0);
            }
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                float awake = Ease(i * 1.5f, 8 + i * 1.5f, seconds);
                float pulse = seconds < 78 ? .15f * Mathf.Sin(seconds * 1.8f + i) : 0;
                lights[i].intensity = Mathf.Lerp(.08f, 2.6f + pulse, awake) * Mathf.Lerp(1, .6f, Ease(78, 87, seconds));
                if (presentationPivot != null)
                {
                    var target = presentationPivot.position + Vector3.up * .35f;
                    float sweep = (1 - Ease(26, 40, seconds)) * .23f;
                    target += new Vector3(Mathf.Sin(seconds * .7f + i) * sweep, 0, Mathf.Cos(seconds * .7f + i) * sweep);
                    lights[i].transform.LookAt(target);
                }
            }
            foreach (var cue in effects) SetEffect(cue, seconds >= cue.start && seconds < cue.end);
            foreach (var sound in sounds)
                if (!sound.played && seconds >= sound.time)
                {
                    sound.played = true;
                    if (audioSource != null && sound.clip != null)
                    { ApplyMute(); audioSource.PlayOneShot(sound.clip, sound.volume); }
                }
        }

        public void SetRest(bool earned)
        {
            Initialize();
            CurrentPhase = earned ? Phase.Earned : Phase.Empty;
            CeremonySeconds = earned ? 90 : 0;
            if (presentationPivot != null) presentationPivot.SetLocalPositionAndRotation(restPosition, restRotation);
            foreach (var light in lights) if (light != null) light.intensity = earned ? 1.55f : .08f;
            foreach (var cue in effects) SetEffect(cue, earned && cue.earnedAmbient);
            if (audioSource != null) audioSource.Stop();
        }

        static void SetEffect(EffectCue cue, bool active)
        {
            if (cue.effect == null || (cue.active == active && cue.effect.activeSelf == active)) return;
            cue.active = active;
            if (!active)
                foreach (var system in cue.effect.GetComponentsInChildren<ParticleSystem>(true))
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            cue.effect.SetActive(active);
            if (active)
                foreach (var system in cue.effect.GetComponentsInChildren<ParticleSystem>(true)) system.Play(false);
        }

        public void CameraPose(float seconds, out Vector3 position, out Quaternion rotation, out float fov)
        {
            if (seconds >= shots[shots.Length - 1].time)
            {
                var end = shots[shots.Length - 1];
                position = end.viewpoint.position; rotation = end.viewpoint.rotation; fov = end.fieldOfView;
                return;
            }
            var first = shots[0];
            var last = shots[shots.Length - 1];
            for (int i = 1; i < shots.Length; i++)
                if (seconds <= shots[i].time)
                {
                    first = shots[i - 1]; last = shots[i]; break;
                }
            float t = Ease(first.time, last.time, seconds);
            position = Vector3.Lerp(first.viewpoint.position, last.viewpoint.position, t);
            rotation = Quaternion.Slerp(first.viewpoint.rotation, last.viewpoint.rotation, t);
            fov = Mathf.Lerp(first.fieldOfView, last.fieldOfView, t);
        }
    }
}
