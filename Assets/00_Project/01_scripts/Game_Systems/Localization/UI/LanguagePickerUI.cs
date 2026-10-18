using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Language window: one button per language. Opens on first launch and from the menus. </summary>
public class LanguagePickerUI : MonoBehaviour
{
    private const string ResourcePath = "LanguagePicker";

    private static LanguagePickerUI _open;

    [SerializeField] private RectTransform window;
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private LanguageButtonUI buttonPrefab;
    [SerializeField] private Button closeButton;

    [Header("Layout")]
    [Tooltip("Window height above and below the button grid.")]
    [SerializeField] private float chromeHeight = 200f;

    private readonly List<LanguageButtonUI> _buttons = new List<LanguageButtonUI>();
    private bool _canClose;

    /// <summary> True while the picker is on screen. </summary>
    public static bool IsOpen => _open != null;

    /// <summary> Opens the picker; canClose false forces a choice (first launch). </summary>
    public static void Show(bool canClose)
    {
        if (_open != null) return;

        var prefab = Resources.Load<LanguagePickerUI>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("LanguagePicker prefab missing in Resources.");
            return;
        }

        _open = Instantiate(prefab);
        _open.Setup(canClose);
    }

    private void Setup(bool canClose)
    {
        _canClose = canClose;
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(canClose);
            closeButton.onClick.AddListener(Close);
        }

        Build();

        // The shared entrance component owns the window motion.
    }

    private void OnEnable()
    {
        Localizer.LanguageChanged += Build;
        PauseHandler.AddInterceptor(OnPausePressed);
    }

    private void OnDisable()
    {
        Localizer.LanguageChanged -= Build;
        PauseHandler.RemoveInterceptor(OnPausePressed);
    }

    private void OnDestroy()
    {
        if (_open == this) _open = null;
    }

    /// <summary> Rebuilds the rows so the active language is highlighted. </summary>
    private void Build()
    {
        if (this == null || listRoot == null || buttonPrefab == null) return;

        foreach (LanguageButtonUI old in _buttons)
            if (old != null) Destroy(old.gameObject);
        _buttons.Clear();

        IReadOnlyList<LanguageInfo> languages = Localizer.Languages;
        string current = Localizer.CurrentCode;
        for (int i = 0; i < languages.Count; i++)
        {
            LanguageButtonUI row = Instantiate(buttonPrefab, listRoot);
            row.Bind(languages[i], languages[i].Code == current, Pick);
            _buttons.Add(row);
        }

        if (window != null && listRoot.TryGetComponent(out GridLayoutGroup grid))
        {
            int columns = Mathf.Max(1, grid.constraintCount);
            int rows = Mathf.CeilToInt(languages.Count / (float)columns);
            float gridHeight = rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
            window.sizeDelta = new Vector2(window.sizeDelta.x, chromeHeight + gridHeight);
        }
    }

    /// <summary> Saves the chosen language and closes the window. </summary>
    private void Pick(string code)
    {
        Localizer.SetLanguage(code);
        Close(force: true);
    }

    /// <summary> Closes the window if closing is allowed. </summary>
    public void Close() => Close(force: false);

    private void Close(bool force)
    {
        if (!_canClose && !force) return;
        Destroy(gameObject);
    }

    /// <summary> Pause key closes the picker instead of toggling the pause menu. </summary>
    private bool OnPausePressed()
    {
        Close(force: false);
        return true;
    }
}
