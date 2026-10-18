using UnityEngine;

namespace TinyRiders.Quests
{
    // Append only: the first seven values are present in shipped career saves.
    public enum CollectorQuestGoal { SortedCars, FullShelves, OwnedUpgrades, DiscoveredDesigns, CompletedThemes, EntireStore, SpecificTheme,
        ThemeSortedCars, FamilySortedCars, UpgradeLevel, TotalUpgradeLevels }
    public enum CollectorQuestCategory { Sorting, Discoveries, Shelves, Themes, CarFamilies, Upgrades }

    [CreateAssetMenu(menuName = "Tiny Riders/Collector Quest")]
    public sealed class CollectorQuestDefinition : ScriptableObject
    {
        public string id, title, description;
        public CollectorQuestGoal goal;
        public CollectorQuestCategory category;
        public string subjectId, seriesId, titleKey, descriptionKey;
        public int stageOrder, authoredOrder;
        [Range(0,1)] public float targetFraction;
        [Min(1)] public int target = 1;
        [Min(0)] public int coins, xp;
    }
}
