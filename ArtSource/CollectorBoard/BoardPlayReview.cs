using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using TinyRiders.Quests;
using Object = UnityEngine.Object;

public static class BoardPlayReview
{
    const string Output = "IntegrationEvidence/CollectorBoard";
    public static string LiveAudit()
    {
        var board=Object.FindFirstObjectByType<CollectorQuestBoard>();var manager=CollectorQuestManager.Instance;
        if(!Application.isPlaying||!manager||!manager.Ready)throw new Exception("Play session not ready");
        if(!SessionState.GetBool("Quest240QA.Active",false))throw new Exception("Use the isolated quest QA session");
        var privateFlags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var storage=(string)typeof(CollectorQuestManager).GetField("storageKey",privateFlags).GetValue(manager);
        if(!storage.StartsWith(SessionState.GetString("Quest240QA.Prefix","missing")))throw new Exception("Career not isolated");
        var origin=board.transform.TransformPoint(new Vector3(0,1.6f,-2.5f));
        var handler=Object.FindFirstObjectByType<InteractionHandler>();int mask=new SerializedObject(handler).FindProperty("interactableMask").intValue;
        if(!Physics.Raycast(origin,board.transform.forward,out var hit,3,mask)||hit.collider.GetComponent<CollectorQuestBoard>()!=board)throw new Exception("Interaction ray missed board");
        board.OnInteract(handler);if(!board.interfaceUI.IsOpen)throw new Exception("Board did not open menu");
        board.interfaceUI.Close();if(board.interfaceUI.IsOpen)throw new Exception("Menu did not close");
        var engine=(CollectorQuestProgress)typeof(CollectorQuestManager).GetField("engine",privateFlags).GetValue(manager);
        var record=engine.Career.quests.First(q=>!q.claimed&&!q.challenge);
        record.complete=true;record.progress=record.target;
        typeof(CollectorQuestManager).GetMethod("Publish",privateFlags).Invoke(manager,null);
        if(!board.rewardIndicator.activeSelf)throw new Exception("Reward medallion did not appear");
        var ready=manager.Quests.Where(q=>q.Complete&&!q.Claimed).Select(q=>q.Id).ToArray();
        foreach(var id in ready)if(!manager.Claim(id))throw new Exception("Claim failed");
        if(manager.ClaimableCount!=0||board.rewardIndicator.activeSelf)throw new Exception("Reward medallion did not clear");
        foreach(string language in new[]{"en","ar"})
        {
            Localizer.SetLanguage(language);
            foreach(var text in board.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                text.ForceMeshUpdate();if(text.isTextOverflowing||string.IsNullOrWhiteSpace(text.text))throw new Exception("Board label does not fit: "+language+"/"+text.name);
            }
        }
        Localizer.SetLanguage("en");
        var result="PASS: isolated career; board interaction ray, menu open/close, reward-ready medallion, actual reward claims, indicator clearing, English/Arabic nonempty labels without overflow. Frame "+Time.frameCount;
        File.WriteAllText(Output+"/playmode-audit.txt",result);return result;
    }
}
