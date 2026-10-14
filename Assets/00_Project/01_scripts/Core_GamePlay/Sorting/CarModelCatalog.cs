using System;

/// <summary>Player-facing names for the fifteen stable TinyRiders model IDs.</summary>
public static class CarModelCatalog
{
    public static readonly string[] EnglishNames =
    {
        "Touring Coupe", "Sprint Coupe", "Rally Beetle", "Rally SUV", "Rally Hatchback",
        "Formula Racer", "Delivery Van", "Open Cockpit Racer", "Track Prototype", "Endurance Coupe",
        "Offroad Buggy", "Trail Jeep", "Cargo Truck", "Desert Buggy", "Classic Roadster"
    };

    public static string Id(int index) => "C" + (index + 1).ToString("000");

    public static string EnglishName(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 4 || id[0] != 'C' ||
            !int.TryParse(id.Substring(1), out int number) || number < 1 || number > EnglishNames.Length)
            throw new ArgumentException("Unknown car model ID: " + id, nameof(id));
        return EnglishNames[number - 1];
    }

    public static string DisplayName(int index) =>
        Localizer.GetOr("category." + Id(index), EnglishNames[index]);
}
