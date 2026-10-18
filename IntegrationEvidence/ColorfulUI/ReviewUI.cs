using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReviewUI
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    const string Prefix = "ToyShopUI.Review.";
    static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);

    public static string Show(string screen, int width, int height, string language = "en")
    {
        if (!SessionState.GetBool(Prefix + "saved", false))
        {
            SessionState.SetBool(Prefix + "saved", true);
            SessionState.SetBool(Prefix + "hadLanguage", PlayerPrefs.HasKey("Language"));
            SessionState.SetString(Prefix + "language", Localizer.CurrentCode);
            SessionState.SetBool(Prefix + "background", Application.runInBackground);
        }
        Application.runInBackground = true;
        foreach (var root in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(g => g.name == "UI Review" || g.name == "Car Store HUD").ToArray()) Object.DestroyImmediate(root);
        if (LanguagePickerUI.IsOpen) Object.FindFirstObjectByType<LanguagePickerUI>().Close();
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (canvas.name == "Main Menu Canvas") canvas.enabled = screen == "menu";
        Localizer.SetLanguage(language);
        CinematicMenuTests.SetGameSize(width, height);
        var view = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Focus(); view.Repaint();
        if (screen == "pause")
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/00_Project/02_prefabs/UI/Pause Panel Canvas.prefab");
            var panel = Object.Instantiate(prefab); panel.name = "UI Review"; panel.SetActive(true);
            Invoke(panel.GetComponent<PauseMenu>(), "SetOpen", true);
        }
        if (screen == "language") LanguagePickerUI.Show(true);
        if (screen == "tutorial" || screen == "completion" || screen == "directory")
        {
            var controller = new GameObject("UI Review").AddComponent<CarStoreUI>();
            controller.enabled = false;
            Invoke(controller, "BuildUI");
            Invoke(controller, "Refresh");
            var field = screen == "tutorial" ? "tutorial" : "finish";
            if (screen != "directory") ((GameObject)typeof(CarStoreUI).GetField(field, Private).GetValue(controller)).SetActive(true);
        }
        return screen + " " + width + "x" + height + " " + language;
    }

    public static object Inspect()
    {
        Canvas.ForceUpdateCanvases();
        var outside = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Where(b=>b.isActiveAndEnabled && b.GetComponentInParent<Canvas>() && b.GetComponentInParent<Canvas>().enabled).Where(b=>
            {
                var corners = new Vector3[4]; ((RectTransform)b.transform).GetWorldCorners(corners);
                return corners.Any(p=>p.x < -1 || p.x > Screen.width+1 || p.y < -1 || p.y > Screen.height+1);
            }).Select(b=>b.name).ToArray();
        var overflow = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled && t.GetComponentInParent<Canvas>() && t.GetComponentInParent<Canvas>().enabled)
            .Where(t=> { t.ForceMeshUpdate(); return t.isTextOverflowing; }).Select(t=>new {t.name,t.text}).ToArray();
        return new { width=Screen.width,height=Screen.height,outside,overflow };
    }

    public static void Restore()
    {
        if (SessionState.GetBool(Prefix + "saved", false))
        {
            Localizer.SetLanguage(SessionState.GetString(Prefix + "language", "en"));
            if (!SessionState.GetBool(Prefix + "hadLanguage", false)) PlayerPrefs.DeleteKey("Language");
            Application.runInBackground = SessionState.GetBool(Prefix + "background", false);
            SessionState.EraseBool(Prefix + "saved");
        }
        if (SessionState.GetBool("CinematicMenu.SizeSaved", false))
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var viewType = assembly.GetType("UnityEditor.GameView");
            viewType.GetProperty("selectedSizeIndex", Private|BindingFlags.Public).SetValue(EditorWindow.GetWindow(viewType), SessionState.GetInt("CinematicMenu.PreviousSize",0));
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
            int count=(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
            for (int i=count-1;i>=SessionState.GetInt("CinematicMenu.PreviousCustomCount",count);i--)
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});
            SessionState.SetBool("CinematicMenu.SizeSaved",false);
        }
    }
}
