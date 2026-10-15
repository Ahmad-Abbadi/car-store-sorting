public static class CompleteCollectionRun
{
    public static string Artwork()
    {
        CompleteThemedCollectionBuilder.Complete();
        CompleteThemedCollectionBuilder.RepairNewShapeMounts();
        return "Artwork and catalog complete";
    }
    public static string Gameplay()
    {
        CompleteThemedCollectionBuilder.RepairNewShapeMounts();
        CompleteThemedCollectionBuilder.Integrate();
        return "Gameplay integration complete";
    }
    public static string Materials()
    {
        CompleteThemedCollectionBuilder.RepairMaterialReferences();
        return "Material references verified";
    }
}
