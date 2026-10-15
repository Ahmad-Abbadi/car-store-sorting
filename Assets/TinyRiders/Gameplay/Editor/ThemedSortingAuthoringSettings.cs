using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName = "Tiny Riders/Sorting Authoring Settings")]
public sealed class ThemedSortingAuthoringSettings : ScriptableObject
{
    [Min(1)] public int copiesPerDesign = 2;
    [Range(12, 180)] public int visiblePileLimit = 108;
    public int seed = 73481;
    public static ThemedSortingAuthoringSettings Load()
    {
        const string path = "Assets/TinyRiders/Gameplay/Editor/ThemedSortingSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<ThemedSortingAuthoringSettings>(path);
        if (settings == null)
        {
            settings = CreateInstance<ThemedSortingAuthoringSettings>();
            AssetDatabase.CreateAsset(settings, path);
        }
        return settings;
    }
}
