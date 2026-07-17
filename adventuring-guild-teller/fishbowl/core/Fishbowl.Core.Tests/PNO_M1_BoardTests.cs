using System.Text.Json.Nodes;
using Fishbowl.Core.Api;
using Fishbowl.Core.Data;
using Fishbowl.Core.Engine;
using Fishbowl.Core.Model;
using Xunit;

namespace Fishbowl.Core.Tests;

/// <summary>
/// PNO.M1 — the board. Its accept criterion is "a shortage files a posting; it stands, ages, expires;
/// every transition has a because-list", and the readout half of it is <see cref="WorldView.BoardJson"/>:
/// until that existed the board filled and emptied every run with nothing able to render it.
/// <para><b>Named PNO_M1 rather than M1 on purpose.</b> `M1_ClockworkDeterminismTests` is VFB.M1 — a
/// different plan's milestone numbering, in the same folder. The two Ms are unrelated and a reader who
/// assumes otherwise will look for the hash pin in here.</para>
/// <para><b>None of this can run on the golden fixture</b> — it is posting-free forever (PNO.D2), so it
/// has no board. See <see cref="TestSupport.LoadLiveTown"/> for why that means asserting the machine
/// rather than the live town's current numbers.</para>
/// </summary>
public class PNO_M1_BoardTests
{
    // --- the projection ------------------------------------------------------------------

    [Fact]
    public void Board_Projection_Renders_A_Filed_Posting_With_Names_Not_Ids()
    {
        var world = World.Build(TestSupport.LoadLiveTown());
        var filed = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0);
        Assert.NotNull(filed);

        var board = Parse(WorldView.BoardJson(world));
        var rows = board["postings"]!.AsArray();
        var row = Assert.Single(rows);

        // The whole point of a projection: the view gets names, not slugs. Board.Expire refuses to put
        // a raw id in a rendered line on AGT.10 grounds (the summary is gossip, not telemetry) and the
        // board panel is the same surface one layer over.
        Assert.Equal("petch", (string?)row!["requester"]);
        Assert.Equal("Petch", (string?)row["requester_name"]);
        Assert.Equal("the Sedge Fen", (string?)row["site_name"]);
        Assert.Equal("posting", (string?)row["reach"]);
        Assert.Equal("Guildhall Steps", (string?)board["place_name"]);
    }

    [Fact]
    public void An_Errand_Carries_No_Site_And_No_Site_Name()
    {
        // reach:"errand" is in-town by definition — a neighbour handles it, there is nowhere to go.
        // The panel branches on exactly this to render "in town" instead of a destination, so a
        // non-null site_name here would put a place in the fiction that the errand never visits.
        var world = World.Build(TestSupport.LoadLiveTown());
        Board.File(world, "pellow-flour-run", "nan-pellow", day: 1, slot: 0);

        var row = Parse(WorldView.BoardJson(world))["postings"]!.AsArray()[0]!;
        Assert.Equal("errand", (string?)row["reach"]);
        Assert.Null((string?)row["site"]);
        Assert.Null((string?)row["site_name"]);
    }

    [Fact]
    public void A_Standing_Posting_Has_No_Taker_At_M1()
    {
        // Not a placeholder assertion — it is the load-bearing reason the panel prints "untaken"
        // rather than a blank. A posting WITH a taker is PostingState.Taken, and World.Board is the
        // Standing subset, so a taker on the board is unreachable by construction until PNO.M2 makes
        // Taken a state paper can be in. If this ever fails, the board is showing paper somebody took.
        var world = World.Build(TestSupport.LoadLiveTown());
        Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0);

        var row = Parse(WorldView.BoardJson(world))["postings"]!.AsArray()[0]!;
        Assert.Null((string?)row["taker"]);
        Assert.Null((string?)row["taker_name"]);
    }

    [Fact]
    public void The_Frozen_Fixture_Projects_An_Empty_Board()
    {
        // Two things at once: the fixture is still posting-free (PNO.D2 — if this fails, someone added
        // a posting to the town that pins the three hash literals), and the projection's empty case is
        // a real, well-formed payload rather than a crash or a null. The panel's "No paper up." branch
        // is the most common state on screen — the live town files nothing until day 2 — so the empty
        // board is the shape the view sees at every boot.
        var world = World.Build(TestSupport.LoadGoldenTown());
        var board = Parse(WorldView.BoardJson(world));

        Assert.Empty(board["postings"]!.AsArray());
        Assert.Equal(1, (int)board["day"]!);
    }

    // --- the lifecycle -------------------------------------------------------------------

    [Fact]
    public void Paper_Files_Stands_And_Expires_Over_A_Fortnight()
    {
        // PNO.M1's accept criterion, asserted as an invariant rather than as this town's current dates.
        // A posting town must be able to do the whole arc; WHEN it does is authoring, and authoring is
        // allowed to change underneath this test (see TestSupport.LoadLiveTown).
        var sim = new Simulation(TestSupport.LoadLiveTown());
        sim.RunDays(14);

        Assert.NotEmpty(sim.World.Postings);
        Assert.Contains(sim.World.Postings, p => p.State == PostingState.Expired);
        Assert.All(sim.World.Postings, p => Assert.True(p.ExpiresDay > p.FiledDay,
            $"posting '{p.Id}' expires day {p.ExpiresDay} on or before its filing day {p.FiledDay}"));

        // Every transition explainable on the same terms as everything else (AGR.2). Expiry is a board
        // MECHANISM, not a rule, so it is the one chronicle entry in the project built outside
        // StoryletEngine.BuildEntry — which is exactly why it is worth asserting that it still carries
        // the contract that BuildEntry gives everything else for free.
        var expiries = sim.World.Chronicle.Where(e => e.StoryletId == Board.ExpiredStoryletId).ToList();
        Assert.NotEmpty(expiries);
        Assert.All(expiries, e =>
        {
            Assert.NotEmpty(e.Because);
            Assert.Contains(e.Because, b => b.Label == "posting");
            Assert.Contains(e.Because, b => b.Label == "stood");
            Assert.Single(e.PostingIds);
            // The id is a debug handle ("template#requester#dN"). It belongs in the because-list and in
            // PostingIds, where machines look, and never in a rendered register.
            Assert.DoesNotContain("#", e.Report);
            Assert.DoesNotContain("#", e.Gossip);
            Assert.DoesNotContain("#", e.Hearsay);
        });
    }

    [Fact]
    public void A_Standing_Posting_Never_Shows_A_Countdown_Below_One_Day()
    {
        // The boundary Board states in its own comment: expiry runs at `day >= ExpiresDay`, at the dawn
        // of the incoming day, so anything still standing has ExpiresDay > Day. A 0 would mean "already
        // gone" and is unreachable — paper comes down before any projection could show it. Checked at
        // every dawn because that is the one moment the two could disagree.
        var sim = new Simulation(TestSupport.LoadLiveTown());
        for (int day = 0; day < 14; day++)
        {
            sim.RunToDawn();
            foreach (var row in Parse(WorldView.BoardJson(sim.World))["postings"]!.AsArray())
            {
                int left = (int)row!["days_to_expiry"]!;
                Assert.True(left >= 1,
                    $"posting '{(string?)row["id"]}' is on the board on day {sim.World.Day} showing {left} days left");
            }
        }
    }

    [Fact]
    public void Expiry_Takes_The_Paper_Off_The_Board_And_Leaves_It_In_The_Record()
    {
        // The board is an INDEX over state (PNO.D1: "a new data type for standing" reads as *the board
        // is data, not gameplay*), not a separate collection. So expiring must remove it from one and
        // keep it in the other — a posting that vanished from World.Postings would take its history with it.
        var world = World.Build(TestSupport.LoadLiveTown());
        var p = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0)!;
        Assert.Single(world.Board);

        Board.ResolveDay(world, p.ExpiresDay);

        Assert.Empty(world.Board);
        Assert.Contains(p, world.Postings);
        Assert.Equal(PostingState.Expired, p.State);
    }

    [Fact]
    public void Filing_The_Same_Paper_Twice_In_A_Day_Is_Refused()
    {
        // The posting id is content-derived — (template, requester, day) — precisely so it is stable
        // under bank growth rather than being a counter or a Guid (both banned or worse for
        // determinism). The cost of that choice is that the same need on the same day is the same id,
        // so File has to refuse rather than shadow. If this regressed, a rule firing twice in a day
        // would silently double the board.
        var world = World.Build(TestSupport.LoadLiveTown());
        Assert.NotNull(Board.File(world, "sedgewort-short", "petch", day: 3, slot: 0));
        Assert.Null(Board.File(world, "sedgewort-short", "petch", day: 3, slot: 9));
        Assert.Single(world.Postings);
    }

    [Fact]
    public void An_Unknown_Template_Files_Nothing()
    {
        var world = World.Build(TestSupport.LoadLiveTown());
        Assert.Null(Board.File(world, "no-such-template", "petch", day: 1, slot: 0));
        Assert.Empty(world.Postings);
    }

    [Fact]
    public void Posting_Expiry_Scale_Is_Baked_At_Filing_And_Cannot_Re_Date_Standing_Paper()
    {
        // The knob's semantics, pinned — and they are the reason the observatory's `_knob` callback
        // does NOT repaint the board. Board.File reads the scale once, at the moment of filing, and
        // bakes it into ExpiresDay; the countdown the board shows is arithmetic over that stored date.
        // So turning the dial can never make a rendered days-to-expiry stale, and a board repaint on
        // knob-change would imply a retroactivity the engine does not have.
        //
        // This test exists because the M1 handoff asserted the opposite ("add it to _knob(), or
        // posting_expiry_scale will leave days-to-expiry lying"). It is a claim about the code, so it
        // is checkable, and it is false — this is the check.
        var world = World.Build(TestSupport.LoadLiveTown());
        var standing = Board.File(world, "sedgewort-short", "petch", day: 1, slot: 0)!;
        int wasExpiring = standing.ExpiresDay;

        world.SetKnob("posting_expiry_scale", 3.0);

        // Paper already up does not move.
        Assert.Equal(wasExpiring, standing.ExpiresDay);

        // Paper filed AFTER the turn does — which is what makes it a live knob rather than a dead one.
        var later = Board.File(world, "sedgewort-short", "petch", day: 2, slot: 0)!;
        Assert.Equal(2 + (standing.ExpiresDay - 1) * 3, later.ExpiresDay);
    }

    // --- validate-then-run ---------------------------------------------------------------

    [Theory]
    // Board.File is a copier: it stamps the template's fields onto the runtime posting and nothing
    // downstream re-checks any of them. So each of these is a posting that exists and is quietly wrong
    // for the rest of the run — never a crash, which is what makes catching them at load worth doing.
    [InlineData("nobody-at-all", "posting", "the-sedge-fen", 4, 0.25, "is not a townee")]
    [InlineData("petch", "postng", "the-sedge-fen", 4, 0.25, "unrecognized")]
    [InlineData("petch", "posting", null, 4, 0.25, "no site")]
    [InlineData("petch", "errand", "the-sedge-fen", 4, 0.25, "but names site")]
    [InlineData("petch", "posting", "the-sedge-fen", 0, 0.25, "expires_days")]
    [InlineData("petch", "posting", "the-sedge-fen", 4, -0.5, "reward")]
    public void A_Malformed_Posting_Template_Fails_Validation(
        string requester, string reach, string? site, int expiresDays, double reward, string expected)
    {
        var town = WithPosting(new PostingTemplateDto
        {
            Id = "t", Requester = requester, Reach = reach, Site = site,
            ExpiresDays = expiresDays, Reward = reward,
        });

        var errors = SchemaValidator.Collect(town);
        Assert.Contains(errors, e => e.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void A_Duplicate_Posting_Template_Fails_Validation()
    {
        // Board.File resolves by FirstOrDefault(p => p.Id == templateId), so a duplicate does not
        // conflict — the first silently wins and the second is authored prose nothing can reach.
        var town = WithPosting(
            new PostingTemplateDto { Id = "dup", Requester = "petch", Reach = "errand", ExpiresDays = 1 },
            new PostingTemplateDto { Id = "dup", Requester = "petch", Reach = "errand", ExpiresDays = 1 });

        Assert.Contains(SchemaValidator.Collect(town), e => e.Contains("more than once", StringComparison.Ordinal));
    }

    [Fact]
    public void The_Live_Towns_Own_Templates_Validate()
    {
        // The checks above are only worth having if they accept the real thing. This is the guard
        // against a rule so strict it condemns the town it ships with — `--lint` has twice shipped a
        // check that got its condemnations backwards, and both were hand-modelling the engine.
        Assert.Empty(SchemaValidator.Collect(TestSupport.LoadLiveTown())
            .Where(e => e.StartsWith("posting '", StringComparison.Ordinal)));
    }

    // --- helpers -------------------------------------------------------------------------

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    /// <summary>The live town with its posting bank replaced — real places/townees (so a requester can
    /// resolve) and exactly the templates under test.</summary>
    private static Town WithPosting(params PostingTemplateDto[] postings)
    {
        var live = TestSupport.LoadLiveTown();
        return new Town
        {
            Config = live.Config, Places = live.Places, Townees = live.Townees,
            DayPlans = live.DayPlans, Traits = live.Traits,
            // No storylets: SchemaValidator's `post` effect check asserts the template a rule names
            // exists, and swapping the bank out from under the real rules would fire that instead of
            // the checks under test.
            Storylets = Array.Empty<StoryletDto>(), Golden = null, Postings = postings,
            PlaceById = live.PlaceById, TowneeById = live.TowneeById, TraitById = live.TraitById,
            StoryletById = new Dictionary<string, StoryletDto>(),
        };
    }
}
