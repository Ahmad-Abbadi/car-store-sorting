using TinyRiders.Editor;
using System.IO;
using Newtonsoft.Json.Linq;
public class RunExpansionAll {public static void Run(){foreach(var theme in JObject.Parse(File.ReadAllText("TinyRidersProduction/ThemesExpansion01/texture_recipes.json"))["themes"])ThemesExpansionBuilder.Build((string)theme["id"]);ThemesExpansionBuilder.Validate("first");}}
