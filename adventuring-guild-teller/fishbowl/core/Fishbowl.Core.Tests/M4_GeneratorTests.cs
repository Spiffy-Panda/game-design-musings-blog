using Fishbowl.Core.Engine;
using Fishbowl.Core.Model;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>VFB.M4 — town generator: seeded and deterministic, and it guarantees the
/// invariants the observatory relies on.</summary>
public class M4_GeneratorTests
{
    [Fact]
    public void Generator_Is_Deterministic_For_A_Seed()
    {
        var town = TestSupport.LoadGoldenTown();
        var cfg = new GenConfig { Seed = 42, Count = 14 };
        var a = TownGenerator.Generate(town, cfg).Townees.Select(t => t.Id).ToList();
        var b = TownGenerator.Generate(town, cfg).Townees.Select(t => t.Id).ToList();
        Assert.Equal(a, b);
        Assert.Equal(14, a.Count);
    }

    [Fact]
    public void Generated_Town_Holds_The_Invariants_And_Validates()
    {
        var town = TestSupport.LoadGoldenTown();
        var generated = TownGenerator.Generate(town, new GenConfig { Seed = 7, Count = 12 });

        // Everyone has a home; non-adventurers have a work; homes/works are real places.
        Assert.All(generated.Townees, t =>
        {
            Assert.True(town.PlaceById.ContainsKey(t.Home));
            if (!t.Adventurer) Assert.True(t.Work is not null && town.PlaceById.ContainsKey(t.Work));
        });

        // At least one gossip-carrier so the summary can never starve for a witness.
        string? carrier = town.Traits.First(x => x.HearsayCarrier).Id;
        Assert.Contains(generated.Townees, t => t.Traits.Contains(carrier!));

        // The generated set, dropped into the existing places/traits/dayplans, passes validation.
        var mergedTown = BuildTownWith(town, generated.Townees);
        Fishbowl.Core.Data.SchemaValidator.Validate(mergedTown); // throws on any dangling reference
    }

    private static Town BuildTownWith(Town template, List<TowneeDto> townees) => new()
    {
        // A freshly generated cast carries no golden-anchored storylets (their _bindings name the
        // original townees) — its own rules are authored separately, so validate with none.
        Config = template.Config, Places = template.Places, Townees = townees,
        DayPlans = template.DayPlans, Traits = template.Traits,
        Storylets = Array.Empty<StoryletDto>(), Golden = null,
        PlaceById = template.PlaceById, TraitById = template.TraitById,
        StoryletById = new Dictionary<string, StoryletDto>(),
        TowneeById = townees.ToDictionary(t => t.Id),
    };
}
