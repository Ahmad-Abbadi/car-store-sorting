using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Authors both lighting themes and the menu control without rebuilding room geometry.</summary>
public static class ShowroomThemeBuilder
{
    public static string ReflectionPath(Scene scene, LightingTheme theme) => ShowroomLightingBuilder.Root
        + "/Reflections/" + theme + "/" + scene.name.Replace(" ", "_") + ".exr";

    public static void RestoreDark(Scene scene)
    {
        var rig = scene.GetRootGameObjects().FirstOrDefault(g => g.name == ShowroomLightingBuilder.RigName);
        var controller = rig != null ? rig.GetComponent<ShowroomLightingController>() : null;
        if (controller != null) controller.ApplyTheme(LightingTheme.Dark);
    }

    public static void Configure(Scene scene, GameObject rig, Light key, Light[] lamps, Volume volume, ReflectionProbe probe)
    {
        var controller = rig.GetComponent<ShowroomLightingController>() ?? rig.AddComponent<ShowroomLightingController>();
        controller.darkPreset = Preset(LightingTheme.Dark, volume.sharedProfile);
        controller.brightPreset = Preset(LightingTheme.Bright, BrightProfile(volume.sharedProfile));
        controller.darkPreset.ceilingMaterials.Clear();
        controller.brightPreset.ceilingMaterials.Clear();
        controller.keyLight = key;
        controller.ceilingLights = lamps;
        controller.volume = volume;
        controller.reflectionProbe = probe;
        controller.surfaces = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
            .Where(r => r.name == "Store Ceiling" || r.name == "Acoustic Ceiling Panels" || r.name == "Racing Ceiling Ribbon"
                || (r.sharedMaterial != null && r.sharedMaterial.name.EndsWith("Even Opal Diffusers", StringComparison.Ordinal)))
            .Select(r => new ShowroomLightingController.Surface { renderer = r, dark = r.sharedMaterial, bright = BrightMaterial(r.sharedMaterial) }).ToArray();
        foreach (var surface in controller.surfaces)
        {
            if (!controller.darkPreset.ceilingMaterials.Contains(surface.dark)) controller.darkPreset.ceilingMaterials.Add(surface.dark);
            if (!controller.brightPreset.ceilingMaterials.Contains(surface.bright)) controller.brightPreset.ceilingMaterials.Add(surface.bright);
        }
        controller.darkReflection = AssetDatabase.LoadAssetAtPath<Texture>(ReflectionPath(scene, LightingTheme.Dark));
        if (controller.darkReflection == null) controller.darkReflection = probe.customBakedTexture;
        controller.brightReflection = AssetDatabase.LoadAssetAtPath<Texture>(ReflectionPath(scene, LightingTheme.Bright));
        controller.ApplyTheme(LightingTheme.Bright);
        EditorUtility.SetDirty(controller);
        foreach (var preset in new[] { controller.brightPreset, controller.darkPreset })
        { EditorUtility.SetDirty(preset); AssetDatabase.SaveAssetIfDirty(preset); }
        if (scene.path == CinematicMenuBuilder.MenuPath) ConfigureMenu(scene);
    }

    private static ShowroomLightingPreset Preset(LightingTheme theme, VolumeProfile profile)
    {
        string path = ShowroomLightingBuilder.Root + "/" + theme + "Theme.asset";
        var preset = AssetDatabase.LoadAssetAtPath<ShowroomLightingPreset>(path);
        if (preset == null) { preset = ScriptableObject.CreateInstance<ShowroomLightingPreset>(); AssetDatabase.CreateAsset(preset, path); }
        bool bright = theme == LightingTheme.Bright;
        // Bright reproduces the original Level lighting, before the showroom pass.
        preset.ambientSky = bright ? new Color(.85f, .88f, .9f) : new Color(.52f, .58f, .69f);
        preset.ambientEquator = bright ? new Color(.65f, .68f, .66f) : new Color(.42f, .46f, .53f);
        preset.ambientGround = bright ? new Color(.4f, .39f, .35f) : new Color(.25f, .27f, .32f);
        preset.keyColor = bright ? new Color(1, .96f, .89f) : new Color(.96f, .975f, 1);
        preset.keyIntensity = bright ? .72f : 1.15f;
        preset.shadowStrength = bright ? .68f : .7f;
        preset.keyRotation = bright
            ? new Quaternion(.40821794f, -.23456973f, .10938166f, .8754261f)
            : Quaternion.Euler(52, -32, 0);
        preset.keyUseColorTemperature = true;
        preset.keyColorTemperature = 3900;
        preset.shadowBias = bright ? .05f : .035f;
        preset.shadowNormalBias = bright ? .4f : .16f;
        preset.reflectionIntensity = bright ? .6f : .75f;
        preset.ceilingLightColor = bright ? new Color(1, .95f, .85f) : new Color(1, .84f, .66f);
        preset.ceilingLightIntensity = bright ? 1 : 5;
        preset.ceilingLightRange = bright ? 12 : 8;
        preset.volumeProfile = profile;
        return preset;
    }

    private static VolumeProfile BrightProfile(VolumeProfile dark)
    {
        string path = ShowroomLightingBuilder.Root + "/BrightVolume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(dark), path)) throw new InvalidOperationException("Could not create bright Volume.");
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        }
        profile.TryGet<Tonemapping>(out var tone); tone.mode.Override(TonemappingMode.None);
        profile.TryGet<ColorAdjustments>(out var color); color.postExposure.Override(0); color.contrast.Override(0); color.saturation.Override(0);
        profile.TryGet<Bloom>(out var bloom); bloom.intensity.Override(0);
        profile.TryGet<DepthOfField>(out var depth); depth.mode.Override(DepthOfFieldMode.Off);
        profile.TryGet<MotionBlur>(out var motion); motion.intensity.Override(0);
        profile.TryGet<Vignette>(out var vignette); vignette.intensity.Override(0);
        foreach (var effect in profile.components) EditorUtility.SetDirty(effect);
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
        return profile;
    }

    private static Material BrightMaterial(Material dark)
    {
        const string prefix = "Showroom ";
        string originalName = dark.name.StartsWith(prefix, StringComparison.Ordinal) ? dark.name.Substring(prefix.Length) : dark.name;
        string path = ShopInteriorPolishBuilder.ArtRoot + "/Materials/" + originalName + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) throw new InvalidOperationException("Original ceiling material missing: " + path);
        return material;
    }

    public static void ConfigureMenu(Scene scene)
    {
        EnsureTranslations();
        var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenu>(true)).Single();
        var data = new SerializedObject(menu);
        var details = (UnityEngine.UI.Button)data.FindProperty("itemDetailButton").objectReferenceValue;
        var buttons = details.transform.parent;
        var existing = buttons.Find("Button_LightingTheme");
        var button = existing != null ? existing.GetComponent<UnityEngine.UI.Button>()
            : Object.Instantiate(details.gameObject, buttons).GetComponent<UnityEngine.UI.Button>();
        button.name = "Button_LightingTheme";
        button.transform.SetSiblingIndex(details.transform.GetSiblingIndex() + 1);
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        foreach (var localized in button.GetComponentsInChildren<LocalizedText>(true)) Object.DestroyImmediate(localized);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        label.SetLocalized(Localizer.Format("menu.theme", Localizer.Get("menu.theme.bright")));
        data.FindProperty("themeButton").objectReferenceValue = button;
        data.FindProperty("themeLabel").objectReferenceValue = label;
        data.ApplyModifiedPropertiesWithoutUndo();
        float spacing = buttons.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing;
        float row = UnityEngine.UI.LayoutUtility.GetPreferredHeight(details.GetComponent<RectTransform>()) + spacing;
        var card = (RectTransform)buttons.parent;
        card.sizeDelta = new Vector2(card.sizeDelta.x, 440 + row);
        var title = card.parent.Find("Title").GetComponent<RectTransform>();
        title.anchoredPosition = new Vector2(title.anchoredPosition.x, 250 + row * .5f);
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(card);
        EditorUtility.SetDirty(menu);
    }

    private static void EnsureTranslations()
    {
        var db = LocalizationCsvTool.LoadOrCreateDatabase();
        string[] keys = { "menu.theme", "menu.theme.bright", "menu.theme.dark" };
        string[][] values = {
            new[] { "Theme: {0}", "Tema: {0}", "Tema: {0}", "Tema: {0}", "Tema: {0}", "Motyw: {0}", "Giao diện: {0}", "المظهر: {0}" },
            new[] { "Bright", "Claro", "Claro", "Terang", "Aydınlık", "Jasny", "Sáng", "فاتح" },
            new[] { "Dark", "Oscuro", "Escuro", "Gelap", "Karanlık", "Ciemny", "Tối", "داكن" }
        };
        string[] codes = { "en", "es", "pt-BR", "id", "tr", "pl", "vi", "ar" };
        var list = db.Keys.ToList();
        for (int k = 0; k < keys.Length; k++)
        {
            int index = list.IndexOf(keys[k]);
            if (index < 0) { index = list.Count; list.Add(keys[k]); }
            foreach (var column in db.Columns)
            {
                int language = Array.IndexOf(codes, column.Code);
                var cells = column.Values.ToList();
                while (cells.Count < list.Count) cells.Add("");
                cells[index] = values[k][language >= 0 ? language : 0];
                column.Values = cells.ToArray();
            }
        }
        db.SetTable(list.ToArray(), db.Columns);
        AssetDatabase.SaveAssetIfDirty(db);
        Localizer.RefreshDatabase();
    }
}
