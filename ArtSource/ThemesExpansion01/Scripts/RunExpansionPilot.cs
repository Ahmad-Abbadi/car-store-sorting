using TinyRiders.Editor;
public class RunExpansionPilot {
 public static void Run(){ThemesExpansionBuilder.CaptureBaseline();foreach(var theme in new[]{"TH_WILD_WEST","TH_SCIENCE_LAB","TH_BARBERSHOP"})ThemesExpansionBuilder.Build(theme,"C001,C007,C010");ThemesExpansionBuilder.Validate("pilots");}
}
