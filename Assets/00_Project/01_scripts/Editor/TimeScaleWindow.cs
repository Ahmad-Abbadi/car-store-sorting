using UnityEngine;
using UnityEditor;

/// <summary>
/// Tiny testing utility: drives <see cref="Time.timeScale"/> from a slider so you can slow-mo or
/// fast-forward play mode while balancing. Handy presets, a reset to 1x, and it snaps back to 1x
/// automatically when you exit play mode so a leftover slow/fast scale never leaks between sessions.
/// Window ▸ RoomClean ▸ Time Scale.
/// </summary>
public class TimeScaleWindow : EditorWindow
{
    private const float MinScale = 0f;
    private const float MaxScale = 8f;

    [MenuItem("Window/RoomClean/Time Scale")]
    private static void Open()
    {
        var win = GetWindow<TimeScaleWindow>("Time Scale");
        win.minSize = new Vector2(260f, 120f);
        win.Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    // Repaint live so the slider reflects timeScale even if something else changes it.
    private void OnInspectorUpdate() => Repaint();

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Never leave a modified scale behind when leaving play mode.
        if (state == PlayModeStateChange.EnteredEditMode)
            Time.timeScale = 1f;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Time Scale", EditorStyles.boldLabel);

        float current = Time.timeScale;

        EditorGUI.BeginChangeCheck();
        float slider = EditorGUILayout.Slider(current, MinScale, MaxScale);
        if (EditorGUI.EndChangeCheck())
            Time.timeScale = slider;

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause (0x)")) Time.timeScale = 0f;
        if (GUILayout.Button("0.25x")) Time.timeScale = 0.25f;
        if (GUILayout.Button("0.5x")) Time.timeScale = 0.5f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset (1x)")) Time.timeScale = 1f;
        if (GUILayout.Button("1.25x")) Time.timeScale = 1.25f;
        if (GUILayout.Button("1.5x")) Time.timeScale = 1.5f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("2x")) Time.timeScale = 2f;
        if (GUILayout.Button("4x")) Time.timeScale = 4f;
        if (GUILayout.Button("8x")) Time.timeScale = 8f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            $"Current: {Time.timeScale:0.00}x" +
            (Application.isPlaying ? "" : "  (set it now; snaps to 1x when play mode ends)"),
            MessageType.None);
    }
}
