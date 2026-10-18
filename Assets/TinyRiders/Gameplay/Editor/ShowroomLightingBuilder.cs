using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Shared, repeatable showroom lighting for the store and its menu snapshot.</summary>
public static class ShowroomLightingBuilder
{
    public const string Root = "Assets/Settings/ShowroomLighting";
    public const string Evidence = "IntegrationEvidence/ShowroomLighting";
    public const string LevelPath = "Assets/00_Project/00_Scenes/Level.unity";
    public const string RigName = "Showroom Lighting";

    [MenuItem("Tools/Tiny Riders/Lighting/Apply To Both Scenes")]
    public static void ApplyBoth()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var original = SceneManager.GetActiveScene().path;
        if (SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        try
        {
            foreach (var path in new[] { LevelPath, CinematicMenuBuilder.MenuPath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                Apply(scene);
                BakeReflection(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        }
    }

    public static void Apply(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode only.");
        if (scene.path != LevelPath && scene.path != CinematicMenuBuilder.MenuPath)
            throw new InvalidOperationException("Expected a store or main-menu scene.");
        EnsureFolder(Root);
        ConfigurePipelines();
        SceneManager.SetActiveScene(scene);
        ShowroomThemeBuilder.RestoreDark(scene);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.52f, .58f, .69f);
        RenderSettings.ambientEquatorColor = new Color(.42f, .46f, .53f);
        RenderSettings.ambientGroundColor = new Color(.25f, .27f, .32f);
        RenderSettings.ambientIntensity = 1;
        RenderSettings.reflectionIntensity = .75f;
        RenderSettings.fog = false;

        var rig = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RigName);
        if (rig == null) { rig = new GameObject(RigName); SceneManager.MoveGameObjectToScene(rig, scene); }
        var lights = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true)).ToArray();
        var key = lights.FirstOrDefault(l => l.type == LightType.Directional);
        if (key == null) { var go = new GameObject("Showroom Key Light"); go.transform.SetParent(rig.transform); key = go.AddComponent<Light>(); }
        key.type = LightType.Directional;
        key.color = new Color(.96f, .975f, 1);
        key.intensity = 1.15f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = .7f;
        key.shadowBias = .035f;
        key.shadowNormalBias = .16f;
        key.transform.rotation = Quaternion.Euler(52, -32, 0);
        RenderSettings.sun = key;
        EditorUtility.SetDirty(key);
        foreach (var light in lights.Where(l => l.name == "Soft Shop Light"))
        {
            light.type = LightType.Point;
            light.color = new Color(1, .84f, .66f);
            light.intensity = 5;
            light.range = 8;
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(light);
        }

        foreach (var renderer in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)))
        {
            bool ceiling = renderer.name == "Store Ceiling" || renderer.name == "Acoustic Ceiling Panels" || renderer.name == "Racing Ceiling Ribbon";
            bool diffuser = renderer.sharedMaterial != null && renderer.sharedMaterial.name.EndsWith("Even Opal Diffusers", StringComparison.Ordinal);
            if (!ceiling && !diffuser) continue;
            renderer.sharedMaterial = LitCeilingMaterial(renderer.sharedMaterial, diffuser);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorUtility.SetDirty(renderer);
        }

        var volume = rig.GetComponent<Volume>() ?? rig.AddComponent<Volume>();
        volume.isGlobal = true; volume.priority = 50; volume.weight = 1;
        volume.sharedProfile = Profile();
        foreach (var camera in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
        {
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.volumeLayerMask |= 1;
            EditorUtility.SetDirty(camera); EditorUtility.SetDirty(data);
        }

        var probe = rig.GetComponentInChildren<ReflectionProbe>();
        if (probe == null) { var go = new GameObject("Baked Showroom Reflection"); go.transform.SetParent(rig.transform); probe = go.AddComponent<ReflectionProbe>(); }
        probe.transform.position = new Vector3(500, 1.55f, 0);
        // Custom mode serializes the offline-baked cubemap directly; bakedTexture alone
        // relies on a LightingDataAsset and is lost when reopening these procedural scenes.
        probe.mode = ReflectionProbeMode.Custom;
        probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(ReflectionPath(scene));
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.boxProjection = true;
        probe.size = new Vector3(24, 6, 26);
        probe.center = Vector3.zero;
        probe.blendDistance = 1;
        probe.resolution = 128;
        probe.hdr = true;
        probe.intensity = .85f;
        probe.nearClipPlane = .1f; probe.farClipPlane = 50;
        EditorUtility.SetDirty(probe);
        RemoveRedundantStaticOverrides(scene);
        ShowroomThemeBuilder.Configure(scene, rig, key, lights.Where(l => l.name == "Soft Shop Light").ToArray(), volume, probe);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    public static void BakeReflection(Scene scene, params LightingTheme[] themes)
    {
        var controller = scene.GetRootGameObjects().Single(g => g.name == RigName).GetComponent<ShowroomLightingController>();
        int quality = QualitySettings.GetQualityLevel();
        try
        {
            QualitySettings.SetQualityLevel(Array.IndexOf(QualitySettings.names, "High"), true);
            foreach (LightingTheme theme in themes.Length == 0 ? new[] { LightingTheme.Dark, LightingTheme.Bright } : themes)
            {
                controller.ApplyTheme(theme);
                BakeCurrentReflection(scene, theme);
                if (theme == LightingTheme.Bright) controller.brightReflection = controller.reflectionProbe.customBakedTexture;
                else controller.darkReflection = controller.reflectionProbe.customBakedTexture;
            }
        }
        finally { QualitySettings.SetQualityLevel(quality, true); controller.ApplyTheme(LightingTheme.Bright); }
        EditorUtility.SetDirty(controller);
    }

    private static void BakeCurrentReflection(Scene scene, LightingTheme theme)
    {
        EnsureFolder(Root + "/Reflections/" + theme);
        var probe = scene.GetRootGameObjects().Single(g => g.name == RigName).GetComponentInChildren<ReflectionProbe>();
        string path = ShowroomThemeBuilder.ReflectionPath(scene, theme);
        var otherRoots = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
            .Where(s => s.isLoaded && s != scene).SelectMany(s => s.GetRootGameObjects()).Where(g => g.activeSelf).ToArray();
        // The room's procedural meshes are not necessarily marked static. Include the
        // architecture in the bake without changing its runtime batching/static flags.
        var store = scene.GetRootGameObjects().First(g => g.name == "Car Store Generated" || g.name == "Cinematic Store");
        var surfaces = store.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => !r.GetComponentsInParent<Transform>(true).Any(t => t.name == "Staged Collection Cars"))
            .Select(r => r.gameObject).Distinct().ToArray();
        var flags = surfaces.Select(GameObjectUtility.GetStaticEditorFlags).ToArray();
        var prefabRoots = surfaces.Select(PrefabUtility.GetOutermostPrefabInstanceRoot).Where(g => g != null).Distinct().ToArray();
        var prefabOverrides = prefabRoots.Select(PrefabUtility.GetPropertyModifications).ToArray();
        var previousMode = probe.mode;
        try
        {
            probe.mode = ReflectionProbeMode.Baked;
            foreach (var root in otherRoots) root.SetActive(false);
            for (int i = 0; i < surfaces.Length; i++) GameObjectUtility.SetStaticEditorFlags(surfaces[i], flags[i] | StaticEditorFlags.ReflectionProbeStatic);
            if (!Lightmapping.BakeReflectionProbe(probe, path)) throw new InvalidOperationException("Reflection bake failed: " + scene.name);
        }
        finally
        {
            probe.mode = previousMode;
            for (int i = 0; i < surfaces.Length; i++) if (surfaces[i] != null) GameObjectUtility.SetStaticEditorFlags(surfaces[i], flags[i]);
            for (int i = 0; i < prefabRoots.Length; i++) if (prefabRoots[i] != null) PrefabUtility.SetPropertyModifications(prefabRoots[i], prefabOverrides[i]);
            foreach (var root in otherRoots) if (root != null) root.SetActive(true);
        }
        AssetDatabase.ImportAsset(path);
        probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
        probe.mode = ReflectionProbeMode.Custom;
        if (probe.customBakedTexture == null) throw new InvalidOperationException("Reflection texture missing: " + path);
        EditorUtility.SetDirty(probe);
    }

    private static string ReflectionPath(Scene scene) => Root + "/Reflections/" + scene.name.Replace(" ", "_") + ".exr";

    private static void RemoveRedundantStaticOverrides(Scene scene)
    {
        var roots = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Select(t => PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)).Where(g => g != null).Distinct();
        foreach (var root in roots)
        {
            var overrides = PrefabUtility.GetPropertyModifications(root);
            if (overrides == null) continue;
            var retained = overrides.Where(p => p.propertyPath != "m_StaticEditorFlags" || !(p.target is GameObject original)
                || p.value != ((int)GameObjectUtility.GetStaticEditorFlags(original)).ToString()).ToArray();
            if (retained.Length != overrides.Length) PrefabUtility.SetPropertyModifications(root, retained);
        }
    }

    private static void ConfigurePipelines()
    {
        foreach (var level in new[] { "Low", "Medium", "High" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/" + level + "_URP.asset");
            if (asset == null) throw new InvalidOperationException("Missing quality asset: " + level);
            asset.supportsHDR = true;
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_AdditionalLightsRenderingMode").intValue = 1;
            serialized.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 4;
            serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            serialized.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serialized.FindProperty("m_ShadowDistance").floatValue = 40;
            serialized.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
        }
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Default_Renderer.asset");
        var data = new SerializedObject(renderer);
        data.FindProperty("postProcessData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
        if (data.FindProperty("postProcessData").objectReferenceValue == null) throw new InvalidOperationException("URP post-process resources missing.");
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(renderer);
    }

    private static VolumeProfile Profile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "/Showroom.asset");
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, Root + "/Showroom.asset"); }
        var tone = Effect<Tonemapping>(profile); tone.mode.Override(TonemappingMode.ACES);
        var color = Effect<ColorAdjustments>(profile); color.postExposure.Override(.15f); color.contrast.Override(10); color.saturation.Override(5);
        var bloom = Effect<Bloom>(profile); bloom.threshold.Override(1.2f); bloom.intensity.Override(.13f); bloom.scatter.Override(.45f);
        Effect<DepthOfField>(profile).mode.Override(DepthOfFieldMode.Off);
        Effect<MotionBlur>(profile).intensity.Override(0);
        Effect<Vignette>(profile).intensity.Override(0);
        foreach (var component in profile.components) { EditorUtility.SetDirty(component); AssetDatabase.SaveAssetIfDirty(component); }
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
        return profile;
    }

    private static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var effect)) return effect;
        effect = profile.Add<T>(); AssetDatabase.AddObjectToAsset(effect, profile); return effect;
    }

    private static Material LitCeilingMaterial(Material source, bool diffuser)
    {
        string name = source.name.StartsWith("Showroom ", StringComparison.Ordinal) ? source.name : "Showroom " + source.name;
        string path = Root + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(source) { name = name }; AssetDatabase.CreateAsset(material, path); }
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.SetColor("_BaseColor", diffuser ? new Color(.8f, .75f, .65f) : new Color(.46f, .49f, .54f));
        material.SetFloat("_Smoothness", .2f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", diffuser ? new Color(1.7f, 1.35f, .9f) : new Color(.08f, .09f, .11f));
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
    }

    // Captures authored shots without changing the scene's saved camera or quality setting.
    public static void CaptureMenuViews(string label)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != CinematicMenuBuilder.MenuPath) throw new InvalidOperationException("Open the menu first.");
        var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single();
        var position = camera.transform.position; var rotation = camera.transform.rotation; float fov = camera.fieldOfView;
        try
        {
            var shots = scene.GetRootGameObjects().Single(g => g.name == "Cinematic Cameras").transform;
            foreach (Transform shot in shots)
            {
                var start = shot.Find("Start Pose");
                camera.transform.SetPositionAndRotation(start.position, start.rotation);
                camera.fieldOfView = shot.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>().Lens.FieldOfView;
                Capture(camera, label + "/" + shot.name.Substring(0, 2) + ".png");
            }
        }
        finally { camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = fov; }
    }

    public static void Capture(Camera camera, string filename)
    {
        string path = Evidence + "/" + filename; Directory.CreateDirectory(Path.GetDirectoryName(path));
        var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGBHalf);
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            if (texture != null) Object.DestroyImmediate(texture); RenderTexture.ReleaseTemporary(target);
        }
    }
}
