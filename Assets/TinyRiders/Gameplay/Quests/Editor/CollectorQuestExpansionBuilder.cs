using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyRiders.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TinyRiders.Editor
{
    public static partial class CollectorQuestBuilder
    {
        public static readonly string[] LegacyIds={"first_display","getting_organized","full_house","better_tools","growing_collection","theme_collector","showroom_specialist","store_master"};

        [MenuItem("Tiny Riders/Quests/Expand to 240 Collector Quests")]
        public static void Expand()
        {
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/00_Project/00_Scenes/Level.unity")
                throw new InvalidOperationException("Open Level outside Play mode before expanding quests.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var manager=Object.FindFirstObjectByType<CollectorQuestManager>();
            if(!manager) throw new InvalidOperationException("Build the initial quest system before expanding it.");
            manager.definitions=Definitions(); EditorUtility.SetDirty(manager);
            BuildUI(); // Update the existing prefab; the physical board and its scene placement stay intact.
            Polish();
            Scrapbook();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }

        static CollectorQuestCategory CategoryFor(CollectorQuestGoal goal)
        {
            switch(goal)
            {
                case CollectorQuestGoal.DiscoveredDesigns: return CollectorQuestCategory.Discoveries;
                case CollectorQuestGoal.FullShelves: return CollectorQuestCategory.Shelves;
                case CollectorQuestGoal.CompletedThemes: case CollectorQuestGoal.ThemeSortedCars: case CollectorQuestGoal.SpecificTheme: return CollectorQuestCategory.Themes;
                case CollectorQuestGoal.FamilySortedCars: return CollectorQuestCategory.CarFamilies;
                case CollectorQuestGoal.OwnedUpgrades: case CollectorQuestGoal.UpgradeLevel: case CollectorQuestGoal.TotalUpgradeLevels: return CollectorQuestCategory.Upgrades;
                default: return CollectorQuestCategory.Sorting;
            }
        }

        static CollectorQuestDefinition[] ExpandDefinitions(CollectorQuestDefinition[] legacy)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
            var session=Object.FindFirstObjectByType<ThemedCollectionSession>();
            var catalog=session ? session.catalog : AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
            var entries=ThemedShelfCapacity.EligibleDefinitions(catalog).ToArray();
            int copies=session ? session.copiesPerDesign : 2;
            int cars=entries.Length*copies;
            var layout=session && session.roomLayout ? session.roomLayout :
                AssetDatabase.LoadAssetAtPath<CollectorRoomLayout>("Assets/TinyRiders/Gameplay/Compact/Promenade/PromenadeLayout.asset");
            if(!layout || layout.shelves.Length==0 || entries.Length==0) throw new InvalidOperationException("Quest generation requires the authored collection and room layout.");
            // EditMode runners can temporarily open an empty scene. Never infer reward tiers from live scene objects.
            int shelfCount=layout.shelves.Length;
            var upgrades=AssetDatabase.FindAssets("t:UpgradeSO",new[]{"Assets/00_Project/03_scriptableObjects/Upgrades"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<UpgradeSO>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(u=>u.SaveId,StringComparer.Ordinal).ToArray();
            var list=new List<CollectorQuestDefinition>(legacy);
            var themeIds=entries.Select(e=>e.themeId).Distinct().OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            var familyIds=entries.Select(e=>e.baseCarId).Distinct().OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            foreach(var theme in themeIds) AddStages(list,"theme",theme,CollectorQuestGoal.ThemeSortedCars);
            foreach(var family in familyIds) AddStages(list,"family",family,CollectorQuestGoal.FamilySortedCars);
            AddMilestones(list,"sort",CollectorQuestGoal.SortedCars,new[]{10,15,20,35,50,75,125,150,200,250,300,400,500,600,700,800,900,1000,1100,1150},cars);
            AddMilestones(list,"discover",CollectorQuestGoal.DiscoveredDesigns,new[]{1,5,10,25,50,75,100,150,200,300,450,600},entries.Length);
            AddMilestones(list,"shelf",CollectorQuestGoal.FullShelves,new[]{2,3,5,8,10,15,20,30,40,50,65,80},shelfCount);
            AddMilestones(list,"themes",CollectorQuestGoal.CompletedThemes,new[]{2,5,10,15,20,25,30,40},themeIds.Length);
            foreach(var u in upgrades) Add(list,"upgrade_own_"+u.SaveId,CollectorQuestGoal.UpgradeLevel,1,0,"upgrade_own",u.SaveId,0,"upgrade_own");
            foreach(var u in upgrades.Where(u=>u.MaxLevel>1)) Add(list,"upgrade_max_"+u.SaveId,CollectorQuestGoal.UpgradeLevel,u.MaxLevel,2,"upgrade_max",u.SaveId,0,"upgrade_max");
            foreach(int target in new[]{5,10,20})
                Add(list,"upgrade_levels_"+target,CollectorQuestGoal.TotalUpgradeLevels,target,target==5 ? 0 : target==10 ? 1 : 2,"upgrade_levels",null,0,"upgrade_levels");
            return list.ToArray();
            }
            finally {AssetDatabase.StopAssetEditing();}
        }

        static void AddStages(List<CollectorQuestDefinition> list,string prefix,string subject,CollectorQuestGoal goal)
        {
            float[] fractions={.25f,.5f,1};
            for(int i=0;i<3;i++) Add(list,prefix+"_"+subject+"_"+(i+1),goal,1,i,prefix+"."+(i+1),subject,fractions[i],prefix+"_"+subject);
        }

        static void AddMilestones(List<CollectorQuestDefinition> list,string prefix,CollectorQuestGoal goal,int[] targets,int capacity)
        {
            foreach(int target in targets)
            {
                double ratio=capacity>0 ? (double)target/capacity : 1;
                Add(list,prefix+"_"+target,goal,target,ratio<=.25 ? 0 : ratio<=.6 ? 1 : 2,prefix,null,0,prefix);
            }
        }

        static void Add(List<CollectorQuestDefinition> list,string id,CollectorQuestGoal goal,int target,int tier,string template,string subject,float fraction,string series)
        {
            string path=Folder+"/"+id+".asset";
            var d=AssetDatabase.LoadAssetAtPath<CollectorQuestDefinition>(path);
            if(!d){d=ScriptableObject.CreateInstance<CollectorQuestDefinition>(); AssetDatabase.CreateAsset(d,path);}
            d.id=id; d.goal=goal; d.category=CategoryFor(goal); d.target=target; d.targetFraction=fraction; d.subjectId=subject;
            d.seriesId=series; d.stageOrder=fraction>0 ? tier+1 : target; d.authoredOrder=list.Count;
            d.coins=new[]{5,10,20}[tier]; d.xp=new[]{15,30,60}[tier];
            d.titleKey="quest.expansion."+template+".title";
            d.descriptionKey="quest.expansion."+(fraction>0 ? template.Split('.')[0] : template)+".desc";
            d.title=TitleTemplate(template); d.description=DescriptionTemplate(goal);
            EditorUtility.SetDirty(d); list.Add(d);
        }

        static string TitleTemplate(string template)
        {
            switch(template)
            {
                case "theme.1": return "{1} Starter"; case "theme.2": return "{1} Specialist"; case "theme.3": return "{1} Master";
                case "family.1": return "{1} Enthusiast"; case "family.2": return "{1} Expert"; case "family.3": return "{1} Champion";
                case "sort": return "{0} Cars on Display"; case "discover": return "{0} Designs Discovered";
                case "shelf": return "{0} Shelves of Pride"; case "themes": return "{0} Complete Worlds";
                case "upgrade_own": return "Unlock {1}"; case "upgrade_max": return "Master {1}";
                default: return "{0} Levels of Improvement";
            }
        }
        static string DescriptionTemplate(CollectorQuestGoal goal)
        {
            switch(goal)
            {
                case CollectorQuestGoal.ThemeSortedCars: return "Correctly shelve {0} cars from the {1} theme";
                case CollectorQuestGoal.FamilySortedCars: return "Correctly shelve {0} {1} cars across themes";
                case CollectorQuestGoal.DiscoveredDesigns: return "Discover {0} different car designs";
                case CollectorQuestGoal.FullShelves: return "Fill {0} shelves correctly";
                case CollectorQuestGoal.CompletedThemes: return "Complete {0} themes";
                case CollectorQuestGoal.UpgradeLevel: return "Reach level {0} of {1}";
                case CollectorQuestGoal.TotalUpgradeLevels: return "Own {0} total upgrade levels";
                default: return "Correctly shelve {0} cars";
            }
        }
    }
}
