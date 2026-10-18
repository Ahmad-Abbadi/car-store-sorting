var manager = SimpleAudioManager.instance;
if (!manager) return "No audio manager";
System.Collections.IEnumerator Probe()
{
    var lines = new System.Collections.Generic.List<string>();
    System.Action<bool,string> check = (ok,label) => { lines.Add((ok ? "PASS " : "FAIL ") + label); System.IO.File.WriteAllLines("IntegrationEvidence/AudioPackageRefresh/action-runtime-checks.txt", lines); };
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
    System.Action<object,string,object> set = (obj,key,value) => obj.GetType().GetField(key,flags).SetValue(obj,value);
    System.Func<int> index = () => (int)typeof(SimpleAudioManager).GetField("_currentIndex",flags).GetValue(manager);
    var pool = (System.Collections.Generic.List<UnityEngine.AudioSource>)typeof(SimpleAudioManager).GetField("_sourcePool",flags).GetValue(manager);
    System.Func<int,int> count = start => (index()-start+pool.Count)%pool.Count;
    var oldSfx = SimpleAudioManager.SfxMuted;
    bool hadSfx = UnityEngine.PlayerPrefs.HasKey("Audio_SfxMuted"), oldBackground = UnityEngine.Application.runInBackground;
    float oldScale = UnityEngine.Time.timeScale;
    bool oldListenerPause = UnityEngine.AudioListener.pause, completed = false;
    var roots = new System.Collections.Generic.List<UnityEngine.GameObject>();
    var pausedObservers = new System.Collections.Generic.List<UnityEngine.Behaviour>();
    UnityEngine.InputSystem.Keyboard keyboard = null;
    PlayerInput probeInput = null;
    var oldKeyboard = UnityEngine.InputSystem.Keyboard.current;
    object blockOwner = new object();
    System.Action<SortingShelf> filled = null;
    try
    {
        UnityEngine.Application.runInBackground = true;
        UnityEngine.Time.timeScale = 1;
        manager.LockMute(false); manager.ToggleMute(false); SimpleAudioManager.SfxMuted = false;
        var library = manager.GameplaySounds;
        var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None);
        check(buttons.Length > 0 && buttons.All(b => b.GetComponent<UIButtonAudioFeedback>()), "menu button coverage including inactive panels: " + buttons.Length);
        var go = new UnityEngine.GameObject("Audio Button Probe",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Button)); roots.Add(go);
        var button = go.GetComponent<UnityEngine.UI.Button>(); int callbacks = 0;
        button.onClick.AddListener(() => callbacks++); UIButtonAudioFeedback.Ensure(button);
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (!eventSystem) { var es = new UnityEngine.GameObject("Audio EventSystem Probe",typeof(UnityEngine.EventSystems.EventSystem)); roots.Add(es); eventSystem=es.GetComponent<UnityEngine.EventSystems.EventSystem>(); }
        var pointer = new UnityEngine.EventSystems.PointerEventData(eventSystem) { button=UnityEngine.EventSystems.PointerEventData.InputButton.Left };
        int start=index();
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        check(count(start)==1 && callbacks==1,"pointer click: exactly one cue and original callback");
        start=index();
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,new UnityEngine.EventSystems.BaseEventData(eventSystem),UnityEngine.EventSystems.ExecuteEvents.submitHandler);
        check(count(start)==1 && callbacks==2,"keyboard/controller Submit: exactly one cue and original callback");
        button.interactable=false; start=index();
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,new UnityEngine.EventSystems.BaseEventData(eventSystem),UnityEngine.EventSystems.ExecuteEvents.submitHandler);
        check(count(start)==0 && callbacks==2,"disabled pointer and Submit stay silent"); button.interactable=true;
        start=index();
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(go,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
        check(count(start)==0 && callbacks==2,"hover and cancelled press stay silent");
        for(int i=0;i<20;i++) { go.SetActive(false);go.SetActive(true);UIButtonAudioFeedback.Ensure(button); }
        start=index();button.onClick.Invoke();check(count(start)==1 && callbacks==3,"20 panel reopen/rebind cycles do not accumulate listeners");
        button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{callbacks++;go.SetActive(false);});UIButtonAudioFeedback.Ensure(button);
        start=index();button.onClick.Invoke();check(count(start)==1 && callbacks==4,"panel-closing callback still produces one click");go.SetActive(true);
        button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>callbacks++);UIButtonAudioFeedback.Ensure(button);
        start=index();for(int i=0;i<8;i++)button.onClick.Invoke();check(count(start)==8 && callbacks==12,"rapid clicks retain one callback and cue per activation");
        UnityEngine.Time.timeScale=0; manager.ToggleMute(true); UnityEngine.AudioListener.pause=true;
        start=index();manager.PlayUI(library.UIClick);var ui=pool[start];
        check(count(start)==1 && ui.isPlaying && ui.spatialBlend==0 && ui.ignoreListenerPause,"UI plays in 2D during pause, listener pause and temporary gameplay mute");
        start=index();manager.PlayClipAt(library.WrongSFX,UnityEngine.Vector3.zero);check(count(start)==0,"temporary gameplay mute still suppresses world effects");
        SimpleAudioManager.SfxMuted=true;start=index();manager.PlayUI(library.UIClick);check(count(start)==0 && !ui.isPlaying,"saved SFX mute stops and suppresses UI");
        SimpleAudioManager.SfxMuted=false;manager.ToggleMute(false);UnityEngine.AudioListener.pause=false;UnityEngine.Time.timeScale=1;
        start=index();var reused=pool[start];manager.PlayUI(library.UIClick);
        for(int i=1;i<pool.Count;i++)manager.PlayUI(library.UIClick);
        manager.PlayClipAt(library.ShelfPlacementSounds[0],UnityEngine.Vector3.one);
        check(reused.spatialBlend==1 && !reused.ignoreListenerPause && reused.transform.position==UnityEngine.Vector3.one,"pooled UI voice resets to positioned world playback");
        for(int i=1;i<pool.Count;i++)manager.PlayClipAt(library.ShelfPlacementSounds[0],UnityEngine.Vector3.one);
        manager.PlayUI(library.UIClick);check(reused.spatialBlend==0 && reused.ignoreListenerPause,"pooled world voice resets to 2D UI playback");
        yield return new UnityEngine.WaitForSecondsRealtime(.3f);
        foreach(var observer in UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsSortMode.None))
            if(observer.enabled && (observer is CoinsManager || observer is SortingProgressTracker || observer is CarStoreUI))
            { pausedObservers.Add(observer); observer.enabled=false; }
        // Inactive isolated player: direct gameplay APIs run without registering a live player or touching saved inventory.
        var playerGo=new UnityEngine.GameObject("Audio Action Player Probe");playerGo.SetActive(false);roots.Add(playerGo);
        var player=playerGo.AddComponent<Player>();var inventory=playerGo.AddComponent<InventoryHandler>();var stash=playerGo.AddComponent<StashHandler>();
        var handler=playerGo.AddComponent<ItemsHandler>();var collector=playerGo.AddComponent<ItemCollectingBehaviour>();
        player.ItemHandler=handler;player.StashHandler=stash;player.ItemCollector=collector;handler.player=player;
        set(stash,"inventory",inventory);set(stash,"itemsHandler",handler);set(collector,"player",player);set(collector,"flightDuration",0f);
        typeof(ItemsHandler).GetMethod("Awake",flags).Invoke(handler,null);
        var data=UnityEditor.AssetDatabase.FindAssets("t:SortableItem_SO",new[]{"Assets/TinyRiders"}).Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<SortableItem_SO>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(d=>d.HandItem&&d.WorldItem&&d.PlacedOnShelfPrefab&&d.WorldItem.Data==d);
        inventory.SetCapacity(1); start=index();
        var world=UnityEngine.Object.Instantiate(data.WorldItem);roots.Add(world.gameObject);world.gameObject.SetActive(true);
        bool picked=collector.TryCollectItem(world);
        check(picked && inventory.Count==1 && !world.gameObject.activeSelf && count(start)==1 && library.CollectingSounds.Any(s=>s.Clip==pool[start].clip),"successful real world pickup: one plastic cue after collection (picked="+picked+", count="+inventory.Count+", voices="+count(start)+")");
        start=index();bool refused=collector.TryCollectItem(data);
        check(!refused && count(start)==1 && pool[start].clip==library.WrongSFX.Clip,"full inventory: one distinct soft refusal (accepted="+refused+", voices="+count(start)+")");
        var worldsBefore=UnityEngine.Object.FindObjectsByType<WorldItemBase>(UnityEngine.FindObjectsSortMode.None).ToHashSet();
        start=index();typeof(ItemsHandler).GetMethod("ExecuteThrow",flags).Invoke(handler,new object[]{.5f});
        check(count(start)==0,"throw pull animation starts silently");
        yield return new UnityEngine.WaitForSecondsRealtime(.25f);
        check(count(start)==1 && library.ThrowingSounds.Any(s=>s.Clip==pool[start].clip),"actual throw: one whoosh at release");
        foreach(var thrown in UnityEngine.Object.FindObjectsByType<WorldItemBase>(UnityEngine.FindObjectsSortMode.None)) if(!worldsBefore.Contains(thrown))roots.Add(thrown.gameObject);
        collector.TryCollectItem(data);
        player.Input=playerGo.AddComponent<PlayerInput>();probeInput=player.Input;player.Input.Enable();
        keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));UnityEngine.InputSystem.InputSystem.Update();
        ItemsHandler.BlockThrow(blockOwner,data);start=index();
        bool consumed=(bool)typeof(ItemsHandler).GetMethod("UpdateThrowInput",flags).Invoke(handler,null);
        check(consumed && count(start)==1 && pool[start].clip==library.WrongSFX.Clip && handler.CurrentEquipped!=null,"blocked real throw input: one refusal, no release whoosh");ItemsHandler.UnblockThrow(blockOwner);
        player.Input.Disable();
        // Real shelf tweens, wrong feedback, completion and silent restore on isolated shelves.
        SortingShelf MakeShelf(string name,int spots,SortableItem_SO accepted)
        {
            var root=new UnityEngine.GameObject(name);root.SetActive(false);roots.Add(root);
            var shelf=root.AddComponent<SortingShelf>();var transforms=new System.Collections.Generic.List<UnityEngine.Transform>();
            for(int i=0;i<spots;i++){var spot=new UnityEngine.GameObject("Spot "+i);spot.transform.SetParent(root.transform);spot.transform.localPosition=UnityEngine.Vector3.right*i;transforms.Add(spot.transform);}
            set(shelf,"placingSpots",transforms);set(shelf,"acceptedItem",accepted);root.SetActive(true);return shelf;
        }
        PlacedOnShelfItemBase MakePlaced()
        {
            var item=UnityEngine.Object.Instantiate(data.PlacedOnShelfPrefab);typeof(PlacedOnShelfItemBase).GetMethod("Init",flags).Invoke(item,new object[]{data});return item;
        }
        var correct=MakeShelf("Audio Correct Shelf Probe",2,data);int completions=0;filled=s=>{if(s==correct)completions++;};SortingShelf.OnShelfFilled+=filled;
        start=index();var first=MakePlaced();correct.AddItem(first,0);var second=MakePlaced();correct.AddItem(second,1);
        check(count(start)==0 && !first.IsSettled && !second.IsSettled,"placement flights are silent before shelf contact");
        yield return new UnityEngine.WaitForSecondsRealtime(.7f);
        check(first.IsSettled && second.IsSettled && count(start)==3 && completions==1 && correct.IsCompleted,"two landing clacks and exactly one completion chime");
        yield return new UnityEngine.WaitForSecondsRealtime(.3f);check(count(start)==3 && completions==1,"completion does not replay after feedback finishes");
        var wrong=MakeShelf("Audio Wrong Shelf Probe",1,null);start=index();var bad=MakePlaced();wrong.AddItem(bad,0);
        check(count(start)==1 && bad.IsWrong && pool[start].clip==library.WrongSectionSFX.Clip,"wrong placement: one warning with red feedback");
        yield return new UnityEngine.WaitForSecondsRealtime(.7f);check(count(start)==1 && !wrong.IsCompleted,"wrong placement lands without success or completion sound");
        var restored=MakeShelf("Audio Restored Shelf Probe",1,data);start=index();bool loaded=restored.RestoreItem(0,data);
        check(loaded && restored.IsCompleted && count(start)==0,"saved shelf restoration remains silent");
        completed=true;
    }
    finally
    {
        if(filled!=null)SortingShelf.OnShelfFilled-=filled;
        ItemsHandler.UnblockThrow(blockOwner);
        if(keyboard!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
        if(oldKeyboard!=null)oldKeyboard.MakeCurrent();
        // Unity does not call OnDestroy on a GameObject that was never active.
        if(probeInput)typeof(PlayerInput).GetMethod("OnDestroy",flags).Invoke(probeInput,null);
        foreach(var root in roots)if(root)UnityEngine.Object.Destroy(root);
        foreach(var observer in pausedObservers)if(observer)observer.enabled=true;
        UnityEngine.AudioListener.pause=oldListenerPause;UnityEngine.Time.timeScale=oldScale;
        manager.LockMute(false);manager.ToggleMute(false);SimpleAudioManager.SfxMuted=oldSfx;
        if(!hadSfx)UnityEngine.PlayerPrefs.DeleteKey("Audio_SfxMuted");UnityEngine.PlayerPrefs.Save();
        UnityEngine.Application.runInBackground=oldBackground;
        if(!completed)lines.Add("FAIL probe interrupted; inspect Unity console");
        System.IO.File.WriteAllLines("IntegrationEvidence/AudioPackageRefresh/action-runtime-checks.txt",lines);
    }
}
manager.StartCoroutine(Probe());return "Action and UI playback probe started";
