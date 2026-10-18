using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEngine;

public class PileStartupRegressionTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private ThemedCollectionSession session;

    [SetUp] public void SetUp()
    {
        root = new GameObject("Pile startup regression");
        root.SetActive(false);
        session = root.AddComponent<ThemedCollectionSession>();
        session.pileOrigin = root.transform;
        session.visiblePileLimit = 1200;
        session.pileSpacing = new Vector3(.75f, .3f, .75f);
    }

    [TearDown] public void TearDown() => Object.DestroyImmediate(root);

    private Queue<Vector3> Positions(int count) => (Queue<Vector3>)typeof(ThemedCollectionSession)
        .GetMethod("FindFallbackPositions", Private).Invoke(session, new object[] { count });

    [Test] public void FullCollectionFallbackIsFastAndDoesNotOverlap()
    {
        var clock = Stopwatch.StartNew();
        Vector3[] positions = Positions(1200).ToArray();
        clock.Stop();
        Assert.AreEqual(1200, positions.Length);
        Assert.Less(clock.Elapsed.TotalSeconds, 2, "Fallback search must not block startup for minutes.");
        float clearance = session.pileSpacing.x * session.pileSpacing.x * .65f;
        for (int i = 0; i < positions.Length; i++)
            for (int j = 0; j < i; j++)
                Assert.GreaterOrEqual((positions[i] - positions[j]).sqrMagnitude, clearance);
    }

    [Test] public void FallbackReservesSavedCarsBeforeTheyAreInstantiated()
    {
        Vector3 reserved = Positions(1).Dequeue();
        var records = (Dictionary<string, ThemedInstanceRecord>)typeof(ThemedCollectionSession)
            .GetField("records", Private).GetValue(session);
        records.Add("saved", new ThemedInstanceRecord { id = "saved", state = "world", position = reserved });
        foreach (Vector3 position in Positions(10))
            Assert.GreaterOrEqual((position - reserved).sqrMagnitude, .75f * .75f * .65f);
    }

    [Test] public void FallbackIsDeterministicAndEmptyWhenNoCarsAreNeeded()
    {
        CollectionAssert.AreEqual(Positions(20).ToArray(), Positions(20).ToArray());
        Assert.IsEmpty(Positions(0));
    }

    [Test] public void AuthoredPoseMatchesDesignAndCopyAcrossSaveSeeds()
    {
        var pose = new ThemedInstanceRecord { id = "TRWORLD-73481:TR-C001-TH_ALIEN-V01:0001", position = new Vector3(2, 1, 3) };
        var copies = (Dictionary<string, ThemedInstanceRecord>)typeof(ThemedCollectionSession)
            .GetField("bakedCopies", Private).GetValue(session);
        copies.Add("TR-C001-TH_ALIEN-V01:0001", pose);
        var method = typeof(ThemedCollectionSession).GetMethod("TryGetBakedPose", Private);
        object[] args = { "TRWORLD-0:TR-C001-TH_ALIEN-V01:0001", null };
        Assert.IsTrue((bool)method.Invoke(session, args));
        Assert.AreSame(pose, args[1]);
        args = new object[] { "TRWORLD-0:TR-C001-TH_ALIEN-V01:0002", null };
        Assert.IsFalse((bool)method.Invoke(session, args), "Different copies must retain different poses.");
    }
}
