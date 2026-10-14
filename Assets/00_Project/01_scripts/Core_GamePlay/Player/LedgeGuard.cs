using UnityEngine;

/// <summary>
/// Stops the character from walking off ledges into the void. KCC lets characters
/// walk off edges by design, so this adds "look before you step": it raycasts down
/// at a point ahead in the intended move direction and, if there's no ground there,
/// removes the part of the move that heads toward the drop.
///
/// It filters the <b>movement input</b> (world space) rather than the motor's
/// velocity, so it composes cleanly with KCC and stays a standalone, reusable
/// detector — the movement states call <see cref="FilterMove"/> before feeding
/// the character.
///
/// Extra left/right samples let the character slide <i>along</i> an edge instead of
/// stopping dead when only part of the path ahead is a drop.
/// </summary>
public class LedgeGuard : MonoBehaviour
{
    [Header("Probe (world units, from this transform's position)")]
    [Tooltip("Start the down-rays this far above the pivot.")]
    [SerializeField] private float heightOffset = 1.0f;

    [Tooltip("How far ahead (along the move direction) to sample for ground.")]
    [SerializeField] private float forwardOffset = 0.6f;

    [Tooltip("Lateral sample distance used to work out the shape of the edge.")]
    [SerializeField] private float sideOffset = 0.35f;

    [Tooltip("How far down to look for ground before calling it a drop.")]
    [SerializeField] private float rayDistance = 1.5f;

    [Tooltip("What counts as walkable ground. Set this to your floor layer(s) for best results.")]
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    /// <summary>
    /// Given a desired horizontal world-space move vector, returns it with any
    /// component that heads off a ledge removed. Perpendicular (edge-sliding)
    /// movement is preserved. Pass Vector3.zero-ish through unchanged.
    /// </summary>
    public Vector3 FilterMove(Vector3 worldMove)
    {
        Vector3 flat = new Vector3(worldMove.x, 0f, worldMove.z);
        if (flat.sqrMagnitude < 1e-6f) return worldMove;

        Vector3 dir = flat.normalized;
        Vector3 origin = transform.position + Vector3.up * heightOffset;
        Vector3 ahead = origin + dir * forwardOffset;

        // Ground directly ahead in the move direction → nothing to block.
        if (HasGround(ahead)) return worldMove;

        // No ground ahead: probe the sides to see which way the edge falls away,
        // so we can still slide along it rather than freeze.
        Vector3 side = Vector3.Cross(Vector3.up, dir); // "right" of the move direction
        bool leftOk = HasGround(ahead - side * sideOffset);
        bool rightOk = HasGround(ahead + side * sideOffset);

        Vector3 blockDir = dir; // default: drop is straight ahead → block the whole forward
        if (leftOk && !rightOk) blockDir = (dir + side).normalized;      // drop on the right
        else if (rightOk && !leftOk) blockDir = (dir - side).normalized; // drop on the left

        float toward = Vector3.Dot(worldMove, blockDir);
        if (toward > 0f) worldMove -= blockDir * toward;
        return worldMove;
    }

    private bool HasGround(Vector3 point)
    {
        bool hit = Physics.Raycast(point, Vector3.down, rayDistance, groundLayers, QueryTriggerInteraction.Ignore);
        if (drawDebug) Debug.DrawRay(point, Vector3.down * rayDistance, hit ? Color.green : Color.red);
        return hit;
    }
}
