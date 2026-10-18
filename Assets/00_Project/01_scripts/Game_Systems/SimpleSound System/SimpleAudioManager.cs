
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public partial class SimpleAudioManager : MonoBehaviour
{
    public static SimpleAudioManager instance;

    [Header("Settings")]
    [SerializeField] private int _poolSize = 15;
    [SerializeField] private GameObject _audioSourcePrefab;
    [SerializeField] private AudioSource _MusicSource;

    [Header("Libraries")]
    [SerializeField] private GameplaySounds_SO _gameplaySounds;

    /// <summary> Gameplay SFX library; null when not assigned. </summary>
    public GameplaySounds_SO GameplaySounds => _gameplaySounds;

    [Header("Music")]
    [Tooltip("Default seconds for a full music switch: half fades out, half fades back in.")]
    [SerializeField] private float _musicFadeDuration = 1f;

    private float _musicVolume = 1f;
    private Coroutine _musicFade;

    private bool _isMute;
    private bool _isMuteLocked;

    private const string SfxMutedKey = "Audio_SfxMuted";
    private const string MusicMutedKey = "Audio_MusicMuted";

    /// <summary> Raised when the player's saved SFX or music mute changes. </summary>
    public static event System.Action OnMuteSettingsChanged;

    // Cached saved mutes; null until first read from PlayerPrefs.
    private static bool? _sfxMuted;
    private static bool? _musicMuted;

    /// <summary> Player's saved SFX mute, independent of temporary panel mutes. </summary>
    public static bool SfxMuted
    {
        get => _sfxMuted ??= PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
        set { _sfxMuted = value; SaveMuteSetting(SfxMutedKey, value); }
    }

    /// <summary> Player's saved music mute, independent of temporary panel mutes. </summary>
    public static bool MusicMuted
    {
        get => _musicMuted ??= PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        set { _musicMuted = value; SaveMuteSetting(MusicMutedKey, value); }
    }

    /// <summary> Saves a mute setting, applies it, and notifies listeners. </summary>
    private static void SaveMuteSetting(string key, bool muted)
    {
        PlayerPrefs.SetInt(key, muted ? 1 : 0);
        PlayerPrefs.Save();
        if (instance != null) instance.ApplyMuteState();
        OnMuteSettingsChanged?.Invoke();
    }
    private List<AudioSource> _sourcePool = new List<AudioSource>();
    private int _currentIndex = 0;
    private float[] _stopAt; // per pooled source: unscaled time to stop, 0 = none

    float _lastTimePlay; 
    [SerializeField] float musicFadeDuration = 0.35f;
    


    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            InitPool();

            if (_MusicSource != null) _musicVolume = _MusicSource.volume;
            ApplyMuteState();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void InitPool()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            GameObject obj = Instantiate(_audioSourcePrefab, transform);
            AudioSource s = obj.GetComponent<AudioSource>();
            s.playOnAwake = false;
            _sourcePool.Add(s);
        }
        _stopAt = new float[_poolSize];
    }

    private void Update()
    {
        StopCutoffSources();
    }

    /// <summary> Stops pooled sources that reached their sound's end cutoff. </summary>
    private void StopCutoffSources()
    {
        if (_stopAt == null) return;

        float now = Time.unscaledTime;
        for (int i = 0; i < _stopAt.Length; i++)
        {
            if (_stopAt[i] <= 0f || now < _stopAt[i]) continue;
            _stopAt[i] = 0f;
            _sourcePool[i].Stop();
        }
    }

    private AudioSource GetNextSourceInLoop(out int index)
    {
        index = _currentIndex;
        _currentIndex = (_currentIndex + 1) % _poolSize;
        return _sourcePool[index];
    }

    /// <summary> Plays a sound on the next pooled source; null when skipped. </summary>
    private AudioSource PlaySound(GameSound sound, Vector3 position, float pitchScale, float threshold, bool ui = false)
    {
        if (!sound.IsValid || (!ui && _isMute) || SfxMuted) return null;
        if (!ui && threshold > 0f && Time.unscaledTime - _lastTimePlay < threshold) return null;

        AudioSource source = GetNextSourceInLoop(out int index);
        float pitch = sound.SafePitch * pitchScale;

        source.Stop();
        source.spatialBlend = ui ? 0f : 1f;
        source.ignoreListenerPause = ui;
        source.loop = false;
        source.mute = false;
        source.transform.position = position;
        source.clip = sound.Clip;
        source.volume = sound.Volume;
        source.pitch = pitch;
        source.timeSamples = Mathf.Min((int)(sound.StartTime * sound.Clip.frequency), sound.Clip.samples - 1);
        source.Play();

        // Real-time length of the cut region, scaled by pitch.
        _stopAt[index] = sound.EndsEarly ? Time.unscaledTime + (sound.EndTime - sound.StartTime) / pitch : 0f;
        if (!ui) _lastTimePlay = Time.unscaledTime;
        return source;
    }

    /// <summary> Plays a 2D interface cue, respecting saved SFX mute even while gameplay is paused or muted. </summary>
    public void PlayUI(GameSound sound) => PlaySound(sound, Vector3.zero, 1f, 0f, true);

    /// <summary> Plays a sound at a world position. </summary>
    public void PlayClipAt(GameSound sound, Vector3 position, float threshold = 0f)
    {
        PlaySound(sound, position, 1f, threshold);
    }

    /// <summary> Plays a sound for a set time, then fades it out instead of cutting it. </summary>
    public void PlayClipForDuration(GameSound sound, Vector3 position, float duration, float fadeDuration = 0.25f)
    {
        AudioSource source = PlaySound(sound, position, 1f, 0f);
        if (source != null)
            StartCoroutine(PlayForDurationRoutine(source, sound.Clip, duration, fadeDuration, sound.Volume));
    }

    /// <summary> Camera-positioned version of <see cref="PlayClipForDuration"/>. </summary>
    public void PlayGlobalForDuration(GameSound sound, float duration, float fadeDuration = 0.25f)
    {
        if (Camera.main != null)
            PlayClipForDuration(sound, Camera.main.transform.position, duration, fadeDuration);
    }

    // Unscaled time so it still fades while paused. Bails if the pool reused the source.
    private IEnumerator PlayForDurationRoutine(AudioSource source, AudioClip clip, float duration, float fadeDuration, float volume)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            if (source.clip != clip) yield break;
            yield return null;
        }

        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            if (source.clip != clip) yield break;
            source.volume = Mathf.Lerp(volume, 0f, t / fadeDuration);
            yield return null;
        }

        if (source.clip != clip) yield break;

        source.Stop();
        source.volume = volume; // restored for the next pooled use
    }

    /// <summary> Plays a sound at a position with random ±10% pitch on top of its own. </summary>
    public void PlayJuicyClip(GameSound sound, Vector3 position, float threshold = 0f)
    {
        PlaySound(sound, position, Random.Range(0.9f, 1.1f), threshold);
    }

    /// <summary> Camera-positioned version of <see cref="PlayJuicyClip"/>. </summary>
    public void PlayJuicyGlobalClip(GameSound sound, float threshold = 0f)
    {
        if (Camera.main != null)
            PlayJuicyClip(sound, Camera.main.transform.position, threshold);
    }

    /// <summary> Plays a sound at the camera position. </summary>
    public void PlayGlobal(GameSound sound, float threshold = 0f)
    {
        if (Camera.main != null)
            PlayClipAt(sound, Camera.main.transform.position, threshold);
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;

        // A running fade owns the volume and will land on the new target itself.
        if (_MusicSource != null && _musicFade == null)
            _MusicSource.volume = volume;
    }

    /// <summary> Track the music source is set to right now, null when there is none. </summary>
    public AudioClip CurrentMusic => _MusicSource != null ? _MusicSource.clip : null;

    /// <summary> Fades the music out, swaps the track mid-fade, then fades back in. </summary>
    public void SwitchMusic(AudioClip clip)
    {
        if (_MusicSource == null) return;
        if (musicFadeDuration < 0f) musicFadeDuration = _musicFadeDuration;
        if (clip != null && _MusicSource.clip == clip && _MusicSource.isPlaying) return;

        if (_musicFade != null) StopCoroutine(_musicFade);
        _musicFade = StartCoroutine(SwitchMusicRoutine(clip, musicFadeDuration));
    }

    private IEnumerator SwitchMusicRoutine(AudioClip clip, float fadeDuration)
    {
        yield return FadeMusicTo(0f, fadeDuration * 0.5f);

        _MusicSource.clip = clip;
        if (clip != null) _MusicSource.Play();
        else _MusicSource.Stop();

        yield return FadeMusicTo(_musicVolume, fadeDuration * 0.5f);
        _musicFade = null;
    }

    // Unscaled time, so a paused game still fades.
    private IEnumerator FadeMusicTo(float target, float duration)
    {
        if (duration <= 0f)
        {
            _MusicSource.volume = target;
            yield break;
        }

        float start = _MusicSource.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _MusicSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        _MusicSource.volume = target;
    }

    public void ToggleMute(bool isMute)
    {
        if (_isMuteLocked && !isMute ) return; 
        _isMute = isMute;
        ApplyMuteState();
    }

    /// <summary> Applies temporary and saved mutes to the music and SFX sources. </summary>
    private void ApplyMuteState()
    {
        if (_MusicSource != null) _MusicSource.mute = _isMute || MusicMuted;

        // Cut playing SFX when they become muted.
        if (_isMute || SfxMuted)
        {
            foreach (var source in _sourcePool)
                if (SfxMuted || source.spatialBlend > 0f) source.Stop();
        }
    }

    public void LockMute(bool mute)
    {
        _isMuteLocked = mute; 
    }
}
