using UnityEngine;
using UnityEngine.UI;

/// <summary> Persistent controller: opens on the player's death event, declares its needs to the manager. </summary>
public class DeathMenu : MonoBehaviour, IUIPanel
{
    [Tooltip("The death panel visual toggled open/closed.")]
    public GameObject Panel;

    public Button Restart_Button;
    public Button Quit_Button;

    [Tooltip("Scene loaded when Quit is pressed.")]
    public string MainMenuScene = "MainMenu Scene";

    // Conditions applied while the death menu is open.
    public bool RequirePause => true;
    public bool RequireMouse => true;
    public bool PausePlayer => true;
    public bool FreezePlayer => false;
    public bool MuteMusic => true ;

    private void Awake()
    {
        if (Restart_Button != null) Restart_Button.onClick.AddListener(() => GameFlowManager.Instance.Restart());
        if (Quit_Button != null) Quit_Button.onClick.AddListener(() => GameFlowManager.Instance.LoadScene(MainMenuScene));
        if (Panel != null) Panel.SetActive(false); // start hidden
    }

    private void OnEnable() => StatsBehaviour.OnDeath += HandleDeath;
    private void OnDisable() => StatsBehaviour.OnDeath -= HandleDeath;

    private void OnDestroy()
    {
        UIPanelsManager.Instance?.Deactivate(this);
        if (Restart_Button != null) Restart_Button.onClick.RemoveAllListeners();
        if (Quit_Button != null) Quit_Button.onClick.RemoveAllListeners();
    }

    /// <summary> Opens the death menu when the player dies. </summary>
    private void HandleDeath(StatsBehaviour stats) => SetOpen(true);

    /// <summary> Shows/hides the panel and registers/unregisters with the manager. </summary>
    private void SetOpen(bool open)
    {
        if (Panel != null) Panel.SetActive(open);
        if (open) UIPanelsManager.Instance?.Activate(this);
        else UIPanelsManager.Instance?.Deactivate(this);
    }
}
