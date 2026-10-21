#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TinyRiders;

// Editor-only marketing tooling. No authored scene or gameplay asset is saved.
[InitializeOnLoad]
public static class CrazyGamesCapture
{
    public const string Root = "Marketing/CrazyGames";
    const string Active = "CrazyGames.Capture.Active";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    [Serializable] public class Pref { public string key, kind, text; public int integer; public float number; }
    static CrazyGamesCapture()
    {
        SceneManager.sceneLoaded += Isolate;
    }
    static string RegistryPath => "Software\\Unity\\UnityEditor\\" + Application.companyName + "\\" + Application.productName;
    public static List<Pref> ReadPrefs()
    {
        var list = new List<Pref>();
        using (var registry = Registry.CurrentUser.OpenSubKey(RegistryPath))
        {
            if (registry == null) return list;
            foreach (var name in registry.GetValueNames())
            {
                var key = Regex.Replace(name, "_h[0-9]+$", "");
                if (!PlayerPrefs.HasKey(key)) continue;
                var p = new Pref {key=key};
                string value = PlayerPrefs.GetString(key, "__MISSING_MARKETING__");
                if (value != "__MISSING_MARKETING__") {p.kind="string";p.text=value;}
                else if (PlayerPrefs.GetInt(key,int.MinValue) != int.MinValue) {p.kind="int";p.integer=PlayerPrefs.GetInt(key);}
                else {p.kind="float";p.number=PlayerPrefs.GetFloat(key);}
                list.Add(p);
            }
        }
        return list.OrderBy(p=>p.key).ToList();
    }
    public static string Begin()
    {
        if (EditorApplication.isPlaying || SessionState.GetBool(Active,false)) throw new Exception("Capture already active or Editor playing");
        if (SceneManager.GetActiveScene().isDirty) throw new Exception("Save current scene before capture");
        Directory.CreateDirectory(Root+"/Backup");
        PlayerPrefs.Save();
        File.WriteAllText(Root+"/Backup/playerprefs.json",JsonConvert.SerializeObject(ReadPrefs(),Formatting.Indented));
        SessionState.SetString("CrazyGames.Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool("CrazyGames.Background",Application.runInBackground);
        SessionState.SetBool(Active,true);
        EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/Level.unity");
        Application.runInBackground=true;
        EditorApplication.isPlaying=true;
        return "Capture isolation armed; entering Level";
    }
    static void Isolate(Scene scene, LoadSceneMode mode)
    {
        if (!SessionState.GetBool(Active,false)) return;
        foreach (var type in new[]{typeof(LevelSaveManager),typeof(PlayerSavePosition),typeof(CoinsManager)})
            foreach (var obj in UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None))
                type.GetField("saveKey",Flags)?.SetValue(obj,"CrazyGames.Capture."+type.Name);
        var s=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
        if(s) {s.suspendPersistenceForTests=true;s.completeStoreWhenSorted=false;}
    }
    public static string Restore()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop play mode before restoring");
        var before=JsonConvert.DeserializeObject<List<Pref>>(File.ReadAllText(Root+"/Backup/playerprefs.json"));
        foreach(var p in ReadPrefs()) if(!before.Any(b=>b.key==p.key)) PlayerPrefs.DeleteKey(p.key);
        foreach(var p in before)
            if(p.kind=="string")PlayerPrefs.SetString(p.key,p.text);
            else if(p.kind=="int")PlayerPrefs.SetInt(p.key,p.integer);
            else PlayerPrefs.SetFloat(p.key,p.number);
        PlayerPrefs.Save();
        bool same=JsonConvert.SerializeObject(before)==JsonConvert.SerializeObject(ReadPrefs());
        File.WriteAllText(Root+"/Backup/restoration.json",JsonConvert.SerializeObject(new{playerPrefsRestored=same,keys=before.Count},Formatting.Indented));
        SessionState.SetBool(Active,false);
        Application.runInBackground=SessionState.GetBool("CrazyGames.Background",true);
        RestoreSize();
        EditorSceneManager.OpenScene(SessionState.GetString("CrazyGames.Scene","Assets/00_Project/00_Scenes/Level.unity"));
        return "PlayerPrefs restored="+same;
    }
    public static void SetSize(int width,int height)
    {
        var asm=typeof(Editor).Assembly;
        var sizesType=asm.GetType("UnityEditor.GameViewSizes");
        var instance=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(instance,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
        var viewType=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);
        if(!SessionState.GetBool("CrazyGames.SizeSaved",false))
        {
            SessionState.SetInt("CrazyGames.Size",(int)viewType.GetProperty("selectedSizeIndex",Flags).GetValue(view));
            SessionState.SetInt("CrazyGames.CustomCount",(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null));
            SessionState.SetBool("CrazyGames.SizeSaved",true);
        }
        var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),Flags,null,new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,height,"CrazyGames "+width+"x"+height},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        viewType.GetProperty("selectedSizeIndex",Flags).SetValue(view,count-1);view.Focus();
    }
    static void RestoreSize()
    {
        if(!SessionState.GetBool("CrazyGames.SizeSaved",false))return;
        var asm=typeof(Editor).Assembly;var vt=asm.GetType("UnityEditor.GameView");
        vt.GetProperty("selectedSizeIndex",Flags).SetValue(EditorWindow.GetWindow(vt),SessionState.GetInt("CrazyGames.Size",0));
        var st=asm.GetType("UnityEditor.GameViewSizes");var sizes=typeof(ScriptableSingleton<>).MakeGenericType(st).GetProperty("instance").GetValue(null);
        var group=st.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
        for(int i=(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null)-1;i>=SessionState.GetInt("CrazyGames.CustomCount",0);i--)group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});
        SessionState.SetBool("CrazyGames.SizeSaved",false);
    }
    public static string StartTake(string orientation, bool preview=false)
    {
        if(!EditorApplication.isPlaying||!SessionState.GetBool(Active,false))throw new Exception("Begin isolation first");
        if(UnityEngine.Object.FindFirstObjectByType<CrazyGamesCaptureRunner>())throw new Exception("Runner already exists");
        SetSize(orientation=="landscape"?1920:1080,orientation=="landscape"?1080:1620);
        new GameObject("Editor Marketing Capture").AddComponent<CrazyGamesCaptureRunner>().Begin(orientation,preview);
        return "Recording "+orientation;
    }
}

public class CrazyGamesCaptureRunner:MonoBehaviour
{
    string folder; bool preview; Camera cam; ThemedCollectionSession session;
    readonly List<object> events=new List<object>();
    public void Begin(string orientation,bool test){folder=CrazyGamesCapture.Root+"/Frames/"+orientation;preview=test;StartCoroutine(GuardedRun());}
    IEnumerator GuardedRun()
    {
        var routine=Run();
        while(true)
        {
            bool next=false;Exception error=null;
            try{next=routine.MoveNext();}catch(Exception ex){error=ex;}
            if(error!=null)
            {
                Time.captureFramerate=0;
                File.WriteAllText(CrazyGamesCapture.Root+"/status.txt","FAILED: "+error);
                Debug.LogException(error);Destroy(gameObject);yield break;
            }
            if(!next)yield break;
            yield return routine.Current;
        }
    }
    IEnumerator Run()
    {
        Directory.CreateDirectory(folder);Time.timeScale=1;Time.captureFramerate=30;
        Application.runInBackground=true;
        for(int i=0;i<30;i++)yield return null;
        session=ThemedCollectionSession.Instance;session.suspendPersistenceForTests=true;
        cam=Camera.main;
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if(canvas.name=="Controls Hint Canvas"||canvas.name=="CollectorQuestUI"||canvas.name=="EquipmentItems_UI Canvas")canvas.gameObject.SetActive(false);
            if(canvas.name=="HUD Counters Canvas")
            {
                var prompt=canvas.transform.Find("ScreenSpaceActionPrompt");if(prompt)prompt.gameObject.SetActive(false);
                var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();if(scaler)scaler.referenceResolution=new Vector2(2560,1440);
            }
        }
        foreach(var b in cam.GetComponents<Behaviour>())if(b.GetType().Name=="CinemachineBrain")b.enabled=false;
        Player.Instance.Look.enabled=false;
        Player.Instance.Input.enabled=false;
        session.DebugResetCollection();
        yield return null;yield return null;
        var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
            .Where(s=>s.GetComponent<ThemeShelfBinding>()!=null).OrderBy(s=>s.SaveId).ToArray();
        string[] themes={"TH_PIRATE","TH_SPACE","TH_WIZARD"};
        var selected=themes.Select(t=>shelves.First(s=>s.GetComponent<ThemeShelfBinding>().themeId==t)).ToArray();
        foreach(var s in selected)
        {
            int fill=s==selected[0]?Math.Min(7,s.SpotCount-1):s==selected[1]?Math.Min(10,s.SpotCount-1):s.SpotCount-1;
            for(int j=0;j<fill;j++) {var world=FindCar(s);if(!world)throw new Exception("Not enough matching cars");Place(world,s,j);yield return null;}
        }
        var heroShelf=selected[2];
        foreach(var s in shelves.Where(s=>s!=heroShelf&&Vector3.Distance(s.transform.position,heroShelf.transform.position)<4.5f))
            for(int j=0;j<s.SpotCount;j++){var w=FindCar(s);if(w){Place(w,s,j);yield return null;}}
        for(int i=0;i<45;i++)yield return null;
        int frame=0;
        for(int shot=0;shot<3;shot++)
        {
            var shelf=selected[shot];int spot=Enumerable.Range(0,shelf.SpotCount).First(shelf.IsSpotFree);
            var world=FindCar(shelf);var target=shelf.GetSpot(spot).position;
            Vector3 front=shelf.transform.forward;
            Vector3 eye=target+front*(folder.EndsWith("portrait")?2.8f:2.1f)+Vector3.up*.65f;
            Vector3 pile=target+front*1.1f;pile.y=.45f;
            world.transform.position=pile;if(world.Body){world.Body.isKinematic=true;world.Body.position=pile;}
            for(int n=0;n<150;n++)
            {
                float t=n/149f;
                Vector3 aim=Vector3.Lerp(pile,target,Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.12f)/.36f)));
                cam.transform.SetPositionAndRotation(eye+front*(.16f*(1-t)),Quaternion.LookRotation(aim-eye));
                cam.fieldOfView=folder.EndsWith("portrait")?64:58;
                if(n==30){bool ok=Player.Instance.ItemCollector.TryCollectItem(world);events.Add(new{shot,action="pickup",ok});if(!ok)throw new Exception("Pickup failed");}
                if(n==90){if(!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var item))throw new Exception("No held item");shelf.AddItem(item,spot);events.Add(new{shot,action="placement",sorted=session.Sorted});}
                Cursor.visible=false;
                yield return new WaitForEndOfFrame();
                SaveFrame(frame++);
            }
            events.Add(new{shot,completed=shelf.IsCompleted});
        }
        // The last shot shows the actual finished shelf, with a slow lateral reveal.
        var hero=selected[2];var center=Enumerable.Range(0,hero.SpotCount).Select(i=>hero.GetSpot(i).position).Aggregate(Vector3.zero,(a,b)=>a+b)/hero.SpotCount;
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.renderMode!=RenderMode.WorldSpace)canvas.enabled=false;
        var direction=hero.transform.forward;
        for(int n=0;n<150;n++)
        {
            var pos=center+direction*(folder.EndsWith("portrait")?3.9f:2.7f)+Vector3.up*.7f+hero.transform.right*Mathf.Lerp(-.4f,.4f,n/149f);
            cam.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(center-pos));
            Cursor.visible=false;yield return new WaitForEndOfFrame();SaveFrame(frame++);
        }
        Time.captureFramerate=0;
        File.WriteAllText(folder+"/capture.json",JsonConvert.SerializeObject(new{frames=frame,width=Screen.width,height=Screen.height,events},Formatting.Indented));
        File.WriteAllText(CrazyGamesCapture.Root+"/status.txt","Complete: "+folder);
        Destroy(gameObject);
    }
    WorldItemBase FindCar(SortingShelf shelf)=>WorldItemBase.All.FirstOrDefault(w=>w&&w.gameObject.activeInHierarchy&&session.Definition(w.Data)?.themeId==shelf.GetComponent<ThemeShelfBinding>().themeId);
    void Place(WorldItemBase world,SortingShelf shelf,int slot)
    {
        if(!Player.Instance.ItemCollector.TryCollectItem(world)||!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var item))throw new Exception("Staging pickup failed");
        shelf.AddItem(item,slot);
    }
    void SaveFrame(int n)
    {
        if(preview&&n%30!=0)return;
        var tex=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(folder+"/"+n.ToString("D5")+".png",tex.EncodeToPNG());Destroy(tex);
        if(n%30==0)File.WriteAllText(CrazyGamesCapture.Root+"/status.txt",folder+" frame "+n);
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
#endif
