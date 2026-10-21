using System;
using System.IO;
using UnityEditor;

public static class RenderingReleaseVerification
{
    public static string Schedule()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Editor must be idle before release verification.");
        File.WriteAllText("IntegrationEvidence/RenderingArtifacts/release-status.txt","Queued");
        
        {
            // The complete bake was validated immediately before this build.
            // Do not mutate those inputs again while assembling the release.
            bool previousSkip = CarVisualBatchBuilder.SkipForBenchmark;
            try
            {
                CarVisualBatchBuilder.SkipForBenchmark = true;
                File.WriteAllText("IntegrationEvidence/RenderingArtifacts/release-status.txt","Building");
                CrazyGamesReleaseBuild.Build();
                File.WriteAllText("IntegrationEvidence/RenderingArtifacts/release-status.txt","Succeeded");
            }
            catch(Exception e)
            {
                File.WriteAllText("IntegrationEvidence/RenderingArtifacts/release-status.txt","Failed: "+e);
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                CarVisualBatchBuilder.SkipForBenchmark = previousSkip;
                // Keep recording off during the coordinated browser comparisons.
                // Restore the original saved preference after both chats finish QA.
                UnityEditorInternal.ProfilerDriver.enabled=false;
            }
        }
        return File.ReadAllText("IntegrationEvidence/RenderingArtifacts/release-status.txt");
    }
}

