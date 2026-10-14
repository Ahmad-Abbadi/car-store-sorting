public static class RunRefinement
{
    public static void Run(string theme)
    {
        if(theme=="ALL")TinyRiders.Editor.ThemedCarBuilder.BuildAll();
        else TinyRiders.Editor.ThemedCarBuilder.BuildTheme(theme);
    }
}
