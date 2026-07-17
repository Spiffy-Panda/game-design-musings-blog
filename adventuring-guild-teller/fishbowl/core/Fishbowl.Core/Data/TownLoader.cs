using Fishbowl.Core.Json;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Data;

/// <summary>Loads and validates a data/ directory into a <see cref="Town"/>.</summary>
public static class TownLoader
{
    public static Town Load(string dataDir)
    {
        var town = LoadUnvalidated(dataDir);
        SchemaValidator.Validate(town);
        return town;
    }

    /// <summary>
    /// Parse a town without the validation gate. <b>Only <c>--lint</c> should call this</b> — the sim
    /// requires <see cref="Load"/>, because validate-then-run is the whole discipline and a dangling
    /// reference resolving to nothing mid-tick is the exact poison it exists to stop. The linter is
    /// the one caller that must be able to read a town that does not load, so it can say *why*.
    /// <para>Malformed JSON and missing required files still throw here: there is no town to report on.</para>
    /// </summary>
    public static Town LoadUnvalidated(string dataDir)
    {
        if (!Directory.Exists(dataDir))
            throw new DirectoryNotFoundException($"Town data directory not found: {dataDir}");

        var config = DataJson.Deserialize<SimConfig>(Read(dataDir, "simconfig.json"));
        var authoredPlaces = DataJson.Deserialize<PlacesFile>(Read(dataDir, "places.json")).Places;
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

        // Postings: OPTIONAL, exactly like storylets/ and golden/ above — absent means a
        // posting-free town, which the frozen golden fixture is by ruling (PNO.D2). Required-ness
        // here would force that fixture to carry an empty stub about a system it must never know.
        IReadOnlyList<PostingTemplateDto> postings = Array.Empty<PostingTemplateDto>();
        var postingsPath = Path.Combine(dataDir, "postings.json");
        if (File.Exists(postingsPath))
            postings = DataJson.Deserialize<PostingsFile>(DataJson.ReadText(postingsPath)).Postings;

        // Sites: OPTIONAL like postings/, and each one is ALSO a place. A site is authored once in
        // sites.json (its leg track); the offscreen place it needs for co-presence is synthesized here
        // rather than hand-authored in places.json twice, so an author cannot forget one or drift them
        // apart. board:false + offscreen:true keeps sites out of daily routing and out of the place board.
        IReadOnlyList<SiteDto> sites = Array.Empty<SiteDto>();
        var sitesPath = Path.Combine(dataDir, "sites.json");
        if (File.Exists(sitesPath))
            sites = DataJson.Deserialize<SitesFile>(DataJson.ReadText(sitesPath)).Sites;

        var places = sites.Count > 0
            ? authoredPlaces.Concat(sites.Select(s => SitePlace(s, config.SlotsPerDay))).ToList()
            : authoredPlaces;

        var town = new Town
        {
            Config = config,
            Places = places,
            Townees = townees,
            DayPlans = dayplans,
            Traits = traits,
            Storylets = storylets,
            Golden = golden,
            Postings = postings,
            Sites = sites,
            PlaceById = ToLookup(places, p => p.Id, "place"),
            TowneeById = ToLookup(townees, t => t.Id, "townee"),
            TraitById = ToLookup(traits, t => t.Id, "trait"),
            StoryletById = ToLookup(storylets, s => s.Id, "storylet"),
        };

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
            // Carried explicitly. This initializer is hand-written, so a field omitted here is
            // silently dropped from every rebuilt town rather than failing to compile — which is
            // the exact shape of the btn-generate bug (a Rebuild that quietly kept the wrong
            // Storylets). If you add a field to Town, add it here in the same commit.
            Postings = from.Postings,
            // The synthesized site-places already ride along in `p` (they are in from.Places); Sites is
            // the parallel authored list and must be carried too, or an outing's SiteById lookup goes null.
            Sites = from.Sites,
            PlaceById = ToLookup(p, x => x.Id, "place"),
            TowneeById = ToLookup(t, x => x.Id, "townee"),
            TraitById = from.TraitById, StoryletById = from.StoryletById,
        };
        SchemaValidator.Validate(town);
        return town;
    }

    /// <summary>The offscreen place a site is co-present at. Hours span the whole day and capacity is
    /// generous because neither is read at runtime for an offscreen place (only range-checked at load);
    /// what matters is <c>Board=false</c> (off the place board) and <c>Offscreen=true</c> (out of daily
    /// routing and out of <c>TownGenerator</c>'s home pool).</summary>
    private static PlaceDto SitePlace(SiteDto s, int slots) => new()
    {
        Id = s.Id, Name = s.Name, Kind = s.Kind,
        Hours = new HoursDto { Open = 0, Close = slots }, Capacity = 99,
        Board = false, Shut = false, Offscreen = true,
    };

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
