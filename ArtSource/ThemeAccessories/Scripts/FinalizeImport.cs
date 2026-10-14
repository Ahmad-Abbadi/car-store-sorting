using UnityEditor;
using TinyRiders.Editor;
public static class FinalizeImport
{
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();
        UnityEngine.Debug.Log("R03_FINAL_IMPORT_COMPLETE");
    }
}
