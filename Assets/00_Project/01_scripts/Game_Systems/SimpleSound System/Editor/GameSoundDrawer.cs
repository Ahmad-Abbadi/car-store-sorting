using UnityEditor;
using UnityEngine;

/// <summary> One-line GameSound field: clip, settings summary, and an edit button. </summary>
[CustomPropertyDrawer(typeof(GameSound))]
public class GameSoundDrawer : PropertyDrawer
{
    private const float ButtonWidth = 24f;
    private const float SummaryWidth = 150f;
    private const float Gap = 3f;

    private static GUIContent _editIcon;

    private static GUIContent EditIcon
    {
        get
        {
            if (_editIcon == null)
            {
                _editIcon = new GUIContent(EditorGUIUtility.IconContent("editicon.sml"));
                _editIcon.tooltip = "Edit sound: pitch, volume and cutoff";
            }
            return _editIcon;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect field = EditorGUI.PrefixLabel(position, label);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        SerializedProperty clip = property.FindPropertyRelative(nameof(GameSound.Clip));
        bool showSummary = field.width > 300f && clip.objectReferenceValue != null;

        Rect button = new Rect(field.xMax - ButtonWidth, field.y, ButtonWidth, field.height);
        float clipWidth = field.width - ButtonWidth - Gap - (showSummary ? SummaryWidth + Gap : 0f);
        Rect clipRect = new Rect(field.x, field.y, clipWidth, field.height);

        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(clipRect, clip, GUIContent.none);
        if (EditorGUI.EndChangeCheck()) ApplyClipDefaults(property);

        if (showSummary)
        {
            Rect summary = new Rect(clipRect.xMax + Gap, field.y, SummaryWidth, field.height);
            EditorGUI.LabelField(summary, Summary(property), EditorStyles.miniLabel);
        }

        if (GUI.Button(button, EditIcon, EditorStyles.iconButton))
            GameSoundEditorWindow.Open(property);

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    /// <summary> Short "volume · pitch · start–end" text for the inline field. </summary>
    private static string Summary(SerializedProperty property)
    {
        float volume = property.FindPropertyRelative(nameof(GameSound.Volume)).floatValue;
        float pitch = property.FindPropertyRelative(nameof(GameSound.Pitch)).floatValue;
        GameSoundEditorWindow.GetEffectiveCutoff(property, out float start, out float end);
        return $"v{volume:0.##} · p{pitch:0.##} · {start:0.00}–{end:0.00}s";
    }

    /// <summary> Fresh clip: full-length cutoff, and 1 for unset pitch or volume. </summary>
    public static void ApplyClipDefaults(SerializedProperty property)
    {
        SerializedProperty pitch = property.FindPropertyRelative(nameof(GameSound.Pitch));
        SerializedProperty volume = property.FindPropertyRelative(nameof(GameSound.Volume));
        SerializedProperty cutoff = property.FindPropertyRelative(nameof(GameSound.Cutoff));
        AudioClip clip = property.FindPropertyRelative(nameof(GameSound.Clip)).objectReferenceValue as AudioClip;

        // Zero pitch means a never-initialised struct, so volume is unset too.
        if (pitch.floatValue <= 0f)
        {
            pitch.floatValue = 1f;
            volume.floatValue = 1f;
        }

        cutoff.vector2Value = new Vector2(0f, clip != null ? clip.length : 0f);
    }
}
