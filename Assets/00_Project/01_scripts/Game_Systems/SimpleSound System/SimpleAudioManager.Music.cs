using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SimpleAudioManager
{
    [Header("Scene Music")]
    [SerializeField] private AudioClip _menuMusic;
    [SerializeField] private AudioClip _gameplayMusic;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += SelectSceneMusic;
        SceneManager.sceneUnloaded += RefreshAfterSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= SelectSceneMusic;
        SceneManager.sceneUnloaded -= RefreshAfterSceneUnloaded;
        RestoreSuppressedListeners();
    }

    private void Start()
    {
        SelectSceneMusic(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void SelectSceneMusic(Scene scene, LoadSceneMode mode)
    {
        if (instance != this) return;
        RefreshSceneListener(scene);
        if (_MusicSource == null) return;
        AudioClip track = scene.name == "MainMenu Scene" ? _menuMusic :
            scene.name == "Level" || scene.name == "ThemedSorting" ? _gameplayMusic : null;
        // Keep the current theme through the initializer and loading screen.
        if (track == null) return;
        _MusicSource.loop = true;
        SwitchMusic(track);
    }
}
