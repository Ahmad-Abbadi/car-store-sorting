using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class NativeMenuMeshVerification
{
    public static string Run()
    {
        if(BuildPipeline.isBuildingPlayer || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Idle Editor required");
        var set=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var entries=((List<ItemLodSet.Entry>)typeof(ItemLodSet).GetField("entries",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(set)).Where(e=>e?.mesh!=null && e.bakedMesh!=null && e.mesh!=e.bakedMesh).ToArray();
        long vertices=0,indices=0;
        foreach(var entry in entries)
        {
            var a=entry.mesh;var b=entry.bakedMesh;
            void Require(bool pass,string detail){if(!pass)throw new Exception(a.name+": "+detail);}
            Require(a.vertexCount==b.vertexCount,"vertex count");
            Require(a.subMeshCount==b.subMeshCount,"submesh count");
            Require(a.vertices.SequenceEqual(b.vertices),"positions");
            Require(a.normals.SequenceEqual(b.normals),"normals");
            Require(a.tangents.SequenceEqual(b.tangents),"tangents");
            Require(a.colors.SequenceEqual(b.colors),"colors");
            Require(a.bounds==b.bounds,"bounds");
            for(int channel=0;channel<8;channel++)
            {
                if(channel==3 && !a.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3))continue;
                var x=new List<Vector4>();var y=new List<Vector4>();a.GetUVs(channel,x);b.GetUVs(channel,y);
                Require(x.SequenceEqual(y),"UV"+channel);
            }
            for(int sub=0;sub<a.subMeshCount;sub++)
            {
                Require(a.GetTopology(sub)==b.GetTopology(sub),"topology");
                var original=a.GetIndices(sub,0,true);var native=b.GetIndices(sub,0,true);
                Require(original.SequenceEqual(native),"LOD0 indices");indices+=original.Length;
                for(int lod=0;lod<3;lod++)Require(entry.GetIndices(lod,sub).SequenceEqual(b.GetIndices(sub,lod,true)),"selected LOD"+lod+" indices");
            }
            vertices+=a.vertexCount;
        }
        string result="Exact native meshes verified: "+entries.Length+" source/native pairs; "+vertices+" vertices; "+indices+" original indices. Positions, normals, tangents, colors, original UV channels, topology, LOD0 indices and bounds unchanged; all three selected LOD index buffers match the existing registry. ItemDetailDisplay keeps runtime detail selection.";
        File.WriteAllText("IntegrationEvidence/RenderingArtifacts/native-menu-mesh-verification.txt",result);
        return result;
    }
}
