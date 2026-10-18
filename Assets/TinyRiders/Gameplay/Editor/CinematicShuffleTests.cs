using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class CinematicShuffleTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(0), TestCase(1), TestCase(2), TestCase(6)]
    public void Rounds_VisitEveryView_WithoutAdjacentRepeats(int count)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Shuffle test");
        SceneManager.MoveGameObjectToScene(root,scene);
        var randomState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(71421);
            var tour = root.AddComponent<MenuCinematicTour>();
            var shots = new MenuCinematicTour.Shot[count];
            for (int i = 0; i < count; i++)
            {
                var camera = new GameObject("View " + i).AddComponent<CinemachineCamera>();
                camera.transform.SetParent(root.transform,false);
                var end = new GameObject("End " + i).transform; end.SetParent(root.transform,false);
                shots[i] = new MenuCinematicTour.Shot { camera=camera,start=camera.transform,end=end };
            }
            typeof(MenuCinematicTour).GetField("shots",Private).SetValue(tour,shots);
            var begin = typeof(MenuCinematicTour).GetMethod("OnEnable",Private);
            var advance = typeof(MenuCinematicTour).GetMethod("AdvanceShot",Private);
            begin.Invoke(tour,null);
            if (count == 0)
            {
                Assert.IsFalse((bool)typeof(MenuCinematicTour).GetField("running",Private).GetValue(tour));
                return;
            }
            var permutations = new HashSet<string>();
            for (int round = 0; round < 100; round++)
            {
                var views = new List<int>();
                for (int i = 0; i < count; i++)
                {
                    int previous = tour.CurrentShot;
                    views.Add(previous);
                    Assert.AreEqual(20,shots[previous].camera.Priority.Value);
                    Assert.AreEqual(1,shots.Count(s=>s.camera.Priority.Value==20));
                    advance.Invoke(tour,null);
                    if (count > 1) Assert.AreNotEqual(previous,tour.CurrentShot,"Adjacent views must differ, including round boundaries.");
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(0,count),views);
                Assert.AreEqual(round+1,tour.CompletedLoops);
                permutations.Add(string.Join(",",views));
            }
            if (count > 2) Assert.Greater(permutations.Count,1,"New rounds must vary their order.");
            var starts = new HashSet<int>();
            for (int i = 0; i < 50; i++)
            {
                begin.Invoke(tour,null);
                Assert.AreEqual(0,tour.CompletedLoops);
                starts.Add(tour.CurrentShot);
            }
            if (count > 1) Assert.Greater(starts.Count,1,"Reopening must select from more than the fixed first view.");
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
