using System.Text;
using TMPro;
using UnityEngine;

/// <summary> Pulses one word of a TMP label in scale and color; Arabic-safe. </summary>
[RequireComponent(typeof(TMP_Text))]
public class TextWordPulse : MonoBehaviour
{
    [Tooltip("Pulses per second.")]
    [SerializeField, Min(0f)] private float pulsesPerSecond = 1.5f;
    [Tooltip("Extra scale at the pulse peak.")]
    [SerializeField, Min(0f)] private float scaleAmount = 0.15f;
    [Tooltip("Word color at the pulse peak.")]
    [SerializeField] private Color pulseColor = new Color(1f, 0.78f, 0.1f);

    private readonly StringBuilder _shown = new StringBuilder();
    private TMP_Text _label;
    private TMP_MeshInfo[] _source; // unmodified vertices, rewritten each frame
    private string _word;
    private int _first = -1, _last = -1;
    private bool _dirty;
    private bool _rebuilding;

    private TMP_Text Label => _label != null ? _label : (_label = GetComponent<TMP_Text>());

    /// <summary> Word to pulse; call after setting the label text. Empty stops. </summary>
    public void SetWord(string word)
    {
        _word = word;
        _dirty = true;
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _dirty = true;
    }

    private void OnDisable() => TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);

    private void OnTextChanged(Object changed)
    {
        if (!_rebuilding && changed == Label) _dirty = true;
    }

    private void LateUpdate()
    {
        if (_dirty) Rebuild();
        if (_first < 0 || _source == null) return;

        float t = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * pulsesPerSecond * 2f * Mathf.PI);
        float scale = 1f + scaleAmount * t;
        TMP_TextInfo info = Label.textInfo;
        Vector3 center = WordCenter(info);

        for (int i = _first; i <= _last; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            int m = ch.materialReferenceIndex, v = ch.vertexIndex;
            if (m >= _source.Length) continue;
            Vector3[] src = _source[m].vertices;
            Color32[] srcColors = _source[m].colors32;
            Vector3[] dst = info.meshInfo[m].vertices;
            Color32[] dstColors = info.meshInfo[m].colors32;
            for (int k = 0; k < 4; k++)
            {
                dst[v + k] = center + (src[v + k] - center) * scale;
                if (ch.elementType != TMP_TextElementType.Character) continue;
                Color32 c = Color32.Lerp(srcColors[v + k], pulseColor, t);
                c.a = srcColors[v + k].a;
                dstColors[v + k] = c;
            }
        }
        Label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary> Regenerates the mesh, caches it and finds the word's characters. </summary>
    private void Rebuild()
    {
        _dirty = false;
        _first = _last = -1;
        _source = null;
        if (string.IsNullOrEmpty(_word) || !Label.isActiveAndEnabled) return;

        _rebuilding = true;
        Label.ForceMeshUpdate();
        _rebuilding = false;

        TMP_TextInfo info = Label.textInfo;
        if (info.characterCount == 0) return;
        _source = info.CopyMeshInfoVertexData();

        _shown.Clear();
        for (int i = 0; i < info.characterCount; i++) _shown.Append(info.characterInfo[i].character);
        string shown = _shown.ToString();

        int index = Find(shown, _word);
        string needle = _word;
        if (index < 0 && ArabicText.NeedsShaping(_word))
        {
            needle = ArabicText.ToVisual(_word); // single-line shaped order
            index = Find(shown, needle);
            if (index < 0)
            {
                needle = Reverse(ArabicText.ToLayout(_word)); // TMP right-to-left order
                index = Find(shown, needle);
            }
        }

        // Not found: pulse the whole label.
        _first = index >= 0 ? index : 0;
        _last = index >= 0 ? index + needle.Length - 1 : info.characterCount - 1;
    }

    /// <summary> Center of the word's unscaled bounds. </summary>
    private Vector3 WordCenter(TMP_TextInfo info)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue);
        for (int i = _first; i <= _last; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible || ch.materialReferenceIndex >= _source.Length) continue;
            Vector3[] src = _source[ch.materialReferenceIndex].vertices;
            for (int k = 0; k < 4; k++)
            {
                min = Vector3.Min(min, src[ch.vertexIndex + k]);
                max = Vector3.Max(max, src[ch.vertexIndex + k]);
            }
        }
        return min.x > max.x ? Vector3.zero : (min + max) * 0.5f;
    }

    private static int Find(string text, string word) => text.IndexOf(word, System.StringComparison.Ordinal);

    private static string Reverse(string text)
    {
        char[] chars = text.ToCharArray();
        System.Array.Reverse(chars);
        return new string(chars);
    }
}
