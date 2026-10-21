using UnityEngine;

/// <summary>Detail selection for visual-only menu cars, without gameplay or physics.</summary>
[DisallowMultipleComponent]
public sealed class ItemDetailDisplay : MonoBehaviour
{
    private MeshRenderer[] renderers;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        renderers = GetComponentsInChildren<MeshRenderer>(true);
        ItemDetail.OnChanged += Apply;
        Apply();
    }

    private void OnDisable() => ItemDetail.OnChanged -= Apply;

    private void Apply()
    {
        int lod = ItemDetail.Lod;
        foreach (var renderer in renderers) ItemDetail.ApplyTo(renderer, lod);
    }
}
