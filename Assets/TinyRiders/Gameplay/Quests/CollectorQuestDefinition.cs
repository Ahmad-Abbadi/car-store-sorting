using UnityEngine;

namespace TinyRiders.Quests
{
    public enum CollectorQuestGoal { SortedCars, FullShelves, OwnedUpgrades, DiscoveredDesigns, CompletedThemes, EntireStore, SpecificTheme }

    [CreateAssetMenu(menuName = "Tiny Riders/Collector Quest")]
    public sealed class CollectorQuestDefinition : ScriptableObject
    {
        public string id, title, description;
        public CollectorQuestGoal goal;
        [Min(1)] public int target = 1;
        [Min(0)] public int coins, xp;
    }
}
