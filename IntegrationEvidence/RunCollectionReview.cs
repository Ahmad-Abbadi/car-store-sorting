public static class RunCollectionReview
{
    public static string Build()
    {
        ThemedCollectionReviewBuilder.Build();
        return ThemedCollectionReviewBuilder.ScenePath;
    }
}
