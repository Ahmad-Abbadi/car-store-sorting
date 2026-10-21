using UnityEditor;

public static class CarBatchFixtureSetup
{
    public static int Detail()
    {
        // Keep all live geometry, texture, lighting and first-frame checks enabled.
        // The dedicated image comparison owns screenshot output for this validation run.
        SessionState.SetBool("Details.Validation.skipCaptures", true);
        return 1;
    }
}
