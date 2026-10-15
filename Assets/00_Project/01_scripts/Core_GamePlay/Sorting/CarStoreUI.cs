using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Store HUD, first visit help, section progress, and persistent completion flow.</summary>
public sealed class CarStoreUI : MonoBehaviour
{
    private Canvas canvas;
    private TMP_Text overall;
    private TMP_Text nearby;
    private TMP_Text directory;
    private TMP_Text directoryRight;
    private TMP_Text directoryTitle;
    private TMP_Text directoryLegend;
    private GameObject tutorial;
    private GameObject finish;
    private TMP_Text tutorialText;
    private TMP_Text finishText;
    private TMP_Text startLabel;
    private TMP_Text replayLabel;
    private Section[] sections;
    private SortingShelf[] shelves;
    private LevelSaveManager save;
    private bool shownComplete;
    private GameObject themeDirectory;

    private void OnEnable()
    {
        SortingProgressTracker.OnProgressChanged += OnProgress;
        SortingShelf.OnShelfFilled += OnShelfFilled;
        Localizer.LanguageChanged += Refresh;
    }

    private void OnDisable()
    {
        SortingProgressTracker.OnProgressChanged -= OnProgress;
        SortingShelf.OnShelfFilled -= OnShelfFilled;
        Localizer.LanguageChanged -= Refresh;
    }

    private IEnumerator Start()
    {
        bool themed = TinyRiders.ThemedCollectionSession.Instance != null;
        if (!themed && GetComponent<CarVisualCulling>() == null) gameObject.AddComponent<CarVisualCulling>();
        BuildUI();
        yield return null; // save restore and progress recount happen first
        yield return null;
        sections = FindObjectsByType<Section>(FindObjectsSortMode.None);
        shelves = FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        save = FindFirstObjectByType<LevelSaveManager>();
        if (themed) { Refresh(); yield break; }
        if (save != null && save.StoreCompleted) ShowCompletion();
        else if (SortingProgressTracker.Instance != null && SortingProgressTracker.Instance.Collected == 0)
        {
            tutorial.SetActive(true);
            GameFlowManager.Instance?.FreezeGame();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        Refresh();
    }

    private void Update()
    {
        if (TinyRiders.ThemedCollectionSession.Instance != null)
        {
            if (Player.Instance?.Input != null && Player.Instance.Input.CollectionPressed && themeDirectory != null)
                themeDirectory.SetActive(!themeDirectory.activeSelf);
            if (Time.frameCount % 20 == 0) Refresh(); return;
        }
        if (sections == null || sections.Length == 0 || nearby == null) return;
        if (Time.frameCount % 20 == 0) RefreshNearby();
    }

    private void OnProgress(int collected, int total)
    {
        Refresh();
        if (TinyRiders.ThemedCollectionSession.Instance != null) return;
        if (total == 525 && collected == total) StartCoroutine(CheckCompletion());
    }

    private void OnShelfFilled(SortingShelf shelf)
    {
        Refresh();
        if (TinyRiders.ThemedCollectionSession.Instance != null) return;
        if (SortingProgressTracker.Instance != null && SortingProgressTracker.Instance.Collected == 525)
            StartCoroutine(CheckCompletion());
    }

    private IEnumerator CheckCompletion()
    {
        yield return new WaitForSeconds(0.6f); // wait for the final jump to settle
        if (shownComplete || shelves == null || shelves.Length != 75) yield break;
        SortingProgressTracker progress = SortingProgressTracker.Instance;
        if (progress == null || progress.Total != 525 || progress.Collected != 525) yield break;
        foreach (SortingShelf shelf in shelves)
            if (shelf == null || !shelf.IsCompleted || shelf.SpotCount != 7) yield break;
        LevelSaveManager.MarkStoreCompleted();
        ShowCompletion();
    }

    private void ShowCompletion()
    {
        shownComplete = true;
        tutorial.SetActive(false);
        finish.SetActive(true);
        GameFlowManager.Instance?.SetState(GameFlowManager.GameState.LevelFinished);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Refresh();
    }

    private void DismissTutorial()
    {
        tutorial.SetActive(false);
        GameFlowManager.Instance?.SetState(GameFlowManager.GameState.Playing);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Replay()
    {
        save?.Clear();
        FindFirstObjectByType<PlayerSavePosition>()?.Clear();
        CoinsManager.Instance?.ResetForReplay();
        UpgradeManager.Instance?.ResetForReplay();
        Time.timeScale = 1f;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.LoadScene(SceneManager.GetActiveScene().name);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Refresh()
    {
        if (overall == null) return;
        var session = TinyRiders.ThemedCollectionSession.Instance;
        if (session != null)
        {
            overall.text = $"Sorted {session.Sorted} / {session.Total}  ·  Designs {session.UniqueDiscovered} / {System.Linq.Enumerable.Count(session.ActiveDefinitions)}";
            nearby.fontSize = 19;
            directoryTitle.text = "THEME COLLECTIONS";
            var held = Player.Instance?.ItemHandler?.CurrentEquipped;
            var entry = held != null ? session.Definition(held.Data) : null;
            nearby.text = entry != null ? "Carrying: " + session.ThemeName(entry.themeId) + "\n" + session.Message : session.Message;
            directoryLegend.text = entry != null ? "Carrying: " + session.ThemeName(entry.themeId) : "E / left click: pick up   ·   F / right click: place or drop";
            if (themeDirectory != null && !themeDirectory.activeSelf) return;
            var left = new StringBuilder(); var right = new StringBuilder(); int index = 0;
            foreach (var theme in session.catalog.themes)
            {
                if (!theme.enabled) continue;
                var counts = session.ThemeCounts(theme.id);
                (index++ < 20 ? left : right).Append(theme.displayName).Append("  ").Append(counts.sorted).Append('/').Append(counts.total).Append('\n');
            }
            directory.text = left.ToString(); directoryRight.text = right.ToString();
            directory.fontSize = directoryRight.fontSize = 18;
            ((RectTransform)directory.transform.parent).sizeDelta = new Vector2(700, 525);
            ((RectTransform)directory.transform).sizeDelta = ((RectTransform)directoryRight.transform).sizeDelta = new Vector2(320, 430);
            return;
        }
        SortingProgressTracker progress = SortingProgressTracker.Instance;
        int count = progress != null ? progress.Collected : 0;
        overall.SetLocalized(Localizer.FormatText(Localizer.GetOr("store.progress", "Cars sorted: {0} / 525"), count));
        if (directory != null)
        {
            var left = new StringBuilder();
            var right = new StringBuilder();
            for (int i = 0; i < CarModelCatalog.EnglishNames.Length; i++)
            {
                char zone = i < 5 ? 'W' : i < 10 ? 'E' : i < 13 ? 'B' : 'F';
                StringBuilder column = i < 8 ? left : right;
                if (column.Length > 0) column.Append('\n');
                column.Append(zone).Append(' ').Append((i + 1).ToString("00")).Append("  ")
                    .Append(CarModelCatalog.DisplayName(i));
            }
            directory.SetLocalized(left.ToString());
            directoryRight.SetLocalized(right.ToString());
            directoryTitle.SetLocalized(Localizer.GetOr("store.model_directory_title", "MODEL DIRECTORY"));
            directoryLegend.SetLocalized(Localizer.GetOr("store.zone_legend", "W WEST  •  E EAST  •  B BACK  •  F FRONT"));
        }
        if (directory != null)
            directory.alignment = Localizer.IsRtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        if (directoryRight != null)
            directoryRight.alignment = Localizer.IsRtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        if (tutorialText != null) tutorialText.SetLocalized(Localizer.GetOr("store.tutorial",
            "Welcome to the toy car store! Pick up cars from the floor pile. Match each car to the shelf with its model and color. Each shelf needs seven copies. Wrong cars can be taken back. Buy upgrades with coins from correct placements. There is no timer."));
        if (startLabel != null) startLabel.SetLocalized(Localizer.GetOr("store.start", "Start sorting"));
        if (replayLabel != null) replayLabel.SetLocalized(Localizer.GetOr("store.replay", "Replay store"));
        if (finishText != null)
        {
            float seconds = save != null ? save.ElapsedSeconds : 0;
            string time = System.TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
            int coins = CoinsManager.Instance != null ? CoinsManager.Instance.Coins : 0;
            finishText.SetLocalized(Localizer.FormatText(Localizer.GetOr("store.complete",
                "Store complete!\n525 cars sorted across 75 shelves.\nTime: {0}  •  Coins left: {1}"), time, coins));
        }
        RefreshNearby();
    }

    private void RefreshNearby()
    {
        if (sections == null || sections.Length == 0 || nearby == null || Player.Instance == null) return;
        Section closest = null;
        float best = float.MaxValue;
        Vector3 position = Player.Instance.transform.position;
        foreach (Section section in sections)
        {
            if (section == null) continue;
            float distance = (section.transform.position - position).sqrMagnitude;
            if (distance < best) { best = distance; closest = section; }
        }
        if (closest == null) return;
        int sorted = 0;
        List<(int spot, SortableItem_SO data)> buffer = new List<(int, SortableItem_SO)>();
        foreach (SortingShelf shelf in closest.GetComponentsInChildren<SortingShelf>())
        {
            shelf.GetPlacedItems(buffer);
            foreach (var placed in buffer)
                if (shelf.IsCorrectItem(placed.data)) sorted++;
        }
        string model = closest.Catagory != null ? closest.Catagory.DisplayName : closest.name;
        nearby.SetLocalized(Localizer.FormatText(Localizer.GetOr("store.section_progress", "{0}: {1} / 35"), model, sorted));
    }

    private void BuildUI()
    {
        GameObject root = new GameObject("Car Store HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        GameObject progressBackground = new GameObject("Progress Background", typeof(RectTransform), typeof(Image));
        progressBackground.transform.SetParent(root.transform, false);
        RectTransform progressRect = (RectTransform)progressBackground.transform;
        progressRect.anchorMin = progressRect.anchorMax = new Vector2(0.5f, 1f);
        progressRect.pivot = new Vector2(0.5f, 1f);
        progressRect.anchoredPosition = new Vector2(0, -8);
        progressRect.sizeDelta = new Vector2(570, 116);
        progressBackground.GetComponent<Image>().color = new Color(0.03f, 0.09f, 0.15f, 0.82f);
        overall = Text(root.transform, "Overall Progress", new Vector2(20, -20), new Vector2(520, 55), 30, TextAlignmentOptions.TopLeft);
        nearby = Text(root.transform, "Nearby Section", new Vector2(20, -75), new Vector2(520, 46), 24, TextAlignmentOptions.TopLeft);
        RectTransform overallRect = (RectTransform)overall.transform;
        overallRect.anchorMin = overallRect.anchorMax = new Vector2(0.5f, 1f);
        overallRect.pivot = new Vector2(0.5f, 1f);
        overallRect.anchoredPosition = new Vector2(0, -18);
        RectTransform nearbyRect = (RectTransform)nearby.transform;
        nearbyRect.anchorMin = nearbyRect.anchorMax = new Vector2(0.5f, 1f);
        nearbyRect.pivot = new Vector2(0.5f, 1f);
        nearbyRect.anchoredPosition = new Vector2(0, -70);
        GameObject directoryPanel = new GameObject("Model Directory Background", typeof(RectTransform), typeof(Image));
        directoryPanel.transform.SetParent(root.transform, false);
        RectTransform directoryRect = (RectTransform)directoryPanel.transform;
        directoryRect.anchorMin = directoryRect.anchorMax = Vector2.zero;
        directoryRect.pivot = Vector2.zero;
        directoryRect.anchoredPosition = new Vector2(12, 12);
        directoryRect.sizeDelta = new Vector2(700, 335);
        directoryPanel.GetComponent<Image>().color = new Color(0.03f, 0.09f, 0.15f, 0.88f);
        if (TinyRiders.ThemedCollectionSession.Instance != null)
        {
            themeDirectory = directoryPanel;
            themeDirectory.SetActive(false);
            var toggle = Button(root.transform, "Collection Toggle", Vector2.zero,
                () => themeDirectory.SetActive(!themeDirectory.activeSelf));
            toggle.text = "Collection [C]"; toggle.fontSize = 21;
            var toggleRect = (RectTransform)toggle.transform.parent;
            toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(0, 0);
            toggleRect.pivot = new Vector2(0, 0);
            toggleRect.anchoredPosition = new Vector2(16, 16);
            toggleRect.sizeDelta = new Vector2(240, 50);
            ((RectTransform)toggle.transform).sizeDelta = new Vector2(235, 45);
            directoryRect.anchoredPosition = new Vector2(12, 75);
        }
        directoryTitle = Text(directoryPanel.transform, "Directory Title", new Vector2(18, -9),
            new Vector2(660, 33), 23, TextAlignmentOptions.TopLeft);
        directoryLegend = Text(directoryPanel.transform, "Zone Legend", new Vector2(18, -43),
            new Vector2(660, 28), 15, TextAlignmentOptions.TopLeft);
        directory = Text(directoryPanel.transform, "Directory Left", new Vector2(18, 12),
            new Vector2(320, 250), 15, TextAlignmentOptions.TopLeft, false);
        directoryRight = Text(directoryPanel.transform, "Directory Right", new Vector2(360, 12),
            new Vector2(320, 250), 15, TextAlignmentOptions.TopLeft, false);
        tutorial = Panel(root.transform, "Tutorial", new Color(0.04f, 0.09f, 0.14f, 0.96f));
        tutorialText = Text(tutorial.transform, "Tutorial Text", new Vector2(0, 95), new Vector2(930, 380), 37, TextAlignmentOptions.Center);
        startLabel = Button(tutorial.transform, "Start Button", new Vector2(0, -230), DismissTutorial);
        tutorial.SetActive(false);
        finish = Panel(root.transform, "Store Complete", new Color(0.04f, 0.12f, 0.13f, 0.97f));
        finishText = Text(finish.transform, "Completion Stats", new Vector2(0, 95), new Vector2(940, 360), 42, TextAlignmentOptions.Center);
        replayLabel = Button(finish.transform, "Replay Button", new Vector2(0, -220), Replay);
        finish.SetActive(false);
    }

    private static GameObject Panel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text Text(Transform parent, string name, Vector2 position, Vector2 size,
        float fontSize, TextAlignmentOptions alignment, bool top = true)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = top ? new Vector2(0, 1) : new Vector2(0, 0);
        rect.pivot = top ? new Vector2(0, 1) : new Vector2(0, 0);
        if (alignment == TextAlignmentOptions.Center) { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); }
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.enableWordWrapping = true;
        return label;
    }

    private static TMP_Text Button(Transform parent, string name, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(360, 90);
        go.GetComponent<Image>().color = new Color(0.95f, 0.68f, 0.22f);
        go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(action);
        TMP_Text label = Text(go.transform, "Label", Vector2.zero, new Vector2(340, 82), 30, TextAlignmentOptions.Center);
        label.color = new Color(0.08f, 0.1f, 0.14f);
        return label;
    }
}
