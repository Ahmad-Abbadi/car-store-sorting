using UnityEngine;

/// <summary>Forwards collisions on the shelf's aiming surface to automatic placement.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class SortingShelfPlacementSurface : MonoBehaviour
{
    private SortingShelf _shelf;

    private void Awake() => _shelf = GetComponentInParent<SortingShelf>();

    private void OnCollisionEnter(Collision collision)
    {
        if (_shelf == null) return;
        WorldItemBase item = collision.collider.GetComponentInParent<WorldItemBase>();
        if (item != null) _shelf.TryDistanceShelf(item);
    }
}
