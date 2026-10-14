using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Main menu: Play, item detail cycle and Quit buttons. </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Scene the Play button loads.")]
    [SerializeField] private string levelScene = "Level";
    [SerializeField] private Button playButton;
    [SerializeField] private Button itemDetailButton;
    [SerializeField] private TMP_Text ItemDetails;
    [Tooltip("Hidden on WebGL, where quitting does nothing.")]
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (playButton) playButton.onClick.AddListener(Play);
        if (itemDetailButton) itemDetailButton.onClick.AddListener(CycleItemDetail);
        if (quitButton) quitButton.onClick.AddListener(Quit);
#if UNITY_WEBGL && !UNITY_EDITOR
        if (quitButton) quitButton.gameObject.SetActive(false);
#endif
    }

    private void OnEnable() => Localizer.LanguageChanged += RefreshLabel;

    private void OnDisable() => Localizer.LanguageChanged -= RefreshLabel;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshLabel();
        if (!Localizer.HasChosenLanguage && Localizer.Languages.Count > 1) LanguagePickerUI.Show(canClose: false);
    }

    private void OnDestroy()
    {
        if (playButton) playButton.onClick.RemoveListener(Play);
        if (itemDetailButton) itemDetailButton.onClick.RemoveListener(CycleItemDetail);
        if (quitButton) quitButton.onClick.RemoveListener(Quit);
    }

    /// <summary> Loads the gameplay level through the loading screen. </summary>
    public void Play() => SceneLoader.LoadWithLoadingScreen(levelScene);

    /// <summary> Cycles item detail High, Medium, Low; saved for every scene. </summary>
    public void CycleItemDetail()
    {
        ItemDetail.Set((ItemDetailLevel)(((int)ItemDetail.Current + 1) % 3));
        RefreshLabel();
    }

    /// <summary> Quits the app; stops Play mode in the Editor. </summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary> Shows the current detail level on its button. </summary>
    private void RefreshLabel()
    {
        if (ItemDetails) ItemDetails.SetLocalized(Localizer.Format("menu.details", Localizer.Get("menu.detail." + ItemDetail.Current.ToString().ToLowerInvariant())));
    }
}
