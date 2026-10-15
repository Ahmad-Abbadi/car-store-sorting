using System;
using System.IO;
using UnityEditor;
[InitializeOnLoad]
public static class SortingIntegrationBootstrap
{
    static SortingIntegrationBootstrap()
    {
        EditorApplication.delayCall += () => {
            if (!File.Exists("IntegrationEvidence/reconnect.request")) return;
            try {
                EditorApplication.ExecuteMenuItem("Window/Pipeline/Start Server");
                File.WriteAllText("IntegrationEvidence/editor_ready.txt", DateTime.UtcNow.ToString("O"));
                File.Delete("IntegrationEvidence/reconnect.request");
            } catch (Exception e) { File.WriteAllText("IntegrationEvidence/editor_error.txt", e.ToString()); }
        };
    }
}
