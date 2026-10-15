using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KinematicCharacterController.Examples;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class CarContactCameraTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly FieldInfo LastTarget = typeof(CameraStateMachine).GetField("_lastTargetPosition", Private);
    static readonly FieldInfo Offset = typeof(CameraStateMachine).GetField("_horizontalOffset", Private);
    static readonly MethodInfo Follow = typeof(CameraStateMachine).GetMethod("FollowPitchPivot", Private);
    static ThemedCollectionSession Session => ThemedCollectionSession.Instance;
    static PlayerCharacterInputs Input(Vector2 move) => new PlayerCharacterInputs
        { MoveAxisRight = move.x, MoveAxisForward = move.y, CameraRotation = Quaternion.identity };

    [UnityTest, Timeout(180000)]
    public IEnumerator PushingCars_SmoothsContactAndRecoversExactFollow()
    {
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        CompactCollectionTests.Isolate(default, UnityEngine.SceneManagement.LoadSceneMode.Single);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        while (Session == null || Player.Instance == null || Time.frameCount < 10) yield return null;
        Session.suspendPersistenceForTests = true;
        var player = Player.Instance;
        var rig = Object.FindFirstObjectByType<CameraStateMachine>();
        Assert.IsNotNull(rig);
        var pivot = (Transform)typeof(CameraStateMachine).GetField("pitchPivot", Private).GetValue(rig);
        player.StateMachine.enabled = false;
        player.Look.enabled = false;
        player.Character.MaxStableMoveSpeed = 3f;
        var origin = Session.roomLayout.spawn;
        int oldFps = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
        float maximumLag = 0f;
        try
        {
            QualitySettings.vSyncCount = 0;
            foreach (int fps in new[] { 30, 60, 120 })
            foreach (var direction in new[] { Vector2.up, Vector2.right, Vector2.one.normalized })
            foreach (int count in new[] { 1, 3 })
            {
                Application.targetFrameRate = fps;
                var idle = Input(Vector2.zero);
                player.Character.SetInputs(ref idle);
                player.Motor.SetPositionAndRotation(origin, Quaternion.identity);
                player.Motor.BaseVelocity = Vector3.zero;
                var cars = new List<WorldItemBase>();
                try
                {
                    Vector3 forward = new Vector3(direction.x, 0, direction.y);
                    for (int i = 0; i < count; i++)
                    {
                        var car = Object.Instantiate(Session.ActiveDefinitions.First().sortingItem.WorldItem,
                            origin + forward * (.9f + i * .3f), Quaternion.identity);
                        car.gameObject.SetActive(true);
                        ItemPhysicsCulling.Track(car);
                        car.Body.isKinematic = false;
                        car.Body.WakeUp();
                        cars.Add(car);
                    }
                    float until = Time.realtimeSinceStartup + .6f;
                    while (Time.realtimeSinceStartup < until) yield return null;
                    Vector3 carStart = cars[0].Body.position;
                    int contacts = 0;
                    float lagSeen = 0f;
                    var input = Input(direction);
                    until = Time.realtimeSinceStartup + 1.4f;
                    while (Time.realtimeSinceStartup < until)
                    {
                        player.Character.SetInputs(ref input);
                        yield return null;
                        if (player.Character.HasRecentLooseCarContact) contacts++;
                        Vector3 target = (Vector3)LastTarget.GetValue(rig);
                        Vector3 delta = rig.transform.position - target;
                        float lag = new Vector2(delta.x, delta.z).magnitude;
                        lagSeen = Mathf.Max(lagSeen, lag);
                        maximumLag = Mathf.Max(maximumLag, lag);
                        Assert.LessOrEqual(lag, .0501f, "Contact lag exceeded 5 cm.");
                        Assert.Less(Quaternion.Angle(rig.transform.rotation, pivot.rotation), .01f);
                        Assert.Less((rig.CameraPivot.localPosition - rig.RestLocalPosition).magnitude, .0001f);
                    }
                    Assert.Greater(contacts, 0, "No live car contact: " + fps + " FPS, " + direction + ", cars=" + count);
                    Assert.Greater(lagSeen, .0001f, "Contact smoothing never activated.");
                    Assert.Greater(Vector3.Distance(carStart, cars[0].Body.position), .01f, "Car was not pushed.");
                    player.Character.SetInputs(ref idle);
                }
                finally
                {
                    foreach (var car in cars) if (car != null) Object.Destroy(car.gameObject);
                }
                float recoverUntil = Time.realtimeSinceStartup + .6f;
                while (Time.realtimeSinceStartup < recoverUntil) yield return null;
                Assert.False(player.Character.HasRecentLooseCarContact);
                Assert.AreEqual(Vector3.zero, (Vector3)Offset.GetValue(rig), "Contact offset failed to settle.");
            }

            // A braced pile should stop the player without growing camera lag or shaking at rest.
            player.Motor.SetPositionAndRotation(origin, Quaternion.identity);
            player.Motor.BaseVelocity = Vector3.zero;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = origin + new Vector3(0, .5f, 1.8f);
            wall.transform.localScale = new Vector3(2, 1, .2f);
            var bracedCars = new List<WorldItemBase>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var car = Object.Instantiate(Session.ActiveDefinitions.First().sortingItem.WorldItem,
                        origin + Vector3.forward * (.9f + i * .2f), Quaternion.identity);
                    car.gameObject.SetActive(true);
                    ItemPhysicsCulling.Track(car);
                    car.Body.isKinematic = false;
                    car.Body.WakeUp();
                    bracedCars.Add(car);
                }
                var push = Input(Vector2.up);
                float blockedUntil = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < blockedUntil)
                {
                    player.Character.SetInputs(ref push);
                    yield return null;
                    Assert.LessOrEqual(((Vector3)Offset.GetValue(rig)).magnitude, .0501f);
                }
                var idle = Input(Vector2.zero);
                player.Character.SetInputs(ref idle);
                float settleUntil = Time.realtimeSinceStartup + .6f;
                while (Time.realtimeSinceStartup < settleUntil) yield return null;
                Assert.Less(((Vector3)Offset.GetValue(rig)).magnitude, .001f);
            }
            finally
            {
                foreach (var car in bracedCars) if (car != null) Object.Destroy(car.gameObject);
                Object.Destroy(wall);
            }
            yield return null;
            player.Motor.SetPositionAndRotation(origin, Quaternion.identity);
            player.Motor.BaseVelocity = Vector3.zero;
            float restUntil = Time.realtimeSinceStartup + .6f;
            while (Time.realtimeSinceStartup < restUntil) yield return null;

            // Ordinary movement must regain exact horizontal follow after contact recovery.
            var walk = Input(Vector2.up);
            float walkingUntil = Time.realtimeSinceStartup + .25f;
            while (Time.realtimeSinceStartup < walkingUntil)
            {
                player.Character.SetInputs(ref walk);
                yield return null;
                Assert.AreEqual(Vector3.zero, (Vector3)Offset.GetValue(rig));
            }
            var stop = Input(Vector2.zero);
            player.Character.SetInputs(ref stop);
            float groundY = player.Motor.TransientPosition.y;
            var jump = Input(Vector2.zero);
            jump.JumpDown = true;
            player.Character.SetInputs(ref jump);
            float jumpingUntil = Time.realtimeSinceStartup + .15f;
            while (Time.realtimeSinceStartup < jumpingUntil) yield return null;
            player.Character.SetInputs(ref stop);
            Assert.Greater(player.Motor.TransientPosition.y, groundY + .05f, "Real jump movement was lost.");
            Assert.Less((rig.CameraPivot.localPosition - rig.RestLocalPosition).magnitude, .0001f);

            // Re-enabling and a teleport must discard both horizontal and vertical lag.
            Offset.SetValue(rig, new Vector3(.03f, 0f, .02f));
            rig.enabled = false;
            rig.enabled = true;
            Assert.Less(Vector3.Distance(rig.transform.position, pivot.position), .0001f);
            Offset.SetValue(rig, new Vector3(.03f, 0f, .02f));
            player.Motor.SetPosition(origin + Vector3.right * 2);
            yield return null;
            Follow.Invoke(rig, null);
            Assert.AreEqual(Vector3.zero, (Vector3)Offset.GetValue(rig));
            player.Look.SetLook(25, 15);
            player.Look.enabled = true;
            yield return null;
            Follow.Invoke(rig, null);
            Assert.Less(Quaternion.Angle(rig.transform.rotation, pivot.rotation), .01f);
            Time.timeScale = 0f;
            yield return null;
            Follow.Invoke(rig, null);
            Assert.False(float.IsNaN(rig.transform.position.x));
            Time.timeScale = 1f;
            yield return null;
            Follow.Invoke(rig, null);
            Assert.False(float.IsNaN(rig.transform.position.y), "Pause corrupted vertical smoothing velocity.");
            Debug.Log("CAR_CONTACT_CAMERA_PASSED: 18 live push scenarios at requested 30/60/120 FPS; maximum lag=" + maximumLag);
        }
        finally
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = oldFps;
            QualitySettings.vSyncCount = oldVsync;
        }
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EditorApplication.isPlaying)
        {
            Time.timeScale = 1f;
            if (Session != null) Session.suspendPersistenceForTests = true;
            yield return new ExitPlayMode();
        }
        SessionState.SetBool("TinyRiders.Compact.QA.Active", false);
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.position");
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.coins");
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
    }
}
