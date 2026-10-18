using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace ToyShop.UI.Editor
{
    public sealed class ToyShopUITests
    {
        private static int clicks;
        private static void CountClick() => clicks++;
        [Test]
        public void ThemeAndPrefabs_KeepLocalizationAndButtonCallbacks()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ToyShopTheme>(ToyShopUIBuilder.ThemePath);
            Assert.IsNotNull(theme);
            Assert.IsNotNull(theme.rounded);
            Assert.IsTrue(theme.font.fallbackFontAssetTable.Any(f => f.name.Contains("Bhaijaan")));
            foreach (string path in new[] { "Assets/00_Project/02_prefabs/UI/Pause Panel Canvas.prefab",
                "Assets/00_Project/02_prefabs/UI/Upgrades_UI/Upgrade Panel Canvas.prefab",
                "Assets/00_Project/03_scriptableObjects/Resources/LanguagePicker.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var b in prefab.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    Assert.AreEqual(1, b.GetComponents<ToyShopButtonFeedback>().Length, path + "/" + b.name);
                Assert.IsNotEmpty(prefab.GetComponentsInChildren<ToyShopPanelEntrance>(true));
            }
        }

        [UnityTest]
        public IEnumerator Motion_WorksPausedAndRestoresBaselinesAfterRapidToggles()
        {
            SessionState.SetString("ToyShopUI.TestScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            Time.timeScale = 0;
            var panel = new GameObject("Panel test", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)panel.transform;
            rect.anchoredPosition = new Vector2(40, 70);
            var group = panel.GetComponent<CanvasGroup>();
            group.alpha = .8f;
            group.blocksRaycasts = false;
            panel.AddComponent<ToyShopPanelEntrance>();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(group.alpha, Is.EqualTo(.8f).Within(.001));
            Assert.That(Vector2.Distance(rect.anchoredPosition, new Vector2(40, 70)), Is.LessThan(.001));
            Assert.IsFalse(group.blocksRaycasts);
            for (int i = 0; i < 20; i++)
            {
                panel.SetActive(false);
                panel.SetActive(true);
                yield return null;
            }
            panel.SetActive(false);
            Assert.That(group.alpha, Is.EqualTo(.8f).Within(.001));
            Assert.AreEqual(new Vector2(40, 70), rect.anchoredPosition);

            var buttonObject = new GameObject("Button test", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            buttonObject.transform.localScale = new Vector3(.8f, .8f, 1);
            var feedback = buttonObject.AddComponent<ToyShopButtonFeedback>();
            clicks = 0;
            button.onClick.AddListener(CountClick);
            feedback.OnPointerEnter(null);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.Greater(buttonObject.transform.localScale.x, .8f);
            feedback.OnPointerExit(null);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(buttonObject.transform.localScale.x, Is.EqualTo(.8f).Within(.001));
            feedback.OnSubmit(null);
            button.onClick.Invoke();
            Assert.AreEqual(1, clicks, "Presentation must not invoke gameplay callbacks.");
            buttonObject.SetActive(false);
            Assert.AreEqual(new Vector3(.8f, .8f, 1), buttonObject.transform.localScale);
            Object.Destroy(panel);
            Object.Destroy(buttonObject);
            Time.timeScale = 1;
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (EditorApplication.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
            string scene = SessionState.GetString("ToyShopUI.TestScene", "");
            if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene);
            SessionState.EraseString("ToyShopUI.TestScene");
        }
    }
}
