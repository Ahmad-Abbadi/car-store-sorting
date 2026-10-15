using System.Linq;
using TinyRiders;
using TMPro;
using UnityEngine;

/// <summary>Reserve the band above raised shelf sculptures for existing room signs.</summary>
public static class ThemedShelfSignClearance
{
    public static void Apply()
    {
        var session=Object.FindFirstObjectByType<ThemedCollectionSession>();
        if(session==null||session.roomLayout==null||ThemedShelfArtBuilder.Catalog==null||!ThemedShelfArtBuilder.Catalog.entries.Any(e=>e.signatureRevision>0))return;
        Vector3 origin=session.roomLayout.origin;
        var transforms=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach(var t in transforms.Where(t=>t.name.StartsWith("Category ")))
        {
            var p=t.position;p.y=origin.y+2.67f;
            // The entrance category and directory otherwise overlap in the same sightline.
            if(t.name=="Category 1"||t.name=="Category Name 0")p.x=origin.x+.6f;
            t.position=p;
        }
        var board=transforms.FirstOrDefault(t=>t.name=="Directory Board");
        if(board!=null){var p=board.position;p.y=origin.y+2.78f;board.position=p;var s=board.localScale;s.y=.62f;board.localScale=s;}
        var directory=transforms.FirstOrDefault(t=>t.name=="Store Directory");
        if(directory!=null)
        {
            var p=directory.position;p.y=origin.y+2.78f;directory.position=p;
            var label=directory.GetComponent<TextMeshPro>();label.rectTransform.sizeDelta=new Vector2(6.2f,.56f);
            label.enableAutoSizing=true;label.fontSizeMin=.8f;label.fontSizeMax=1.45f;
        }
    }
}
