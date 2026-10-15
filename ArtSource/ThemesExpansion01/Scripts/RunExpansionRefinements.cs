using TinyRiders.Editor;
public class RunExpansionRefinements {public static void Run(){foreach(var theme in new[]{"TH_WILD_WEST","TH_NINJA","TH_ANCIENT_EGYPT"})ThemesExpansionBuilder.Build(theme);ThemesExpansionBuilder.Validate("refined");}}
