using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class SmoothCarPhysicsTests
{
    const string Evidence = "IntegrationEvidence/SmoothCarPhysics";
    static ThemedCollectionSession Session => ThemedCollectionSession.Instance;
    static IEnumerator Tick(float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < until) yield return null;
    }
    static WorldItemBase Spawn(Vector3 position)
    {
        Assert.IsNotNull(Session, "Collection session disappeared during physics test.");
        var entry = Session.ActiveDefinitions.First();
        Assert.IsNotNull(entry.sortingItem);
        var source = entry.sortingItem.WorldItem;
        Assert.IsNotNull(source);
        var item = UnityEngine.Object.Instantiate(source, position, Quaternion.identity);
        item.gameObject.SetActive(true);
        Assert.IsNotNull(item.Body, "Spawned car did not initialize its Rigidbody.");
        ItemPhysicsCulling.Track(item);
        item.Body.isKinematic = false; item.Body.WakeUp();
        return item;
    }
    static void Control(WorldItemBase item, string method, params object[] args) =>
        typeof(WorldItemBase).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(item, args);

    [UnityTest, Timeout(240000)]
    public IEnumerator Capsules_SettlePushPullAndReuse()
    {
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.position");
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        CompactCollectionTests.Isolate(default, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        while (Session == null || Player.Instance == null || Session.LivePileCount != 1200) yield return null;
        Session.suspendPersistenceForTests = true;
        var player = Player.Instance;
        player.StateMachine.enabled = false;
        if (player.Look != null) player.Look.enabled = false;
        var idle = IdleInputs(); player.Character.SetInputs(ref idle);
        Assert.False(Session.roomLayout.walkThroughLooseCars);
        Assert.True(KinematicCharacterSystem.Settings.Interpolate);
        Assert.True(WorldItemBase.All.All(w => w.GetComponent<CapsuleCollider>() != null));
        var report = new List<string>();

        // Activating near stock must preserve sleep over many fixed steps.
        Vector3 origin = Session.roomLayout.spawn;
        var sleeper = Spawn(origin + new Vector3(.8f, 0, .25f));
        yield return Tick(5);
        Assert.True(sleeper.Body.IsSleeping());
        Vector3 rest = sleeper.Body.position;
        yield return Tick(10);
        Assert.True(sleeper.Body.IsSleeping(), "Nearby resting car was needlessly woken.");
        Assert.Less(Vector3.Distance(rest, sleeper.Body.position), .002f);
        UnityEngine.Object.Destroy(sleeper.gameObject);
        report.Add("Nearby resting car stayed asleep and drifted less than 2 mm over 10 seconds.");

        // Remove actual support, then verify falling and quiet settling.
        var lower = Spawn(origin + new Vector3(.9f, 0, .5f));
        // Keep this isolated support fixture from rolling apart before removal.
        lower.Body.constraints = RigidbodyConstraints.FreezeRotation;
        yield return Tick(2);
        var capsule = lower.GetComponent<CapsuleCollider>();
        var upper = Spawn(lower.Body.position + Vector3.up * (capsule.radius * 2 + .008f));
        upper.Body.constraints = RigidbodyConstraints.FreezeRotation;
        yield return Tick(4);
        float upperY = upper.Body.position.y;
        Control(lower, "BeginPowerControl", "physics-test", true);
        lower.transform.position += Vector3.up * 1.5f;
        yield return Tick(5);
        Assert.Less(upper.Body.position.y, upperY - capsule.radius);
        Assert.True(upper.Body.IsSleeping());
        Assert.True(lower.GetComponent<CapsuleCollider>().isTrigger);
        Control(lower, "EndPowerControl", .1f);
        yield return Tick(6);
        Assert.False(lower.GetComponent<CapsuleCollider>().isTrigger);
        Assert.True(lower.Body.IsSleeping());
        report.Add("Support removal caused a natural fall; released car restored solid contact and settled.");
        Control(lower, "BeginPowerControl", "physics-test", true);
        Assert.True(Session.Recycle(lower));
        Assert.False(lower.gameObject.activeSelf);
        Assert.True(lower.Body.isKinematic);
        Assert.False(lower.GetComponent<CapsuleCollider>().isTrigger);
        // Avoid capturing locals in a closure across EnterPlayMode's domain reload.
        foreach (var child in lower.GetComponentsInChildren<Collider>(true))
            if (child.transform != lower.transform) Assert.True(child.isTrigger);
        report.Add("Recycling a controlled car restored its solid body and retained authored pick triggers.");
        // The throw must retain its impulse even when released from an overlapping pose.
        upper.transform.position = origin + new Vector3(-.9f, -.02f, .5f);
        Control(upper, "BeginPowerControl", "physics-test", true);
        Control(upper, "EndThrowControl", Vector3.up * .3f, Vector3.zero);
        yield return Tick(.2f);
        Assert.False(upper.Body.isKinematic);
        Assert.False(upper.GetComponent<CapsuleCollider>().isTrigger);
        Assert.Greater(upper.Body.position.y, -.02f);
        report.Add("Throw release resolved a floor overlap and restored dynamic solid collision.");
        UnityEngine.Object.Destroy(lower.gameObject); UnityEngine.Object.Destroy(upper.gameObject);

        foreach (int fps in new[] { 30, 60, 120 })
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = fps;
            player.Motor.SetPositionAndRotation(origin, Quaternion.identity);
            player.Motor.BaseVelocity = Vector3.zero;
            player.Character.MaxStableMoveSpeed = 3;
            var car = Spawn(origin + Vector3.forward * .9f);
            yield return Tick(2);
            Vector3 start = player.Motor.TransientPosition, carStart = car.Body.position;
            float minY = start.y, maxY = start.y;
            float until = Time.realtimeSinceStartup + 1.4f;
            var input = new PlayerCharacterInputs { MoveAxisForward = 1, CameraRotation = Quaternion.identity };
            while (Time.realtimeSinceStartup < until)
            {
                player.Character.SetInputs(ref input);
                minY = Mathf.Min(minY, player.Motor.TransientPosition.y);
                maxY = Mathf.Max(maxY, player.Motor.TransientPosition.y);
                yield return null;
            }
            var stop = IdleInputs(); player.Character.SetInputs(ref stop);
            float advance = player.Motor.TransientPosition.z - start.z;
            Assert.Greater(advance, 1.2f, "Player snagged at " + fps + " FPS.");
            Assert.Greater(Vector3.Distance(carStart, car.Body.position), .1f);
            Assert.Less(maxY - minY, .12f, "Player lifted by stock at " + fps + " FPS.");
            report.Add(fps + " FPS: player advance=" + advance + ", height variation=" + (maxY - minY));
            UnityEngine.Object.Destroy(car.gameObject);
        }
        Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/playmode.txt", report);
        yield return new ExitPlayMode();
    }

    static PlayerCharacterInputs IdleInputs() => new PlayerCharacterInputs { CameraRotation = Quaternion.identity };

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EditorApplication.isPlaying)
        {
            if (Session != null) Session.suspendPersistenceForTests = true;
            yield return new ExitPlayMode();
        }
        SessionState.SetBool("TinyRiders.Compact.QA.Active", false);
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA"); PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.position");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.coins");
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
    }
}
