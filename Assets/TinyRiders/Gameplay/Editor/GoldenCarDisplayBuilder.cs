using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TinyRiders
{
    /// <summary>Installs the grand exhibit through Unity, retaining authored nested art prefabs.</summary>
    public static class GoldenCarDisplayBuilder
    {
        const string Folder = "Assets/TinyRiders/Gameplay/GoldenCar";
        const string FxFolder = Folder + "/CeremonyEffects";
        const string Arsenal = "Assets/Polygon Arsenal/Prefabs/";
        public const string DisplayPath = "Assets/TinyRiders/Art/GoldenReward/Prefabs/TR_GoldenReward_Display.prefab";
        public const string CarPath = "Assets/TinyRiders/Art/GoldenReward/Prefabs/TR_GoldenCar_Reward.prefab";

        [MenuItem("Tools/Tiny Riders/Collector Shop/Install Golden Car Display")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CompactCollectionBuilder.ScenePath) throw new InvalidOperationException("Open Level first.");
            var displayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DisplayPath);
            var carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPath);
            if (displayPrefab == null || carPrefab == null) throw new InvalidOperationException("Golden reward art is missing.");
            if (!displayPrefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Anchor_Car") ||
                !displayPrefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "GlassCover"))
                throw new InvalidOperationException("Reward station requires its authored anchor and glass.");
            if (carPrefab.GetComponentsInChildren<WorldItemBase>(true).Length != 0 || carPrefab.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("The reward must remain decorative.");
            Directory.CreateDirectory(FxFolder);
            AssetDatabase.Refresh();
            var controllers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GoldenCarReveal>(true)).ToArray();
            if (controllers.Length > 1) throw new InvalidOperationException("Duplicate reward controllers.");
            var counter = GameObject.Find("Collector Counter") ?? GameObject.Find("Upgrade Box");
            if (counter == null) throw new InvalidOperationException("Collector counter is missing.");
            var controller = controllers.SingleOrDefault();
            if (controller == null)
            {
                var go = new GameObject("Golden Car Display");
                Undo.RegisterCreatedObjectUndo(go, "Install grand reward");
                go.transform.SetParent(counter.transform.parent, false);
                controller = Undo.AddComponent<GoldenCarReveal>(go);
            }
            var root = controller.transform;
            Undo.RecordObject(root, "Place grand exhibit");
            Undo.RecordObject(controller, "Configure grand ceremony");
            var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
            var layout = session.roomLayout;
            var blockers = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.enabled && !c.isTrigger &&
                !c.transform.IsChildOf(root) && c.GetComponentInParent<Player>() == null &&
                c.GetComponentInParent<WorldItemBase>() == null && c.bounds.max.y > .3f && c.bounds.min.y < 2.3f).ToArray();
            root.position = FindPosition(counter.transform.position, layout, blockers);
            root.rotation = Quaternion.identity;
            root.localScale = Vector3.one;

            var pedestal = controller.pedestal;
            if (pedestal == null) pedestal = Shape("Trophy Pedestal", root, true).transform;
            Undo.RecordObject(pedestal, "Widen reward base");
            pedestal.localPosition = new Vector3(0, .1f, 0);
            pedestal.localScale = new Vector3(3, .1f, 3);
            pedestal.GetComponent<Renderer>().sharedMaterial = Material("PedestalTeal", new Color(.025f, .09f, .10f), .25f, .45f);
            controller.pedestal = pedestal;
            var foot = root.Find("Gold Foot") ?? Shape("Gold Foot", root, false).transform;
            foot.localPosition = new Vector3(0, .025f, 0); foot.localScale = new Vector3(3.06f, .025f, 3.06f);
            foot.GetComponent<Renderer>().sharedMaterial = Material("TrophyGold", new Color(1, .64f, .13f), .85f, .82f);
            foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true)) Undo.DestroyObjectImmediate(text.gameObject);
            controller.placard = null;
            foreach (var obsolete in new[] { "Illuminated Rim", "Velvet Display Pad" })
                if (root.Find(obsolete) != null) Undo.DestroyObjectImmediate(root.Find(obsolete).gameObject);

            var installation = Child(root, "Grand Exhibit Installation");
            installation.localPosition = new Vector3(0, .2f, 0); installation.localScale = Vector3.one * 5;
            var display = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t =>
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)) == DisplayPath);
            if (display == null) display = ((GameObject)PrefabUtility.InstantiatePrefab(displayPrefab, installation)).transform;
            Undo.SetTransformParent(display, installation, "Install enlarged station");
            display.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); display.localScale = Vector3.one;
            var anchor = display.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Anchor_Car");
            var glass = display.GetComponentsInChildren<Transform>(true).Single(t => t.name == "GlassCover");
            string glassPath=Folder+"/GrandExhibitGlass.mat";
            var glassMaterial=AssetDatabase.LoadAssetAtPath<Material>(glassPath);
            if(glassMaterial==null)
            {
                glassMaterial=new Material(Shader.Find("TinyRiders/GoldenRewardGlass"));
                AssetDatabase.CreateAsset(glassMaterial,glassPath);
            }
            foreach(var renderer in glass.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=glassMaterial;
            var pivot = Child(anchor, "Ceremony Presentation Pivot");
            pivot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); pivot.localScale = Vector3.one;
            var car = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t =>
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)) == CarPath);
            if (car == null) car = ((GameObject)PrefabUtility.InstantiatePrefab(carPrefab, pivot)).transform;
            Undo.SetTransformParent(car, pivot, "Keep authored car beneath presentation pivot");
            car.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity); car.localScale = Vector3.one;
            if (controller.trophy != null && controller.trophy != car.gameObject) Undo.DestroyObjectImmediate(controller.trophy);
            if (controller.cover != null && controller.cover != glass) Undo.DestroyObjectImmediate(controller.cover.gameObject);
            controller.trophy = car.gameObject; controller.cover = glass; controller.keepCoverVisible = true;
            controller.duration = 90; controller.revealFieldOfView = 43;
            glass.gameObject.SetActive(true); car.gameObject.SetActive(false);
            var barrier = root.GetComponent<CapsuleCollider>();
            if (barrier == null) barrier = Undo.AddComponent<CapsuleCollider>(root.gameObject);
            barrier.radius = 1.37f; barrier.height = 2.3f; barrier.center = Vector3.up * 1.15f;

            var ceremony = root.GetComponent<GoldenRewardCeremony>();
            if (ceremony == null) ceremony = Undo.AddComponent<GoldenRewardCeremony>(root.gameObject);
            Undo.RecordObject(ceremony, "Author energy ceremony");
            controller.ceremony = ceremony; ceremony.presentationPivot = pivot; ceremony.riseHeight = .32f;
            ceremony.protectedLooseBounds = new Bounds(root.position + Vector3.up, new Vector3(5.5f, 2, 5.5f));
            if (controller.displayLight != null) Undo.DestroyObjectImmediate(controller.displayLight.gameObject);
            var lighting = Child(root, "Ceremony Lighting");
            var lights = new List<Light>();
            for (int i = 0; i < 4; i++)
            {
                var lamp = Child(lighting, "Gold Spotlight " + i);
                float angle = (45 + i * 90) * Mathf.Deg2Rad;
                lamp.localPosition = new Vector3(Mathf.Sin(angle) * 1.1f, 2.18f, Mathf.Cos(angle) * 1.1f);
                var light = lamp.GetComponent<Light>();
                if (light == null) light = Undo.AddComponent<Light>(lamp.gameObject);
                light.type = LightType.Spot; light.range = 5; light.spotAngle = 55; light.innerSpotAngle = 25;
                light.color = new Color(1, .78f, .38f); light.intensity = .08f; light.shadows = LightShadows.None;
                lamp.LookAt(root.position + Vector3.up * .8f); lights.Add(light);
            }
            ceremony.lights = lights.ToArray(); controller.displayLight = null;
            var cameraRig = Child(root, "Ceremony Camera Path");
            var offsets = new[] { new Vector3(2.6f,2.6f,4.5f), new Vector3(2.3f,2.45f,4.4f),
                new Vector3(.8f,1.8f,3.8f), new Vector3(-.8f,2.5f,4.3f), new Vector3(1.8f,2.1f,4.1f), new Vector3(2.6f,2.6f,4.5f) };
            var times = new[] { 12f, 28, 45, 62, 78, 84 };
            ceremony.shots = offsets.Select((offset, i) =>
            {
                var view = Child(cameraRig, "Shot " + i); view.localPosition = offset;
                view.LookAt(root.position + Vector3.up * 1.12f);
                return new GoldenRewardCeremony.CameraShot { viewpoint = view, time = times[i], fieldOfView = i == 2 ? 40 : 43 };
            }).ToArray();
            controller.cameraViewpoint = ceremony.shots[0].viewpoint;
            if (Camera.main != null && Camera.main.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var cameraData))
            { Undo.RecordObject(cameraData,"Enable reward particle depth"); cameraData.requiresDepthTexture=true; EditorUtility.SetDirty(cameraData); }
            var oldView = root.Find("Golden Car Camera Viewpoint"); if (oldView != null) Undo.DestroyObjectImmediate(oldView.gameObject);

            var fx = Child(root, "Ceremony Effects");
            var cues = new List<GoldenRewardCeremony.EffectCue>();
            void Effect(string name, string source, Vector3 p, float scale, float start, float end, bool loop, bool ambient = false)
            {
                var prefab = EffectVariant(name, Arsenal + source + ".prefab", loop, ambient);
                var instance = fx.Find(name);
                if (instance == null) instance = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, fx)).transform;
                instance.name = name; instance.localPosition = p; instance.localScale = Vector3.one * scale;
                instance.gameObject.SetActive(false);
                cues.Add(new GoldenRewardCeremony.EffectCue { effect = instance.gameObject, start = start, end = end, earnedAmbient = ambient });
            }
            Effect("Gold Motes", "Interactive/Sparkle/SparkleYellow", new Vector3(0,1.05f,0), 1, 3, 90, true, true);
            Effect("Gathering Streams", "Interactive/BeamUp/Chargeup/BeamupChargeupYellow", new Vector3(0,.52f,0), .48f, 12, 32, true);
            Effect("Golden Spiral", "Combat/Aura/SwirlAura/SwirlAuraYellow", new Vector3(0,.72f,0), 1.6f, 28, 45, true);
            Effect("Crown Beam", "Interactive/BeamUp/Cylinder/BeamupCylinderYellow", new Vector3(0,.51f,0), .5f, 30, 35, true);
            Effect("Manifestation Burst", "Combat/Explosions/Magic/Magic/MagicExplosionYellow", new Vector3(0,.9f,0), .3f, 32, 36, false);
            Effect("Platter Rings", "Interactive/Loot/LootRing/LootRingYellow", new Vector3(0,.49f,0), 2.5f, 12, 78, true);
            Effect("Landing Burst", "Combat/Explosions/Magic/Magic/MagicExplosionYellow", new Vector3(0,.6f,0), .45f, 76, 80, false);
            Effect("Final Gold Fireworks Left", "Environment/Fireworks/Explosion/FireworksYellow", new Vector3(-1.1f,2.05f,0), .24f, 77, 82, false);
            Effect("Final Gold Fireworks Right", "Environment/Fireworks/Explosion/FireworksYellow", new Vector3(1.1f,2.05f,0), .24f, 79, 84, false);
            ceremony.effects = cues.ToArray();
            var audio = Child(root, "Ceremony Audio");
            ceremony.audioSource = audio.GetComponent<AudioSource>();
            if (ceremony.audioSource == null) ceremony.audioSource = Undo.AddComponent<AudioSource>(audio.gameObject);
            ceremony.audioSource.playOnAwake = false; ceremony.audioSource.spatialBlend = 0;
            var manager = Object.FindFirstObjectByType<SimpleAudioManager>();
            if (manager != null)
            {
                var property = new SerializedObject(manager).FindProperty("_audioSourcePrefab");
                var sourcePrefab = property?.objectReferenceValue as GameObject;
                if (sourcePrefab != null) ceremony.audioSource.outputAudioMixerGroup = sourcePrefab.GetComponent<AudioSource>().outputAudioMixerGroup;
            }
            AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/00_Project/Audio/PackageSelection/SFX/" + name + ".wav") ??
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/00_Project/Audio/Playful/SFX/" + name + ".wav");
            ceremony.sounds = new[] {
                new GoldenRewardCeremony.SoundCue {clip=Clip("Sweep"),time=12,volume=.18f},
                new GoldenRewardCeremony.SoundCue {clip=Clip("Sweep"),time=28,volume=.25f},
                new GoldenRewardCeremony.SoundCue {clip=Clip("Complete"),time=32,volume=.35f},
                new GoldenRewardCeremony.SoundCue {clip=Clip("Sweep"),time=62,volume=.18f},
                new GoldenRewardCeremony.SoundCue {clip=Clip("Complete"),time=76,volume=.4f} };
            ProtectPile(session, ceremony.protectedLooseBounds);
            EditorUtility.SetDirty(ceremony); EditorUtility.SetDirty(controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(display);
            PrefabUtility.RecordPrefabInstancePropertyModifications(car);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("GRAND_GOLDEN_REWARD_READY: 5x art, text-free, 90-second ceremony at " + root.position);
        }

        static Vector3 FindPosition(Vector3 counter, CollectorRoomLayout layout, Collider[] blockers)
        {
            var candidates = new List<Vector3>();
            for (float x = -8; x <= 8; x += .25f)
                for (float z = -10; z <= -7.5f; z += .25f) candidates.Add(layout.origin + new Vector3(x, 0, z));
            foreach (var p in candidates.OrderBy(p => (p - counter).sqrMagnitude))
            {
                if (Vector2.Distance(new Vector2(p.x,p.z), new Vector2(layout.spawn.x,layout.spawn.z)) < 3.1f) continue;
                if (!layout.Contains(p, 2.75f)) continue;
                if (blockers.Any(c =>
                {
                    var b = c.bounds; var closest = b.ClosestPoint(new Vector3(p.x,b.center.y,p.z));
                    return new Vector2(closest.x-p.x,closest.z-p.z).magnitude < 2.75f;
                })) continue;
                return p;
            }
            throw new InvalidOperationException("No nearby exhibit position preserves the 1.2m walkway.");
        }

        [Serializable] sealed class Pile { public int revision; public ThemedInstanceRecord[] placements; }
        static void ProtectPile(ThemedCollectionSession session, Bounds bounds)
        {
            var layout = session.roomLayout;
            Undo.RecordObject(layout, "Protect grand exhibit walkway");
            // Keep prior exclusions; replace only this builder's previous exclusion on repeated installation.
            layout.pileExclusions = layout.pileExclusions.Where(b => b.size != new Vector3(5.5f, 2, 5.5f))
                .Concat(new[] {new Bounds(bounds.center-layout.origin,bounds.size)}).ToArray();
            EditorUtility.SetDirty(layout); AssetDatabase.SaveAssetIfDirty(layout);
            var path = AssetDatabase.GetAssetPath(session.pileLayout);
            var pile = JsonUtility.FromJson<Pile>(session.pileLayout.text);
            var art = session.catalog.entries.Where(e => e.enabled).ToDictionary(e => e.collectibleId,
                e => CollectorRoomBuilder.ArtBounds(e.sortingItem.WorldItem.gameObject));
            var occupied = pile.placements.Select(r => CrowdedCollectorRoomBuilder.TransformedBounds(art[r.collectibleId],r.position,Quaternion.Euler(r.euler))).ToList();
            int moved = 0;
            foreach (var record in pile.placements)
            {
                var world = session.pileOrigin.position + record.position;
                if (!GoldenRewardCeremony.ContainsLoose(bounds, world)) continue;
                bool placed = false;
                for (float z = -10.8f; z < 11 && !placed; z += .35f)
                    for (float x = -10.2f; x < 10.3f && !placed; x += .35f)
                    {
                        var q = Quaternion.Euler(0,record.euler.y,0);
                        var box = CrowdedCollectorRoomBuilder.TransformedBounds(art[record.collectibleId],Vector3.zero,q);
                        var p = new Vector3(x,.03f-box.min.y,z);
                        box.center += p;
                        if (GoldenRewardCeremony.ContainsLoose(bounds,session.pileOrigin.position+p)) continue;
                        var clearance = new Bounds(new Vector3(box.center.x,1,box.center.z),new Vector3(box.size.x+.06f,2,box.size.z+.06f));
                        if (layout.pileExclusions.Any(b => b.Intersects(clearance))) continue;
                        float support = 0;
                        foreach (var other in occupied)
                            if(box.min.x<other.max.x+.02f && box.max.x>other.min.x-.02f && box.min.z<other.max.z+.02f && box.max.z>other.min.z-.02f)
                                support=Mathf.Max(support,other.max.y);
                        p.y+=support;box.center+=Vector3.up*support;
                        if(box.max.y>1.07f)continue;
                        record.position = p; record.euler = new Vector3(0,record.euler.y,0);
                        occupied.Add(box); placed = true; moved++;
                    }
                if (!placed) throw new InvalidOperationException("Cannot relocate loose car outside exhibit walkway.");
            }
            if (moved > 0) { File.WriteAllText(path,JsonUtility.ToJson(pile,true)); AssetDatabase.ImportAsset(path); }
            Debug.Log("Grand exhibit pile exclusion installed; relocated authored poses=" + moved);
        }

        static GameObject EffectVariant(string name, string source, bool loop, bool ambient)
        {
            string path = FxFolder + "/" + name.Replace(" ", "") + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (prefab == null) throw new InvalidOperationException("Import Polygon Arsenal and its bundled URP upgrade: " + source);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
            go.name = name;
            foreach (var script in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            foreach (var light in go.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);
            foreach (var audio in go.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(audio);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.playOnAwake = false; main.useUnscaledTime = true; main.loop = loop;
                main.stopAction = ParticleSystemStopAction.None; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.maxParticles = ambient ? 24 : 160;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1,.72f,.18f,.45f), new Color(1,.94f,.64f,.7f));
                var emission = ps.emission;
                emission.rateOverTimeMultiplier *= ambient ? .08f : .45f;
                if(ambient)
                {
                    if(ps.transform!=go.transform){ps.gameObject.SetActive(false);continue;}
                    main.startLifetime=new ParticleSystem.MinMaxCurve(2,3.5f);
                    main.startSize=new ParticleSystem.MinMaxCurve(.025f,.045f);main.startSpeed=.035f;
                    var shape=ps.shape;shape.radius=.8f;emission.SetBursts(Array.Empty<ParticleSystem.Burst>());emission.rateOverTime=2;
                }
                else if(name=="Gathering Streams")
                {
                    if(ps.name.Contains("End")){ps.gameObject.SetActive(false);continue;}
                    emission.rateOverTime=ps.name.Contains("Dust")?18:1.2f;
                    if(ps.name=="ChargeupOuter")main.startSizeZ=8;
                }
                else if(name=="Golden Spiral")main.startSizeMultiplier*=.35f;
                else if(name=="Crown Beam")
                {
                    emission.rateOverTime=ps.name=="Crown Beam"?24:2;
                    if(main.startSize3D)main.startSizeZ=Mathf.Min(main.startSizeZ.constantMax,8);
                }
                else if(name=="Platter Rings")
                {
                    if(ps.name=="VerticalGlow"){ps.gameObject.SetActive(false);continue;}
                    if(ps.name=="FlatGlow")main.startSizeMultiplier*=1.7f;
                }
                var collision = ps.collision; collision.enabled = false;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(mat =>
                {
                    if (mat == null || (mat.shader != null && (mat.shader.name.Contains("Universal") || mat.shader.name.Contains("/URP/")))) return mat;
                    // The publisher's URP upgrade leaves PolySolidIntense on its legacy shader.
                    // Correct only our variant, preserving all publisher assets.
                    string materialPath = FxFolder + "/" + mat.name + "_URP.mat";
                    var replacement = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (replacement == null)
                    {
                        var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/Polygon Arsenal/Materials/Main/PolySolidGlow.mat");
                        replacement = new Material(template); replacement.name = mat.name + "_URP";
                        AssetDatabase.CreateAsset(replacement,materialPath);
                    }
                    return replacement;
                }).ToArray();
            }
            return PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally { Object.DestroyImmediate(go); }
        }

        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name); if (t != null) return t;
            var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Author ceremony rig");
            go.transform.SetParent(parent,false); return go.transform;
        }
        static GameObject Shape(string name, Transform parent, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name;
            Undo.RegisterCreatedObjectUndo(go,"Author exhibit base"); go.transform.SetParent(parent,false);
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static Material Material(string name, Color color, float metallic, float smoothness)
        {
            string path=Folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);return material;
        }
    }
}
