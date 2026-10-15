using System.Collections.Generic;
using UnityEngine;

/// <summary> Items near the player or still moving are dynamic; far resting items turn kinematic. </summary>
[DefaultExecutionOrder(-200)] // activate contact cars before KCC simulates movement
public class ItemPhysicsCulling : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Physics simulates around this transform.")]
    [SerializeField] private Transform player;

    [Header("Range")]
    [Tooltip("Items inside this radius are dynamic (not kinematic).")]
    [SerializeField, Min(0f)] private float activeRadius = 2.5f;

    [Tooltip("Most items made dynamic by the radius at once; nearest first. Throws don't count toward it.")]
    [SerializeField, Min(1)] private int maxDynamic = 80;

    [Tooltip("Extra distance before a resting item turns kinematic; stops flicker at the edge.")]
    [SerializeField, Min(0f)] private float freezeBuffer = 2f;

    [Tooltip("Radius around a picked-up item whose neighbours are woken so they fall.")]
    [SerializeField] private float wakeRadius = 0.8f;

    private const float MoveSqr = 1e-4f; // below this a dynamic item counts as still for rendering

    private static ItemPhysicsCulling _instance;
    private readonly List<WorldItemBase> _dynamic = new List<WorldItemBase>();
    private readonly List<(float distSqr, WorldItemBase item)> _candidates = new List<(float, WorldItemBase)>();
    private readonly Collider[] _contactHits = new Collider[128];
    private readonly Queue<Vector3> _pendingWake = new Queue<Vector3>();

    private bool WalkThroughStock => TinyRiders.ThemedCollectionSession.Instance is { } session &&
        session.gameObject.scene == gameObject.scene && session.roomLayout != null &&
        session.roomLayout.walkThroughLooseCars;

    /// <summary> Number of items currently dynamic. </summary>
    public int DynamicCount => _dynamic.Count;

    /// <summary> Number of items being managed. </summary>
    public int ManagedCount => WorldItemBase.All.Count;

    private void Awake() => _instance = this;

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Start()
    {
        if (player == null) Debug.LogWarning($"{name}: ItemPhysicsCulling has no player assigned.", this);
    }

    /// <summary> Wakes dynamic items around a removed item so those resting on it fall. </summary>
    public static void WakeAround(Vector3 position)
    {
        if (_instance == null) return;

        _instance._pendingWake.Enqueue(position);
    }

    private void WakeNeighbours(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, wakeRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Rigidbody body = hits[i].attachedRigidbody;
            if (body == null) continue;
            WorldItemBase item = body.GetComponent<WorldItemBase>();
            if (item == null || item.ThrowControlled) continue;
            if (item.Frozen) TryMakeDynamic(item, position, true);
            if (!body.isKinematic) body.WakeUp();
        }
    }

    /// <summary> Starts tracking a item made dynamic elsewhere, e.g. a released throw. </summary>
    public static void Track(WorldItemBase item)
    {
        if (_instance == null || item == null || item.Body == null) return;

        item.Frozen = false;
        if (!_instance._dynamic.Contains(item)) _instance._dynamic.Add(item);
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        Vector3 center = player.position;
        while (_pendingWake.Count > 0) WakeNeighbours(_pendingWake.Dequeue());
        if (!WalkThroughStock) UnfreezeInRadius(center);
        UpdateDynamic(center);
        ActivateContacts();
    }

    private void ActivateContacts()
    {
        if (player == null || WalkThroughStock) return;

        // The motor cannot push a kinematic car. Activate everything touching its
        // capsule before KCC's FixedUpdate, including when the normal budget is full.
        var motor = Player.Instance != null ? Player.Instance.Motor : null;
        if (motor == null) return;
        var capsule = motor.Capsule;
        Vector3 center = capsule.transform.TransformPoint(capsule.center);
        Vector3 axis = capsule.transform.up * Mathf.Max(0, capsule.height * .5f - capsule.radius);
        int count = Physics.OverlapCapsuleNonAlloc(center - axis,
            center + axis, capsule.radius + .15f, _contactHits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Rigidbody body = _contactHits[i].attachedRigidbody;
            if (body == null) continue;
            WorldItemBase item = body.GetComponent<WorldItemBase>();
            if (item == null || item.ThrowControlled) continue;
            if (item.Frozen) TryMakeDynamic(item, center, true);
        }
    }

    /// <summary> Turns the nearest kinematic items inside the radius dynamic, up to the cap. </summary>
    private void UnfreezeInRadius(Vector3 center)
    {
        float wakeSqr = activeRadius * activeRadius;
        IReadOnlyList<WorldItemBase> items = WorldItemBase.All;
        _candidates.Clear();

        // Kinematic items never move, so their cached position keeps this loop managed-only.
        for (int i = 0; i < items.Count; i++)
        {
            WorldItemBase item = items[i];
            if (!item.Frozen) continue;

            Vector3 d = item.RestPosition - center;
            float distSqr = d.x * d.x + d.y * d.y + d.z * d.z;
            if (distSqr <= wakeSqr) _candidates.Add((distSqr, item));
        }

        _candidates.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));
        for (int i = 0; i < _candidates.Count; i++)
            TryMakeDynamic(_candidates[i].item, center, false);
    }

    /// <summary> Detaches moving items from chunks; freezes resting items outside the radius. </summary>
    private void UpdateDynamic(Vector3 center)
    {
        float freezeDist = activeRadius + freezeBuffer;
        float freezeSqr = freezeDist * freezeDist;

        for (int i = _dynamic.Count - 1; i >= 0; i--)
        {
            WorldItemBase item = _dynamic[i];
            Rigidbody body = item != null ? item.Body : null;
            if (body == null || !item.IsRegistered || item.Frozen)
            {
                SwapRemove(i);
                continue;
            }
            if (item.ThrowControlled || body.isKinematic) continue; // the throw pull drives it

            bool sleeping = body.IsSleeping();
            float lin = sleeping ? 0f : body.linearVelocity.sqrMagnitude;
            float ang = sleeping ? 0f : body.angularVelocity.sqrMagnitude;

            if (lin > MoveSqr || ang > MoveSqr)
            {
                ItemChunkRenderer.Detach(item.ChunkMember); // chunk can't show it moving
                item.WasMoving = true;
                item.Moved = true;
            }

            // A slow body may be at the apex of a fall or briefly supported by an
            // unsettled neighbour. Only PhysX sleep establishes a settled pose.
            bool resting = sleeping;
            if (!resting) continue;

            if (item.WasMoving)
            {
                item.WasMoving = false;
                LevelSaveManager.MarkDirty(); // it settled somewhere new
            }

            if (!WalkThroughStock && (body.position - center).sqrMagnitude < freezeSqr) continue;

            SetKinematic(item);
            SwapRemove(i);
        }
    }

    /// <summary>Prioritizes reached cars over distant resting ones when the physics budget is full.</summary>
    private void TryMakeDynamic(WorldItemBase item, Vector3 center, bool force)
    {
        Rigidbody body = item.Body;
        if (body == null || !item.Frozen) return;

        if (_dynamic.Count >= maxDynamic && !force)
        {
            return; // Contact and support-loss activation may exceed the soft budget.
        }

        item.Frozen = false;
        body.isKinematic = false;
        if (force) body.WakeUp();
        else body.Sleep();
        if (!_dynamic.Contains(item)) _dynamic.Add(item);
    }

    /// <summary> Freezes a resting item where it is. </summary>
    private static void SetKinematic(WorldItemBase item)
    {
        item.Body.isKinematic = true;
        item.Frozen = true;
        item.RestPosition = item.Body.position;
    }

    private void SwapRemove(int index)
    {
        int last = _dynamic.Count - 1;
        _dynamic[index] = _dynamic[last];
        _dynamic.RemoveAt(last);
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, activeRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, activeRadius + freezeBuffer);
    }
}
