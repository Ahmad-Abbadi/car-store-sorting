using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary> Edits one GameSound: settings, waveform with draggable cutoff lines, and preview. </summary>
public class GameSoundEditorWindow : EditorWindow
{
    private const float WaveHeight = 150f;
    private const float RulerHeight = 16f;
    private const float GrabDistance = 7f;
    private const float MinCutLength = 0.01f;

    private static readonly Color WaveBackground = new Color(0.13f, 0.13f, 0.13f);
    private static readonly Color WaveColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color CutShade = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color StartColor = new Color(0.3f, 0.9f, 0.45f);
    private static readonly Color EndColor = new Color(1f, 0.35f, 0.35f);
    private static readonly Color PlayheadColor = new Color(1f, 1f, 1f, 0.85f);

    // Survive domain reloads so the window keeps its target.
    [SerializeField] private Object _target;
    [SerializeField] private string _path;

    private SerializedObject _serialized;
    private int _dragLine = -1; // 0 = start, 1 = end, -1 = none
    private int _dragUndoGroup;

    private AudioSource _preview;
    private float _previewEnd;

    private AudioClip _peakClip;
    private float[] _peakMin;
    private float[] _peakMax;

    /// <summary> Opens (or retargets) the editor window on a GameSound property. </summary>
    public static void Open(SerializedProperty property)
    {
        GameSoundEditorWindow window = GetWindow<GameSoundEditorWindow>(false, "Game Sound", true);
        window.minSize = new Vector2(420f, 330f);
        window.StopPreview();
        window._target = property.serializedObject.targetObject;
        window._path = property.propertyPath;
        window._serialized = null;
        window.Show();
    }

    /// <summary> Cutoff with an unset or invalid end shown as the clip's end. </summary>
    public static void GetEffectiveCutoff(SerializedProperty property, out float start, out float end)
    {
        AudioClip clip = property.FindPropertyRelative(nameof(GameSound.Clip)).objectReferenceValue as AudioClip;
        Vector2 cutoff = property.FindPropertyRelative(nameof(GameSound.Cutoff)).vector2Value;
        float length = clip != null ? clip.length : 0f;

        start = Mathf.Clamp(cutoff.x, 0f, length);
        end = cutoff.y <= start || cutoff.y > length ? length : cutoff.y;
    }

    private void OnEnable() => EditorApplication.update += OnEditorUpdate;

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopPreview();
        if (_preview != null) DestroyImmediate(_preview.gameObject);
    }

    private void OnGUI()
    {
        SerializedProperty property = ResolveProperty();
        if (property == null)
        {
            EditorGUILayout.HelpBox("Nothing to edit. Press the edit icon next to a GameSound field.", MessageType.Info);
            return;
        }

        _serialized.Update();
        DrawHeader(property);

        SerializedProperty clipProp = property.FindPropertyRelative(nameof(GameSound.Clip));
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(clipProp);
        if (EditorGUI.EndChangeCheck())
        {
            StopPreview();
            GameSoundDrawer.ApplyClipDefaults(property);
        }

        EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(GameSound.Volume)));
        EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(GameSound.Pitch)));

        AudioClip clip = clipProp.objectReferenceValue as AudioClip;
        if (clip == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Assign a clip to edit its cutoff points.", MessageType.None);
            ApplyChanges();
            return;
        }

        EditorGUILayout.Space();
        DrawCutoffFields(property, clip);
        EditorGUILayout.Space(4f);
        DrawWaveform(property, clip);
        EditorGUILayout.Space(4f);
        DrawPreviewBar(property, clip);

        ApplyChanges();
    }

    /// <summary> Re-finds the target property; null when the target is gone. </summary>
    private SerializedProperty ResolveProperty()
    {
        if (_target == null || string.IsNullOrEmpty(_path)) return null;
        if (_serialized == null || _serialized.targetObject != _target) _serialized = new SerializedObject(_target);
        return _serialized.FindProperty(_path);
    }

    private void ApplyChanges()
    {
        if (_serialized.ApplyModifiedProperties()) InternalEditorUtility.RepaintAllViews();
    }

    private void DrawHeader(SerializedProperty property)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label($"{_target.name}  ›  {property.displayName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select owner", EditorStyles.toolbarButton)) EditorGUIUtility.PingObject(_target);
        }
        EditorGUILayout.Space(2f);
    }

    // ---- Cutoff ----------------------------------------------------------

    private void DrawCutoffFields(SerializedProperty property, AudioClip clip)
    {
        GetEffectiveCutoff(property, out float start, out float end);
        float length = clip.length;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.MinMaxSlider(new GUIContent("Cutoff", "Start and end points in seconds."), ref start, ref end, 0f, length);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUIUtility.labelWidth = 40f;
            start = EditorGUILayout.FloatField("Start", start);
            end = EditorGUILayout.FloatField("End", end);
            EditorGUIUtility.labelWidth = 0f;

            if (GUILayout.Button("Full", GUILayout.Width(44f)))
            {
                start = 0f;
                end = length;
            }
        }

        if (EditorGUI.EndChangeCheck()) SetCutoff(property, start, end, length);

        float kept = end - start;
        EditorGUILayout.LabelField(" ", $"Plays {kept:0.000}s of {length:0.000}s  ({kept / Mathf.Max(0.0001f, property.FindPropertyRelative(nameof(GameSound.Pitch)).floatValue):0.000}s at this pitch)", EditorStyles.miniLabel);
    }

    /// <summary> Writes a clamped cutoff with a small minimum length. </summary>
    private static void SetCutoff(SerializedProperty property, float start, float end, float length)
    {
        start = Mathf.Clamp(start, 0f, Mathf.Max(0f, length - MinCutLength));
        end = Mathf.Clamp(end, start + MinCutLength, length);
        property.FindPropertyRelative(nameof(GameSound.Cutoff)).vector2Value = new Vector2(start, end);
    }

    // ---- Waveform --------------------------------------------------------

    private void DrawWaveform(SerializedProperty property, AudioClip clip)
    {
        Rect area = GUILayoutUtility.GetRect(10f, WaveHeight + RulerHeight, GUILayout.ExpandWidth(true));
        Rect wave = new Rect(area.x, area.y, area.width, WaveHeight);
        Rect ruler = new Rect(area.x, wave.yMax, area.width, RulerHeight);

        GetEffectiveCutoff(property, out float start, out float end);
        float length = clip.length;
        float startX = wave.x + wave.width * (start / length);
        float endX = wave.x + wave.width * (end / length);

        HandleCutoffDrag(property, wave, length, startX, endX);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(wave, WaveBackground);
            DrawPeaks(wave, clip);

            // Dim the parts that are cut away.
            EditorGUI.DrawRect(new Rect(wave.x, wave.y, startX - wave.x, wave.height), CutShade);
            EditorGUI.DrawRect(new Rect(endX, wave.y, wave.xMax - endX, wave.height), CutShade);

            DrawCutLine(wave, startX, StartColor, $"{start:0.00}s", true);
            DrawCutLine(wave, endX, EndColor, $"{end:0.00}s", false);

            if (_preview != null && _preview.isPlaying && _preview.clip == clip)
            {
                float playX = wave.x + wave.width * (_preview.time / length);
                EditorGUI.DrawRect(new Rect(playX, wave.y, 1f, wave.height), PlayheadColor);
            }

            DrawRuler(ruler, length);
        }
    }

    /// <summary> Drags the nearest cutoff line; one undo step per drag. </summary>
    private void HandleCutoffDrag(SerializedProperty property, Rect wave, float length, float startX, float endX)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;

        EditorGUIUtility.AddCursorRect(new Rect(startX - GrabDistance, wave.y, GrabDistance * 2f, wave.height), MouseCursor.ResizeHorizontal);
        EditorGUIUtility.AddCursorRect(new Rect(endX - GrabDistance, wave.y, GrabDistance * 2f, wave.height), MouseCursor.ResizeHorizontal);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button != 0 || !wave.Contains(e.mousePosition)) break;

                float toStart = Mathf.Abs(e.mousePosition.x - startX);
                float toEnd = Mathf.Abs(e.mousePosition.x - endX);
                if (Mathf.Min(toStart, toEnd) > GrabDistance) break;

                _dragLine = toStart <= toEnd ? 0 : 1;
                Undo.IncrementCurrentGroup();
                _dragUndoGroup = Undo.GetCurrentGroup();
                GUIUtility.hotControl = id;
                e.Use();
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != id) break;

                float time = Mathf.Clamp01((e.mousePosition.x - wave.x) / wave.width) * length;
                GetEffectiveCutoff(property, out float start, out float end);
                if (_dragLine == 0) start = Mathf.Min(time, end - MinCutLength);
                else end = Mathf.Max(time, start + MinCutLength);
                SetCutoff(property, start, end, length);
                e.Use();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != id) break;

                GUIUtility.hotControl = 0;
                _dragLine = -1;
                Undo.CollapseUndoOperations(_dragUndoGroup);
                e.Use();
                break;
        }
    }

    private static void DrawCutLine(Rect wave, float x, Color color, string label, bool labelRight)
    {
        EditorGUI.DrawRect(new Rect(x - 1f, wave.y, 2f, wave.height), color);
        EditorGUI.DrawRect(new Rect(x - 5f, wave.y, 10f, 6f), color); // grab tab

        GUIStyle style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
        Vector2 size = style.CalcSize(new GUIContent(label));
        float lx = labelRight ? x + 4f : x - size.x - 4f;
        GUI.Label(new Rect(lx, wave.y + 6f, size.x, size.y), label, style);
    }

    private static void DrawRuler(Rect ruler, float length)
    {
        GUIStyle left = EditorStyles.miniLabel;
        GUIStyle center = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter };
        GUIStyle right = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperRight };

        GUI.Label(ruler, "0s", left);
        GUI.Label(ruler, $"{length * 0.5f:0.00}s", center);
        GUI.Label(ruler, $"{length:0.00}s", right);
    }

    /// <summary> Draws one min/max bar per pixel column from cached peaks. </summary>
    private void DrawPeaks(Rect wave, AudioClip clip)
    {
        if (!EnsurePeaks(clip))
        {
            GUI.Label(wave, "Waveform unavailable for this clip's import settings.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        int columns = Mathf.Max(1, Mathf.FloorToInt(wave.width));
        int buckets = _peakMin.Length;
        float mid = wave.y + wave.height * 0.5f;
        float half = wave.height * 0.5f - 2f;

        for (int c = 0; c < columns; c++)
        {
            int from = c * buckets / columns;
            int to = Mathf.Max(from + 1, (c + 1) * buckets / columns);

            float min = 0f, max = 0f;
            for (int b = from; b < to && b < buckets; b++)
            {
                if (_peakMin[b] < min) min = _peakMin[b];
                if (_peakMax[b] > max) max = _peakMax[b];
            }

            float top = mid - max * half;
            float bottom = mid - min * half;
            EditorGUI.DrawRect(new Rect(wave.x + c, top, 1f, Mathf.Max(1f, bottom - top)), WaveColor);
        }
    }

    // ---- Peak data -------------------------------------------------------

    /// <summary> Builds min/max peaks for the clip once; false when no data. </summary>
    private bool EnsurePeaks(AudioClip clip)
    {
        if (_peakClip == clip && _peakMin != null) return true;

        _peakClip = clip;
        _peakMin = _peakMax = null;
        return TryPeaksFromSamples(clip) || TryPeaksFromImporter(clip);
    }

    /// <summary> Peaks from raw samples; works for decompress-on-load clips. </summary>
    private bool TryPeaksFromSamples(AudioClip clip)
    {
        if (clip.loadType == AudioClipLoadType.Streaming) return false;
        if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();

        int channels = Mathf.Max(1, clip.channels);
        float[] data = new float[clip.samples * channels];
        if (!clip.GetData(data, 0)) return false;

        int frames = clip.samples;
        int buckets = Mathf.Min(2048, frames);
        _peakMin = new float[buckets];
        _peakMax = new float[buckets];

        for (int b = 0; b < buckets; b++)
        {
            int from = (int)((long)b * frames / buckets) * channels;
            int to = (int)((long)(b + 1) * frames / buckets) * channels;
            float min = 0f, max = 0f;
            for (int i = from; i < to; i++)
            {
                float v = data[i];
                if (v < min) min = v;
                if (v > max) max = v;
            }
            _peakMin[b] = min;
            _peakMax[b] = max;
        }
        return true;
    }

    /// <summary> Fallback: Unity's own import-time min/max data, via reflection. </summary>
    private bool TryPeaksFromImporter(AudioClip clip)
    {
        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
        var method = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod("GetMinMaxData",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (importer == null || method == null) return false;

        if (!(method.Invoke(null, new object[] { importer }) is float[] data) || data.Length == 0) return false;

        // Layout: per point, per channel, a min/max pair.
        int channels = Mathf.Max(1, clip.channels);
        int points = data.Length / (2 * channels);
        _peakMin = new float[points];
        _peakMax = new float[points];

        for (int p = 0; p < points; p++)
        {
            float min = 0f, max = 0f;
            for (int ch = 0; ch < channels; ch++)
            {
                int i = 2 * (p * channels + ch);
                min = Mathf.Min(min, data[i]);
                max = Mathf.Max(max, data[i + 1]);
            }
            _peakMin[p] = min;
            _peakMax[p] = max;
        }
        return points > 0;
    }

    // ---- Preview ---------------------------------------------------------

    private void DrawPreviewBar(SerializedProperty property, AudioClip clip)
    {
        bool playing = _preview != null && _preview.isPlaying;

        using (new EditorGUILayout.HorizontalScope())
        {
            GUIContent icon = EditorGUIUtility.IconContent(playing ? "PreMatQuad" : "PlayButton");
            GUIContent content = new GUIContent(playing ? " Stop" : " Preview", icon.image);
            if (GUILayout.Button(content, GUILayout.Width(90f), GUILayout.Height(22f)))
            {
                if (playing) StopPreview();
                else StartPreview(property, clip);
            }

            GUILayout.Label("Plays with this pitch, volume and cutoff.", EditorStyles.miniLabel);
        }
    }

    private void StartPreview(SerializedProperty property, AudioClip clip)
    {
        if (_preview == null)
        {
            GameObject go = EditorUtility.CreateGameObjectWithHideFlags("GameSoundPreview", HideFlags.HideAndDontSave, typeof(AudioSource));
            _preview = go.GetComponent<AudioSource>();
            _preview.playOnAwake = false;
            _preview.spatialBlend = 0f;
        }

        GetEffectiveCutoff(property, out float start, out float end);
        float pitch = property.FindPropertyRelative(nameof(GameSound.Pitch)).floatValue;

        _preview.clip = clip;
        _preview.volume = property.FindPropertyRelative(nameof(GameSound.Volume)).floatValue;
        _preview.pitch = pitch > 0.01f ? pitch : 1f;
        _preview.timeSamples = Mathf.Min((int)(start * clip.frequency), clip.samples - 1);
        _previewEnd = end;
        _preview.Play();
    }

    private void StopPreview()
    {
        if (_preview != null && _preview.isPlaying) _preview.Stop();
    }

    /// <summary> Stops preview at the end cutoff and animates the playhead. </summary>
    private void OnEditorUpdate()
    {
        if (_preview == null || !_preview.isPlaying) return;

        if (_preview.time >= _previewEnd) _preview.Stop();
        Repaint();
    }
}
