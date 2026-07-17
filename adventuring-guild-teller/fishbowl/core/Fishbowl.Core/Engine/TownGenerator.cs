using Fishbowl.Core.Determinism;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>Knobs for the seeded town generator (creation menus, M4).</summary>
public sealed record GenConfig
{
    public long Seed { get; init; } = 1;
    public int Count { get; init; } = 12;
    /// <summary>role id → relative weight. Empty ⇒ mix inferred from the template town.</summary>
    public Dictionary<string, int> RoleMix { get; init; } = new();
    /// <summary>0..1 chance of a directed regard edge between any ordered pair.</summary>
    public double RelationshipDensity { get; init; } = 0.15;
}

/// <summary>
/// Seeded town generator: emits a townees.json-shaped set against an existing town's places,
/// roles and day-plans. Guarantees the invariants the observatory relies on — everyone has a
/// home (and non-adventurers a work), and at least one gossip-carrier exists so the summary
/// never starves for lack of a witness. Fully deterministic from (seed, "gen").
/// </summary>
public static class TownGenerator
{
    private static readonly string[] FirstNames =
        { "Alda", "Bram", "Cael", "Doran", "Elna", "Ferro", "Gwen", "Hobb", "Isolde", "Joss",
          "Ketch", "Lune", "Mabel", "Nils", "Orla", "Pike", "Quill", "Rhea", "Sten", "Turl" };
    private static readonly string[] Surnames =
        { "Ashdown", "Brenner", "Colt", "Dray", "Ewell", "Frost", "Garrow", "Hale", "Ivor", "Jute" };

    public static TowneesFile Generate(Town template, GenConfig cfg)
    {
        var rng = Rng.Stream(cfg.Seed, 0, "gen");

        // Role templates inferred from the example town: role → (dayplan, work, adventurer).
        var roleTemplate = new Dictionary<string, TowneeDto>(StringComparer.Ordinal);
        foreach (var t in template.Townees) roleTemplate.TryAdd(t.Role, t);

        var roleMix = cfg.RoleMix.Count > 0
            ? cfg.RoleMix
            : template.Townees.GroupBy(t => t.Role).ToDictionary(g => g.Key, g => g.Count());
        var roleBag = roleMix.SelectMany(kv => Enumerable.Repeat(kv.Key, Math.Max(1, kv.Value))).ToArray();

        // `!p.Board` catches residences (homes carry no board card), but an offscreen SITE is also
        // board:false and is emphatically NOT somewhere a townee lives (PNO.M2) — housing a generated
        // townee in the fen is the break the drift check flagged. Exclude offscreen places from the pool.
        var homes = template.Places.Where(p => p.Kind == "home" || (!p.Board && !p.Offscreen)).Select(p => p.Id).ToArray();
        var fallbackHome = homes.Length > 0 ? homes : template.Places.Select(p => p.Id).ToArray();
        var traitIds = template.Traits.Select(t => t.Id).ToArray();
        string? gossipTrait = template.Traits.FirstOrDefault(t => t.HearsayCarrier)?.Id;

        var townees = new List<TowneeDto>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < cfg.Count; i++)
        {
            string role = roleBag[rng.NextInt(roleBag.Length)];
            var tmpl = roleTemplate.GetValueOrDefault(role) ?? template.Townees[0];
            string name = UniqueName(rng, used);
            string id = Slug(name);

            var pressures = new Dictionary<string, double>();
            foreach (var d in Town.Drives) pressures[d] = Math.Round(0.25 + rng.NextDouble() * 0.45, 2);

            var traits = PickTraits(rng, traitIds);
            townees.Add(new TowneeDto
            {
                Id = id, Name = name, Role = role, Adventurer = tmpl.Adventurer,
                Traits = traits, Dayplan = tmpl.Dayplan, Home = fallbackHome[rng.NextInt(fallbackHome.Length)],
                Work = tmpl.Adventurer ? null : tmpl.Work, Haunts = tmpl.Haunts.ToList(),
                Pressures = pressures, TellerRegard = 0.5, Bio = $"{name} is one of the village's {role}s.",
            });
        }

        // Invariant: at least one gossip-carrier.
        if (gossipTrait is not null && !townees.Any(t => t.Traits.Contains(gossipTrait)))
            townees[0].Traits.Add(gossipTrait);

        // Relationship density: directed regard edges with a random tag.
        string[] tags = { "kin", "rival", "courting", "debtor" };
        foreach (var a in townees)
            foreach (var b in townees)
                if (!ReferenceEquals(a, b) && rng.NextDouble() < cfg.RelationshipDensity)
                    a.Regard[b.Id] = new RegardDto
                    {
                        Score = Math.Round(rng.NextDouble() * 1.2 - 0.6, 2),
                        Tags = new List<string> { tags[rng.NextInt(tags.Length)] },
                    };

        return new TowneesFile { Version = 1, Townees = townees };
    }

    private static List<string> PickTraits(Rng rng, string[] pool)
    {
        int n = pool.Length == 0 ? 0 : 1 + rng.NextInt(Math.Min(3, pool.Length));
        var chosen = new List<string>();
        while (chosen.Count < n)
        {
            var t = pool[rng.NextInt(pool.Length)];
            if (!chosen.Contains(t)) chosen.Add(t);
        }
        return chosen;
    }

    private static string UniqueName(Rng rng, HashSet<string> used)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            string n = $"{FirstNames[rng.NextInt(FirstNames.Length)]} {Surnames[rng.NextInt(Surnames.Length)]}";
            if (used.Add(n)) return n;
        }
        string fallback = $"Townee {used.Count + 1}";
        used.Add(fallback);
        return fallback;
    }

    private static string Slug(string name) =>
        new string(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
}
