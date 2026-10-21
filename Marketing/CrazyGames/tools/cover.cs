// Evaluate this body in the isolated Unity Play session after the footage.
var old = UnityEngine.GameObject.Find("Marketing Cover");
if(old)UnityEngine.Object.DestroyImmediate(old);
foreach(var world in WorldItemBase.All.ToArray())if(world)world.gameObject.SetActive(false);
foreach(var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
    if(c.renderMode!=UnityEngine.RenderMode.WorldSpace)c.enabled=false;
var cam=UnityEngine.Camera.main;
cam.fieldOfView=58;
var shelf=UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None).Where(s=>s.GetComponent<TinyRiders.ThemeShelfBinding>()?.themeId=="TH_WIZARD").OrderBy(s=>s.SaveId).First();
var center=Enumerable.Range(0,shelf.SpotCount).Select(i=>shelf.GetSpot(i).position).Aggregate(UnityEngine.Vector3.zero,(a,b)=>a+b)/shelf.SpotCount;
var eye=center+shelf.transform.forward*(UnityEngine.Screen.height>UnityEngine.Screen.width?3.9f:2.7f)+UnityEngine.Vector3.up*.7f+shelf.transform.right*.15f;
cam.transform.SetPositionAndRotation(eye,UnityEngine.Quaternion.LookRotation(center-eye));
var root=new UnityEngine.GameObject("Marketing Cover");
root.transform.SetParent(cam.transform,false);
var overlayObj=new UnityEngine.GameObject("Cover foreground camera",typeof(UnityEngine.Camera));overlayObj.transform.SetParent(root.transform,false);
var overlay=overlayObj.GetComponent<UnityEngine.Camera>();overlay.CopyFrom(cam);overlay.cullingMask=1<<31;overlay.depth=cam.depth+1;
var overlayData=overlayObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
overlayData.renderType=UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
var serializedOverlay=new UnityEditor.SerializedObject(overlayData);serializedOverlay.FindProperty("m_ClearDepth").boolValue=true;serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
var baseData=cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();baseData.cameraStack.RemoveAll(c=>c==null);baseData.cameraStack.Add(overlay);
cam.cullingMask&=~(1<<31);
string[] themes={"TH_PIRATE","TH_SPACE","TH_WIZARD"};
bool portrait=UnityEngine.Screen.height>UnityEngine.Screen.width;
bool square=UnityEngine.Screen.height==UnityEngine.Screen.width;
for(int i=0;i<3;i++)
{
    var entry=TinyRiders.ThemedCollectionSession.Instance.ActiveDefinitions.First(e=>e.themeId==themes[i] && e.sortingItem.PlacedOnShelfPrefab!=null);
    var pivot=new UnityEngine.GameObject("Cover Car "+themes[i]);pivot.transform.SetParent(root.transform,false);
    var car=UnityEngine.Object.Instantiate(entry.sortingItem.PlacedOnShelfPrefab.gameObject,pivot.transform);
    foreach(var tr in car.GetComponentsInChildren<UnityEngine.Transform>(true))tr.gameObject.layer=31;
    foreach(var b in car.GetComponentsInChildren<UnityEngine.Behaviour>(true))b.enabled=false;
    foreach(var b in car.GetComponentsInChildren<UnityEngine.Collider>(true))b.enabled=false;
    foreach(var p in car.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))p.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
    car.transform.localPosition=UnityEngine.Vector3.zero;
    car.transform.localRotation=UnityEngine.Quaternion.Euler(15, i==1?-35:-55, i==0?-8:i==2?8:0);
    var renderers=car.GetComponentsInChildren<UnityEngine.Renderer>().Where(r=>r.enabled&&!(r is UnityEngine.ParticleSystemRenderer)).ToArray();
    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
    float desired=portrait?.85f:square?1.0f:1.38f;
    float factor=desired/bounds.size.x;car.transform.localScale*=factor;
    bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
    car.transform.position+=pivot.transform.position-bounds.center;
    if(portrait)pivot.transform.localPosition=new UnityEngine.Vector3(i==1?.28f:-.22f,-.05f-i*.49f,2.8f);
    else pivot.transform.localPosition=new UnityEngine.Vector3((i-1)*(square?.83f:1.35f),i==1?-.9f:-.68f,2.8f);
}
var canvasObj=new UnityEngine.GameObject("Cover Logo",typeof(UnityEngine.Canvas));canvasObj.transform.SetParent(root.transform,false);
var canvas=canvasObj.GetComponent<UnityEngine.Canvas>();canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
var logoObj=new UnityEngine.GameObject("Toy Car Sort",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.RawImage));logoObj.transform.SetParent(canvasObj.transform,false);
var logo=logoObj.GetComponent<UnityEngine.UI.RawImage>();logo.texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(ToyCarSortBranding.LogoPath);
var rect=logo.rectTransform;rect.anchorMin=rect.anchorMax=new UnityEngine.Vector2(.5f,1);rect.pivot=new UnityEngine.Vector2(.5f,1);
float width=UnityEngine.Screen.width*(portrait?.94f:square?.78f:.43f);rect.sizeDelta=new UnityEngine.Vector2(width,width*2/3);rect.anchoredPosition=new UnityEngine.Vector2(0,-UnityEngine.Screen.height*.025f);
UnityEngine.Cursor.visible=false;
return "Cover staged";
