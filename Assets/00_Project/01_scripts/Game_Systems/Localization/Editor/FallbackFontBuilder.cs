#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary> Menu tool that builds the Baloo fallback SDF fonts and links them to Fredoka. </summary>
public static class FallbackFontBuilder
{
    private const string Folder = "Assets/00_Project/04_ِArt/UI/Fonts";
    private const string PrimaryName = "Fredoka-SemiBold SDF";
    private const string LatinName = "Baloo2-SemiBold";
    private const string ArabicName = "BalooBhaijaan2-SemiBold";

    [MenuItem("Tools/Localization/Build Fallback Fonts")]
    public static void Build()
    {
        var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{Folder}/{PrimaryName}.asset");
        TMP_FontAsset latin = EnsureAsset(LatinName);
        TMP_FontAsset arabic = EnsureAsset(ArabicName);

        if (primary.fallbackFontAssetTable == null) primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
        AddFallback(primary, latin);
        AddFallback(primary, arabic);

        string missing = PopulateFromDatabase(primary, latin, arabic);
        EditorUtility.SetDirty(primary);
        EditorUtility.SetDirty(latin);
        EditorUtility.SetDirty(arabic);
        AssetDatabase.SaveAssets();
        Debug.Log("Fallback fonts ready." + (missing.Length > 0 ? " No glyph for: " + missing : string.Empty));
    }

    private static TMP_FontAsset EnsureAsset(string name)
    {
        string path = $"{Folder}/{name} SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>($"{Folder}/{name}.ttf");
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);
        asset.name = name + " SDF";
        AssetDatabase.CreateAsset(asset, path);

        asset.material.name = name + " SDF Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (Texture2D atlas in asset.atlasTextures)
        {
            atlas.name = name + " SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
        }

        var so = new SerializedObject(asset);
        SerializedProperty clear = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (clear != null) clear.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static void AddFallback(TMP_FontAsset primary, TMP_FontAsset fallback)
    {
        if (!primary.fallbackFontAssetTable.Contains(fallback)) primary.fallbackFontAssetTable.Add(fallback);
    }

    /// <summary> Bakes every database character Fredoka lacks into the fallbacks; returns chars no font has. </summary>
    private static string PopulateFromDatabase(TMP_FontAsset primary, TMP_FontAsset latin, TMP_FontAsset arabic)
    {
        var chars = new SortedSet<char>();
        LocalizationDatabase_SO db = LocalizationCsvTool.LoadOrCreateDatabase();
        foreach (LocalizationDatabase_SO.Column column in db.Columns)
        {
            bool isArabic = column.Code.StartsWith("ar");
            foreach (string value in column.Values)
            {
                if (string.IsNullOrEmpty(value)) continue;
                string text = isArabic ? ArabicText.Shape(value) : value;
                foreach (char ch in text) chars.Add(ch);
            }
        }
        foreach (LanguageInfo info in db.Languages)
            foreach (char ch in ArabicText.NeedsShaping(info.NativeName) ? ArabicText.Shape(info.NativeName) : info.NativeName)
                chars.Add(ch);

        var needed = new StringBuilder();
        foreach (char ch in chars)
            if (ch > ' ' && !primary.HasCharacter(ch, false, true)) needed.Append(ch);

        latin.TryAddCharacters(needed.ToString(), out string remaining);
        arabic.TryAddCharacters(remaining, out string missing);
        return missing ?? string.Empty;
    }
}
#endif
