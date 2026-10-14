using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary> Test window: enable or disable each upgrade and set its owned level. </summary>
public class UpgradesTestWindow : EditorWindow
{
    private readonly List<UpgradeSO> _upgrades = new List<UpgradeSO>();
    private readonly Dictionary<UpgradeSO, int> _lastLevel = new Dictionary<UpgradeSO, int>();
    private Vector2 _scroll;

    [MenuItem("Tools/Sorting Template/Upgrades Test")]
    private static void Open() => GetWindow<UpgradesTestWindow>("Upgrades Test");

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Refresh();
    }

    private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

    private void OnProjectChange() => Refresh();

    private void OnPlayModeStateChanged(PlayModeStateChange _) => Refresh();

    private void OnFocus() => Refresh();

    /// <summary> Uses the scene's UpgradeManager when found, else every upgrade asset. </summary>
    private void Refresh()
    {
        _upgrades.Clear();

        UpgradeManager manager = FindFirstObjectByType<UpgradeManager>();
        if (manager != null && manager.Upgrades.Count > 0)
            _upgrades.AddRange(manager.Upgrades);
        else
            _upgrades.AddRange(TypeCache.GetTypesDerivedFrom<UpgradeSO>()
                .Where(t => !t.IsAbstract)
                .SelectMany(t => AssetDatabase.FindAssets("t:" + t.Name))
                .Distinct()
                .Select(guid => AssetDatabase.LoadAssetAtPath<UpgradeSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null)
                .OrderBy(a => a.name));

        Repaint();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) Refresh();
            if (GUILayout.Button("All Max", EditorStyles.toolbarButton, GUILayout.Width(60)))
                foreach (UpgradeSO u in _upgrades) u.SetLevel(u.MaxLevel);
            if (GUILayout.Button("All Off", EditorStyles.toolbarButton, GUILayout.Width(60)))
                foreach (UpgradeSO u in _upgrades) u.SetLevel(0);
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.HelpBox("Changes are saved to PlayerPrefs. Off = level 0.", MessageType.None);

        if (_upgrades.Count == 0)
        {
            EditorGUILayout.HelpBox("No upgrades found.", MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (UpgradeSO upgrade in _upgrades)
            if (upgrade != null) DrawUpgrade(upgrade);
        EditorGUILayout.EndScrollView();
    }

    /// <summary> One row: enable toggle, name and level slider. </summary>
    private void DrawUpgrade(UpgradeSO upgrade)
    {
        int level = upgrade.Level;
        bool enabled = level > 0;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            bool nowEnabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(18));
            if (nowEnabled != enabled)
            {
                // Turning on restores the last level, or level 1.
                if (!nowEnabled) _lastLevel[upgrade] = level;
                int restore = _lastLevel.TryGetValue(upgrade, out int last) && last > 0 ? last : 1;
                upgrade.SetLevel(nowEnabled ? restore : 0);
                level = upgrade.Level;
            }

            string label = string.IsNullOrEmpty(upgrade.DisplayName) ? upgrade.name : upgrade.DisplayName;
            EditorGUILayout.LabelField(label, GUILayout.Width(150));

            using (new EditorGUI.DisabledScope(upgrade.MaxLevel <= 0))
            {
                int newLevel = EditorGUILayout.IntSlider(level, 0, Mathf.Max(1, upgrade.MaxLevel));
                if (newLevel != level) upgrade.SetLevel(newLevel);
            }
        }
    }
}
