var box = UnityEngine.Object.FindFirstObjectByType<UpgradeBox>();
if (!box) return "No upgrade machine in active scene";
var p = box.transform.position;
var go = new GameObject("UpgradeEvidenceCamera");
var cam = go.AddComponent<Camera>();
cam.transform.position = p + new Vector3(2.35f,2.05f,3.8f);
cam.transform.LookAt(p + Vector3.up*1.25f);
cam.fieldOfView = 38; cam.nearClipPlane = .05f; cam.farClipPlane = 80;
var rt = new RenderTexture(1200,1200,24);
var old = RenderTexture.active;
cam.targetTexture=rt;
try
{
    cam.Render(); RenderTexture.active=rt;
    var tex=new Texture2D(1200,1200,TextureFormat.RGB24,false);
    tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();
    System.IO.File.WriteAllBytes("IntegrationEvidence/UpgradeStation/Blender/before.png",tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex);
    cam.transform.position=p+new Vector3(2.9f,2.2f,6.6f);
    cam.transform.LookAt(p+Vector3.up*1.25f);cam.fieldOfView=55;cam.Render();
    var wide=new Texture2D(1200,1200,TextureFormat.RGB24,false);wide.ReadPixels(new Rect(0,0,1200,1200),0,0);wide.Apply();
    System.IO.File.WriteAllBytes("IntegrationEvidence/UpgradeStation/Blender/before-shop.png",wide.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(wide);
}
finally
{
    RenderTexture.active=old;cam.targetTexture=null;
    UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
}
var filters=box.GetComponentsInChildren<MeshFilter>(true);
var report="renderers="+box.GetComponentsInChildren<Renderer>(true).Length+" triangles="+filters.Where(x=>x.sharedMesh).Sum(x=>x.sharedMesh.triangles.Length/3);
System.IO.File.WriteAllText("IntegrationEvidence/UpgradeStation/Blender/before-stats.txt",report);
return report;

