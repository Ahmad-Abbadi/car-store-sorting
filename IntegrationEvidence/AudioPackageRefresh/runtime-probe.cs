var manager=SimpleAudioManager.instance;
if(manager==null)return "No audio manager";
System.Collections.IEnumerator Probe()
{
    var lines=new System.Collections.Generic.List<string>();
    System.Action<bool,string> check=(ok,label)=>lines.Add((ok?"PASS ":"FAIL ")+label);
    bool oldMusic=SimpleAudioManager.MusicMuted,oldSfx=SimpleAudioManager.SfxMuted;
    bool hadMusic=UnityEngine.PlayerPrefs.HasKey("Audio_MusicMuted"),hadSfx=UnityEngine.PlayerPrefs.HasKey("Audio_SfxMuted");
    float oldScale=UnityEngine.Time.timeScale;
    bool oldBackground=UnityEngine.Application.runInBackground;
    UnityEngine.Application.runInBackground=true;
    try
    {
        yield return new UnityEngine.WaitForSecondsRealtime(1.2f);
        check(manager.CurrentMusic!=null&&manager.CurrentMusic.name=="Menu_LifeIsSimple","menu theme selected");
        var music=manager.GetComponentsInChildren<UnityEngine.AudioSource>(true).First(s=>s.clip==manager.CurrentMusic);
        float t=music.time;yield return new UnityEngine.WaitForSecondsRealtime(.2f);
        check(music.isPlaying&&music.time!=t,"music advances");
        SimpleAudioManager.MusicMuted=true;
        check(music.mute&&SimpleAudioManager.SfxMuted==oldSfx,"music mute independent");
        SimpleAudioManager.SfxMuted=true;manager.PlayGlobal(manager.GameplaySounds.CompleteShelfSFX);
        check(!manager.GetComponentsInChildren<UnityEngine.AudioSource>(true).Any(s=>s!=music&&s.isPlaying),"SFX mute suppresses playback");
        SimpleAudioManager.SfxMuted=false;check(music.mute,"SFX unmute preserves music mute");
        var sounds=manager.GameplaySounds.StepsSounds.Concat(manager.GameplaySounds.CollectingSounds).Concat(manager.GameplaySounds.ThrowingSounds).Concat(manager.GameplaySounds.ShelfPlacementSounds).Concat(new[]{manager.GameplaySounds.JumpSound,manager.GameplaySounds.WrongSFX,manager.GameplaySounds.WrongSectionSFX,manager.GameplaySounds.CompleteShelfSFX}).ToArray();
        foreach(var sound in sounds)
        {
            manager.PlayGlobal(sound);yield return null;
            check(manager.GetComponentsInChildren<UnityEngine.AudioSource>(true).Any(s=>s.clip==sound.Clip&&s.isPlaying),"effect plays: "+sound.Clip.name);
            yield return new UnityEngine.WaitForSecondsRealtime(.15f);
        }
        manager.ToggleMute(true);
        check(music.mute&&!manager.GetComponentsInChildren<UnityEngine.AudioSource>(true).Any(s=>s!=music&&s.isPlaying),"temporary mute");
        manager.LockMute(true);manager.ToggleMute(false);check(music.mute,"mute lock");
        manager.LockMute(false);manager.ToggleMute(false);check(music.mute,"saved music mute survives temporary unmute");
        SimpleAudioManager.MusicMuted=false;UnityEngine.Time.timeScale=0;
        var game=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/00_Project/Audio/PackageSelection/Music/Gameplay_LifeIsSimple.wav");
        manager.SwitchMusic(game);yield return new UnityEngine.WaitForSecondsRealtime(1.2f);
        check(manager.CurrentMusic==game&&music.isPlaying&&UnityEngine.Mathf.Abs(music.volume-.18f)<.001f,"fade completes while paused");
        UnityEngine.Time.timeScale=1;SceneLoader.LoadWithLoadingScreen("Level");
        float deadline=UnityEngine.Time.realtimeSinceStartup+60;
        bool listenersStable=true;
        while((UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Level"||SceneLoader.IsLoading)&&UnityEngine.Time.realtimeSinceStartup<deadline)
        {
            listenersStable &= UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>(UnityEngine.FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)==1;
            yield return null;
        }
        check(listenersStable,"exactly one listener throughout loading into gameplay");
        yield return new UnityEngine.WaitForSecondsRealtime(1.2f);
        check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Level"&&manager.CurrentMusic==game&&music.isPlaying,"menu/loading/gameplay music");
        check(UnityEngine.Object.FindObjectsByType<SimpleAudioManager>(UnityEngine.FindObjectsSortMode.None).Length==1,"one persistent manager after loading");
        var gameplayButtons=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None);
        check(gameplayButtons.Length>0&&gameplayButtons.All(b=>b.GetComponent<UIButtonAudioFeedback>()),"all gameplay buttons including dynamically created controls have click audio: "+gameplayButtons.Length);
        check(UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>(UnityEngine.FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)==1,"one active listener");
        t=music.time;manager.SwitchMusic(game);yield return new UnityEngine.WaitForSecondsRealtime(.1f);check(music.time>t,"same theme does not restart");
        SceneLoader.LoadWithLoadingScreen("MainMenu Scene");deadline=UnityEngine.Time.realtimeSinceStartup+60;
        listenersStable=true;
        while((UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MainMenu Scene"||SceneLoader.IsLoading)&&UnityEngine.Time.realtimeSinceStartup<deadline)
        {
            listenersStable &= UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>(UnityEngine.FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)==1;
            yield return null;
        }
        check(listenersStable,"exactly one listener throughout loading into menu");
        yield return new UnityEngine.WaitForSecondsRealtime(1.2f);
        check(manager.CurrentMusic!=null&&manager.CurrentMusic.name=="Menu_LifeIsSimple"&&music.isPlaying,"return to menu theme");
        check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None).All(b=>b.GetComponent<UIButtonAudioFeedback>()),"menu button audio survives scene reload");
        lines.Add("INFO Editor AudioClip bytes: "+sounds.Select(s=>s.Clip).Concat(new[]{game,manager.CurrentMusic}).Distinct().Sum(c=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(c)));
    }
    finally
    {
        UnityEngine.Time.timeScale=oldScale;manager.LockMute(false);manager.ToggleMute(false);
        UnityEngine.Application.runInBackground=oldBackground;
        SimpleAudioManager.MusicMuted=oldMusic;SimpleAudioManager.SfxMuted=oldSfx;
        if(!hadMusic)UnityEngine.PlayerPrefs.DeleteKey("Audio_MusicMuted");
        if(!hadSfx)UnityEngine.PlayerPrefs.DeleteKey("Audio_SfxMuted");
        UnityEngine.PlayerPrefs.Save();
        System.IO.File.WriteAllLines("IntegrationEvidence/AudioPackageRefresh/runtime-checks.txt",lines);
    }
}
manager.StartCoroutine(Probe());
return "Playback probe started";
