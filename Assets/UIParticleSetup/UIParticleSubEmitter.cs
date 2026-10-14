using System.Collections.Generic;
using UnityEngine;

namespace Irahimz.UIParticles
{
// Replacement for Unity's sub-emitter module, for particle trees drawn through the bundled
// UIParticleSystem renderer (this package).
//
// Sub-emitters can't survive that path: UIParticleSystem drives each system with its own
// Simulate() call and draws it as a separate UI Graphic, so the parent's native sub-emitter
// dispatch has nothing to hand its spawns to. This tracks the parent's particles by randomSeed
// instead, spots the ones that appeared (Birth) or vanished (Death) since last frame, and fires
// an explicit burst at that particle's last known position.
//
// Normally added by IrahimzTools > UI Particle Setup, which reads the real subEmitters
// module and fills the trigger list in. Tune Count afterwards -- it's a burst per event, not a
// full sub-system instance per particle (see the class comment on Trigger).
[RequireComponent(typeof(ParticleSystem))]
[DefaultExecutionOrder(100)]
public class UIParticleSubEmitter : MonoBehaviour
{
    // One converted entry from the source system's subEmitters module. Count is a flat burst
    // fired at the event position rather than a running instance of the target's own emission,
    // so a target that trailed particles off a living parent won't reproduce -- only the common
    // "burst of N on death/birth" shape does.
    [System.Serializable]
    public class Trigger
    {
        public ParticleSystem target;
        public ParticleSystemSubEmitterType type = ParticleSystemSubEmitterType.Death;
        [Min(0)] public int count = 8;
        [Range(0f, 1f)] public float probability = 1f;
        public bool inheritColor;
        public bool inheritSize;
        public bool inheritRotation;
        public bool inheritLifetime;
    }

    public List<Trigger> triggers = new List<Trigger>();

    private struct Snapshot
    {
        public Vector3 WorldPosition;
        public Color32 Color;
        public float Size;
        public float Rotation;
        public float RemainingLifetime;
    }

    private ParticleSystem _source;
    private ParticleSystem.Particle[] _buffer;
    private readonly Dictionary<uint, Snapshot> _tracked = new Dictionary<uint, Snapshot>();
    private readonly HashSet<uint> _seenThisFrame = new HashSet<uint>();
    private readonly List<uint> _died = new List<uint>();

    private void Awake()
    {
        _source = GetComponent<ParticleSystem>();
    }

    private void OnDisable()
    {
        _tracked.Clear();
    }

    private void LateUpdate()
    {
        if (_source == null || triggers.Count == 0) return;

        int max = _source.main.maxParticles;
        if (_buffer == null || _buffer.Length < max) _buffer = new ParticleSystem.Particle[max];

        int count = _source.GetParticles(_buffer);
        _seenThisFrame.Clear();

        for (int i = 0; i < count; i++)
        {
            var particle = _buffer[i];
            uint seed = particle.randomSeed;
            _seenThisFrame.Add(seed);

            var snapshot = new Snapshot
            {
                WorldPosition = ToWorld(particle.position),
                Color = particle.GetCurrentColor(_source),
                Size = particle.GetCurrentSize(_source),
                Rotation = particle.rotation,
                RemainingLifetime = particle.remainingLifetime,
            };

            if (!_tracked.ContainsKey(seed)) Fire(ParticleSystemSubEmitterType.Birth, snapshot);
            _tracked[seed] = snapshot;
        }

        // Gone since last frame means dead. The snapshot is that particle's final state, which is
        // exactly what a death sub-emitter would have inherited.
        _died.Clear();
        foreach (var pair in _tracked)
            if (!_seenThisFrame.Contains(pair.Key)) _died.Add(pair.Key);

        for (int i = 0; i < _died.Count; i++)
        {
            Fire(ParticleSystemSubEmitterType.Death, _tracked[_died[i]]);
            _tracked.Remove(_died[i]);
        }
    }

    private Vector3 ToWorld(Vector3 particlePosition)
    {
        return _source.main.simulationSpace == ParticleSystemSimulationSpace.Local
            ? transform.TransformPoint(particlePosition)
            : particlePosition;
    }

    private void Fire(ParticleSystemSubEmitterType type, Snapshot snapshot)
    {
        for (int i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
            if (trigger == null || trigger.target == null) continue;
            if (trigger.type != type || trigger.count <= 0) continue;
            if (trigger.probability < 1f && Random.value > trigger.probability) continue;

            var targetMain = trigger.target.main;
            var emit = new ParticleSystem.EmitParams
            {
                position = targetMain.simulationSpace == ParticleSystemSimulationSpace.Local
                    ? trigger.target.transform.InverseTransformPoint(snapshot.WorldPosition)
                    : snapshot.WorldPosition,
                applyShapeToPosition = true,
            };

            if (trigger.inheritColor) emit.startColor = snapshot.Color;
            if (trigger.inheritSize) emit.startSize = snapshot.Size;
            if (trigger.inheritRotation) emit.rotation = snapshot.Rotation;
            if (trigger.inheritLifetime) emit.startLifetime = Mathf.Max(0.01f, snapshot.RemainingLifetime);

            trigger.target.Emit(emit, trigger.count);
        }
    }
}
}
