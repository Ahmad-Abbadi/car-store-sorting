using UnityEngine;
using UnityEngine.UI;

/// <summary> Persistent controller: opens on the PauseHandler event, declares its needs to the manager. </summary>
public class PauseMenu : MonoBehaviour, IUIPanel
{
    [Tooltip("The pause panel visual toggled open/closed.")]
    public GameObject Panel;

    public Button Continue_Button;
    public Button Restart_Button;
    public Button Quit_Button;

    [Tooltip("Scene loaded when Quit is pressed.")]
    public string MainMenuScene = "MainMenu Scene";

    [Tooltip("Objects outside the panel shown only while the menu is open.")]
    public GameObject[] OutsidePanelObjects;

    // Conditions applied while the pause menu is open.
    public bool RequirePause => true;
    public bool RequireMouse => true;
    public bool PausePlayer => false;
    public bool FreezePlayer => false;
    public bool MuteMusic => false;

    private void Awake()
    {
        // Continue closes the panel; the panel manager then resumes play through the flow.
        if (Continue_Button != null) Continue_Button.onClick.AddListener(Close);
        if (Restart_Button != null) Restart_Button.onClick.AddListener(() => GameFlowManager.Instance.Restart());
        if (Quit_Button != null) Quit_Button.onClick.AddListener(() => GameFlowManager.Instance.LoadScene(MainMenuScene));
        if (Panel != null) Panel.SetActive(false); // start hidden
        SetOutsideObjects(false);
    }

    /// <summary> Shows/hides the objects that live outside the panel. </summary>
    private void SetOutsideObjects(bool visible)
    {
        if (OutsidePanelObjects == null) return;
        foreach (GameObject obj in OutsidePanelObjects)
            if (obj != null) obj.SetActive(visible);
    }

    private void OnEnable() => PauseHandler.OnPauseToggled += Toggle;
    private void OnDisable() => PauseHandler.OnPauseToggled -= Toggle;

    private void OnDestroy()
    {
        // Drop from the manager if destroyed (e.g. scene reload) while still open.
        UIPanelsManager.Instance?.Deactivate(this);

        if (Continue_Button != null) Continue_Button.onClick.RemoveAllListeners();
        if (Restart_Button != null) Restart_Button.onClick.RemoveAllListeners();
        if (Quit_Button != null) Quit_Button.onClick.RemoveAllListeners();
    }

    /// <summary> Flips the pause menu open/closed. </summary>
    private void Toggle() => SetOpen(Panel == null || !Panel.activeSelf);

    /// <summary> Closes the menu; the manager resumes play when no panel needs a pause. </summary>
    public void Close() => SetOpen(false);

    /// <summary> Shows/hides the panel and registers/unregisters with the manager. </summary>
    private void SetOpen(bool open)
    {
        if (Panel != null) Panel.SetActive(open);
        SetOutsideObjects(open);
        if (open) UIPanelsManager.Instance?.Activate(this);
        else UIPanelsManager.Instance?.Deactivate(this);
    }
}
