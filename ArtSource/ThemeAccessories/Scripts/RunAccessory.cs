using TinyRiders.Editor;
public static class RunAccessory
{
    public static void Run(string action,string theme="TH_SPACE",string bases="ALL")
    {
        if(action=="before")ThemeAccessoryBuilder.CaptureBefore();
        else if(action=="apply")ThemeAccessoryBuilder.Apply(theme,bases);
        else if(action=="all")
        {
            var library=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(ThemeAccessoryBuilder.Prod+"/theme_accessory_library.json"));
            foreach(var item in library)
            {
                ThemeAccessoryBuilder.Apply((string)item["themeId"]);
                var state=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(ThemeAccessoryBuilder.Prod+"/production_state.json"));
                state["stage"]="unity_assembly_and_visual_validation";
                state["lastAssembledTheme"]=(string)item["themeId"];
                state["updatedVariantCount"]=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(ThemeAccessoryBuilder.Prod+"/variant_assignments.json")).Count;
                System.IO.File.WriteAllText(ThemeAccessoryBuilder.Prod+"/production_state.json",state.ToString());
            }
        }
        else throw new System.Exception("Unknown action "+action);
    }
}
