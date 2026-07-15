using Xunit;

namespace MorningQueue.Core.Tests;

public class ValidatorTests
{
    // ---- green over the REAL data ------------------------------------------------

    [Fact]
    public void RealBanks_ValidateClean()
    {
        var data = MorningQueueData.ParseBanks(TestData.BanksPayload());
        var errors = Validator.ValidateBanks(data);
        Assert.True(errors.Count == 0, "banks errors: " + string.Join(" | ", errors));
    }

    [Fact]
    public void RealDay0Shift_ValidatesClean()
    {
        var wrapper = System.Text.Json.JsonSerializer.Deserialize<Wrapper>(TestData.Visitors, Json.Options)!;
        var errors = Validator.ValidateShift(wrapper.Visitors);
        Assert.True(errors.Count == 0, "shift errors: " + string.Join(" | ", errors));
    }

    // ---- red fixtures: one per banks validation family ---------------------------

    [Fact]
    public void Townee_BadDues_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(townees:
            "\"townee-x\": { \"dues\": \"pending\", \"owed\": 0, \"owns\": [] }"));
        Assert.Contains("townee 'townee-x' dues not current|owing", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Townee_OwedNotNumber_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(townees:
            "\"townee-x\": { \"dues\": \"current\", \"owed\": \"lots\", \"owns\": [] }"));
        Assert.Contains("townee 'townee-x' owed is not a number", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Townee_OwnsUnknownPosting_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(townees:
            "\"townee-x\": { \"dues\": \"current\", \"owed\": 0, \"owns\": [\"ghost-order\"] }"));
        Assert.Contains("townee 'townee-x' owns unknown posting 'ghost-order'", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Adventurer_BadRank_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(advs:
            "\"adv-x\": { \"rank\": \"mithril\", \"dues\": \"current\", \"chapter\": \"hollowmere\" }"));
        Assert.Contains("adventurer 'adv-x' rank 'mithril' not in rank_order", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Adventurer_UnknownChapter_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(advs:
            "\"adv-x\": { \"rank\": \"bronze\", \"dues\": \"current\", \"chapter\": \"nowhere\" }"));
        Assert.Contains("adventurer 'adv-x' chapter 'nowhere' not in cipher_table", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Adventurer_LogbookArchiveMissing_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(advs:
            "\"adv-x\": { \"rank\": \"bronze\", \"dues\": \"current\", \"chapter\": \"hollowmere\", " +
            "\"logbook\": { \"archive_id\": \"no-such-logbook\", \"entries\": 3, \"distinct_seals\": 3 } }"));
        Assert.Contains("adventurer 'adv-x' logbook archive_id not in archive", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Generation_UnknownTask_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(
            generation: "{ \"task_weights\": { \"item_check\": 10, \"nap_time\": 5 } }"));
        Assert.Contains("generation task_weights has unknown task 'nap_time'", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Generation_BadActorPool_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(
            generation: "{ \"per_task\": { \"item_check\": { \"actor_pool\": \"wizard\", \"failure_axes\": [\"amount\"] } } }"));
        Assert.Contains("generation per_task['item_check'] actor_pool invalid", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Generation_BadFailureAxis_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(
            generation: "{ \"per_task\": { \"item_check\": { \"actor_pool\": \"townee_walkin\", \"failure_axes\": [\"vibes\"] } } }"));
        Assert.Contains("generation per_task['item_check'] axis 'vibes' not in enum", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Generation_SeasonOutOfWheel_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(
            generation: "{ \"season_schedule\": { \"wheel\": [\"spring\",\"summer\"], \"by_day\": { \"1\": \"monsoon\" } } }"));
        Assert.Contains("generation season_schedule.by_day['1'] = 'monsoon' not in wheel", Validator.ValidateBanks(data));
    }

    [Fact]
    public void Generation_InvalidRateOutOfRange_Fires()
    {
        var data = MorningQueueData.ParseBanks(Banks(generation: "{ \"invalid_rate\": 1.7 }"));
        Assert.Contains("generation invalid_rate not in [0,1]", Validator.ValidateBanks(data));
    }

    [Fact]
    public void StandingOrder_MissingLimit_Fires()
    {
        // A standing_order posting with neither accept nor total.
        var refs = "{ \"rank_order\": [\"bronze\"], \"cipher_table\": { \"hollowmere\": { \"mark\": \"m\" } }, " +
                   "\"postings\": { \"broken-order\": { \"type\": \"standing_order\", \"item\": \"moonwort\" } }, " +
                   "\"archive\": {} }";
        var data = MorningQueueData.ParseBanks(
            $"{{\"references\":{refs},\"townees\":{{}},\"adventurers\":{{}},\"generation\":{{}}}}");
        Assert.Contains("standing_order 'broken-order' has no accept/total limit", Validator.ValidateBanks(data));
    }

    // ---- helpers -----------------------------------------------------------------

    // A minimal-but-coherent banks payload; drop one bad row into a directory or override
    // the generation block to isolate a single validation family.
    private static string Banks(string townees = "", string advs = "", string generation = "{}")
    {
        var refs = "{ \"rank_order\": [\"copper\",\"bronze\",\"silver\",\"gold\",\"platinum\"], " +
                   "\"cipher_table\": { \"hollowmere\": { \"mark\": \"triple-notched wave\" } }, " +
                   "\"postings\": { \"apothecary-standing-order\": { \"type\": \"standing_order\", " +
                   "\"item\": \"moonwort\", \"accept\": { \"min\": 2, \"max\": 4, \"unit\": \"dram\" } } }, " +
                   "\"archive\": { \"adv-real-logbook\": { \"entries\": 3, \"distinct_seals\": 3 } } }";
        return $"{{\"references\":{refs},\"townees\":{{{townees}}},\"adventurers\":{{{advs}}},\"generation\":{generation}}}";
    }

    private sealed class Wrapper
    {
        public List<Visit> Visitors { get; set; } = new();
    }
}
