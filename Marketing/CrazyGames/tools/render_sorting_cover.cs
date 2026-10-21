// Evaluate in the idle Unity Editor. Uses an isolated PreviewRenderUtility scene.
// Set SessionState "SortingCover.Layer" to pile, shelves, hero, or gold.
string layer = UnityEditor.SessionState.GetString("SortingCover.Layer", "pile");
string output = "Marketing/CrazyGames/SourceRender";
System.IO.Directory.CreateDirectory(output);
var session = UnityEngine.Object.FindFirstObjectByType<TinyRiders.ThemedCollectionSession>();
var catalog = session.catalog;
var preview = new UnityEditor.PreviewRenderUtility();
UnityEngine.Bounds BoundsOf(UnityEngine.GameObject go)
{
    var renderers = go.GetComponentsInChildren<UnityEngine.MeshRenderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
    var bounds = renderers[0].bounds;
    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
    return bounds;
}
void Strip(UnityEngine.GameObject go)
{
    foreach (var text in go.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.gameObject.SetActive(false);
    foreach (var canvas in go.GetComponentsInChildren<UnityEngine.Canvas>(true)) canvas.gameObject.SetActive(false);
    foreach (var behaviour in go.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true).Reverse()) UnityEngine.Object.DestroyImmediate(behaviour);
    foreach (var collider in go.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
    foreach (var body in go.GetComponentsInChildren<UnityEngine.Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
    foreach (var light in go.GetComponentsInChildren<UnityEngine.Light>(true)) UnityEngine.Object.DestroyImmediate(light);
    foreach (var ps in go.GetComponentsInChildren<UnityEngine.ParticleSystem>(true)) ps.gameObject.SetActive(false);
    foreach (var renderer in go.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
    { renderer.lightmapIndex = -1; renderer.realtimeLightmapIndex = -1; renderer.renderingLayerMask = 1; }
}
UnityEngine.GameObject Art(UnityEngine.GameObject source, UnityEngine.Transform parent, float footprint)
{
    var go = UnityEngine.Object.Instantiate(source);
    preview.AddSingleGO(go);
    go.transform.SetParent(parent, false);
    go.transform.localPosition = UnityEngine.Vector3.zero;
    go.transform.localRotation = UnityEngine.Quaternion.identity;
    Strip(go);
    var bounds = BoundsOf(go);
    go.transform.localScale *= footprint / UnityEngine.Mathf.Max(bounds.size.x, bounds.size.z);
    bounds = BoundsOf(go);
    go.transform.position -= new UnityEngine.Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    return go;
}
TinyRiders.ThemedCarCatalog.Entry Entry(string theme, string model) => catalog.entries.First(e => e.enabled && e.themeId == theme && e.baseCarId == model);
var root = new UnityEngine.GameObject("Marketing-only " + layer);
preview.AddSingleGO(root);
try
{
    if (layer == "pile")
    {
        string[] themes = {"TH_PIRATE", "TH_SPACE", "TH_ICE", "TH_FIRE", "TH_BEACH", "TH_WIZARD", "TH_CANDY", "TH_CONSTRUCTION"};
        string[] models = {"C001", "C005", "C010", "C014"};
        var selected = catalog.entries.Where(e => e.enabled && themes.Contains(e.themeId)).ToArray();
        for (int i = 0; i < 28; i++)
        {
            var entry = selected[(i * 37 + 5) % selected.Length];
            var car = Art(entry.prefab, root.transform, i < 18 ? 1.3f : 1.22f);
            int row = i < 18 ? i / 6 : (i - 18) / 4;
            int col = i < 18 ? i % 6 : (i - 18) % 4;
            car.transform.position += i < 18
                ? new UnityEngine.Vector3((col - 2.5f) * .96f, .05f + .05f * (i % 3), row * .88f)
                : new UnityEngine.Vector3((col - 1.5f) * .92f, .65f + row * .18f, .5f + row * .8f);
            car.transform.Rotate(UnityEngine.Vector3.up, (i * 113) % 280 - 140, UnityEngine.Space.World);
            car.transform.Rotate(UnityEngine.Vector3.forward, (i % 5 - 2) * 7, UnityEngine.Space.World);
            car.transform.Rotate(UnityEngine.Vector3.right, (i % 3 - 1) * 9, UnityEngine.Space.World);
        }
    }
    else if (layer == "shelves")
    {
        var allShelves = UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None);
        string[] themes = { "TH_PIRATE", "TH_SPACE" };
        for (int bay = 0; bay < 2; bay++)
        {
            var source = allShelves.First(s => s.GetComponent<TinyRiders.ThemeShelfBinding>()?.themeId == themes[bay]);
            var shelf = Art(source.gameObject, root.transform, 3.3f);
            shelf.transform.Rotate(UnityEngine.Vector3.up, 180, UnityEngine.Space.World);
            shelf.transform.position += new UnityEngine.Vector3((bay - .5f) * 3.42f, 0, .1f * bay);
            var spots = shelf.GetComponentsInChildren<UnityEngine.Transform>(true).Where(t => t.name.StartsWith("Slot_R", System.StringComparison.Ordinal)).OrderBy(t => t.name).ToArray();
            var entries = catalog.entries.Where(e => e.enabled && e.themeId == themes[bay]).OrderBy(e => e.baseCarId).ToArray();
            for (int i = 0; i < spots.Length; i++)
            {
                var car = Art(entries[i % entries.Length].prefab, root.transform, .49f);
                car.transform.position += spots[i].position;
                car.transform.Rotate(UnityEngine.Vector3.up, 180, UnityEngine.Space.World);
            }
        }
    }
    else if (layer == "hero")
    {
        var entry = catalog.entries.First(e => e.enabled && e.themeId == "TH_BEACH" && e.collectibleId.Contains("C005"));
        var car = Art(entry.prefab, root.transform, 3.4f);
        car.transform.Rotate(UnityEngine.Vector3.up, 110, UnityEngine.Space.World);
    }
    else if (layer == "gold")
    {
        var reward = UnityEngine.Object.FindFirstObjectByType<TinyRiders.GoldenCarReveal>();
        var pedestal = Art(reward.pedestal.gameObject, root.transform, 1.9f);
        var top = BoundsOf(pedestal).max.y;
        var car = Art(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/TinyRiders/Art/GoldenReward/Prefabs/TR_GoldenCar_Reward.prefab"), root.transform, 1.65f);
        car.transform.position += UnityEngine.Vector3.up * top;
        car.transform.Rotate(UnityEngine.Vector3.up, 110, UnityEngine.Space.World);
    }
    var bounds = BoundsOf(root);
    var camera = preview.camera;
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    camera.backgroundColor = new UnityEngine.Color(0, 0, 0, 0);
    camera.orthographic = true;
    camera.nearClipPlane = .01f; camera.farClipPlane = 100;
    camera.allowHDR = true; camera.allowMSAA = true;
    var direction = (layer == "shelves" ? new UnityEngine.Vector3(2.7f, 2.4f, -12f) : new UnityEngine.Vector3(5f, 4.5f, -11f)).normalized;
    camera.transform.position = bounds.center + direction * 25;
    camera.transform.LookAt(bounds.center);
    preview.lights[0].intensity = .95f;
    preview.lights[0].enabled = true;
    preview.lights[0].color = new UnityEngine.Color(1f, .96f, .88f);
    preview.lights[0].transform.rotation = UnityEngine.Quaternion.Euler(40, -35, 0);
    preview.lights[1].intensity = .55f;
    preview.lights[1].enabled = true;
    preview.lights[1].color = new UnityEngine.Color(.78f, .91f, 1f);
    preview.lights[1].transform.rotation = UnityEngine.Quaternion.Euler(35, 145, 0);
    preview.ambientColor = new UnityEngine.Color(.3f, .33f, .38f);
    int width = layer == "shelves" ? 2400 : 2000;
    int height = layer == "shelves" ? 1800 : 1500;
    float aspect = (float)width / height;
    float maxX = 0, maxY = 0;
    for (int i = 0; i < 8; i++)
    {
        var corner = bounds.center + UnityEngine.Vector3.Scale(bounds.extents, new UnityEngine.Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
        var local = camera.transform.InverseTransformPoint(corner);
        maxX = UnityEngine.Mathf.Max(maxX, UnityEngine.Mathf.Abs(local.x));
        maxY = UnityEngine.Mathf.Max(maxY, UnityEngine.Mathf.Abs(local.y));
    }
    camera.orthographicSize = UnityEngine.Mathf.Max(maxY, maxX / aspect) * 1.05f;
    foreach (string matte in new[] { "black", "white" })
    {
        preview.BeginStaticPreview(new UnityEngine.Rect(0, 0, width, height));
        camera.backgroundColor = matte == "black" ? UnityEngine.Color.black : UnityEngine.Color.white;
        preview.Render(true, false);
        var image = preview.EndStaticPreview();
        System.IO.File.WriteAllBytes(output + "/" + layer + "-" + matte + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    }
    return new { layer, bounds = bounds.size.ToString(), width, height };
}
finally { preview.Cleanup(); }
