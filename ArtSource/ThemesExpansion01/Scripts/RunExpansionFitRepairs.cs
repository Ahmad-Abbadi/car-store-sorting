using TinyRiders.Editor;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
public class RunExpansionFitRepairs {public static void Run(){var changes=JArray.Parse(File.ReadAllText("TinyRidersProduction/ThemesExpansion01/fit_overrides.json"));var products=JArray.Parse(File.ReadAllText("TinyRidersProduction/ThemesExpansion01/products.json"));foreach(var group in products.Where(p=>changes.Any(c=>(string)c["variantId"]==(string)p["variantId"])).GroupBy(p=>(string)p["themeId"]))ThemesExpansionBuilder.Build(group.Key,string.Join(",",group.Select(p=>(string)p["baseCarId"])));ThemesExpansionBuilder.Validate("fit_repaired");}}
