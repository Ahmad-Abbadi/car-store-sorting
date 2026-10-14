using System.Collections.Generic;
using UnityEngine;

/// <summary> Builds mesh-only hologram copies of GameObjects using a Resources material. </summary>
public static class HologramCreator
{
    public const string DefaultMaterialPath = "Hologram/HologramMaterial";

    private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    /// <summary> Copies source, strips non-mesh components, applies hologram material. Returns the copy. </summary>
    public static GameObject Create(GameObject source, string materialPath = DefaultMaterialPath)
    {
        if (source == null)
        {
            Debug.LogError("[HologramCreator] Source GameObject is null.");
            return null;
        }

        Material hologramMaterial = LoadMaterial(materialPath);
        if (hologramMaterial == null) return null;

        // Inactive holder prevents Awake/OnEnable running on the copy's scripts.
        GameObject holder = new GameObject("HologramHolder");
        holder.SetActive(false);

        GameObject hologram = Object.Instantiate(source, holder.transform);
        hologram.name = source.name + "_Hologram";

        StripComponents(hologram);
        ApplyMaterial(hologram, hologramMaterial);

        // Match source parent and local pose.
        Transform sourceTransform = source.transform;
        Transform hologramTransform = hologram.transform;
        hologramTransform.SetParent(sourceTransform.parent, false);
        hologramTransform.localPosition = sourceTransform.localPosition;
        hologramTransform.localRotation = sourceTransform.localRotation;
        hologramTransform.localScale = sourceTransform.localScale;

        Object.Destroy(holder);
        return hologram;
    }

    /// <summary> Loads and caches a material from Resources. </summary>
    private static Material LoadMaterial(string materialPath)
    {
        if (materialCache.TryGetValue(materialPath, out Material cached) && cached != null)
            return cached;

        Material material = Resources.Load<Material>(materialPath);
        if (material == null)
        {
            Debug.LogError($"[HologramCreator] No material found at Resources/{materialPath}.");
            return null;
        }

        materialCache[materialPath] = material;
        return material;
    }

    /// <summary> Removes every component except Transform, MeshFilter and MeshRenderer. </summary>
    private static void StripComponents(GameObject root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in transforms)
        {
            Component[] components = t.GetComponents<Component>();

            // Reverse order removes dependents before their required components.
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component == null) continue; // Missing script.
                if (component is Transform || component is MeshFilter || component is MeshRenderer) continue;

                Object.DestroyImmediate(component);
            }
        }
    }

    /// <summary> Replaces every material slot on all MeshRenderers with the hologram material. </summary>
    private static void ApplyMaterial(GameObject root, Material material)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer meshRenderer in renderers)
        {
            int slotCount = Mathf.Max(1, meshRenderer.sharedMaterials.Length);
            Material[] materials = new Material[slotCount];
            for (int i = 0; i < slotCount; i++)
                materials[i] = material;

            meshRenderer.sharedMaterials = materials;
        }
    }
}
