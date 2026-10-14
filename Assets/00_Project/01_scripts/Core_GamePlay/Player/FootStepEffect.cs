using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a pooled particle effect under each foot as the character walks. Drop this on the
/// character, assign the <see cref="effectPrefab"/> (a particle-system prefab) plus the
/// <see cref="leftFoot"/>/<see cref="rightFoot"/> bone transforms, then call
/// <see cref="PlayLeftStep"/> / <see cref="PlayRightStep"/> from Animation Events on the walk clip
/// (add an event at each foot-plant keyframe and pick the matching function).
///
/// Effects are pooled and detached to world space on spawn, so a moving character never drags the
/// live particles around and a long walk never churns Instantiate/Destroy.
/// </summary>
public class FootStepEffect : MonoBehaviour
{
    [Header("Effect")]
    [Tooltip("Particle-system prefab spawned once per footstep.")]
    [SerializeField] private GameObject effectPrefab;

    [Header("Feet")]
    [Tooltip("Transform of the left foot bone. The effect spawns at its world position.")]
    [SerializeField] private Transform leftFoot;
    [Tooltip("Transform of the right foot bone. The effect spawns at its world position.")]
    [SerializeField] private Transform rightFoot;
    [Tooltip("World-space offset applied at spawn (e.g. nudge down onto the floor).")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Seconds to wait after the step fires before the effect spawns. 0 = spawn immediately. " +
             "The foot position/rotation are captured at the moment of the step, so the delay doesn't " +
             "drift as the character keeps moving.")]
    [SerializeField] private float stepDelay = 0f;

    [Header("Rotation")]
    [Tooltip("The spawned effect takes THIS object's world rotation (not the foot's, not the ground). " +
             "If left empty, identity rotation is used.")]
    [SerializeField] private Transform rotationSource;

    [Header("Pool")]
    [Tooltip("How many instances to pre-create on Awake so the first steps don't allocate.")]
    [SerializeField] private int initialSize = 6;
    [Tooltip("Grow the pool when every instance is busy. If off, extra steps are silently dropped.")]
    [SerializeField] private bool expandable = true;
    [Tooltip("Optional container the IDLE instances sit under. Defaults to this object. Live effects " +
             "are detached to the world so the moving character can't drag their particles around.")]
    [SerializeField] private Transform poolParent;

    // One pooled effect plus the time it becomes reclaimable (-1 while idle in the pool).
    private class PooledEffect
    {
        public GameObject go;
        public float duration;
        public float freeAt;
    }

    private readonly List<PooledEffect> _all = new List<PooledEffect>();
    private readonly Queue<PooledEffect> _idle = new Queue<PooledEffect>();

    private void Awake()
    {
        if (poolParent == null) poolParent = transform;
        for (int i = 0; i < initialSize; i++) CreateInstance();
    }

    private void Update()
    {
        // Reclaim any live effect whose particles have finished. Simple time-based reclaim keyed off
        // the prefab's own duration + lifetime, so no per-instance coroutine is needed.
        for (int i = 0; i < _all.Count; i++)
        {
            PooledEffect e = _all[i];
            if (e.freeAt >= 0f && Time.time >= e.freeAt) Reclaim(e);
        }
    }

    /// <summary>Animation Event hook: spawn the footstep effect at the left foot.</summary>
    public void PlayLeftStep() => PlayAtFoot(leftFoot);

    /// <summary>Animation Event hook: spawn the footstep effect at the right foot.</summary>
    public void PlayRightStep() => PlayAtFoot(rightFoot);

    private void PlayAtFoot(Transform foot)
    {
        if (effectPrefab == null || foot == null) return;

        // Capture the pose now so the delay doesn't drift as the character keeps moving.
        Vector3 position = foot.position + spawnOffset;
        Quaternion rotation = rotationSource != null ? rotationSource.rotation : Quaternion.identity;

        if (stepDelay > 0f) StartCoroutine(SpawnAfterDelay(position, rotation, stepDelay));
        else Spawn(position, rotation);
    }

    private IEnumerator SpawnAfterDelay(Vector3 position, Quaternion rotation, float delay)
    {
        yield return new WaitForSeconds(delay);
        Spawn(position, rotation);
    }

    private void Spawn(Vector3 position, Quaternion rotation)
    {
        PooledEffect e = GetIdle();
        if (e == null || e.go == null) return; // pool exhausted and not expandable

        // Detach to the world so the moving character can't drag the live particles around.
        e.go.transform.SetParent(null, false);
        e.go.transform.SetPositionAndRotation(position, rotation);
        e.go.SetActive(true);

        ParticleSystem ps = e.go.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        e.freeAt = Time.time + Mathf.Max(0.05f, e.duration);
    }

    private PooledEffect GetIdle()
    {
        if (_idle.Count == 0 && expandable) CreateInstance();
        return _idle.Count > 0 ? _idle.Dequeue() : null;
    }

    private PooledEffect CreateInstance()
    {
        if (effectPrefab == null) return null;

        GameObject go = Instantiate(effectPrefab, poolParent,true);
        go.SetActive(false);

        PooledEffect e = new PooledEffect
        {
            go = go,
            duration = GetEffectDuration(go),
            freeAt = -1f
        };
        _all.Add(e);
        _idle.Enqueue(e);
        return e;
    }

    private void Reclaim(PooledEffect e)
    {
        e.freeAt = -1f;
        if (e.go != null)
        {
            e.go.SetActive(false);
            e.go.transform.SetParent(poolParent, false);
        }
        _idle.Enqueue(e);
    }

    // Longest (main.duration + max start lifetime) across every particle system in the prefab, so the
    // reclaim wait covers the whole effect finishing rather than cutting it short.
    private float GetEffectDuration(GameObject go)
    {
        float max = 0f;
        ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in systems)
        {
            ParticleSystem.MainModule main = ps.main;
            float life = main.duration + main.startLifetime.constantMax;
            if (life > max) max = life;
        }
        return max > 0f ? max : 1f;
    }
}
