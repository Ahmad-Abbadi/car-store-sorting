using TinyRiders.Editor;
using System.IO;
using Newtonsoft.Json.Linq;
public class RunExpansionIdempotence {public static void Run(){foreach(var t in JObject.Parse(File.ReadAllText("TinyRidersProduction/ThemesExpansion01/texture_recipes.json"))["themes"])ThemesExpansionBuilder.Build((string)t["id"],"ALL",false);foreach(var t in JObject.Parse(File.ReadAllText("TinyRidersProduction/ThemesExpansion01/texture_recipes.json"))["themes"])ThemesExpansionBuilder.Build((string)t["id"],"C004,C005",false);ThemesExpansionBuilder.Validate("second");ThemesExpansionBuilder.CaptureOldAfter();}}
