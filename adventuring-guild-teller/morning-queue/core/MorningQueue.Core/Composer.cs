using System.Text.Json;
using System.Text.Json.Nodes;

namespace MorningQueue.Core;

/// <summary>
/// The procedural shift composer — the Core port of the retired
/// scripts/gen/ShiftGenerator.gd (1,151 lines of GDScript). PURE: everything comes in via
/// arguments (day, parsed banks — including the LIVE dues state the Deck passes, which the
/// pay-dues floor beat mutates at runtime — and the compose-time Humanizer); nothing is
/// read from globals and nothing is written anywhere.
///
/// Behavioral contract: CONTENT-BANKS.md §4 recipes — task mix from task_weights,
/// per-task admissible-failure-axis logic gated on actual bank material, coherent
/// actor/gate/drop pairing, directory sample-without-replacement per shift, the isolated
/// season-vs-reach dungeon axes, dues short-circuits, walk-in naming from name_pools —
/// emitting the EXACT visitors.json schema. Determinism: seed = day over the self-owned
/// PCG32 stream (Rng.cs); the golden-week fixtures in the test project pin days 1-7.
///
/// The composed visits then run through the same Shift.Prepare validate + derive pass as
/// the curated day 0, so every generated visit carries inspections.scale.verdict and is
/// held to the same schema contract.
/// </summary>
public static class Composer
{
    public sealed record ComposeResult(JsonArray Visits, int FallbackCount);

    /// <summary>
    /// The bridge entry point for day &gt; 0: banks payload (same shape as Validate's) +
    /// locale JSON in, `{ "visitors": [...validated, verdict-annotated...], "errors": [...] }` out.
    /// </summary>
    public static string GenerateJson(int day, string banksJson, string localeJson)
    {
        var banks = MorningQueueData.ParseBanks(banksJson);
        var humanizer = Humanizer.FromLocaleJson(localeJson);
        var composed = Compose(day, banks, humanizer);
        var payload = new JsonObject { ["visitors"] = composed.Visits }.ToJsonString(Json.Options);
        var prepared = Shift.Prepare(banks.References, payload);
        return Shift.ResultToJson(prepared);
    }

    /// <summary>Compose the full shift for a day &gt; 0. Visits come back in `order` (1..N).</summary>
    public static ComposeResult Compose(int day, MorningQueueData banks, Humanizer humanizer)
    {
        var gen = banks.Generation;
        if (gen is null)
            return new ComposeResult(new JsonArray(), 0);

        var ctx = new Ctx(day, banks, gen, humanizer);
        int min = gen.Shift?.VisitsMin ?? 12;
        int max = gen.Shift?.VisitsMax ?? 16;
        int count = ctx.Rng.RangeInt(min, max);
        double invalidRate = InvalidRate(gen, day);

        var visits = new JsonArray();
        int fallbacks = 0;
        for (int n = 1; n <= count; n++)
        {
            string task = WeightedKey(ctx.Rng, gen.TaskWeights);
            bool isValid = ctx.Rng.NextDouble() >= invalidRate;
            var visit = ComposeVisit(task, isValid, ctx);
            if (visit is null)
            {
                // No material for this (task, valid) combo this shift — fall back to a clean
                // item_check so the queue length is honored and the shift stays coherent.
                fallbacks++;
                visit = ItemCheck(true, ctx) ?? new JsonObject();
            }
            visit["id"] = $"gen-d{day}-{n}";
            visit["order"] = n;
            visit["portrait"] = null;
            visits.Add(visit);
        }
        return new ComposeResult(visits, fallbacks);
    }

    // --- context -----------------------------------------------------------------

    private sealed class Ctx
    {
        public readonly Rng Rng;
        public readonly References Refs;
        public readonly GenerationConfig Gen;
        public readonly Humanizer Loc;
        public readonly Dictionary<string, Townee> Townees;
        public readonly Dictionary<string, Adventurer> Adventurers;
        public readonly string[] RankOrder;
        public readonly string Season;
        public readonly int MaxReach;
        public readonly List<(string Id, int RankIdx)> GatePostings;
        public readonly List<string> StandingOrders;
        public readonly List<(string Id, int Floor, string Season, int Base)> Drops;
        public readonly List<(string Id, string Posting, string Assigned)> Tokens;
        public readonly HashSet<string> UsedTownees = new();
        public readonly HashSet<string> UsedAdventurers = new();
        public readonly HashSet<string> UsedAxes = new();

        public Ctx(int day, MorningQueueData banks, GenerationConfig gen, Humanizer humanizer)
        {
            Rng = new Rng(day);
            Refs = banks.References;
            Gen = gen;
            Loc = humanizer;
            Townees = banks.Townees;
            Adventurers = banks.Adventurers;
            RankOrder = Refs.RankOrder.Length > 0
                ? Refs.RankOrder
                : new[] { "copper", "bronze", "silver", "gold", "platinum" };
            Season = SeasonFor(gen, day, Refs);

            MaxReach = 0;
            foreach (var p in Refs.Roster?.Parties ?? new List<RosterParty>())
                MaxReach = Math.Max(MaxReach, p.ReachFloor);

            GatePostings = new List<(string, int)>();
            StandingOrders = new List<string>();
            foreach (var (pid, p) in Refs.Postings)
            {
                if (p.Type == "standing_order")
                {
                    StandingOrders.Add(pid);
                    continue;
                }
                // Pure rank gates: rank_min present, no ward requirement (ward gates are
                // quest_file material; rank_gate uses pure rank gates).
                if (p.RankMin is null) continue;
                if (p.WardRequired is { Length: > 0 }) continue;
                GatePostings.Add((pid, Array.IndexOf(RankOrder, p.RankMin)));
            }

            Drops = new List<(string, int, string, int)>();
            foreach (var (did, d) in Refs.DropTable)
                if (d.IsDrop)
                    Drops.Add((did, d.Floor, d.Season ?? "", d.BaseBounty));

            Tokens = new List<(string, string, string)>();
            foreach (var (tid, t) in Refs.Archive)
                if (t.Posting is not null && t.AssignedTo is not null)
                    Tokens.Add((tid, t.Posting, t.AssignedTo));
        }

        public int RankIdx(string rank) => Array.IndexOf(RankOrder, rank);
    }

    private static string SeasonFor(GenerationConfig gen, int day, References refs)
    {
        var byDay = gen.SeasonSchedule?.ByDay;
        var key = day.ToString();
        if (byDay is not null && byDay.TryGetValue(key, out var sv) && sv.ValueKind == JsonValueKind.String)
            return sv.GetString() ?? "summer";
        var wheel = gen.SeasonSchedule?.Wheel
                    ?? refs.Season?.Wheel
                    ?? new[] { "spring", "summer", "autumn", "winter" };
        if (wheel.Length == 0) return "summer";
        return wheel[day % wheel.Length];
    }

    private static double InvalidRate(GenerationConfig gen, int day)
    {
        var byDay = gen.InvalidRateByDay;
        if (byDay is not null && byDay.TryGetValue(day.ToString(), out var rv) && Json.AsNumber(rv) is { } r)
            return r;
        return Json.AsNumber(gen.InvalidRate) ?? 0.45;
    }

    // --- dispatch ------------------------------------------------------------------

    private static JsonObject? ComposeVisit(string task, bool isValid, Ctx ctx) => task switch
    {
        "item_check" => ItemCheck(isValid, ctx),
        "rank_gate" => RankGate(isValid, ctx),
        "quest_file" => QuestFile(isValid, ctx),
        "completion_claim" => CompletionClaim(isValid, ctx),
        "rank_up" => RankUp(isValid, ctx),
        "roster_change" => RosterChange(isValid, ctx),
        "dungeon_drop" => DungeonDrop(isValid, ctx),
        _ => ItemCheck(isValid, ctx),
    };

    // --- pools & picking -------------------------------------------------------------

    private static string WeightedKey(Rng rng, Dictionary<string, JsonElement>? weights)
    {
        if (weights is null) return "";
        double total = 0;
        var keys = new List<string>();
        foreach (var (k, v) in weights)
        {
            if (k.StartsWith('_')) continue;
            var w = Json.AsNumber(v) ?? 0;
            if (w <= 0) continue;
            total += w;
            keys.Add(k);
        }
        if (keys.Count == 0) return "";
        double roll = rng.NextDouble() * total;
        double acc = 0;
        foreach (var k in keys)
        {
            acc += Json.AsNumber(weights[k]) ?? 0;
            if (roll <= acc) return k;
        }
        return keys[^1];
    }

    /// <summary>
    /// Weighted pick over failure_axis_weights restricted to `admissible` (weight 1 when
    /// unlisted). Per-shift sample-without-replacement bias (a port-time design change,
    /// flagged in the handoff): an axis not yet emitted this shift is preferred over a
    /// repeat, so a shift's rejects spread across the reachable axes instead of the heavy
    /// weights starving the rare ones (fieldability / claimant / reach never surfaced in a
    /// whole pure-weighted week). Weights still decide among the fresh axes; when every
    /// admissible axis has already appeared, the plain weighted pick returns.
    /// </summary>
    private static string PickAxis(Ctx ctx, List<string> admissible)
    {
        if (admissible.Count == 0) return "";
        var fresh = admissible.Where(a => !ctx.UsedAxes.Contains(a)).ToList();
        var pool = fresh.Count > 0 ? fresh : admissible;
        // Among FRESH axes the pick is uniform (a rare axis gets fair first refusal); the
        // authored failure_axis_weights apply once every admissible axis has appeared.
        bool uniform = fresh.Count > 0;
        var weights = ctx.Gen.FailureAxisWeights;
        var sub = new Dictionary<string, JsonElement>();
        foreach (var a in pool)
        {
            double w = 1.0;
            if (!uniform && weights is not null && weights.TryGetValue(a, out var wv))
                w = Json.AsNumber(wv) ?? 1.0;
            sub[a] = JsonSerializer.SerializeToElement(w);
        }
        var pick = WeightedKey(ctx.Rng, sub);
        if (pick.Length > 0)
            ctx.UsedAxes.Add(pick);
        return pick;
    }

    /// <summary>
    /// Pick an id preferring one not yet used this shift; marks it used. Falls back to a
    /// used id when the pool is exhausted — correctness over no-repeat.
    /// </summary>
    private static string PickUnused(Rng rng, List<string> ids, HashSet<string> used)
    {
        if (ids.Count == 0) return "";
        var fresh = ids.Where(id => !used.Contains(id)).ToList();
        var pool = fresh.Count > 0 ? fresh : ids;
        var pick = pool[rng.RangeInt(0, pool.Count - 1)];
        used.Add(pick);
        return pick;
    }

    private static List<string> TowneeIds(Ctx ctx, bool ownersOnly)
        => ctx.Townees
            .Where(kv => !ownersOnly || (kv.Value.Owns is { Length: > 0 }))
            .Select(kv => kv.Key).ToList();

    private static List<string> Owing(Ctx ctx, IEnumerable<string> ids, bool adventurers)
        => ids.Where(id => Dues(ctx, id, adventurers) == "owing").ToList();

    private static List<string> Current(Ctx ctx, IEnumerable<string> ids, bool adventurers)
        => ids.Where(id => Dues(ctx, id, adventurers) == "current").ToList();

    private static string Dues(Ctx ctx, string id, bool adventurers)
        => adventurers
            ? ctx.Adventurers.TryGetValue(id, out var a) ? a.Dues ?? "current" : "current"
            : ctx.Townees.TryGetValue(id, out var t) ? t.Dues ?? "current" : "current";

    private static string WalkinName(Ctx ctx)
    {
        var given = ctx.Gen.NamePools?.Given is { Length: > 0 } g ? g : new[] { "Corin" };
        var sur = ctx.Gen.NamePools?.Surname is { Length: > 0 } s ? s : new[] { "Ashdown" };
        return $"{ctx.Rng.Pick(given)} {ctx.Rng.Pick(sur)}";
    }

    private static readonly string[] WalkinProfessionsFallback =
        { "Courier", "Porter", "Runner", "Carrier", "Errand-hand", "Drayman" };

    private static string WalkinProfession(Ctx ctx)
    {
        var pros = ctx.Gen.NamePools?.Professions is { Length: > 0 } p ? p : WalkinProfessionsFallback;
        return ctx.Rng.Pick(pros);
    }

    // --- unit / number / prose helpers -------------------------------------------------

    private static string NumStr(double a)
        => a == Math.Floor(a) && !double.IsInfinity(a)
            ? ((long)a).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : a.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string Units(double amount, string unit)
    {
        var plural = (amount == 1.0 || unit.Length == 0) ? unit : unit + "s";
        return $"{NumStr(amount)} {plural}";
    }

    private static string OwedStr(JsonElement? owed) => NumStr(Json.AsNumber(owed) ?? 0);

    private static string FmtArr(Ctx ctx, string[]? arr)
        => arr is null || arr.Length == 0 ? "—" : string.Join(" / ", arr.Select(ctx.Loc.Humanize));

    // --- inspection builders -----------------------------------------------------------

    private static JsonObject Insp(string glassReading, bool glassRel,
                                   string scaleReading, double? amount, string? unit, bool scaleRel)
        => new()
        {
            ["glass"] = new JsonObject { ["reading"] = glassReading, ["relevant"] = glassRel },
            ["scale"] = new JsonObject
            {
                ["reading"] = scaleReading,
                ["amount"] = amount is { } a ? JsonValue.Create(a == Math.Floor(a) ? (long)a : a) : null,
                ["unit"] = unit,
                ["relevant"] = scaleRel,
            },
        };

    // A decoy Scale reading for a non-weighed subject. Reads generation.json's
    // decoy_scales[kind]; the in-code table is a fallback ONLY if the key is absent.
    private static (string Reading, double? Amount, string? Unit) DecoyScale(string kind, Ctx ctx)
    {
        if (ctx.Gen.DecoyScales is not null && ctx.Gen.DecoyScales.TryGetValue(kind, out var e)
            && e.ValueKind == JsonValueKind.Object)
        {
            var d = e.Deserialize<DecoyScale>(Json.Options);
            if (d is not null)
                return (d.Reading ?? "", Json.AsNumber(d.Amount), d.Unit);
        }
        return kind switch
        {
            "rank_card" => ("A guild card in its case: 2 drams on the pan.", 2, "dram"),
            "transfer_seal" => ("The transfer card: 3 drams of waxed board.", 3, "dram"),
            "completion_token" => ("The token disc: 4 drams of good brass.", 4, "dram"),
            "logbook" => ("The logbook: 6 drams of vellum.", 6, "dram"),
            _ => ("Nothing on the pan; a petition, not a delivery.", null, null),
        };
    }

    // --- item_check ---------------------------------------------------------------------

    private static JsonObject? ItemCheck(bool isValid, Ctx ctx)
    {
        var orders = ctx.StandingOrders;
        if (orders.Count == 0) return null;

        string axis = "";
        if (!isValid)
        {
            // Admissible axes depend on the order/item; pick the axis first, then an order
            // that admits it (identity needs a confusable item; paperwork needs `requires`).
            var admissible = new List<string> { "amount" };
            if (OrdersWithConfusable(ctx, orders).Count > 0) admissible.Add("identity");
            if (OrdersWithRequires(ctx, orders).Count > 0) admissible.Add("paperwork");
            axis = PickAxis(ctx, admissible);
        }

        string orderId = axis switch
        {
            "identity" => PickOne(ctx.Rng, OrdersWithConfusable(ctx, orders)),
            "paperwork" => PickOne(ctx.Rng, OrdersWithRequires(ctx, orders)),
            _ => PickOne(ctx.Rng, orders),
        };
        var order = ctx.Refs.Postings[orderId];
        var item = order.Item ?? "";
        ctx.Refs.Book.TryGetValue(item, out var itemRec);
        var unit = itemRec?.Unit ?? "dram";

        var name = WalkinName(ctx);
        var profession = WalkinProfession(ctx);
        var summary = $"A delivery of \"{ctx.Loc.Humanize(item)}\" against the {ctx.Loc.Humanize(orderId)}.";
        var asserts = new JsonObject { ["item"] = item, ["against"] = orderId };

        var checks = new JsonArray();
        var glassReading = itemRec?.Glass ?? "";
        bool glassRel = false, scaleRel = false;
        double amount = AmountWithin(ctx.Rng, order);
        JsonObject truth;

        if (isValid)
        {
            // Both tools are load-bearing on a clean item_check (identity + amount).
            glassRel = true;
            scaleRel = true;
            checks.Add(Chk("book", item, itemRec?.Tells, "the delivered item", "match"));
            checks.Add(Chk("posting", orderId, new[] { LimitStr(order) }, "weighed amount", LimitResult(order, amount)));
            truth = Approve();
        }
        else if (axis == "identity")
        {
            var conf = itemRec?.ConfusableWith?[0] ?? "";
            ctx.Refs.Book.TryGetValue(conf, out var confRec);
            glassReading = confRec?.Glass ?? "";
            glassRel = true;
            checks.Add(Chk("book", item, itemRec?.Tells, "the delivered item",
                $"mismatch: it is {ctx.Loc.Humanize(conf)}"));
            checks.Add(Chk("book", conf, confRec?.Tells, "the delivered item", "match"));
            checks.Add(Chk("posting", orderId, new[] { LimitStr(order) }, "weighed amount",
                LimitResult(order, amount) + " — but the item is wrong"));
            truth = Reject("identity",
                $"It is {ctx.Loc.Humanize(conf)}, not {ctx.Loc.Humanize(item)}. Same family, but the tells part them — the book calls it on the look-alike.");
        }
        else if (axis == "paperwork")
        {
            var req = order.Requires;
            checks.Add(Chk("book", item, itemRec?.Tells, "the delivered item", "match"));
            checks.Add(Chk("posting", orderId, new[] { LimitStr(order), $"{FmtArr(ctx, req)} present" },
                "the delivery", $"amount within range; {FmtArr(ctx, req)} absent"));
            truth = Reject("paperwork",
                $"The goods are right and within the limit, but the order requires {FmtArr(ctx, req)} the deliverer never obtained — right goods, incomplete request.");
        }
        else // amount
        {
            scaleRel = true;
            // `total` orders only fail by falling short (more than needed still meets);
            // `accept` windows can fail either side. Never emit a negative amount: an
            // under-fail that would go below zero flips to an over-fail (the GD original
            // retried "over" after the fact; here the guard is up front).
            var dir = "under";
            if (order.Accept is not null)
                dir = ctx.Rng.NextDouble() < 0.5 ? "over" : "under";
            amount = AmountFail(ctx.Rng, order, dir);
            if (amount < 0 && order.Accept is not null)
            {
                dir = "over";
                amount = AmountFail(ctx.Rng, order, "over");
            }
            amount = Math.Max(0, amount);
            checks.Add(Chk("book", item, itemRec?.Tells, "the delivered item", "match"));
            checks.Add(Chk("posting", orderId, new[] { LimitStr(order) }, "weighed amount",
                $"{dir} the order's limit"));
            truth = Reject("amount",
                $"The item is genuine, but the weighed amount is {dir} the order's limit — {Units(amount, unit)} does not fill {LimitStr(order)}.");
        }

        var scaleReading = $"The scale settles at {Units(amount, unit)}.";
        var insp = Insp(glassReading, glassRel, scaleReading, amount, unit, scaleRel);
        var story = string.Format("Book to {0}; {1}. Weigh it: {2}. {3}",
            ctx.Loc.Humanize(item),
            (isValid || axis != "identity") ? "tells match" : "the tells fall to the look-alike",
            Units(amount, unit),
            isValid ? "Stamp APPROVED." : "Stamp REJECTED.");
        return Visit(name, "townee", profession, "item_check", summary, asserts, truth, checks,
            insp, story, $"Generated item_check ({(isValid ? "clean" : axis)}).");
    }

    private static List<string> OrdersWithConfusable(Ctx ctx, List<string> orders)
        => orders.Where(oid =>
        {
            var item = ctx.Refs.Postings[oid].Item ?? "";
            return ctx.Refs.Book.TryGetValue(item, out var rec) && rec.ConfusableWith is { Length: > 0 };
        }).ToList();

    private static List<string> OrdersWithRequires(Ctx ctx, List<string> orders)
        => orders.Where(oid => ctx.Refs.Postings[oid].Requires is { Length: > 0 }).ToList();

    private static string PickOne(Rng rng, List<string> arr)
        => arr.Count == 0 ? "" : arr[rng.RangeInt(0, arr.Count - 1)];

    private static int AmountWithin(Rng rng, Posting order)
    {
        if (order.Accept is { } acc) return rng.RangeInt(acc.Min, acc.Max);
        if (order.Total is { } tot) return tot.Needed;
        return 1;
    }

    private static int AmountFail(Rng rng, Posting order, string dir)
    {
        int delta = rng.RangeInt(1, 3);
        if (order.Accept is { } acc)
            return dir == "over" ? acc.Max + delta : acc.Min - delta;
        if (order.Total is { } tot)
            return Math.Max(0, tot.Needed - 1); // under only; clamped non-negative
        return 0;
    }

    private static string LimitStr(Posting order)
    {
        if (order.Accept is { } a) return $"accept {a.Min}-{a.Max} {a.Unit}";
        if (order.Total is { } t) return $"total {t.Needed} {t.Unit}";
        return "no limit";
    }

    private static string LimitResult(Posting order, double amount)
    {
        if (order.Accept is { } acc)
        {
            if (amount < acc.Min) return "under range";
            if (amount > acc.Max) return "over range";
            return "within range";
        }
        if (order.Total is { } tot)
            return amount >= tot.Needed ? "meets" : "under";
        return "no limit to measure";
    }

    // --- rank_gate ------------------------------------------------------------------------

    private static JsonObject? RankGate(bool isValid, Ctx ctx)
    {
        var gates = ctx.GatePostings;
        if (gates.Count == 0) return null;
        var allAdv = ctx.Adventurers.Keys.ToList();

        string axis = "";
        if (!isValid)
        {
            var admissible = new List<string> { "rank", "unverifiable" };
            if (Owing(ctx, allAdv, adventurers: true).Count > 0) admissible.Add("dues");
            axis = PickAxis(ctx, admissible);
        }

        if (!isValid && axis == "unverifiable")
        {
            // A walk-in claiming a rank with no card on file — mirrors ganton-reeve. The
            // check entry deliberately deep-links the curated no-card ledger row (the GD
            // original did the same; see the port report).
            var claimedGate = gates[ctx.Rng.RangeInt(0, gates.Count - 1)];
            var claimedRank = ctx.Refs.Postings[claimedGate.Id].RankMin ?? "bronze";
            var name = WalkinName(ctx);
            var uchecks = new JsonArray
            {
                Chk("ledger", "ganton-reeve", new[] { "any card on file" }, "the ledger",
                    "no card on file under that name"),
            };
            var utruth = Reject("unverifiable", "No card, no dues record — word alone is not proof at the desk.");
            var (ur, ua, uu) = DecoyScale("default", ctx);
            var uinsp = Insp("He lays no card down — nothing to examine but his word.", false, ur, ua, uu, false);
            return Visit(name, "adventure", "Freelancer", "rank_gate",
                $"Claims rank {ctx.Loc.Humanize(claimedRank)} for the {ctx.Loc.Humanize(claimedGate.Id)}, but presents no card.",
                new JsonObject { ["rank"] = claimedRank, ["posting"] = claimedGate.Id, ["proof"] = "none" },
                utruth, uchecks, uinsp,
                "Search the directory — no card on file. Nothing to check against. Stamp WITHHELD.",
                "Generated rank_gate (unverifiable).");
        }

        // Directory-actor branches. Pick a coherent (actor, gate) pair so a valid actor truly
        // meets its gate and a rank-fail actor truly falls short.
        string advId;
        (string Id, int RankIdx) gate = default;
        bool haveGate = false;
        if (isValid)
        {
            advId = PickUnused(ctx.Rng, AdvsMeetingGate(ctx, gates, allAdv, meets: true, currentOnly: true), ctx.UsedAdventurers);
            if (advId == "") return null;
            haveGate = GateMeeting(ctx, gates, ctx.RankIdx(ctx.Adventurers[advId].Rank ?? ""), true, out gate);
        }
        else if (axis == "dues")
        {
            advId = PickUnused(ctx.Rng, Owing(ctx, allAdv, adventurers: true), ctx.UsedAdventurers);
            if (advId == "") return null;
            haveGate = GateMeeting(ctx, gates, ctx.RankIdx(ctx.Adventurers[advId].Rank ?? ""), true, out gate);
        }
        else // rank
        {
            advId = PickUnused(ctx.Rng, AdvsMeetingGate(ctx, gates, allAdv, meets: false, currentOnly: true), ctx.UsedAdventurers);
            if (advId == "")
                return RankGate(true, ctx); // no under-ranked material: degrade to a clean gate (GD parity)
            haveGate = GateMeeting(ctx, gates, ctx.RankIdx(ctx.Adventurers[advId].Rank ?? ""), false, out gate);
        }
        if (advId == "" || !haveGate) return null;

        var adv = ctx.Adventurers[advId];
        var rankMin = ctx.Refs.Postings[gate.Id].RankMin ?? "";
        var summary = $"Rank {ctx.Loc.Humanize(adv.Rank ?? "")}, taking the {ctx.Loc.Humanize(gate.Id)} ({ctx.Loc.Humanize(rankMin)} or better).";
        var asserts = new JsonObject { ["rank"] = adv.Rank, ["posting"] = gate.Id };
        var checks = new JsonArray();
        JsonObject truth;
        var (dr, da, du) = DecoyScale("rank_card", ctx);

        if (isValid)
        {
            checks.Add(Chk("adventurer_directory", advId, new[] { adv.Rank ?? "", "dues current" }, "the directory", "match"));
            checks.Add(Chk("posting", gate.Id, new[] { $"rank_min: {rankMin}" }, "the rank", "meets"));
            truth = Approve();
        }
        else if (axis == "dues")
        {
            checks.Add(Chk("adventurer_directory", advId, new[] { "dues current" }, "the directory",
                $"owing — {OwedStr(adv.Owed)}g outstanding"));
            checks.Add(Chk("posting", gate.Id, new[] { $"rank_min: {rankMin}" }, "the rank", "meets, but membership lapsed"));
            truth = Reject("dues",
                $"The rank clears the gate, but guild dues are {OwedStr(adv.Owed)}g in arrears — membership lapsed; clear the dues before taking the posting.");
        }
        else
        {
            checks.Add(Chk("adventurer_directory", advId, new[] { adv.Rank ?? "" }, "the directory", "match"));
            checks.Add(Chk("posting", gate.Id, new[] { $"rank_min: {rankMin}" }, adv.Rank ?? "", "below"));
            truth = Reject("rank",
                $"Under-ranked: a {ctx.Loc.Humanize(adv.Rank ?? "")} card against a {ctx.Loc.Humanize(rankMin)} gate — a rung short. That is Floor work, not a desk exception.");
        }

        var insp = Insp($"A {adv.Rank} guild card, dues column inked.", false, dr, da, du, false);
        var story = string.Format("Pull the card from the directory — {0}, {1}. Posting reads {2}-or-better. {3}",
            ctx.Loc.Humanize(adv.Rank ?? ""),
            adv.Dues == "current" ? "dues current" : "dues owing",
            ctx.Loc.Humanize(rankMin),
            isValid ? "Stamp ASSIGNED." : "Stamp REFUSED.");
        return Visit(adv.Name ?? "", "adventure", adv.Profession ?? "Adventurer", "rank_gate",
            summary, asserts, truth, checks, insp, story,
            $"Generated rank_gate ({(isValid ? "clean" : axis)}).");
    }

    private static bool GateMeeting(Ctx ctx, List<(string Id, int RankIdx)> gates, int rankIdx,
                                    bool meets, out (string Id, int RankIdx) gate)
    {
        var pool = gates.Where(g => g.RankIdx >= 0 && (meets ? g.RankIdx <= rankIdx : g.RankIdx > rankIdx)).ToList();
        if (pool.Count == 0) { gate = default; return false; }
        gate = pool[ctx.Rng.RangeInt(0, pool.Count - 1)];
        return true;
    }

    private static List<string> AdvsMeetingGate(Ctx ctx, List<(string Id, int RankIdx)> gates,
                                                List<string> allAdv, bool meets, bool currentOnly)
    {
        var outp = new List<string>();
        foreach (var aid in allAdv)
        {
            var adv = ctx.Adventurers[aid];
            if (currentOnly && adv.Dues != "current") continue;
            int ri = ctx.RankIdx(adv.Rank ?? "");
            if (gates.Any(g => g.RankIdx >= 0 && (meets ? g.RankIdx <= ri : g.RankIdx > ri)))
                outp.Add(aid);
        }
        return outp;
    }

    // --- quest_file --------------------------------------------------------------------

    private static JsonObject? QuestFile(bool isValid, Ctx ctx)
    {
        var owners = TowneeIds(ctx, ownersOnly: true);
        if (owners.Count == 0) return null;

        string axis = "";
        if (!isValid)
        {
            var admissible = new List<string>();
            if (Owing(ctx, owners, adventurers: false).Count > 0) admissible.Add("dues");
            if (FieldabilityOwners(ctx, owners).Count > 0) admissible.Add("fieldability");
            if (admissible.Count == 0) return null;
            axis = PickAxis(ctx, admissible);
        }

        string tId, postingId;
        if (isValid)
        {
            var pairs = ValidQuestPairs(ctx, owners);
            if (pairs.Count == 0) return null;
            (tId, postingId) = pairs[ctx.Rng.RangeInt(0, pairs.Count - 1)];
            ctx.UsedTownees.Add(tId);
        }
        else if (axis == "dues")
        {
            tId = PickUnused(ctx.Rng, Owing(ctx, owners, adventurers: false), ctx.UsedTownees);
            postingId = ctx.Townees[tId].Owns![0];
        }
        else // fieldability
        {
            var fowners = FieldabilityOwners(ctx, owners);
            (tId, postingId) = fowners[ctx.Rng.RangeInt(0, fowners.Count - 1)];
            ctx.UsedTownees.Add(tId);
        }

        var t = ctx.Townees[tId];
        ctx.Refs.Postings.TryGetValue(postingId, out var posting);
        var ward = posting?.WardRequired ?? Array.Empty<string>();
        var summary = $"Files the {ctx.Loc.Humanize(postingId)} for posting.";
        var asserts = new JsonObject { ["posting"] = postingId };
        if (ward.Length > 0)
            asserts["ward_required"] = new JsonArray(ward.Select(w => (JsonNode)w).ToArray());
        var checks = new JsonArray();
        JsonObject truth;
        var (fr, fa, fu) = DecoyScale("default", ctx);
        const string glass = "A filing slip, not a specimen — only ink under the lens.";

        if (isValid)
        {
            checks.Add(Chk("townee_directory", tId, new[] { "dues current" }, "the directory", "dues current"));
            checks.Add(Chk("posting", postingId, new[] { "posting open" }, "the filing", "open"));
            checks.Add(Chk("roster", "*", new[] { "an eligible party is active" }, "active parties",
                "match: a party can field it"));
            truth = Approve();
        }
        else if (axis == "dues")
        {
            checks.Add(Chk("townee_directory", tId, new[] { "dues current" }, "the directory",
                $"owing — {OwedStr(t.Owed)}g outstanding"));
            truth = Reject("dues",
                $"Posting rights lapsed — {OwedStr(t.Owed)}g in dues outstanding; clear them before filing a new posting.");
        }
        else
        {
            checks.Add(Chk("posting", postingId, new[] { $"ward_required: {FmtArr(ctx, ward)}" }, "the request", "ward required"));
            checks.Add(Chk("roster", "*", new[] { $"wards contains {FmtArr(ctx, ward)}" }, "every active party",
                "no eligible party registered"));
            truth = Reject("fieldability",
                $"No {FmtArr(ctx, ward)}-warded party is registered as active — no one on the roster could take it, whoever comes or goes.");
        }

        var insp = Insp(glass, false, fr, fa, fu, false);
        var story = string.Format("Item n/a, rank n/a. {0} {1}",
            isValid ? "Dues current; the roster can field it."
                    : (axis == "dues" ? "Dues owing — no new post." : "No warded party on the books."),
            isValid ? "Stamp FILED." : "Stamp REJECTED.");
        return Visit(t.Name ?? "", "townee", t.Profession ?? "Petitioner", "quest_file",
            summary, asserts, truth, checks, insp, story,
            $"Generated quest_file ({(isValid ? "clean" : axis)}).");
    }

    private static List<(string Townee, string Posting)> ValidQuestPairs(Ctx ctx, List<string> owners)
    {
        var have = RosterWards(ctx.Refs);
        var outp = new List<(string, string)>();
        foreach (var tid in owners)
        {
            var t = ctx.Townees[tid];
            if (t.Dues != "current") continue;
            foreach (var pid in t.Owns ?? Array.Empty<string>())
            {
                ctx.Refs.Postings.TryGetValue(pid, out var p);
                var ward = p?.WardRequired ?? Array.Empty<string>();
                if (ward.Length == 0 || WardsSatisfied(ward, have))
                    outp.Add((tid, pid));
            }
        }
        return outp;
    }

    private static List<(string Townee, string Posting)> FieldabilityOwners(Ctx ctx, List<string> owners)
    {
        var have = RosterWards(ctx.Refs);
        var outp = new List<(string, string)>();
        foreach (var tid in owners)
        {
            var t = ctx.Townees[tid];
            if (t.Dues != "current") continue;
            foreach (var pid in t.Owns ?? Array.Empty<string>())
            {
                ctx.Refs.Postings.TryGetValue(pid, out var p);
                var ward = p?.WardRequired ?? Array.Empty<string>();
                if (ward.Length > 0 && !WardsSatisfied(ward, have))
                    outp.Add((tid, pid));
            }
        }
        return outp;
    }

    private static HashSet<string> RosterWards(References refs)
    {
        var have = new HashSet<string>();
        foreach (var p in refs.Roster?.Parties ?? new List<RosterParty>())
            foreach (var w in p.Wards ?? Array.Empty<string>())
                have.Add(w);
        return have;
    }

    /// <summary>A ward_required is satisfied if ANY listed ward is on the roster (cleric OR water, etc.).</summary>
    private static bool WardsSatisfied(string[] wardRequired, HashSet<string> have)
        => wardRequired.Any(have.Contains);

    // --- completion_claim ---------------------------------------------------------------

    private static JsonObject? CompletionClaim(bool isValid, Ctx ctx)
    {
        if (ctx.Tokens.Count == 0) return null;
        var tok = ctx.Tokens[ctx.Rng.RangeInt(0, ctx.Tokens.Count - 1)];

        string axis = "";
        if (!isValid)
            axis = PickAxis(ctx, new List<string> { "claimant", "authenticity" });

        var name = WalkinName(ctx);
        var claimant = tok.Assigned;
        var glass = "A completion slip under a genuine guild seal — the wax unbroken, the impression clean.";
        bool glassRel = false;
        var checks = new JsonArray();
        JsonObject truth;

        if (isValid)
        {
            checks.Add(Chk("archive", tok.Id, new[] { "seal genuine" }, "the token", "match"));
            checks.Add(Chk("posting", tok.Posting, new[] { $"assigned_to: {ctx.Loc.Humanize(tok.Assigned)}" },
                "the payee", "match: the assigned party claims it"));
            truth = Approve();
        }
        else if (axis == "claimant")
        {
            claimant = OtherParty(ctx, tok.Assigned);
            checks.Add(Chk("archive", tok.Id, new[] { "seal genuine" }, "the token", "match"));
            checks.Add(Chk("posting", tok.Posting, new[] { $"assigned_to: {ctx.Loc.Humanize(tok.Assigned)}" },
                "the payee", "mismatch: payee is not the assigned party"));
            // A hold under four-verdict; a reject under STRICT_BINARY (mirrors ivy-threnody).
            truth = new JsonObject
            {
                ["valid"] = false,
                ["stamp"] = "hold",
                ["binary"] = "reject",
                ["failure"] = new JsonObject
                {
                    ["axis"] = "claimant",
                    ["reason"] = $"The token is real, but the quest was assigned to {ctx.Loc.Humanize(tok.Assigned)} — right proof, wrong claimant.",
                },
            };
        }
        else // authenticity
        {
            glass = "The completion seal is broken and re-pressed — the wax cracked, the impression doubled.";
            glassRel = true;
            checks.Add(Chk("archive", tok.Id, new[] { "seal genuine" }, "the token", "mismatch: seal broken, re-pressed"));
            truth = Reject("authenticity",
                "The completion seal is forged — broken and re-pressed. No genuine slip backs it in the archive.");
        }

        var (dr, da, du) = DecoyScale("completion_token", ctx);
        var summary = $"Brings a completion token for the {ctx.Loc.Humanize(tok.Posting)} and asks the bounty be paid out.";
        var asserts = new JsonObject { ["posting"] = tok.Posting, ["token"] = tok.Id, ["claimant"] = claimant };
        var insp = Insp(glass, glassRel, dr, da, du, false);
        var story = "Match the token's seal, then the assigned party. " +
            (isValid ? "Both hold — stamp PAID."
                     : axis == "claimant" ? "Right seal, wrong party — stamp HELD."
                                          : "Seal forged — stamp REJECTED.");
        return Visit(name, "townee", "Claimant", "completion_claim", summary, asserts, truth, checks,
            insp, story, $"Generated completion_claim ({(isValid ? "clean" : axis)}).");
    }

    private static string OtherParty(Ctx ctx, string notThis)
    {
        var ids = (ctx.Refs.Roster?.Parties ?? new List<RosterParty>())
            .Where(p => p.Id is not null && p.Id != notThis)
            .Select(p => p.Id!).ToList();
        return ids.Count == 0 ? notThis : ids[ctx.Rng.RangeInt(0, ids.Count - 1)];
    }

    // --- rank_up ---------------------------------------------------------------------

    private static JsonObject? RankUp(bool isValid, Ctx ctx)
    {
        var thresholds = ctx.Refs.RankupThresholds;
        var allAdv = ctx.Adventurers.Keys.ToList();

        string axis = "";
        if (!isValid)
        {
            var admissible = new List<string>();
            if (DupAdventurers(ctx).Count > 0) admissible.Add("duplicate");
            if (UnderThresholdAdventurers(ctx).Count > 0) admissible.Add("rank");
            if (Owing(ctx, allAdv, adventurers: true).Count > 0) admissible.Add("dues");
            if (admissible.Count == 0) return null;
            axis = PickAxis(ctx, admissible);
        }

        string advId = isValid
            ? PickUnused(ctx.Rng, ValidRankupAdventurers(ctx), ctx.UsedAdventurers)
            : axis switch
            {
                "duplicate" => PickUnused(ctx.Rng, DupAdventurers(ctx), ctx.UsedAdventurers),
                "rank" => PickUnused(ctx.Rng, UnderThresholdAdventurers(ctx), ctx.UsedAdventurers),
                // Prefer an owing adventurer whose logbook is otherwise valid (a pure dues fail).
                _ => PickUnused(ctx.Rng, Owing(ctx, allAdv, adventurers: true), ctx.UsedAdventurers),
            };
        if (advId == "") return null;

        var adv = ctx.Adventurers[advId];
        var lb = adv.Logbook!;
        var from = lb.From ?? "";
        var to = lb.To ?? "";
        int threshold = thresholds.TryGetValue($"{from}->{to}", out var thr) ? thr : 3;
        var archiveId = lb.ArchiveId ?? "";
        int entries = lb.Entries, distinct = lb.DistinctSeals;

        var summary = $"A rank-up application, {ctx.Loc.Humanize(from)} to {ctx.Loc.Humanize(to)}, with a logbook of sealed completions.";
        var asserts = new JsonObject { ["from"] = from, ["to"] = to, ["logged_completions"] = entries };
        var checks = new JsonArray();
        var glass = $"{entries} slips, {distinct} distinct seals — each grain its own.";
        bool glassRel = false;
        JsonObject truth;
        var (dr, da, du) = DecoyScale("logbook", ctx);

        if (isValid)
        {
            checks.Add(Chk("archive", archiveId, new[] { $"{distinct} distinct seals", "all his" },
                "archived slips", "match"));
            checks.Add(Chk("adventurer_directory", advId, new[] { $"{from}->{to}: {threshold}" },
                $"{distinct} completions", "meets"));
            truth = Approve();
        }
        else if (axis == "duplicate")
        {
            glass = $"{entries} slips — but two seals share one grain, struck from a single die; only {distinct} are distinct.";
            glassRel = true;
            checks.Add(Chk("archive", archiveId, new[] { $"{entries} entries", "distinct seals" },
                "the logbook", "mismatch: two seals identical"));
            truth = Reject("duplicate",
                $"Padded logbook: two entries carry one seal impression — only {distinct} distinct completions exist, short of the {threshold} needed.");
        }
        else if (axis == "rank")
        {
            checks.Add(Chk("archive", archiveId, new[] { $"{distinct} distinct seals" }, "the logbook", "match, but too few"));
            checks.Add(Chk("adventurer_directory", advId, new[] { $"{from}->{to}: {threshold}" },
                $"{distinct} completions", "below threshold"));
            truth = Reject("rank",
                $"Honest work, but {distinct} sealed completions is short of the {threshold} the {ctx.Loc.Humanize(from)}->{ctx.Loc.Humanize(to)} step requires.");
        }
        else // dues
        {
            checks.Add(Chk("archive", archiveId, new[] { $"{distinct} distinct seals" }, "the logbook", "match"));
            checks.Add(Chk("adventurer_directory", advId, new[] { "dues current" }, "the directory",
                $"owing — {OwedStr(adv.Owed)}g outstanding"));
            truth = Reject("dues",
                $"The logbook meets the threshold, but guild dues are {OwedStr(adv.Owed)}g in arrears — settle them before the promotion is stamped.");
        }

        var insp = Insp(glass, glassRel, dr, da, du, false);
        var story = "Count the sealed entries against the archive. " +
            (isValid ? "All distinct, threshold met — stamp PROMOTED." : "Stamp DENIED.");
        return Visit(adv.Name ?? "", "adventure", adv.Profession ?? "Adventurer", "rank_up",
            summary, asserts, truth, checks, insp, story,
            $"Generated rank_up ({(isValid ? "clean" : axis)}).");
    }

    private static List<string> ValidRankupAdventurers(Ctx ctx)
        => ctx.Adventurers.Where(kv =>
        {
            var adv = kv.Value;
            if (adv.Dues != "current" || adv.Logbook is null) return false;
            var lb = adv.Logbook;
            int thr = ctx.Refs.RankupThresholds.TryGetValue($"{lb.From}->{lb.To}", out var t) ? t : 3;
            return lb.DistinctSeals >= thr && lb.DistinctSeals == lb.Entries;
        }).Select(kv => kv.Key).ToList();

    private static List<string> DupAdventurers(Ctx ctx)
        => ctx.Adventurers.Where(kv => kv.Value.Logbook is { } lb && lb.DistinctSeals < lb.Entries)
            .Select(kv => kv.Key).ToList();

    private static List<string> UnderThresholdAdventurers(Ctx ctx)
        => ctx.Adventurers.Where(kv =>
        {
            if (kv.Value.Logbook is not { } lb) return false;
            int thr = ctx.Refs.RankupThresholds.TryGetValue($"{lb.From}->{lb.To}", out var t) ? t : 3;
            return lb.Entries < thr;
        }).Select(kv => kv.Key).ToList();

    // --- roster_change ---------------------------------------------------------------

    private static JsonObject? RosterChange(bool isValid, Ctx ctx)
    {
        var ciphers = ctx.Refs.CipherTable;
        var allAdv = ctx.Adventurers.Keys.ToList();

        string axis = "";
        if (!isValid)
        {
            var admissible = new List<string> { "authenticity" };
            if (Owing(ctx, allAdv, adventurers: true).Count > 0) admissible.Add("dues");
            axis = PickAxis(ctx, admissible);
        }

        string advId;
        if (!isValid && axis == "dues")
            advId = PickUnused(ctx.Rng, Owing(ctx, allAdv, adventurers: true), ctx.UsedAdventurers);
        else if (isValid)
            advId = PickUnused(ctx.Rng, Current(ctx, allAdv, adventurers: true), ctx.UsedAdventurers);
        else
            advId = PickUnused(ctx.Rng, allAdv, ctx.UsedAdventurers);
        if (advId == "") return null;

        var adv = ctx.Adventurers[advId];
        var chapter = adv.Chapter ?? "";
        ciphers.TryGetValue(chapter, out var cipher);
        var mark = cipher?.Mark ?? "";
        var seal = cipher?.Seal ?? "";
        var summary = $"Presents a transfer card from the {ctx.Loc.Humanize(chapter)} chapter; asks to be entered on the roster as {ctx.Loc.Humanize(adv.Rank ?? "")}.";
        var asserts = new JsonObject { ["action"] = "welcome", ["chapter"] = chapter, ["rank"] = adv.Rank };
        var checks = new JsonArray();
        var glass = cipher?.Glass ?? "";
        bool glassRel = false;
        JsonObject truth;
        var (dr, da, du) = DecoyScale("transfer_seal", ctx);

        if (isValid)
        {
            glassRel = true;
            checks.Add(Chk("cipher", chapter, new[] { $"mark: {mark}", $"seal: {seal}" }, "the card", "match"));
            checks.Add(Chk("adventurer_directory", advId, new[] { "dues stamped current" }, "the card", "match"));
            truth = Approve();
            truth["roster_write"] = new JsonObject
            {
                ["party"] = advId,
                ["rank"] = adv.Rank,
                ["wards"] = new JsonArray((adv.Wards ?? Array.Empty<string>()).Select(w => (JsonNode)w).ToArray()),
            };
        }
        else if (axis == "dues")
        {
            checks.Add(Chk("cipher", chapter, new[] { $"mark: {mark}" }, "the card", "match"));
            checks.Add(Chk("adventurer_directory", advId, new[] { "dues stamped current" }, "the card",
                $"owing — {OwedStr(adv.Owed)}g outstanding"));
            truth = Reject("dues",
                $"The cipher matches, but the card's dues stamp is stale — {OwedStr(adv.Owed)}g outstanding. No roster-write until the dues clear.");
        }
        else // authenticity — a composed forged reading (borrow another chapter's mark)
        {
            var other = OtherChapter(ctx, ciphers, chapter);
            var otherMark = ciphers.TryGetValue(other, out var oc) ? oc.Mark ?? "a foreign mark" : "a foreign mark";
            glass = $"The mark is a {otherMark} pressed in wax where the {ctx.Loc.Humanize(chapter)}'s {mark} belongs.";
            glassRel = true;
            checks.Add(Chk("cipher", chapter, new[] { $"mark: {mark}", $"seal: {seal}" }, "the card",
                $"mismatch: mark is a {otherMark}, not the {ctx.Loc.Humanize(chapter)}'s"));
            truth = Reject("authenticity",
                $"Forged card: the seal carries a {otherMark}, not the {ctx.Loc.Humanize(chapter)}'s {mark} — an impostor claiming a chapter that never sealed it.");
        }

        var insp = Insp(glass, glassRel, dr, da, du, false);
        var story = string.Format("Cipher table to {0}: {1} in wax. Read the card's seal. {2}",
            ctx.Loc.Humanize(chapter), mark,
            isValid ? "Match — copy name, rank, wards into the roster; stamp ENROLLED." : "Stamp REJECTED.");
        return Visit(adv.Name ?? "", "adventure", adv.Profession ?? "Adventurer", "roster_change",
            summary, asserts, truth, checks, insp, story,
            $"Generated roster_change ({(isValid ? "clean" : axis)}).");
    }

    private static string OtherChapter(Ctx ctx, Dictionary<string, Cipher> ciphers, string notThis)
    {
        var ids = ciphers.Keys.Where(c => c != notThis).ToList();
        return ids.Count == 0 ? notThis : ids[ctx.Rng.RangeInt(0, ids.Count - 1)];
    }

    // --- dungeon_drop ---------------------------------------------------------------------

    private static JsonObject? DungeonDrop(bool isValid, Ctx ctx)
    {
        var season = ctx.Season;
        int maxReach = ctx.MaxReach;
        var allTownees = TowneeIds(ctx, ownersOnly: false);
        var inSeason = ctx.Drops.Where(d => d.Season == season).ToList();

        string axis = "";
        if (!isValid)
        {
            var admissible = new List<string> { "identity" };
            if (Owing(ctx, allTownees, adventurers: false).Count > 0) admissible.Add("dues");
            if (DropsOutOfSeason(ctx, season, maxReach).Count > 0) admissible.Add("season");
            if (DropsUnreachable(ctx, season, maxReach).Count > 0) admissible.Add("reach");
            axis = PickAxis(ctx, admissible);
        }

        // Actor.
        string tId = (!isValid && axis == "dues")
            ? PickUnused(ctx.Rng, Owing(ctx, allTownees, adventurers: false), ctx.UsedTownees)
            : PickUnused(ctx.Rng, Current(ctx, allTownees, adventurers: false), ctx.UsedTownees);
        if (tId == "")
            tId = PickUnused(ctx.Rng, allTownees, ctx.UsedTownees);
        if (tId == "") return null;
        var t = ctx.Townees[tId];

        var checks = new JsonArray();
        JsonObject truth;
        var (fr, fa, fu) = DecoyScale("default", ctx);
        const string glass = "A commission slip — the drop itself is still down in the deep.";
        string item;

        if (isValid)
        {
            var pool = ctx.Drops.Where(d => d.Season == season && d.Floor <= maxReach).ToList();
            if (pool.Count == 0) return null; // no valid drop this season/reach; caller falls back
            var d = pool[ctx.Rng.RangeInt(0, pool.Count - 1)];
            item = d.Id;
            var quote = Quote(ctx.Refs, d.Base, d.Floor);
            checks.Add(Chk("townee_directory", tId, new[] { "dues current" }, "the directory", "dues current"));
            checks.Add(Chk("drop_table", item, new[] { "is_drop: true", $"floor: {d.Floor}" },
                "the commission", $"genuine drop, Floor {d.Floor}"));
            checks.Add(Chk("season", item, new[] { $"season: {season}", $"current: {season}" },
                "the season wheel", "in season"));
            checks.Add(Chk("roster", "*", new[] { $"reach_floor >= {d.Floor}" }, "active parties",
                $"match: a party reaches Floor {maxReach}"));
            truth = Approve();
            truth["quote"] = quote;
        }
        else if (axis == "dues")
        {
            item = inSeason.Count > 0
                ? inSeason[ctx.Rng.RangeInt(0, inSeason.Count - 1)].Id
                : ctx.Drops.Count > 0 ? ctx.Drops[0].Id : "";
            checks.Add(Chk("townee_directory", tId, new[] { "dues current" }, "the directory",
                $"owing — {OwedStr(t.Owed)}g outstanding"));
            truth = Reject("dues",
                $"Posting rights lapsed — {OwedStr(t.Owed)}g in dues outstanding; the commission cannot be quoted until the account is cleared.");
        }
        else if (axis == "season")
        {
            var pool = DropsOutOfSeason(ctx, season, maxReach);
            var d = pool[ctx.Rng.RangeInt(0, pool.Count - 1)];
            item = d.Id;
            checks.Add(Chk("townee_directory", tId, new[] { "dues current" }, "the directory", "dues current"));
            checks.Add(Chk("drop_table", item, new[] { "is_drop: true" }, "the commission", "genuine drop"));
            checks.Add(Chk("season", item, new[] { $"season: {d.Season}", $"current: {season}" },
                "the season wheel", "out of season — short-circuits the quote"));
            truth = Reject("season",
                $"A real drop, but out of season — {ctx.Loc.Humanize(item)} only forms in {ctx.Loc.Humanize(d.Season)}, and it is {ctx.Loc.Humanize(season)}. No quote.");
        }
        else if (axis == "reach")
        {
            var pool = DropsUnreachable(ctx, season, maxReach);
            var d = pool[ctx.Rng.RangeInt(0, pool.Count - 1)];
            item = d.Id;
            checks.Add(Chk("townee_directory", tId, new[] { "dues current" }, "the directory", "dues current"));
            checks.Add(Chk("drop_table", item, new[] { "is_drop: true", $"floor: {d.Floor}" },
                "the drop table", $"Floor {d.Floor}"));
            checks.Add(Chk("roster", "*", new[] { $"reach_floor >= {d.Floor}" }, "active parties",
                $"no party reaches Floor {d.Floor} (deepest is {maxReach})"));
            truth = Reject("reach",
                $"In season, but on Floor {d.Floor} — the deepest active party reaches Floor {maxReach}. Beyond current reach; no quote.");
        }
        else // identity — a shop-craftable item that is not a genuine drop
        {
            item = FakeDropItem(ctx);
            checks.Add(Chk("townee_directory", tId, new[] { "dues current" }, "the directory", "dues current"));
            checks.Add(Chk("book", item, new[] { "shop-craftable" }, "the commission",
                "a bench-craftable item — not a dungeon drop"));
            truth = Reject("identity",
                $"{ctx.Loc.Humanize(item)} is shop-craftable, not a genuine dungeon drop — nothing to fetch from a floor; refer it to a craftsman.");
        }

        var summary = $"Commissions the guild to fetch \"{ctx.Loc.Humanize(item)}\" and asks what it will cost.";
        var asserts = new JsonObject { ["item"] = item, ["action"] = "commission" };
        var insp = Insp(glass, false, fr, fa, fu, false);
        var story = "Drop pipeline: real drop? in season? reachable? " +
            (isValid ? "All clear — stamp QUOTED." : "A gate fails — stamp DECLINED.");
        return Visit(t.Name ?? "", "townee", t.Profession ?? "Commissioner", "dungeon_drop",
            summary, asserts, truth, checks, insp, story,
            $"Generated dungeon_drop ({(isValid ? "clean" : axis)}).");
    }

    // Out-of-season, but reachable — so the season axis is isolated (not confounded by reach).
    private static List<(string Id, int Floor, string Season, int Base)> DropsOutOfSeason(Ctx ctx, string season, int maxReach)
        => ctx.Drops.Where(d => d.Season != season && d.Floor <= maxReach).ToList();

    // In-season, but too deep — so the reach axis is isolated (season passes).
    private static List<(string Id, int Floor, string Season, int Base)> DropsUnreachable(Ctx ctx, string season, int maxReach)
        => ctx.Drops.Where(d => d.Season == season && d.Floor > maxReach).ToList();

    /// <summary>
    /// A book item that is NOT a genuine drop (the dungeon identity fail). Prefers a
    /// mineral/relic/reagent — plausibly "shop-craftable" — never one that shares a drop id.
    /// </summary>
    private static string FakeDropItem(Ctx ctx)
    {
        var dropIds = ctx.Drops.Select(d => d.Id).ToHashSet();
        var pool = ctx.Refs.Book
            .Where(kv => !dropIds.Contains(kv.Key)
                         && kv.Value.Category is "mineral" or "relic" or "reagent")
            .Select(kv => kv.Key).ToList();
        return pool.Count == 0 ? "moonstone" : pool[ctx.Rng.RangeInt(0, pool.Count - 1)];
    }

    private static JsonObject Quote(References refs, int baseBounty, int depth)
    {
        var payout = refs.Payout;
        int premium = payout?.InSeasonPremium ?? 10;
        double rate = payout?.DepthRate ?? 0.25;
        double mult = 1.0 + rate * depth;
        // Godot's round() is half-away-from-zero; keep that (Math.Round defaults to banker's).
        int total = (int)Math.Round(baseBounty * mult, MidpointRounding.AwayFromZero) + premium;
        return new JsonObject
        {
            ["base"] = baseBounty,
            ["depth_multiplier"] = mult,
            ["in_season_premium"] = premium,
            ["total"] = total,
            ["currency"] = "g",
        };
    }

    // --- assembly ------------------------------------------------------------------------

    private static JsonObject Chk(string consult, string entry, string[]? compare, string against, string result)
        => new()
        {
            ["consult"] = consult,
            ["entry"] = entry,
            ["compare"] = new JsonArray((compare ?? Array.Empty<string>()).Select(c => (JsonNode)c).ToArray()),
            ["against"] = against,
            ["result"] = result,
        };

    private static JsonObject Approve()
        => new() { ["valid"] = true, ["stamp"] = "approve", ["binary"] = "approve", ["failure"] = null };

    private static JsonObject Reject(string axis, string reason)
        => new()
        {
            ["valid"] = false,
            ["stamp"] = "reject",
            ["binary"] = "reject",
            ["failure"] = new JsonObject { ["axis"] = axis, ["reason"] = reason },
        };

    private static JsonObject Visit(string name, string affiliation, string profession, string taskType,
                                    string summary, JsonObject asserts, JsonObject truth, JsonArray checks,
                                    JsonObject inspections, string story, string notes)
        => new()
        {
            ["name"] = name,
            ["affiliation"] = affiliation,
            ["profession"] = profession,
            ["task_type"] = taskType,
            ["claim"] = new JsonObject { ["summary"] = summary, ["asserts"] = asserts },
            ["truth"] = truth,
            ["checks"] = checks,
            ["inspections"] = inspections,
            ["player_story"] = story,
            ["notes"] = notes,
        };
}
