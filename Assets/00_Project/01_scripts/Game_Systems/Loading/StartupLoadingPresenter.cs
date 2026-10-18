using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Covers system initialization, then hands off to the existing async loader.</summary>
public sealed class StartupLoadingPresenter : MonoBehaviour
{
    [SerializeField] private LoadingPresentation presentation;
    [SerializeField] private Camera startupCamera;

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;
    private void Start() => presentation.SetIndeterminate();

    public void OpenMenu() => SceneLoader.LoadWithLoadingScreen("MainMenu Scene", 0f);

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneLoader.LoadingSceneName) return;
        // The new canvas is already present, with the same background and layout.
        if (presentation) presentation.gameObject.SetActive(false);
        if (startupCamera) startupCamera.gameObject.SetActive(false);
    }
}
