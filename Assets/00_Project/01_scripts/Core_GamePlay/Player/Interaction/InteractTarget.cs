using UnityEngine;

/// <summary>
/// A lightweight snapshot of whatever the interaction ray is currently hitting:
/// the hit GameObject plus whichever interaction interfaces it happens to implement.
/// Built once per detected object so the handler doesn't re-run GetComponent every frame.
/// </summary>
public struct InteractTarget
{
    public GameObject GO;
    public IInteractable Interactable;
    public IInteractableSecondary InteractableSecondary;
    public IHoverable Hoverable;
    public IHoldable Holdable;

    /// <summary>Caches every interaction interface present on <paramref name="target"/> in one pass.</summary>
    public InteractTarget(GameObject target)
    {
        GO = target;
        Interactable = null;
        InteractableSecondary = null;
        Hoverable = null;
        Holdable = null;

        if (target == null) return;

        Interactable = target.GetComponentInParent<IInteractable>();
        InteractableSecondary = target.GetComponentInParent<IInteractableSecondary>();
        Hoverable = target.GetComponentInParent<IHoverable>();
        Holdable = target.GetComponentInParent<IHoldable>();
    }

    /// <summary>True when the object exists and offers at least one interaction interface.</summary>
    public bool IsValid => GO != null &&
        (Interactable != null || InteractableSecondary != null || Hoverable != null || Holdable != null);

    /// <summary>True when both snapshots point at the exact same GameObject (used to detect target changes).</summary>
    public bool SameGO(in InteractTarget other) => GO == other.GO;

    /// <summary>An explicit "nothing targeted" value.</summary>
    public static InteractTarget None => new InteractTarget(null);
}
