using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// -----------------------------------------------------------------------------
//  Batched playback for SimpleAudioManager.
//
//  Plays a per-item clip for isolated hits, but when many hits arrive
//  back-to-back (a stack dumping, a combo, rapid pickups...) it stops
//  machine-gunning the individual sounds and collapses the stream into a
//  single throttled "burst" clip instead.
//
//  Each caller is tracked separately via 'key' (pass 'this'), so multiple
//  emitters batch independently. A gap longer than burstInterval starts a
//  fresh batch. Reset is lazy — no timers needed.
// -----------------------------------------------------------------------------
public partial class SimpleAudioManager
{
    [Serializable]
    public struct BatchConfig
    {
        [Tooltip("How many hits within the interval before the burst clip takes over.")]
        public int burstThreshold;
        [Tooltip("Max gap (s) between two hits for them to count as 'together'.")]
        public float burstInterval;
        [Tooltip("Min gap (s) between burst-clip plays so a long stream keeps sounding without spamming.")]
        public float burstReplayCooldown;
        [Tooltip("Randomise pitch a little for a juicy feel.")]
        public bool juicy;

        public static BatchConfig Default => new BatchConfig
        {
            burstThreshold = 4,
            burstInterval = 0.12f,
            burstReplayCooldown = 0.35f,
            juicy = true
        };
    }

    private class BatchState
    {
        public int streak;
        public float lastReceiveTime = -999f;
        public float lastBurstTime = -999f;
    }

    private readonly Dictionary<object, BatchState> _batches = new Dictionary<object, BatchState>();

    /// <param name="key">Identity of the emitter (usually 'this'); batches are tracked per key.</param>
    /// <param name="singleSounds">Per-item sounds; a random one plays for isolated hits.</param>
    /// <param name="burstSound">Played instead once a burst is detected. If clipless, nothing plays during the burst.</param>
    public void PlayBatchedClip(object key, GameSound[] singleSounds, GameSound burstSound, Vector3 position, BatchConfig config)
    {
        if (_isMute || key == null) return;

        if (!_batches.TryGetValue(key, out BatchState state))
        {
            state = new BatchState();
            _batches[key] = state;
        }

        float now = Time.unscaledTime;
        bool withinWindow = (now - state.lastReceiveTime) <= config.burstInterval;
        state.lastReceiveTime = now;
        state.streak = withinWindow ? state.streak + 1 : 1;

        // Burst detected: collapse the stream into one throttled burst clip.
        if (state.streak >= config.burstThreshold)
        {
            if (now - state.lastBurstTime >= config.burstReplayCooldown)
            {
                state.lastBurstTime = now;
                PlayOneOff(burstSound, position, config.juicy);
            }
            return; // suppress the per-item clip while bursting
        }

        // Isolated hit -> ordinary per-item clip.
        PlayRandom(singleSounds, position, config.juicy);
    }

    /// <summary>Forget a key's batch state (call from the emitter's OnDestroy) to avoid leaking entries.</summary>
    public void ClearBatch(object key)
    {
        if (key != null) _batches.Remove(key);
    }

    private void PlayRandom(GameSound[] sounds, Vector3 position, bool juicy)
    {
        if (sounds == null || sounds.Length == 0) return;
        PlayOneOff(sounds[Random.Range(0, sounds.Length)], position, juicy);
    }

    private void PlayOneOff(GameSound sound, Vector3 position, bool juicy)
    {
        if (!sound.IsValid) return;
        if (juicy) PlayJuicyClip(sound, position);
        else PlayClipAt(sound, position);
    }
}
