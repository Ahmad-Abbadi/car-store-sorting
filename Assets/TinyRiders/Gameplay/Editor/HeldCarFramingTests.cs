using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class HeldCarFramingTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly Rect SafeArea = Rect.MinMaxRect(.58f, .16f, .98f, .60f);
    static ThemedCollectionSession Session => ThemedCollectionSession.Instance;

    public static void AssertVisible(HandItemBase hand, Camera camera, bool checkDefaultFacing = true)
    {
        Vector2 minimum = Vector2.one, maximum = Vector2.zero;
        foreach (var renderer in hand.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled) continue;
            var bounds = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                var point = camera.WorldToViewportPoint(renderer.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, sign)));
                minimum = Vector2.Min(minimum, new Vector2(point.x, point.y));
                maximum = Vector2.Max(maximum, new Vector2(point.x, point.y));
                Assert.That(point.x, Is.InRange(SafeArea.xMin - .0001f, SafeArea.xMax + .0001f), hand.Data.name + " horizontal crop");
                Assert.That(point.y, Is.InRange(SafeArea.yMin - .0001f, SafeArea.yMax + .0001f), hand.Data.name + " vertical crop");
                Assert.Greater(point.z, camera.nearClipPlane + .02f, hand.Data.name + " near clip");
            }
        }
        Assert.True(maximum.x - minimum.x > .27f || maximum.y - minimum.y > .29f,
            $"{hand.Data.name}: held car should fill the larger foreground area; size={maximum - minimum}, aspect={camera.aspect}, fov={camera.fieldOfView}");
        if (!checkDefaultFacing) return;
        Vector3 facing = camera.transform.InverseTransformDirection(hand.transform.forward);
        Vector3 expected = Quaternion.Euler(hand.Data.HeldRotation + new Vector3(-3.659882f, 88.439224f, 5.780028f)) * Vector3.forward;
        Assert.Less(Vector3.Distance(expected, facing), .001f, "Default pose should match the chosen front/side reference angle");
    }

    [Test, Timeout(240000)]
    public void All600Designs_FitAcrossAspectRatiosAndFov_WithoutChangingAssets()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var rig = new GameObject("Framing test socket");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig, scene);
            rig.transform.SetPositionAndRotation(new Vector3(500, 3, -12), Quaternion.Euler(35, 70, 0));
            rig.transform.localScale = Vector3.one * .8f;
            var handler = rig.AddComponent<ItemsHandler>(); handler.FrameCameraFixedCars = true;
            typeof(ItemsHandler).GetField("holdSocket", Private).SetValue(handler, rig.transform);
            var view = new GameObject("Framing test camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(view, scene);
            var camera = view.AddComponent<Camera>(); camera.nearClipPlane = .1f;
            camera.transform.SetPositionAndRotation(new Vector3(500, 3.1f, -12.05f), Quaternion.Euler(36, 72, 0));
            typeof(ItemsHandler).GetField("_presentationCamera", Private).SetValue(handler, camera);
            var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CompactCollectionBuilder.CatalogPath);
            var entries = catalog.entries.Where(e => e.sortingItem != null).ToArray();
            Assert.AreEqual(600, entries.Length);
            foreach (var entry in entries)
            {
                var item = entry.sortingItem;
                Vector3 authoredPosition = item.HeldPosition, authoredRotation = item.HeldRotation;
                Vector3 prefabScale = item.HandItem.transform.localScale, worldScale = item.WorldItem.transform.localScale;
                handler.Equip_ItemData(item);
                var hand = handler.CurrentEquipped;
                foreach (float aspect in new[] { 16f / 9, 16f / 10, 21f / 9 })
                foreach (float fov in new[] { 60f, 75f, 90f })
                {
                    camera.aspect = aspect; camera.fieldOfView = fov;
                    var pose = handler.GetHeldPose(hand);
                    hand.transform.SetLocalPositionAndRotation(pose.Position, pose.Rotation);
                    hand.transform.localScale = pose.Scale;
                    AssertVisible(hand, camera);
                    // Resolving repeatedly must not shrink the already fitted model again.
                    Assert.Less(Vector3.Distance(pose.Scale, handler.GetHeldPose(hand).Scale), .00001f);
                }
                // Include underside, rear, and rolled views for every design and its accessories.
                foreach (var angles in new[] { new Vector3(0, 90, 0), new Vector3(180, 0, 0), new Vector3(60, 180, 45) })
                {
                    typeof(ItemsHandler).GetField("_inspectionRotation", Private).SetValue(handler, Quaternion.Euler(angles));
                    var pose = handler.GetHeldPose(hand);
                    hand.transform.SetLocalPositionAndRotation(pose.Position, pose.Rotation);
                    hand.transform.localScale = pose.Scale;
                    AssertVisible(hand, camera, false);
                }
                typeof(ItemsHandler).GetField("_inspectionRotation", Private).SetValue(handler, Quaternion.identity);
                Assert.AreEqual(authoredPosition, item.HeldPosition); Assert.AreEqual(authoredRotation, item.HeldRotation);
                Assert.AreEqual(prefabScale, item.HandItem.transform.localScale); Assert.AreEqual(worldScale, item.WorldItem.transform.localScale);
                Object.DestroyImmediate(hand.gameObject);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void UnconfiguredOrMissingBounds_UseAuthoredPose()
    {
        var root = new GameObject("Fallback socket");
        var data = ScriptableObject.CreateInstance<ItemDataSO>();
        try
        {
            var handler = root.AddComponent<ItemsHandler>();
            var hand = new GameObject("Empty held car").AddComponent<HandItemBase>();
            hand.gameObject.AddComponent<ThemedCarIdentity>();
            hand.transform.SetParent(root.transform, false); hand.transform.localScale = Vector3.one * 2f;
            typeof(HandItemBase).GetField("data", Private).SetValue(hand, data);
            data.CameraFixed = true; data.HeldPosition = new Vector3(1.6f, -2.1f, 7.8f); data.HeldRotation = new Vector3(0, 150, 0);
            foreach (bool enabled in new[] { false, true })
            {
                handler.FrameCameraFixedCars = enabled;
                var pose = handler.GetHeldPose(hand);
                Assert.AreEqual(data.HeldPosition, pose.Position);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(data.HeldRotation), pose.Rotation), .001f);
                Assert.AreEqual(Vector3.one * 2f, pose.Scale);
            }
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator PickupSwitchAndCameraMotion_KeepTheWholeCarVisible()
    {
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        CompactCollectionTests.Isolate(default, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        while (Session == null || Player.Instance == null || Time.frameCount < 10 || Session.LivePileCount != 1200) yield return null;
        yield return ExerciseMotion();
        yield return new ExitPlayMode();
    }

    // Create the exercise iterator after the domain reload, as in the collection regression suite.
    static IEnumerator ExerciseMotion()
    {
        Session.suspendPersistenceForTests = true;
        var player = Player.Instance;
        Assert.True(player.ItemHandler.FrameCameraFixedCars, "Scene framing setting was not saved");
        var entries = Session.ActiveDefinitions.OrderBy(e => e.collectibleId).Take(3).ToArray();
        for (int i = 0; i < 3; i++)
        {
            var world = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy && w.Data == entries[i].sortingItem);
            Assert.True(player.ItemCollector.TryCollectItem(world));
            yield return null; // supersede pickup flights before they finish
        }
        float until = Time.realtimeSinceStartup + .7f;
        while (Time.realtimeSinceStartup < until) yield return null;
        AssertVisible(player.ItemHandler.CurrentEquipped, Camera.main);
        player.StashHandler.SelectIndex(0);
        until = Time.realtimeSinceStartup + .7f;
        while (Time.realtimeSinceStartup < until) yield return null;
        AssertVisible(player.ItemHandler.CurrentEquipped, Camera.main);
        foreach (float pitch in new[] { -65f, 0f, 65f })
        {
            player.Look.SetLook(25, pitch);
            until = Time.realtimeSinceStartup + .2f;
            while (Time.realtimeSinceStartup < until) yield return null;
            AssertVisible(player.ItemHandler.CurrentEquipped, Camera.main);
        }
        yield return ExerciseInspection(player);
    }

    static IEnumerator ExerciseInspection(Player player)
    {
        var settings = InputSystem.settings;
        var previousBehavior = settings.editorInputBehaviorInPlayMode;
        var previousBackground = settings.backgroundBehavior;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var mouse = InputSystem.AddDevice<Mouse>();
        var input = player.Input;
        var handler = player.ItemHandler;
        void Send(bool down, Vector2 delta)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = delta }.WithButton(MouseButton.Middle, down));
            InputSystem.Update();
            // Exercise reads in camera-before-handler order without waiting for another input update.
            typeof(PlayerInput).GetField("_inspectionFrame", Private).SetValue(input, -1);
        }
        try
        {
            input.Enable();
            input.SendMessage("OnApplicationFocus", true);
            Send(false, Vector2.zero);
            Assert.False(input.IsInspectingCar);
            var initial = handler.GetHeldPose(handler.CurrentEquipped);
            Send(true, new Vector2(80, 40));
            Assert.True(input.IsInspectingCar);
            Assert.AreEqual(Vector2.zero, input.LookDelta, "Inspection must consume camera look in the same frame");
            float yaw = player.Look.Yaw, pitch = player.Look.Pitch;
            player.Look.SendMessage("Update");
            Assert.AreEqual(yaw, player.Look.Yaw); Assert.AreEqual(pitch, player.Look.Pitch);
            handler.SendMessage("Update");
            var rotated = handler.GetHeldPose(handler.CurrentEquipped);
            Assert.Greater(Quaternion.Angle(initial.Rotation, rotated.Rotation), 10f);
            Send(false, Vector2.zero);
            Assert.False(input.IsInspectingCar);
            Assert.Less(Quaternion.Angle(rotated.Rotation, handler.GetHeldPose(handler.CurrentEquipped).Rotation), .001f);

            // Disabling input and focus loss both require release and a new press.
            foreach (bool focusLoss in new[] { false, true })
            {
                Send(true, Vector2.one); Assert.True(input.IsInspectingCar);
                if (focusLoss) input.SendMessage("OnApplicationFocus", false); else input.Disable();
                Assert.False(input.IsInspectingCar);
                if (focusLoss) input.SendMessage("OnApplicationFocus", true); else input.Enable();
                Send(true, Vector2.one); Assert.False(input.IsInspectingCar);
                Send(false, Vector2.zero); Assert.False(input.IsInspectingCar);
            }
            Send(true, Vector2.one); Assert.True(input.IsInspectingCar);
            player.StashHandler.SelectIndex(1);
            Assert.False(input.IsInspectingCar);
            float until = Time.realtimeSinceStartup + .7f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Send(true, Vector2.one); Assert.False(input.IsInspectingCar, "Switching while held must require a fresh press");
            AssertVisible(handler.CurrentEquipped, Camera.main);
            Send(false, Vector2.zero); Assert.False(input.IsInspectingCar);
            Send(true, Vector2.one); Assert.True(input.IsInspectingCar);
            handler.Unequip();
            Assert.False(input.IsInspectingCar);
            Send(true, Vector2.one); Assert.False(input.IsInspectingCar, "An empty hand must not capture camera look");
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            settings.editorInputBehaviorInPlayMode = previousBehavior;
            settings.backgroundBehavior = previousBackground;
            input.CancelCarInspection();
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EditorApplication.isPlaying)
        {
            if (ThemedCollectionSession.Instance != null) ThemedCollectionSession.Instance.suspendPersistenceForTests = true;
            yield return new ExitPlayMode();
        }
        SessionState.SetBool("TinyRiders.Compact.QA.Active", false);
    }
}
