using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Irahimz.UIParticles;

// Converts a Particle System hierarchy into one that renders inside a Canvas, using the bundled
// UIParticleSystem renderer (a re-namespaced copy of the free UI Extensions script).
//
// SELF-CONTAINED: this package ships its own UIParticleSystem + UIParticleSubEmitter
// (namespace Irahimz.UIParticles), so it needs no external UIParticle asset — copy the folder and go.
//
// That component draws particles as UI Graphic quads and takes its texture from the *Graphic's*
// material, not from the ParticleSystemRenderer (which it disables). It also only handles the one
// ParticleSystem on its own GameObject. So a tree of systems needs a component + a matching
// material per node, which is what this tool builds: it reads the texture each system already uses
// on its renderer, bakes a UI material for it into the folder you pick, and wires it up.
//
// Drop the root of the effect into Root, pick a Material Folder, hit Set Up. Re-running is safe --
// existing materials at the same path are reused, not duplicated.
// Open via: IrahimzTools > UI Particle Setup
public class UIParticleSetup : EditorWindow
{
    private const string DefaultShaderName = "UI/Additive";
    private const string FolderPrefKey = "UIParticleSetup.MaterialFolder";

    private GameObject _root;
    private string _materialFolder = "Assets/UI Particle Materials";
    private Shader _shader;
    private bool _forceLocalSpace = true;
    private bool _disableRaycastTarget = true;
    private bool _convertSubEmitters = true;
    private float _scaleMultiplier = 1f;

    private Vector2 _scroll;
    private string _lastReport = "";

    [MenuItem("IrahimzTools/UI Particle Setup")]
    private static void Open()
    {
        GetWindow<UIParticleSetup>("UI Particle Setup").minSize = new Vector2(380, 380);
    }

    private void OnEnable()
    {
        _materialFolder = EditorPrefs.GetString(FolderPrefKey, _materialFolder);
        if (_shader == null) _shader = Shader.Find(DefaultShaderName);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("UI Particle Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drop the root of a particle effect in, pick where the UI materials go, then Set Up. " +
            "Every ParticleSystem under the root gets a UIParticleSystem plus a material built from " +
            "the texture its renderer already uses.",
            MessageType.Info);

        EditorGUILayout.Space();
        _root = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Root", "The particle effect's root. Should live under a Canvas."),
            _root, typeof(GameObject), true);

        EditorGUILayout.BeginHorizontal();
        _materialFolder = EditorGUILayout.TextField(
            new GUIContent("Material Folder", "Generated UI materials are written here. Created if missing."),
            _materialFolder);
        if (GUILayout.Button("...", GUILayout.Width(28)))
        {
            string picked = EditorUtility.OpenFolderPanel("Material Folder", "Assets", "");
            if (!string.IsNullOrEmpty(picked))
            {
                string rel = FileUtil.GetProjectRelativePath(picked);
                if (!string.IsNullOrEmpty(rel)) _materialFolder = rel;
                else EditorUtility.DisplayDialog("Invalid folder", "Pick a folder inside this project's Assets.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        _shader = (Shader)EditorGUILayout.ObjectField(
            new GUIContent("Shader", "Shader for the generated materials. UI/Additive for glows and sparks, " +
                                     "UI/Default for solid art like confetti."),
            _shader, typeof(Shader), false);

        EditorGUILayout.Space();
        _forceLocalSpace = EditorGUILayout.Toggle(
            new GUIContent("Force Local Simulation Space", "World space costs an extra transform per particle per frame."),
            _forceLocalSpace);
        _disableRaycastTarget = EditorGUILayout.Toggle(
            new GUIContent("Disable Raycast Target", "Particles are Graphics, so they eat touches unless this is off."),
            _disableRaycastTarget);
        _convertSubEmitters = EditorGUILayout.Toggle(
            new GUIContent("Convert Sub-Emitters", "Move the subEmitters module onto a UIParticleSubEmitter that " +
                                                   "re-fires it via Emit(). Birth and Death only."),
            _convertSubEmitters);
        _scaleMultiplier = Mathf.Max(0.0001f, EditorGUILayout.FloatField(
            new GUIContent("Scale Multiplier", "Sizes become canvas pixels, so a world-scale effect comes out one pixel " +
                                               "wide. Multiplies start size / speed / gravity / shape. 1 = leave alone."),
            _scaleMultiplier));

        EditorGUILayout.Space();
        int count = Collect().Count;
        using (new EditorGUI.DisabledScope(count == 0 || _shader == null))
        {
            if (GUILayout.Button($"Set Up For UI  ({count} Particle System{(count == 1 ? "" : "s")})", GUILayout.Height(32)))
                Run();
        }
        using (new EditorGUI.DisabledScope(count == 0))
        {
            if (GUILayout.Button("Remove UI Setup", GUILayout.Height(20)))
                Revert();
        }

        if (_root == null)
            EditorGUILayout.LabelField("Assign a Root GameObject.", EditorStyles.miniLabel);
        else if (count == 0)
            EditorGUILayout.LabelField("Root has no ParticleSystem.", EditorStyles.miniLabel);
        else if (_shader == null)
            EditorGUILayout.LabelField($"Shader not set ('{DefaultShaderName}' not found?).", EditorStyles.miniLabel);
        else if (_root.GetComponentInParent<Canvas>() == null)
            EditorGUILayout.HelpBox("Root isn't under a Canvas. Move it under one, or the particles won't draw.",
                MessageType.Warning);

        if (_shader != null && !ShaderHasMainTex(_shader))
            EditorGUILayout.HelpBox(
                $"'{_shader.name}' has no _MainTex. UIParticleSystem only reads textures through _MainTex, so it " +
                "will draw untextured. Pick a UI shader (UI/Additive, UI/Default), not a URP one.",
                MessageType.Warning);

        if (!string.IsNullOrEmpty(_lastReport))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(140));
            EditorGUILayout.TextArea(_lastReport, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }
    }

    private List<ParticleSystem> Collect()
    {
        var result = new List<ParticleSystem>();
        if (_root == null) return result;
        foreach (var ps in _root.GetComponentsInChildren<ParticleSystem>(true))
            if (ps != null) result.Add(ps);
        return result;
    }

    private void Run()
    {
        var systems = Collect();
        if (systems.Count == 0 || _shader == null) return;

        EnsureFolder(_materialFolder);
        EditorPrefs.SetString(FolderPrefKey, _materialFolder);

        var sb = new StringBuilder();
        var materialCache = new Dictionary<Texture, Material>();
        int wired = 0, untextured = 0, subConverted = 0, subDropped = 0, rotating3d = 0, meshMode = 0;

        Undo.SetCurrentGroupName("UI Particle Setup");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var ps in systems)
        {
            var go = ps.gameObject;
            EnsureRectTransform(go);

            var graphic = go.GetComponent<UIParticleSystem>();
            if (graphic == null) graphic = Undo.AddComponent<UIParticleSystem>(go);
            Undo.RecordObject(graphic, "UI Particle Setup");

            // The texture the artist already picked on the renderer is the one we want on the
            // Graphic -- UIParticleSystem disables that renderer and reads _MainTex off the
            // Graphic's material instead.
            var texture = SourceTexture(ps, out string noTextureReason);
            if (texture != null)
            {
                if (!materialCache.TryGetValue(texture, out var mat))
                {
                    mat = GetOrCreateMaterial(texture);
                    materialCache[texture] = mat;
                }
                graphic.material = mat;
                sb.AppendLine($"{HierarchyPath(go)}  ->  {mat.name}");
            }
            else
            {
                untextured++;
                sb.AppendLine($"{HierarchyPath(go)}  ->  NO MATERIAL BUILT: {noTextureReason}");
            }

            if (_disableRaycastTarget) graphic.raycastTarget = false;

            // The 3D path costs a quaternion per particle, so only take it where the system is
            // actually authored to tumble.
            graphic.use3dRotation = UsesRotation3D(ps);
            if (graphic.use3dRotation) rotating3d++;

            var particleRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null && particleRenderer.renderMode == ParticleSystemRenderMode.Mesh)
            {
                meshMode++;
                sb.AppendLine($"    WARNING: render mode is Mesh. UIParticleSystem only builds quads, " +
                              $"so '{go.name}' will draw as flat sprites, not meshes.");
            }

            Undo.RecordObject(ps, "UI Particle Setup");
            var main = ps.main;
            if (_forceLocalSpace) main.simulationSpace = ParticleSystemSimulationSpace.Local;
            if (!Mathf.Approximately(_scaleMultiplier, 1f)) ScaleForCanvas(ps, _scaleMultiplier);
            if (_convertSubEmitters) ConvertSubEmitters(ps, sb, ref subConverted, ref subDropped);

            EditorUtility.SetDirty(graphic);
            EditorUtility.SetDirty(ps);
            wired++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();

        string summary =
            $"Done. Wired {wired} system{(wired == 1 ? "" : "s")}, " +
            $"{materialCache.Count} material{(materialCache.Count == 1 ? "" : "s")} in {_materialFolder}." +
            (untextured > 0 ? $" {untextured} had no source texture." : "") +
            (subConverted > 0 ? $" Converted {subConverted} sub-emitter{(subConverted == 1 ? "" : "s")}." : "") +
            (subDropped > 0 ? $" DROPPED {subDropped} sub-emitter{(subDropped == 1 ? "" : "s")} (Collision/Trigger)." : "") +
            (rotating3d > 0 ? $" Enabled 3D rotation on {rotating3d}." : "") +
            (meshMode > 0 ? $" WARNING: {meshMode} use Mesh render mode and will draw as flat sprites." : "") +
            "\n\nDraw order is hierarchy sibling order now, not the particle render queue.\n\n" + sb;
        _lastReport = summary;
        Debug.Log("[UIParticleSetup] " + summary, _root);
    }

    private void Revert()
    {
        var systems = Collect();
        int removed = 0;

        Undo.SetCurrentGroupName("Remove UI Particle Setup");
        int undoGroup = Undo.GetCurrentGroup();

        int subRestored = 0;
        foreach (var ps in systems)
        {
            // Undo the sub-emitter handover first -- the shim's trigger list is the only record of
            // which targets had their emission switched off.
            var shim = ps.GetComponent<UIParticleSubEmitter>();
            if (shim != null)
            {
                foreach (var trigger in shim.triggers)
                {
                    if (trigger == null || trigger.target == null) continue;
                    Undo.RecordObject(trigger.target, "Remove UI Particle Setup");
                    var targetEmission = trigger.target.emission;
                    targetEmission.enabled = true;
                    EditorUtility.SetDirty(trigger.target);
                }

                Undo.RecordObject(ps, "Remove UI Particle Setup");
                var subEmitters = ps.subEmitters;
                subEmitters.enabled = true;
                EditorUtility.SetDirty(ps);

                Undo.DestroyObjectImmediate(shim);
                subRestored++;
            }

            var graphic = ps.GetComponent<UIParticleSystem>();
            if (graphic == null) continue;
            Undo.DestroyObjectImmediate(graphic);

            // UIParticleSystem switched the real renderer off when it took over; give it back.
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && !renderer.enabled)
            {
                Undo.RecordObject(renderer, "Remove UI Particle Setup");
                renderer.enabled = true;
                EditorUtility.SetDirty(renderer);
            }
            removed++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        _lastReport = $"Removed UIParticleSystem from {removed} system{(removed == 1 ? "" : "s")} and re-enabled their renderers.\n" +
                      (subRestored > 0 ? $"Restored the subEmitters module on {subRestored} system{(subRestored == 1 ? "" : "s")}.\n" : "") +
                      "Generated materials were left on disk.";
        Debug.Log("[UIParticleSetup] " + _lastReport, _root);
    }

    // Hands the subEmitters module over to a UIParticleSubEmitter. The native module can't reach
    // across UIParticleSystem's per-system Simulate() calls, so the module is switched off and the
    // component re-fires its entries with explicit Emit() calls instead. Only Birth and Death port
    // over -- Collision and Trigger depend on physics that a Canvas effect doesn't have, and Manual
    // was already your own code's job.
    private static void ConvertSubEmitters(ParticleSystem ps, StringBuilder sb, ref int converted, ref int dropped)
    {
        var subEmitters = ps.subEmitters;
        if (!subEmitters.enabled || subEmitters.subEmittersCount == 0) return;

        var shim = ps.GetComponent<UIParticleSubEmitter>();
        if (shim == null) shim = Undo.AddComponent<UIParticleSubEmitter>(ps.gameObject);
        Undo.RecordObject(shim, "UI Particle Setup");
        shim.triggers.Clear();

        for (int i = 0; i < subEmitters.subEmittersCount; i++)
        {
            var target = subEmitters.GetSubEmitterSystem(i);
            var type = subEmitters.GetSubEmitterType(i);

            if (target == null) continue;
            if (type != ParticleSystemSubEmitterType.Birth && type != ParticleSystemSubEmitterType.Death)
            {
                dropped++;
                sb.AppendLine($"    sub-emitter '{target.name}' ({type}) DROPPED -- only Birth/Death port to UI");
                continue;
            }

            var properties = subEmitters.GetSubEmitterProperties(i);
            int count = BurstCount(target);
            bool guessed = count == 0;
            if (guessed) count = 10;

            shim.triggers.Add(new UIParticleSubEmitter.Trigger
            {
                target = target,
                type = type,
                count = count,
                probability = subEmitters.GetSubEmitterEmitProbability(i),
                inheritColor = (properties & ParticleSystemSubEmitterProperties.InheritColor) != 0,
                inheritSize = (properties & ParticleSystemSubEmitterProperties.InheritSize) != 0,
                inheritRotation = (properties & ParticleSystemSubEmitterProperties.InheritRotation) != 0,
                inheritLifetime = (properties & ParticleSystemSubEmitterProperties.InheritLifetime) != 0,
            });

            // The shim drives this target by hand now, so its own emission must not also run.
            // Emit() ignores the emission module, so switching it off costs nothing.
            var targetEmission = target.emission;
            if (targetEmission.enabled)
            {
                Undo.RecordObject(target, "UI Particle Setup");
                targetEmission.enabled = false;
                EditorUtility.SetDirty(target);
            }

            converted++;
            sb.AppendLine($"    sub-emitter '{target.name}' ({type}) -> {count} per event" +
                          (guessed ? " (GUESSED -- target had no bursts to read, tune Count on the component)" : ""));
        }

        subEmitters.enabled = false;
        EditorUtility.SetDirty(shim);
    }

    // How many particles the target spawns per trigger, read from its own burst list -- that's what
    // Unity's sub-emitter would have fired. Returns 0 when there's nothing to read.
    private static int BurstCount(ParticleSystem ps)
    {
        var emission = ps.emission;
        if (!emission.enabled || emission.burstCount == 0) return 0;

        var bursts = new ParticleSystem.Burst[emission.burstCount];
        emission.GetBursts(bursts);

        int total = 0;
        foreach (var burst in bursts)
        {
            var c = burst.count;
            float value = c.mode == ParticleSystemCurveMode.TwoConstants
                ? (c.constantMin + c.constantMax) * 0.5f
                : c.constant;
            total += Mathf.RoundToInt(value) * Mathf.Max(1, burst.cycleCount);
        }
        return total;
    }

    // particle.rotation3D only carries x/y when the system is authored for separate axes -- otherwise
    // it's just (0, 0, z) and the cheaper 2D path draws the same thing.
    private static bool UsesRotation3D(ParticleSystem ps)
    {
        if (ps.main.startRotation3D) return true;

        var overLifetime = ps.rotationOverLifetime;
        if (overLifetime.enabled && overLifetime.separateAxes) return true;

        var bySpeed = ps.rotationBySpeed;
        if (bySpeed.enabled && bySpeed.separateAxes) return true;

        return false;
    }

    // Which property holds "the" texture depends on who authored the shader: URP particle shaders
    // use _BaseMap, built-in/legacy ones _MainTex, and Shader Graph invents its own names. Note a
    // URP material still serializes an empty _MainTex, so a name check alone isn't enough -- the
    // property has to actually hold a texture.
    private static readonly string[] PreferredTextureProperties =
    {
        "_BaseMap", "_MainTex", "_BaseColorMap", "_UnlitColorMap",
    };

    private static Texture SourceTexture(ParticleSystem ps, out string reason)
    {
        reason = "";
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null) { reason = "no ParticleSystemRenderer"; return null; }

        var source = renderer.sharedMaterial;
        if (source == null) { reason = "renderer has no material"; return null; }

        foreach (var property in PreferredTextureProperties)
        {
            if (!source.HasProperty(property)) continue;
            var texture = source.GetTexture(property);
            if (texture != null) return texture;
        }

        if (source.mainTexture != null) return source.mainTexture;

        // Shader Graph and anything else custom: take the first texture the shader actually
        // exposes that isn't a normal map.
        var shader = source.shader;
        int count = shader.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            if (shader.GetPropertyType(i) != ShaderPropertyType.Texture) continue;
            string name = shader.GetPropertyName(i);
            if (name.Contains("Bump") || name.Contains("Normal")) continue;
            var texture = source.GetTexture(name);
            if (texture != null) return texture;
        }

        reason = $"'{source.name}' ({shader.name}) has no texture assigned";
        return null;
    }

    private Material GetOrCreateMaterial(Texture texture)
    {
        string path = $"{_materialFolder}/UIP_{SanitizeFileName(texture.name)}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(_shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssignTexture(mat, texture);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.shader != _shader) mat.shader = _shader;
            AssignTexture(mat, texture);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    // UIParticleSystem.Initialize() only picks up a texture via HasProperty("_MainTex") + mainTexture,
    // so the generated material has to speak the built-in convention regardless of what the source
    // material used. _BaseMap is here only to keep a URP shader from silently producing nothing.
    private static void AssignTexture(Material mat, Texture texture)
    {
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
        else if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
        else mat.mainTexture = texture;
    }

    private static bool ShaderHasMainTex(Shader shader)
    {
        int count = shader.GetPropertyCount();
        for (int i = 0; i < count; i++)
            if (shader.GetPropertyType(i) == ShaderPropertyType.Texture && shader.GetPropertyName(i) == "_MainTex")
                return true;
        return false;
    }

    // A Graphic reads transform as RectTransform and null-refs on a plain Transform. AddComponent
    // swaps it in place; local TRS is re-applied because the swap doesn't always carry it over.
    private static void EnsureRectTransform(GameObject go)
    {
        if (go.GetComponent<RectTransform>() != null) return;

        var t = go.transform;
        Vector3 localPosition = t.localPosition;
        Quaternion localRotation = t.localRotation;
        Vector3 localScale = t.localScale;

        var rt = Undo.AddComponent<RectTransform>(go);
        if (rt == null) return;
        rt.localPosition = localPosition;
        rt.localRotation = localRotation;
        rt.localScale = localScale;
    }

    // Particle units are canvas pixels once the Graphic draws them, so a metre-scale effect lands
    // sub-pixel. Everything that carries a length has to move together or the shape drifts.
    private static void ScaleForCanvas(ParticleSystem ps, float m)
    {
        var main = ps.main;
        if (main.startSize3D)
        {
            main.startSizeX = ScaleCurve(main.startSizeX, m);
            main.startSizeY = ScaleCurve(main.startSizeY, m);
            main.startSizeZ = ScaleCurve(main.startSizeZ, m);
        }
        else
        {
            main.startSize = ScaleCurve(main.startSize, m);
        }
        main.startSpeed = ScaleCurve(main.startSpeed, m);
        main.gravityModifier = ScaleCurve(main.gravityModifier, m);

        var shape = ps.shape;
        if (shape.enabled)
        {
            shape.radius *= m;
            shape.scale *= m;
            shape.position *= m;
        }
    }

    private static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve c, float m)
    {
        switch (c.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return new ParticleSystem.MinMaxCurve(c.constant * m);
            case ParticleSystemCurveMode.TwoConstants:
                return new ParticleSystem.MinMaxCurve(c.constantMin * m, c.constantMax * m);
            case ParticleSystemCurveMode.Curve:
                return new ParticleSystem.MinMaxCurve(c.curveMultiplier * m, c.curve);
            case ParticleSystemCurveMode.TwoCurves:
                return new ParticleSystem.MinMaxCurve(c.curveMultiplier * m, c.curveMin, c.curveMax);
            default:
                return c;
        }
    }

    private static string HierarchyPath(GameObject go)
    {
        var sb = new StringBuilder(go.name);
        var t = go.transform.parent;
        while (t != null)
        {
            sb.Insert(0, t.name + "/");
            t = t.parent;
        }
        return sb.ToString();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
