using Fishbowl.Core.Json;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Data;

/// <summary>Loads and validates a data/ directory into a <see cref="Town"/>.</summary>
public static class TownLoader
{
    public static Town Load(string dataDir)
    {
        if (!Directory.Exists(dataDir))
            throw new DirectoryNotFoundException($"Town data directory not found: {dataDir}");

        var config = DataJson.Deserialize<SimConfig>(Read(dataDir, "simconfig.json"));
        var places = DataJson.Deserialize<PlacesFile>(Read(dataDir, "places.json")).Places;
        var townees = DataJson.Deserialize<TowneesFile>(Read(dataDir, "townees.json")).Townees;
        var dayplans = DataJson.Deserialize<DayPlansFile>(Read(dataDir, "dayplans.json")).Dayplans;
        var traits = DataJson.Deserialize<TraitsFile>(Read(dataDir, "traits.json")).Traits;

        // Storylets: one rule per file under storylets/, enumerated in stable filename order.
        var storyletDir = Path.Combine(dataDir, "storylets");
        var storylets = new List<StoryletDto>();
        if (Directory.Exists(storyletDir))
        {
            foreach (var file in Directory.GetFiles(storyletDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
                storylets.Add(DataJson.Deserialize<StoryletDto>(DataJson.ReadText(file)));
        }
        storylets.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); // stable id order for the sim

        GoldenDayFile? golden = null;
        var goldenPath = Path.Combine(dataDir, "golden", "day1.json");
        if (File.Exists(goldenPath))
            golden = DataJson.Deserialize<GoldenDayFile>(DataJson.ReadText(goldenPath));

        var town = new Town
        {
            Config = config,
            Places = places,
            Townees = townees,
            DayPlans = dayplans,
            Traits = traits,
            Storylets = storylets,
            Golden = golden,
            PlaceById = ToLookup(places, p => p.Id, "place"),
            TowneeById = ToLookup(townees, t => t.Id, "townee"),
            TraitById = ToLookup(traits, t => t.Id, "trait"),
            StoryletById = ToLookup(storylets, s => s.Id, "storylet"),
        };

        SchemaValidator.Validate(town);
        return town;
    }

    /// <summary>Rebuild a town swapping in a new townee or place list, re-deriving lookups and
    /// re-validating. Used by the creation menus' hot-add (Town.WithTownee / WithPlace).</summary>
    public static Town Rebuild(Town from, IReadOnlyList<TowneeDto>? townees = null, IReadOnlyList<PlaceDto>? places = null)
    {
        var t = townees ?? from.Townees;
        var p = places ?? from.Places;
        var town = new Town
        {
            Config = from.Config, Places = p, Townees = t, DayPlans = from.DayPlans,
            Traits = from.Traits, Storylets = from.Storylets, Golden = from.Golden,
            PlaceById = ToLookup(p, x => x.Id, "place"),
            TowneeById = ToLookup(t, x => x.Id, "townee"),
            TraitById = from.TraitById, StoryletById = from.StoryletById,
        };
        SchemaValidator.Validate(town);
        return town;
    }

    private static string Read(string dir, string file)
    {
        var path = Path.Combine(dir, file);
        if (!File.Exists(path)) throw new FileNotFoundException($"Required town file missing: {path}");
        return DataJson.ReadText(path);
    }

    private static IReadOnlyDictionary<string, T> ToLookup<T>(IEnumerable<T> items, Func<T, string> key, string what)
    {
        var dict = new Dictionary<string, string>(); // detect dupes with a friendly message
        var result = new Dictionary<string, T>();
        foreach (var item in items)
        {
            var k = key(item);
            if (string.IsNullOrEmpty(k)) throw new InvalidDataException($"Empty {what} id in data.");
            if (!result.TryAdd(k, item)) throw new InvalidDataException($"Duplicate {what} id: {k}");
        }
        return result;
    }
}
