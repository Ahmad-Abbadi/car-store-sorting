using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies item feedback to every mesh of a nested car prefab.</summary>
[DisallowMultipleComponent]
public sealed class ItemVisualGroup : MonoBehaviour
{
    private static readonly List<ItemVisualGroup> AllGroups = new List<ItemVisualGroup>();
    public static IReadOnlyList<ItemVisualGroup> All => AllGroups;

    [SerializeField] private Transform visualRoot;

    private MeshRenderer[] renderers;
    private object[] primaryKeys;
    private object[] secondaryKeys;
    private bool[] originalEnabled;
    private bool far;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGroups() => AllGroups.Clear();

    public MeshRenderer[] Renderers
    {
        get
        {
            EnsureCache();
            return renderers;
        }
    }

    private void Awake() => EnsureCache();

    private void EnsureCache()
    {
        if (renderers != null) return;
        Transform root = visualRoot != null ? visualRoot : transform;
        renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        primaryKeys = new object[renderers.Length];
        secondaryKeys = new object[renderers.Length];
        originalEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            primaryKeys[i] = new object();
            secondaryKeys[i] = new object();
            originalEnabled[i] = renderers[i] != null && renderers[i].enabled;
        }
    }

    private void OnEnable()
    {
        EnsureCache();
        if (!AllGroups.Contains(this)) AllGroups.Add(this);
    }

    public void SetFar(bool value)
    {
        EnsureCache();
        if (far == value) return;
        far = value;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = !far && originalEnabled[i];
    }

    public void ShowPrimary(Color color, float width = SharedOutline.Width) => Show(true, color, width);
    public void HidePrimary() => Hide(true);
    public void ShowSecondary(Color color, float width = SharedOutline.Width) => Show(false, color, width);
    public void HideSecondary() => Hide(false);

    private void Show(bool primary, Color color, float width)
    {
        EnsureCache();
        object[] keys = primary ? primaryKeys : secondaryKeys;
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                SharedOutline.Show(keys[i], renderer, color, width);
            else
                SharedOutline.Hide(keys[i]);
        }
    }

    private void Hide(bool primary)
    {
        if (renderers == null) return;
        object[] keys = primary ? primaryKeys : secondaryKeys;
        for (int i = 0; i < keys.Length; i++) SharedOutline.Hide(keys[i]);
    }

    public void RefreshDetail(short lod)
    {
        EnsureCache();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.TryGetComponent(out MeshFilter filter) ||
                filter.sharedMesh == null || filter.sharedMesh.lodCount <= 1) continue;
            renderer.forceMeshLod = lod;
        }
    }

    private void OnDisable()
    {
        AllGroups.Remove(this);
        SetFar(false);
        HidePrimary();
        HideSecondary();
    }
}
