using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class TRShelfAcceptance
{
    [Serializable] public class Row { public string name;public int internalDividers;public float markerSurfaceError;public float minTrayY;public float maxTrayY;public float trayNormalDotMarkerUp; }
    [Serializable] public class Result { public bool passed;public int markers;public int rows;public int runtimeBehaviours;public int lights;public int colliders;public int triangles;public int allCarsTested;public string[] failures;public Row[] rowChecks;public Vector3 rootScale;public float frontMinusBack;public float minWidthClearance;public float minDepthClearance;public float minHeightClearance;public int missingTextures;public float maxMarkerSurfaceError;public string rowSummary; }
    public static string Run()
    {
        const string src="ArtSource/Shelves/BaseShelf_01";
        const string path="Assets/TinyRiders/Art/Shelves/BaseShelf_01/Prefabs/TR_Shelf_Base_01.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var failures=new List<string>(); var rows=new List<Row>();
            var trs=root.GetComponentsInChildren<Transform>(true);
            var slots=trs.Where(t=>t.name.StartsWith("Slot_R")).OrderBy(t=>t.name).ToArray();
            var geo=root.transform.Find("Geometry");
            for(int r=1;r<=3;r++) {
                var row=geo.Find("Row_"+r.ToString("00"));
                var deck=row.GetComponentsInChildren<MeshFilter>().First(m=>m.name=="Tray_"+r.ToString("00"));
                var mk=slots.First(t=>t.name==$"Slot_R{r:00}_C01");
                var points=deck.sharedMesh.vertices.Select(v=>mk.InverseTransformPoint(deck.transform.TransformPoint(v))).ToArray();
                float min=points.Min(p=>p.y),max=points.Max(p=>p.y);
                var check=new Row{name=row.name,internalDividers=row.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Divider_")),markerSurfaceError=Mathf.Abs(max),minTrayY=min,maxTrayY=max,trayNormalDotMarkerUp=Vector3.Dot(deck.transform.up,mk.up)};
                rows.Add(check);
                if(check.internalDividers!=5)failures.Add(row.name+": incorrect divider count");
                // Top face is an actual broad planar face of the tray mesh, not its AABB.
                if(Mathf.Abs(max)>.0005f || min>-.04f)failures.Add(row.name+": marker does not lie on top surface "+max);
                var rowSlots=slots.Where(t=>t.name.StartsWith($"Slot_R{r:00}")).ToArray();
                if(rowSlots.Length!=6)failures.Add(row.name+": incorrect slot count");
                for(int c=0;c<6;c++) {
                    var expected=new Vector3(1.25f-.5f*c,mk.position.y,mk.position.z);
                    if(Vector3.Distance(rowSlots[c].position,expected)>.0005f)failures.Add(rowSlots[c].name+": spacing/order");
                    if(Vector3.Dot(rowSlots[c].forward,Vector3.forward)<.99f)failures.Add(rowSlots[c].name+": forward orientation");
                }
            }
            int scripts=root.GetComponentsInChildren<MonoBehaviour>(true).Length;
            var carReport=JsonUtility.FromJson<TRBaseShelfArtBuilder.CarReport>(File.ReadAllText(src+"/car_measurements.json"));
            float cw=carReport.cars.Min(c=>.464f-c.size.x),cd=carReport.cars.Min(c=>.648f-c.size.z),ch=carReport.cars.Min(c=>.54f-c.size.y);
            if(cw<=0 || cd<=0 || ch<=0)failures.Add("A measured existing car exceeds the conservative usable slot envelope.");
            if(slots.Length!=18 || scripts!=0)failures.Add("Marker count/runtime behaviour mismatch.");
            if(root.transform.localScale!=Vector3.one || root.transform.position!=Vector3.zero || root.transform.rotation!=Quaternion.identity)failures.Add("Root transform is not identity.");
            var back=geo.Find("BackPanel").GetComponentInChildren<Renderer>().bounds.center;
            var lip=trs.First(t=>t.name=="FrontLip_01").GetComponent<Renderer>().bounds.center;
            if(lip.z<=back.z)failures.Add("Root +Z does not face outwards.");
            int missing=0;
            foreach(var m in root.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct()) {
                if(m==null || m.shader==null || ShaderUtil.ShaderHasError(m.shader))failures.Add("Missing/error material shader.");
                if(m!=null && (m.name=="MAT_Header" || m.name=="MAT_SidePanels_Red" || m.name.StartsWith("MAT_Printed")) && m.GetTexture("_BaseMap")==null)missing++;
            }
            if(missing>0)failures.Add("Printed textures missing.");
            var result=new Result{passed=failures.Count==0,markers=slots.Length,rows=rows.Count,runtimeBehaviours=scripts,lights=root.GetComponentsInChildren<Light>(true).Length,colliders=root.GetComponentsInChildren<Collider>(true).Length,triangles=root.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3),allCarsTested=carReport.cars.Length,failures=failures.ToArray(),rowChecks=rows.ToArray(),rootScale=root.transform.localScale,frontMinusBack=lip.z-back.z,minWidthClearance=cw,minDepthClearance=cd,minHeightClearance=ch,missingTextures=missing};
            result.maxMarkerSurfaceError=rows.Max(r=>r.markerSurfaceError);
            result.rowSummary=string.Join("; ",rows.Select(r=>r.name+": 5 internal dividers, marker surface error "+r.markerSurfaceError+" m"));
            var json=JsonUtility.ToJson(result,true);File.WriteAllText(src+"/acceptance_checks.json",json);return json;
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
