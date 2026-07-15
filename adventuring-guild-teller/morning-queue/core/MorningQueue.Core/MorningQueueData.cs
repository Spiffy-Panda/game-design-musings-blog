using System.Text.Json;
using System.Text.Json.Nodes;

namespace MorningQueue.Core;

/// <summary>
/// The parsed reference banks (references.json), keyed sub-tables filtered of their
/// "_"-prefixed metadata rows (`_tab`, `_note`, …) exactly as the GDScript loader did.
/// </summary>
public sealed class References
{
    public string[] RankOrder { get; init; } = Array.Empty<string>();
    public Dictionary<string, BookItem> Book { get; init; } = new();
    public Dictionary<string, Posting> Postings { get; init; } = new();
    public Dictionary<string, Cipher> CipherTable { get; init; } = new();
    public Dictionary<string, ArchiveEntry> Archive { get; init; } = new();
    public Dictionary<string, DropEntry> DropTable { get; init; } = new();
    public Dictionary<string, RankCard> RankLedger { get; init; } = new();
    public Dictionary<string, int> RankupThresholds { get; init; } = new();
    public Roster? Roster { get; init; }
    public Season? Season { get; init; }
    public Payout? Payout { get; init; }

    public static References Parse(string referencesJson)
    {
        var root = JsonNode.Parse(referencesJson) as JsonObject
                   ?? throw new JsonException("references.json: not an object");
        return new References
        {
            RankOrder = root["rank_order"].Deserialize<string[]>(Json.Options) ?? Array.Empty<string>(),
            Book = Json.ParseTable<BookItem>(root["book"]),
            Postings = Json.ParseTable<Posting>(root["postings"]),
            CipherTable = Json.ParseTable<Cipher>(root["cipher_table"]),
            Archive = Json.ParseTable<ArchiveEntry>(root["archive"]),
            DropTable = Json.ParseTable<DropEntry>(root["drop_table"]),
            RankLedger = Json.ParseTable<RankCard>(root["rank_ledger"]),
            RankupThresholds = Json.ParseIntTable(root["rankup_thresholds"]),
            Roster = root["roster"].Deserialize<Roster>(Json.Options),
            Season = root["season"].Deserialize<Season>(Json.Options),
            Payout = root["payout"].Deserialize<Payout>(Json.Options),
        };
    }
}

/// <summary>
/// The complete loaded data set the Validator judges: the reference banks plus the two
/// directories (as inner id-&gt;record maps) and the generation config. Assembled from the
/// coarse JSON payload the bridge hands across from GDScript.
/// </summary>
public sealed class MorningQueueData
{
    public References References { get; init; } = new();
    public Dictionary<string, Townee> Townees { get; init; } = new();
    public Dictionary<string, Adventurer> Adventurers { get; init; } = new();
    public GenerationConfig? Generation { get; init; }

    /// <summary>
    /// Parse the banks payload: an object with keys `references`, `townees`, `adventurers`,
    /// `generation`, where `townees`/`adventurers` are already the inner id-&gt;record maps
    /// (the GDScript loader injects the directory tabs and keeps these inner maps).
    /// </summary>
    public static MorningQueueData ParseBanks(string banksJson)
    {
        var root = JsonNode.Parse(banksJson) as JsonObject
                   ?? throw new JsonException("banks payload: not an object");

        var referencesNode = root["references"];
        var references = referencesNode is JsonObject
            ? References.Parse(referencesNode.ToJsonString())
            : new References();

        return new MorningQueueData
        {
            References = references,
            Townees = Json.ParseTable<Townee>(root["townees"]),
            Adventurers = Json.ParseTable<Adventurer>(root["adventurers"]),
            Generation = root["generation"].Deserialize<GenerationConfig>(Json.Options),
        };
    }
}
