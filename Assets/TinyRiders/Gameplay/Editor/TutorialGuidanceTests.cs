using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class TutorialGuidanceTests
{
    [TestCase(1920, 1080)]
    [TestCase(1080, 1920)]
    [TestCase(2560, 1080)]
    public void ScreenGuidance_StaysInsideSafeBoundsAndPointsToOffscreenTargets(int width, int height)
    {
        var safe = new Rect(64, 100, width - 128, height - 320);
        var visible = TutorialGuidancePresenter.CalculateLayout(new Vector3(safe.center.x, safe.center.y, 3), safe);
        Assert.IsFalse(visible.atEdge);
        Assert.AreEqual(180, visible.rotation);
        foreach (var point in new[] {
            new Vector3(-width, height / 2, 2), new Vector3(width * 2, height / 2, 2),
            new Vector3(width / 2, height * 2, 2), new Vector3(width / 2, -height, 2),
            new Vector3(safe.center.x, safe.center.y, -3), new Vector3(-width, height / 2, -3) })
        {
            var edge = TutorialGuidancePresenter.CalculateLayout(point, safe);
            Assert.IsTrue(edge.atEdge);
            Assert.IsFalse(float.IsNaN(edge.position.x) || float.IsNaN(edge.position.y) || float.IsNaN(edge.rotation));
            Assert.That(edge.position.x, Is.InRange(safe.xMin - .01f, safe.xMax + .01f));
            Assert.That(edge.position.y, Is.InRange(safe.yMin - .01f, safe.yMax + .01f));
            Assert.IsTrue(Mathf.Abs(edge.position.x - safe.xMin) < .01f || Mathf.Abs(edge.position.x - safe.xMax) < .01f ||
                Mathf.Abs(edge.position.y - safe.yMin) < .01f || Mathf.Abs(edge.position.y - safe.yMax) < .01f);
        }
        var behind = TutorialGuidancePresenter.CalculateLayout(new Vector3(-width, safe.center.y, -1), safe);
        Assert.AreEqual(safe.xMax, behind.position.x, .01f, "A behind-camera projection must not reverse the turn direction.");
    }

    [Test]
    public void AuthoredGuidance_RendersMouseAndTouchIconsAndNeverBlocksControls()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(TutorialBuilder.ScenePath);
            var c = Object.FindFirstObjectByType<TutorialController>();
            var g = c.guidance;
            Assert.IsNotNull(g);
            Assert.IsNull(GameObject.Find("Gentle target glow"));
            Assert.IsFalse(g.targetArrow.raycastTarget);
            Assert.IsFalse(g.edgeArrow.raycastTarget);
            Assert.IsNotNull(g.targetArrow.sprite);
            Assert.AreSame(g.targetArrow.sprite, g.edgeArrow.sprite);
            Assert.IsTrue(c.controlHint.richText);
            Assert.AreSame(g.mouseIcons, c.controlHint.spriteAsset);
            foreach (var icon in new[] { "LMB", "RMB", "Interact", "Place" })
            {
                var asset = icon == "LMB" || icon == "RMB" ? g.mouseIcons : g.touchIcons;
                Assert.IsNotNull(asset);
                c.controlHint.spriteAsset = asset;
                c.controlHint.text = TutorialGuidancePresenter.ComposeHint("Pick up / place", asset, icon);
                c.controlHint.ForceMeshUpdate(true, true);
                Assert.AreEqual(1, c.controlHint.textInfo.characterInfo.Take(c.controlHint.textInfo.characterCount)
                    .Count(ch => ch.elementType == TMP_TextElementType.Sprite && ch.isVisible), icon + " must render as a visible sprite.");
                Assert.IsFalse(c.controlHint.textInfo.characterInfo.Take(c.controlHint.textInfo.characterCount)
                    .Any(ch => ch.character == '<'), "Literal markup must not appear in the rendered label.");
            }
            Assert.AreEqual("Right-click to place", TutorialGuidancePresenter.ComposeHint("Right-click to place", null, "RMB"));
            Assert.AreEqual("Take back", TutorialGuidancePresenter.ComposeHint("Take back", g.mouseIcons, "missing"));
        }
        finally
        {
            if (setup.Any(s => s.isLoaded) && setup.Count(s => s.isActive) == 1 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.OpenScene(TutorialBuilder.ScenePath);
        }
    }
}
