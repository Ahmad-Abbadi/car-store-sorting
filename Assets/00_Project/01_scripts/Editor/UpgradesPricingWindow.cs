using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary> One window to hand-edit every upgrade's levels, costs and values. </summary>
public class UpgradesPricingWindow : EditorWindow
{
    private const float RowLabelWidth = 52f;
    private const float CostWidth = 110f;

    private readonly List<SerializedObject> _upgrades = new List<SerializedObject>();
    private readonly Dictionary<Object, bool> _foldouts = new Dictionary<Object, bool>();
    private Vector2 _scroll;

    [MenuItem("Tools/Sorting Template/Upgrades Pricing")]
    private static void Open() => GetWindow<UpgradesPricingWindow>("Upgrades Pricing");

    private void OnEnable() => Refresh();

    private void OnProjectChange() => Refresh();

    /// <summary> Finds every concrete UpgradeSO asset in the project. </summary>
    private void Refresh()
    {
        _upgrades.Clear();
        IEnumerable<UpgradeSO> assets = TypeCache.GetTypesDerivedFrom<UpgradeSO>()
            .Where(t => !t.IsAbstract)
            .SelectMany(t => AssetDatabase.FindAssets("t:" + t.Name))
            .Distinct()
            .Select(guid => AssetDatabase.LoadAssetAtPath<UpgradeSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(a => a != null)
            .OrderBy(a => a.name);

        foreach (UpgradeSO asset in assets) _upgrades.Add(new SerializedObject(asset));
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_upgrades.Count == 0)
        {
            EditorGUILayout.HelpBox("No upgrade assets found. Create one via ScriptableObject/Upgrades.", MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (SerializedObject so in _upgrades)
        {
            if (so.targetObject == null) continue;
            so.Update();
            DrawUpgrade(so);
            so.ApplyModifiedProperties();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) Refresh();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) AssetDatabase.SaveAssets();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton)) SetAllFoldouts(true);
            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton)) SetAllFoldouts(false);
        }
    }

    private void SetAllFoldouts(bool open)
    {
        foreach (SerializedObject so in _upgrades) _foldouts[so.targetObject] = open;
    }

    /// <summary> Draws one upgrade: header, shop info, then the level table. </summary>
    private void DrawUpgrade(SerializedObject so)
    {
        UpgradeSO upgrade = (UpgradeSO)so.targetObject;
        SerializedProperty levels = so.FindProperty("levels");

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _foldouts.TryGetValue(upgrade, out bool open);
            using (new EditorGUILayout.HorizontalScope())
            {
                string title = string.IsNullOrEmpty(upgrade.DisplayName) ? upgrade.name : upgrade.DisplayName;
                open = EditorGUILayout.Foldout(open, $"{title}   ({levels.arraySize} levels)", true, EditorStyles.foldoutHeader);
                if (GUILayout.Button("Ping", GUILayout.Width(40))) EditorGUIUtility.PingObject(upgrade);
            }
            _foldouts[upgrade] = open;
            if (!open) return;

            EditorGUILayout.PropertyField(so.FindProperty("displayName"));
            EditorGUILayout.PropertyField(so.FindProperty("description"));
            EditorGUILayout.PropertyField(so.FindProperty("icon"));
            EditorGUILayout.PropertyField(so.FindProperty("saveId"));

            EditorGUILayout.Space(4);
            levels.arraySize = Mathf.Max(0, EditorGUILayout.DelayedIntField("Level Count", levels.arraySize));

            DrawValuesRow("Base", null, so.FindProperty("baseValues"));
            for (int i = 0; i < levels.arraySize; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                DrawValuesRow($"Lv {i + 1}", level.FindPropertyRelative("cost"), level.FindPropertyRelative("values"));
            }

            if (Application.isPlaying)
                EditorGUILayout.LabelField("Owned level", $"{upgrade.Level} / {upgrade.MaxLevel}");
        }
    }

    /// <summary> Draws one table row: label, cost (or blank), then each struct field inline. </summary>
    private static void DrawValuesRow(string label, SerializedProperty cost, SerializedProperty values)
    {
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(RowLabelWidth));

            if (cost != null)
            {
                EditorGUIUtility.labelWidth = 32f;
                EditorGUILayout.PropertyField(cost, new GUIContent("Cost"), GUILayout.Width(CostWidth));
            }
            else
            {
                GUILayout.Space(CostWidth + 2f); // keeps value columns aligned with level rows
            }

            SerializedProperty field = values.Copy();
            SerializedProperty end = values.GetEndProperty();
            bool enterChildren = true;
            while (field.NextVisible(enterChildren) && !SerializedProperty.EqualContents(field, end))
            {
                enterChildren = false;
                GUIContent content = new GUIContent(field.displayName, field.tooltip);
                EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(content).x + 6f;
                EditorGUILayout.PropertyField(field, content, GUILayout.MinWidth(EditorGUIUtility.labelWidth + 40f));
            }
        }
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
}
