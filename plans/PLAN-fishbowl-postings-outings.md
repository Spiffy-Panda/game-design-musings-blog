# PLAN — fishbowl-postings-outings (the board, and what leaves town because of it)

**Mnemonic:** `PNO` (gates `PNO.D*`, milestones `PNO.M*`, research questions `PNO.Q*`).
**Status:** **building** — proposed 2026-07-16; **all nine rulings `PNO.D1`–`PNO.D9` landed 2026-07-16.**
**`PNO.M1` (the board) ✅ landed 2026-07-16** — a posting files on day 2 and expires on day 6 in the live
town; suite 53/53; `--lint --town data` clean at `errors=0 accepted=14 warnings=70`. **`PNO.M2` (outings)
is next and carries two hard preconditions:** a ruling + `DEV-LOG.md` entry **before** the three hash
literals move, and the **unresolved `haunt`-vs-restlessness collision** flagged under *The model — outings*.
`PNO.D1` + `PNO.D3`–`PNO.D9` adopted on the recommendation; **`PNO.D2` was ruled *against* the
recommendation**, and the ruling is better than the recommendation (see *The asks*). Re-drift-checked
against the code 2026-07-16 before building: **21 of 22 claims confirmed, 1 drifted** — and **three of
this spec's own supporting arguments were falsified**, struck in place below. The rulings stand; three
of the reasons given for them did not survive contact with the code.
*(Was: "gated on rulings `PNO.D1`–`PNO.D8`" — an off-by-one; *The asks* has always listed nine.)*
**Parent:** [`PLAN-village-fishbowl.md`](./PLAN-village-fishbowl.md) (`VFB`, first release shipped
2026-07-15). Grandparent musing: `../adventuring-guild-teller/` (`AGT`).
**Build target:** `../adventuring-guild-teller/fishbowl/` — extends the existing prototype in
place. Code + frozen interfaces: [`fishbowl/FISHBOWL.md`](../adventuring-guild-teller/fishbowl/FISHBOWL.md).
**Companions:** [`PLAN-fishbowl-postings-outings.doc-reader.md`](./PLAN-fishbowl-postings-outings.doc-reader.md)
— the kickoff prompt Panda pastes into a fresh chat · [`PLAN-fishbowl-postings-outings.handoff.md`](./PLAN-fishbowl-postings-outings.handoff.md)
— the implementing agent's operating brief (the gate, the hard rules, the delegation doctrine, the traps,
the order of work, what "done" means). Its §2 carries a ready-to-fire reader that **drift-checks this
plan's 22 claims about the code** — verified 2026-07-16, and **re-verify before building**.

> **⚠ Isolation rule (inherited from `VFB`, hard, standing).** Postings/outings sessions **do not
> read or modify** `adventuring-guild-teller/morning-queue/**` or `plans/PLAN-morning-queue-tiers.md`.
> This plan is where the two prototypes are most tempted to touch — the desk prototype has its own
> `postings` bank and a dues rule (owing townees can't post), *as recorded in the AGT plan*. That
> record is the only sanctioned channel; convergence stays a post-v1 decision for Panda (`PNO.D9`).
> **Known-taken name:** `moonwort` is a desk-prototype item (per the AGT plan) — do not reuse it
> here. Collision-check every new site/item name against the AGT plan's record, never by opening
> those files.

**Sub-agents:** every sub-agent prompt carries `CLAUDE.md` Rule 1 (no inline interpreter calls —
no `python -c`, `node -e`; if `import` appears in a shell line, **stop** and write
`scrap_scripts/<lang>/<NN>_<slug>.<ext>` instead) **verbatim, with a stern warning**. Sonnet-tier
models have ignored it before.

## The brief (Panda, 2026-07-16 — the chat dump this plan serves)

> The first pass skipped an important aspect of the game: postings and outings.
>
> * **[Quest|Request]? Posting** — Villager → Teller interaction: the villager realizes they need
>   something, they submit a posting at the guild. This seems like it is a good fit for the
>   storylet system.
> * **[Adventurer] Outings** — Teller → Adventurer interaction. The adventurer pops a quest off the
>   board (here just have a new data type for standing, and a way for postings to add/remove them),
>   they then enter a different schedule mode than their daily around-town schedule and progress
>   through an "off screen" dungeon (all areas are dungeons even if they are fields or mountains, as
>   opposed to multi-floor redbox D&D style). Once they complete the quest or die trying they do a
>   cooldown schedule: rest, gear maintenance and the like. Once cooldown is complete they re-enter
>   daily life.

## The seam this fills

`VFB`'s out-of-scope list said it out loud: *"expedition resolution (the away-flag knob is the
stand-in)"*. The stand-in is `Townee.Away` — a bool, and a **one-way trapdoor**: `Clockwork.ResolveDay`
does `if (world.Day > dd) t.Away = true;` with no return path. Brindle Ashe departs on day 1 and is
gone for every day the sim will ever run. There is no board, no reason to leave, and no way home.

So this is not a bolt-on — it is the deferred half of the bowl's own design, and the bowl is already
shaped to receive it. Three things the first pass built turn out to be exactly the sockets:

- **`guildhall-steps`** is already a place (`board: true`, `shut: true`, hours 0–48, capacity 12),
  already in everyone's orbit — Tam haunts it "watching the road", adventurers "loitering by the
  steps". The desk is shut on purpose; **the board hangs outside the shut door.** No teller in v0 —
  which is exactly why postings must be self-served (`PNO.D4`).
- **`adventurer-default`** already authors an `away` block list, and `Clockwork` already picks block
  lists by state: `(t.Away && plan.Away is {Count:>0}) ? plan.Away : plan.Weekday`. The "different
  schedule mode" Panda asks for is a **generalization of a selector that already exists**, not a new
  concept.
- **`chronicle_since: {days, kind}`** already exists as a predicate. It is, unmodified, the mechanism
  by which news gets home from the dungeon (see *Silence and the retelling*).

## Vocabulary (`PNO.D1` — answering the `[Quest|Request]?` in the brief)

Recommended, and the reason matters more than the words:

| Term | Means | Lives as |
|---|---|---|
| **need** | the unmet want, before anyone acts on it | an L2 pressure crossing a threshold |
| **request** | what the villager carries to the guild | (desk-side; **not modelled in the bowl** — no teller) |
| **posting** | the paper on the board. Has a **state**; `standing` = on the board | `Posting` record + the board index |
| **outing** | one adventurer's trip against one posting | `Outing` record + the phase machine |
| **site** | where an outing goes. A fen and a mountain are sites; so is a cellar | `sites.json` + an offscreen place |

**Recommend striking "quest" from the codebase entirely** — reserve it as a word characters say out
loud, never a type name. The research page's whole finding was a **warm-bureaucracy positioning gap**;
paperwork nouns *are* the brand. A posting is a piece of paper on a board that somebody has to take
down. A quest is what a bard calls it afterward. Naming the type `Quest` quietly re-genres the game
into every other guild sim on the shelf.

**On "a new data type for standing":** read as *the board is data, not gameplay* — a `Posting` whose
`state` is `standing` is on the board; the **board** is the index of standing postings; `post` and
`take`/`expire` effects add and remove. Flagged for correction if the intent was a distinct
`Standing` record type; the stored data is the same either way.

## Contract inputs (already fixed — cite, never fork)

- **`AGT.12` — no death.** Settled: gearless respawn at the dungeon entrance; lost gear persists and
  seeds retrieval quests. Panda's brief says *"complete the quest or die trying"* — **read as rout,
  not death** (`PNO.D3`). This is the one place the brief collides with a settled ruling, and the
  resolution is better than either half (see *The loop that closes*).
- **`AGT.3` — day-cadence outside, async underneath.** Expeditions span days; nothing pins to a
  morning tick. An outing is measured in slots and may cross several dawns.
- **`AGT.10` / pillar III's rule — the summary is gossip, not telemetry.** Bios hold every stat.
- **`AGT.11` — the floor never ticks.** The *bowl* ticks. Nothing here adds a clock the player races.
- **`AGT.8` — directed regard, two-score acceptance.** Who takes what is an affinity question in the
  end; in v0 there is no teller, so taking is self-selected (`PNO.D4`) and `teller_regard` stays
  reserved and authored-static.
- **`AGR.5` — never a scripter.** Postings and sites are authored JSON + a generator. No DSL.
- **`VFB.D1`–`D4`** — 48×30-min slots · engine-free core + CLI + xUnit · JSON-only authoring ·
  12 townees / 6 board-places / 2 adventurers.

## The model — postings

A posting is filed when a townee's pressure crosses a threshold **and** they are somewhere they could
plausibly file it. That is a storylet, exactly as Panda guessed — and the bank already contains the
proof of concept: `stock-runs-low` (Petch) + `fetch-arranged` (Sela) is a **proto-posting** that
resolves as an in-town courier favor. Postings generalize that pair from *a favor someone does in
town* to *a job on the board someone leaves town for*.

Keep both. A town where every need becomes guild paperwork loses its texture — so postings carry a
**`reach`**: `errand` (in-town, a neighbour handles it, the existing `fetch-arranged` path) vs
`posting` (the board, an adventurer, a site). The threshold between them is authored per rule.

**Lifecycle:** `standing → taken → resolved` · `standing → expired` · `taken → abandoned → standing`.
Every transition appends a chronicle entry with its because-list, so the board is explainable on the
same terms as everything else (`AGR.2`).

## The model — outings

**The phase machine** (`PNO.D7`) — generalizes the `Away` bool into a three-state phase:

```
daily ──take──▶ outing ──resolve──▶ cooldown ──restored──▶ daily
                  │                                          ▲
                  └────────── turned-back ───────────────────┘
```

`Clockwork` picks the block list by phase: `plan.Outing ?? plan.Away` (back-compat) · `plan.Cooldown
?? cooldown-default` · `plan.Weekday`. **`Away` becomes derived** (`Phase == Outing`), ~~which keeps
the `SetAway` knob, the `away` hash key, and `departs_day` all working unchanged.~~

> **Struck — false on all three counts** (drift check, 2026-07-16). This was the spec's most confident
> sentence and its worst. `Away` is not merely read; it has **four writers**, and three of the four
> "unchanged" things need edits:
>
> - **Four writers won't compile** as a derived property: `Clockwork.cs:26` (`t.Away = true` — must
>   become `Phase = Outing`, an outing with no site, no posting, no leg), `World.cs:66` (initializer),
>   `World.cs:139` (`SetAway`), `Snapshot.cs:47`.
> - **The snapshot is lossy, and `M2_PressuresSnapshotTests` is the tripwire.** `SnapTownee.Away` is a
>   `bool { get; init; }` (`Snapshot.cs:108`). A 3-state phase + site + leg + slots-in-leg cannot round-trip
>   through one bool. **The snapshot schema must grow, or M2 goes red.**
> - **The `away` hash key compiling unchanged IS the bug, not the reassurance.** `World.cs:172` emits
>   `["away"] = t.Away`. Derived from `Phase == Outing`, it captures only the outing/not-outing bit — so
>   **two worlds differing only in Cooldown-vs-InTown hash identically.** The determinism spine silently
>   stops distinguishing a third of the new state space. `ToHashNode` must emit `phase`, not `away`.
> - **`Away`'s two meanings become contradictory.** `Pressures.cs:29` (`if (t.Away) continue;`) means
>   *"off-sim, freeze drift"*, while this spec wants the party **co-present at the site**. Under one
>   derived bool an on-outing townee would be co-present *and* frozen — site storylets would read
>   stale pressures all trip, and a `reward` paid into `purse` would sit un-drifted until return.
>   **Pick: pressures drift during an outing (and `Pressures.cs:29` keys off something else).**
> - **`departs_day` already outranks `SetAway`.** `SetAway(id, false)` is *already* a no-op for a
>   `departs_day` townee: `World.cs:139` sets the flag then calls `Clockwork.ResolveDay` **in the same
>   call**, which re-runs `Clockwork.cs:26` and sets it straight back. There is no working behavior to
>   preserve — the precedence must be **decided** for `Phase`, not inherited. (This also falsifies
>   `PLAN-village-fishbowl.md:240`'s "send/**return** an adventurer".)
> - **Bonus, pre-existing:** only `adventurer-default` authors an `away` list, so the selector no-ops
>   for **10 of 12 townees** — `SetAway("petch", true)` today freezes Petch's drift while he walks his
>   full weekday itinerary, co-present everywhere. The selector's *shape* generalizes; its *coverage*
>   is 2/12, and the phase machine would inherit the gap.
>
> **`PNO.D6` still stands** (`departs_day` survives the release) — but as a *decision*, not as the free
> ride this paragraph promised.

**Ordering constraint (load-bearing):** phase resolves **before** clockwork, because clockwork reads
the phase to choose the block list. In `Simulation.FinalizeDay`, `Outings.ResolveDay(world)` must run
immediately before the existing `Clockwork.ResolveDay(world)`.

> **Two hazards at that exact insertion point** (drift check, 2026-07-16) — the slot is right, the
> naïve implementation is wrong:
>
> - **The day has already advanced when you get there.** `FinalizeDay` is hash → summary →
>   `World.Day = day + 1` (`Simulation.cs:58`) → `Clockwork.ResolveDay`. So `Outings.ResolveDay` sees
>   the **incoming** day, not the one it is resolving. `SubRngFor` (`World.cs:110`) derives from
>   `world.Day` — **pin the day deliberately**, don't read it.
> - **`Clockwork.ResolveDay`'s first act is `ResetDayStreams()`** (`Clockwork.cs:15`). Any
>   `RngFor("outings")` drawn at that point returns the **stale cached stream** from the finished day
>   (`World.cs:104-108` caches by name) — and its draws are then **discarded by the reset**.
>   `SubRngFor` is cache-immune and is the right call, as this spec already says; this is *why*.
> - **Related, and it bites the phase knob:** `SetAway` → `ResolveDay` → `ResetDayStreams()` **rewinds
>   every named stream's cursor to day-start**. Invisible today (nothing draws at rate 1.0). Once `PNO`
>   adds RNG, any phase knob that calls `ResolveDay` mid-day inherits a stream rewind.
>
> **`PNO.M1` gets to prove this slot with zero RNG** (the board's expiry pass needs none), so `PNO.M2`
> inherits an insertion point that has already been exercised.

**The site — "all areas are dungeons"** (`PNO.D7`). Panda's constraint reads as: *dungeon* is the
game's word for an adventure site, and a site is **not** a floor stack. So a site is an ordered track
of **legs** — `approach → search → the-thing → return` — each with a slot duration and a hazard
weight. A field has legs. A mountain has legs. A cellar has legs, and no more of them than a mountain.

A site **is a place** (`board: false`, `offscreen: true`). This is the highest-leverage decision in
the spec: put the party at a site place and every existing storylet mechanism works off-screen —
co-presence, predicates, regard, the chronicle, because-lists. Dungeon events are just storylets whose
co-presence happens to be somewhere with no roof. ~~**with zero engine changes**~~

> **"Zero engine changes" struck — three gates and one real break** (drift check, 2026-07-16). The
> *idea* survives and the **read side is genuinely clean**: every place lookup is a `TryGetValue` with
> a sane fallback (`StoryletEngine.cs:135,242` · `LineRenderer.cs:20` · `Summarizer.cs:78` ·
> `WorldView.cs:97`), `CommonPlace` returns `""` on no overlap so predicates just fail, **`Hours` and
> `Capacity` are never read at runtime at all** (only `SchemaValidator.cs:22-24`), nothing divides by
> capacity, and `Observatory.gd:213` already hides non-board places. But:
>
> - **`SchemaValidator.cs:57-60` hard-rejects any block place token outside `work|home|away|haunt:<id>`** —
>   a bare `"place": "the-sedge-fen"` **throws at load**. `haunt:<site-id>` is the genuine zero-change
>   path; a dedicated `site:<id>` token costs that file **plus** `Clockwork.ResolvePlace`
>   (`Clockwork.cs:67-74`, whose silent catch-all `_ => t.Home` means a typo sends a townee home with
>   **no error**).
> - **THE BREAK — `TownGenerator.cs:44`: `p.Kind == "home" || !p.Board`.** **Any `board:false` place is
>   a home candidate for generated townees.** An offscreen site silently becomes somewhere townees
>   *live*. No throw; just a nonsense town. `Observatory.gd:359-360` leaks the same way — the New
>   Townee dialog's home dropdown has no board filter and would offer the fen as a home.
> - **`haunt:` mislabels the mode.** `ModeOf` (`Clockwork.cs:76-82`) would tag a site visit `haunt`, and
>   mode feeds `Pressures.BaseDaily` (`Pressures.cs:47`) — **restlessness would burn off at the site**,
>   which is precisely backwards for an outing.
>   > **⚠ This now collides with a live ruling, and the collision is UNRESOLVED — `PNO.M2` must settle it**
>   > (raised 2026-07-16, deliberately not decided here). Panda ruled **restlessness ships broken**
>   > (mode-constant `−0.10` engaged / `+0.06` at rest; **16 of the live town's 18 ride a clamp — 13
>   > floored, 3 pegged**), on the reasoning that restlessness is **directional by design** — the buildup
>   > exists to push a townee somewhere, and today there is nowhere to be pushed — and that **`PNO.M2` is
>   > that somewhere.** `data/lint-accepted.json`'s 14 accepted ratchets cite `PNO.M2` by name as the
>   > milestone that discharges them.
>   >
>   > **But that reading and this bullet point in the same direction only if the discharge is an authored
>   > `take`/`resolve` effect.** If M2 takes the cheap `haunt:<site-id>` path, the mode label alone burns
>   > restlessness off *at the site* — the drive would discharge by **going**, not by **doing**, which is
>   > this bullet's "precisely backwards", and it would clear the 14 findings **for the wrong reason**:
>   > green ledger, unchanged design. **That is the `NTD.Q1` trap wearing new clothes** — a mode-constant
>   > shape whose fix relocates the ratchet while silencing the check, *a green gate over a live
>   > countdown*. Whoever opens M2 owns this question; do not let it be answered by an implementation
>   > detail. **The ledger is the test: if the 14 are still there after M2, the ruling was wrong; if they
>   > vanish because a mode label changed, the ruling was dodged.**
> - **`offscreen: true` is silently ignored until `PlaceDto` grows the field** — `DataJson.Options`
>   doesn't set `UnmappedMemberHandling.Disallow`. Additive and safe; also means a **typo'd key is
>   silently dropped**.
> - **The claim was never testable as stated.** `World.SetOccupants` is `internal` with no
>   `InternalsVisibleTo`, and its only caller is `Clockwork.ResolveDay` — **occupancy has exactly one
>   source: dayplan blocks.** "A place no dayplan references, with townees at it" is a state the engine
>   cannot enter. "Already works off-screen" described an unreachable configuration.
>
> **`PNO.D7` still stands.** Legs-not-floors, site-as-place, `kind`-as-tag are all ruled — the model is
> right. It just isn't free.

**Cooldown** is a day-plan, not a new concept: rest, gear maintenance, paying off what the outing was
for. It is where the consequences land in town, which is what makes it worth simulating rather than
skipping.

## The loop that closes (why this feature earns its keep)

The golden cast is already begging for it, and v0 can't deliver:

**Corvo Lunt.** `purse: 0.2`. Trait `indebted-shame`. `regard: marrow-bray {score: -0.2, tags:
["debtor"]}`. Bio: *"still owes Marrow Bray for a re-hafted axe and has been finding reasons not to
walk past the forge."* Today `debt-nagged` fires, Corvo's regard drops, and **that is all that will
ever happen** — for seven days, or seventy. The bowl can nag him forever and can never let him pay.
A posting with a reward is the only exit the fiction has, and the board is how he finds it.

**And the rout closes it the other way.** `AGT.12` says a failed outing means gear lost, and lost gear
*seeds a retrieval quest*. With a board to seed it onto, that stops being a note in a plan and becomes
a cycle: **a failed outing files its own posting.** Corvo goes for the money, is routed, wakes at the
entrance without the axe he still owes for, walks home — and the axe is now standing on the board for
someone else to fetch. The debt deepens instead of resolving, and it deepens *with a paper trail*.
That is the whole musing's thesis (warm bureaucracy; the desk the town crosses) running as a loop.

Outcomes: **`carried`** (success) · **`rout`** (fail → gear lost → retrieval posting; **no death**) ·
**`turned-back`** (abandoned, no loss).

## Where it lands in CPS

| Layer | Change |
|---|---|
| **L1 Clockwork** | phase-selected block lists; sites as offscreen places; party co-present at the site |
| **L2 Pressures** | no new drives. `purse` funds and rewards; `restlessness` pushes toward the board; `trade` is what a shortage posting is *about*. Four drives stay four. |
| **L3 Storylets** | new predicates (`posting`, `phase`) + new effects (`post`, `take`, `resolve`). The bank grows; the language grows by two nouns. |
| **Summarizer** | **unchanged.** See below. |

**Silence and the retelling** (`PNO.D5`). `Summarizer.Candidates` only ever considers *today's*
chronicle, and `IsCarried` only scans the current day's occupancy. So an event at a site — where no
gossip-carrier is present, and none will share a room with the party until they come home — **can
never reach a summary.** That is not a bug to fix; it is `AGT.10` enforcing itself. You do not get
telemetry from the dungeon. You get the tale when they walk back in.

The mechanism: a `tale-told` storylet on the return day, predicated on `chronicle_since: {days: N,
kind: "outing"}` plus co-presence with a carrier at the Long Table. The outing is silent; **the
retelling is the event**, and it is a town event, carried by Odile, like everything else. ~~Zero
summarizer changes. (Caveat: `chronicle_since` ages out at `world.Day - days`, so `N` must exceed the
longest outing, or the rule must key off the outing record instead.)~~

> **The caveat was aimed at the wrong problem** (drift check, 2026-07-16). `chronicle_since` is a
> **global existence check** — `StoryletEngine.cs:184-187` is a bare `world.Chronicle.Any(...)` over
> day-window + `kind`, with **no role scoping of any kind**. It cannot bind the retelling to the
> returning party, or to any participant at all: **any outing beat by anyone satisfies it for
> everyone.** Odile would retell a tale about a party she never met, on behalf of someone still in the
> fen. The window aging out is a footnote next to that.
>
> **The summarizer really is untouched** (`PNO.D5` stands — `Summarizer.Candidates`' `e.Day == day` +
> current-day `IsCarried` genuinely do enforce the silence, and that is `AGT.10` enforcing itself).
> **But "zero engine changes" is now a predicate problem, not a summarizer one:** `tale-told` needs
> either a role-scoped `chronicle_since` or — better, per this spec's own instinct — to **key off the
> outing record**, which sidesteps both the scoping hole and the window. Deferred to `PNO.M3` where
> the retelling lands.

## What the code needs (grounded — file by file)

- **`Model/StoryletDtos.cs`** — `StoryletPredicatesDto` gains `Posting` (`{state, tags, role}`) and
  `Phase` (`{role: phase}`). `StoryletEffectDto`'s union grows: `Post`, `Take`, `Resolve`.
- **`Engine/StoryletEngine.cs`** — the real work:
  - `CandidateBindings` currently yields **townee ids only**, handles **1–2 roles**, and
    `CheckPredicates` intersects `CommonPlace` over ~~*every* bound role~~ the roles in
    `pred.Copresent`. **Posting roles must bind from the board and be excluded from the co-presence
    intersection.** Keep townee-role arity at ≤2 and the O(n²) search is untouched — a
    `(adventurer, posting)` rule is still one townee role.
    > **Better than feared, worse than free** (drift check, 2026-07-16). **The exclusion is already
    > free:** `StoryletEngine.cs:129` intersects over `pred.Copresent`, **not** `bind.Keys` — so a
    > posting role sitting in `bind` is auto-excluded from co-presence with **no change at all**. But
    > three neighbours assume every bound role is a townee, and they are the real `PNO.M1` work:
    > `BuildEntry` (`:237`) builds `participants` from `Copresent` too, so **a posting role never
    > reaches the chronicle**; `ApplyMarks` (`:257-259`) would **throw** on `world.TowneeById[id]` if a
    > posting role were ever marked; and `ForceFire` (`:46-51`) binds `Copresent` only, so the
    > storylet-browser's force-fire can't drive a posting rule.
  - **This is the first system that cannot be `_binding`-anchored.** v0's entire bank is anchored,
    which is what makes the golden day exact — but *who takes which posting* is emergent by
    definition. The search path stops being a "leave it open for later" and becomes load-bearing.
    > **⚠ The search path is narrower than "open", and the linter's own advice walks into it**
    > (2026-07-16). `CandidateBindings` handles **1–2 roles only**. `--lint`'s `partial-binding` check
    > tells authors to *"drop `_binding` to opt into the search path"* — **and its own remediation advice
    > creates the defect it warns about**: proved by taking it, on a 3-role rule, which then fired **0 of
    > 56 days**. For `PNO.M2` this is survivable *only* because `PNO.D8` (solo outings) keeps townee-role
    > arity at ≤2 — `(adventurer, posting)` is one townee role. **`PNO.D8` is therefore load-bearing on
    > the binder, not just on scope.** Anyone reconsidering party outings is also reopening the search
    > path's arity, and should read this first.
  - `Apply` handles exactly Regard/Pressure/Chronicle; add the three new effects.
  - **The `Flag` predicate is hardcoded to one flag** — `bool actual = flag == "departing_today" &&
    ...DepartingToday;`. Any other flag silently evaluates false and fails the predicate rather than
    erroring. Phases need `on_outing` / `in_cooldown`, so **make it a dispatch table** and have
    unknown flags throw at load (`Data/SchemaValidator.cs`).
- **`Engine/Outings.cs`** *(new)* — the phase machine, leg advance, outcome resolution. `ResolveDay`
  (day boundary, before `Clockwork.ResolveDay`) + `StepSlot` (per slot, **after** `Pressures.DriftSlot`
  and **before** `StoryletEngine.RunSlot`, so site storylets see the current leg).
- **`Engine/Board.cs`** *(new)* — the standing index; file/take/expire; the posting generator.
- **`Engine/State.cs`** — `Townee.Phase`, `Away` derived; `Outing` + `Posting` runtime records.
  Note `DayplanId` is `init` ("authored identity — never mutated") — **do not make it settable**;
  phase selects a *variant within* the plan, which is the idiom already there.
- **`Engine/World.cs`** — `ToHashNode` gains `phase`, outing progress, and a postings digest;
  `SetKnob` gains the new knobs.
- **`Api/WorldView.cs`** — board + outing projections for the observatory.
- **`Data/TownLoader.cs`**, **`SchemaValidator.cs`** — `postings.json`, `sites.json`.
  - **`postings.json` / `sites.json` load as OPTIONAL** (like `storylets/` and `golden/day1.json`
    already are), not as new required files. `TownLoader.Read()` throws `FileNotFoundException` on any
    of its 5 required files — making these required would force the frozen golden fixture to carry
    empty stubs it has no business knowing about. Absent ⇒ empty ⇒ posting-free.
  - **`SchemaValidator` only walks `Weekday` + `Away`** (`:48`:
    `plan.Weekday.Concat(plan.Away ?? ...)`). **New `Outing`/`Cooldown` block lists are unvalidated
    until that line is extended** — and because `Clockwork.ResolvePlace` has a silent catch-all
    (`_ => t.Home`), a typo'd token in a `Cooldown` block sends the townee **home with no error**,
    defeating the validate-then-run discipline the file's own header claims.
  - **`TownGenerator.cs:44` must stop treating `board:false` as "somewhere townees live"** before any
    site is added, or generated townees get housed in the fen. `Observatory.gd:359-360` needs the same
    board filter on the New Townee home dropdown.

## Determinism (`PNO.D2` — the one real hazard)

**The invariant that breaks.** `M1_ClockworkDeterminismTests.At_Default_Config_Hash_Is_Seed_Independent`
asserts the day-hash is identical at seed 1123 and seed 999999, with the comment: *"No RNG is consumed
at storylet_rate 1.0, so the run is deterministic regardless of seed."* That holds today because
`FireGate` early-returns without drawing when `rate >= 1.0`, and the whole bank is anchored. **Postings
and outings need RNG by nature** — who takes what, how the legs resolve.

~~**Recommendation: keep the golden town posting-free.** Postings/sites live in a new fixture
(`data/postings/`, empty in golden).~~ **Struck — `PNO.D2` ruled 2026-07-16, and the ruling inverts
this.** Panda: *"make golden separate and secondary. Set the town up to have all features enabled."*

**The ruling: `data/` is the real town, with every feature on. The golden fixture moves out into its
own frozen, secondary town directory.** Postings and outings land in `data/`; golden stops being the
main town and becomes what it always should have been — a test fixture.

**Why this beats the recommendation (the reason this spec missed).** A golden master that lives
*inside* the live data directory is not frozen. Today, editing `data/townees.json` silently changes
what the golden test asserts against — **the fixture tracks the very thing it is supposed to be
pinning.** That is backwards, and it is what forced the recommendation into "keep postings out of the
main town forever." The ruling cuts the knot: freeze the fixture, let the live town grow.

What it costs and buys:

- **the invariant survives, relocated.** All 22 tests reach their town through
  `TestSupport.LoadGoldenTown()`. Retarget that one line at the frozen fixture and seed-independence,
  the 12/6/2 count pins, and the 7 beats all pass **verbatim** — still a posting-free town, just no
  longer `data/`. `TestSupport.DataDir` stays on `data/`, so the Godot-float round-trip suite sweeps
  every new posting file **automatically** (`Directory.GetFiles(DataDir, "*.json", AllDirectories)`);
- **the observatory can finally see the board.** `FishbowlBridge._Ready()` hardcodes `res://data`,
  and `Observatory.gd` never calls `LoadTown`. Under the struck recommendation the board panel would
  have rendered **permanently empty** and needed a new town-switcher. Under the ruling, `data/` *is*
  the posting town. No new UI;
- **the fixture is a full copy** — `TownLoader` requires all 5 files and has no overlay/merge — so it
  drifts from `data/` over time. For a frozen golden master, **drift is the feature**;
- `PNO` still lands additive: no test edited, no beat rebaselined.

**The mechanism, concretely** (verified against the code 2026-07-16):

```
fishbowl/data/                      the LIVE town — all features on; gains postings.json + sites.json
fishbowl/tests/towns/golden-town/   the FROZEN fixture — simconfig/places/townees/dayplans/traits
                                    + storylets/ + golden/day1.json. Posting-free, forever.
```

- `ProjectPaths.GoldenTownDir()` *(new)* → `<FishbowlRoot()>/tests/towns/golden-town`, anchored on
  `Fishbowl.sln` exactly as `DataDir()` already is (Rule 1: never the invocation CWD).
- `TestSupport.LoadGoldenTown()` → `TownLoader.Load(ProjectPaths.GoldenTownDir())`. **This one line
  moves all 22 tests onto the fixture**, since every test reaches its town through it.
- **`TestSupport.DataDir` stays on `data/`** — it feeds `WriteFloatifiedData`'s
  `Directory.GetFiles(DataDir, "*.json", SearchOption.AllDirectories)` (`TestSupport.cs:57`), so
  `M0.GodotStringify_RoundTrip_Of_Every_File` sweeps every new posting file **for free**. That is the
  "round-trip suite extended over every new `data/` file" requirement, satisfied by not touching it.
- **Keep the fixture out of `data/`** — anything under `data/` gets pulled into that recursive sweep.
  `tests/` is untouched by it and already houses `tests/harness/`.
- `M0:19` asserts `town.Golden` is non-null, so **`golden/day1.json` moves *into* the fixture**; the
  live `data/` loses it and reports `Town.Golden == null`, which is fine — `TownLoader` treats it as
  optional and nothing asserts the live town has one.

**The tripwires this must respect** (drift check): `M0:15-17` pins **exact equality** — `12` townees,
`6` `board:true` places, `2` adventurers. Sites are safe only while `board:false` (which `PNO.D7`
specifies anyway). Storylet count is a **lower bound** (`M0:18`, `>= 7`) and `M3:25-31` is a **subset**
assertion, so **the bank can grow freely** — extra day-1 beats do not fail the golden day.

**Streams.** New named stream `outings`; draw via `world.SubRngFor("outings", posting.Id)` — the
`Rng.SubStream(seed, day, stream, key)` primitive already exists for exactly this. Per-posting
sub-streams mean bank growth never shifts another posting's resolution, at any `storylet_rate`.
(Worth noting for later: the shared `storylets` cursor **is** order-dependent under thinning
(`rate < 1.0`) — adding rules shifts which survive. Harmless today at the default rate; the same
sub-stream trick fixes it whenever someone soaks below 1×.)

**Hash.** Adding `phase`/`postings` keys shifts every day-hash — and costs nothing. M1 compares
run-to-run, M3 pins *beats* (`{storylet, participants}`), and `data/golden/day1.json` carries no hash.
Only a human's written-down hash chip goes stale (**the day-1 baseline was `0ccec96222e31dbe` at
`08c9a22`**, recorded here so the shift is documented rather than discovered). Log it in `DEV-LOG.md`
and move on.

> ~~**no test pins a hash literal.**~~ **Struck — false as written, and the falseness is load-bearing**
> (drift check, 2026-07-16). `DeterminismPrimitivesTests.cs:13-15` pins **three literal hex hashes**
> (`""`→`cbf29ce484222325`, `"a"`, `"foobar"`) and `:23` pins a canonical-JSON format literal. They pin
> the **hash function and canonical format**, not any day-state — so the *conclusion* survives intact.
> ~~The accurate statement is narrower: **no test pins a day-hash *value*; the hash *function and format*
> are pinned.** Changing what enters `World.ToHashNode` is free.~~ Changing `FnvHash` or
> `CanonicalJson`'s 6-decimal float format is **not** — that reddens those literals *and* moves every
> day-hash. (Where the doubt likely came from: `FnvHash.cs:31-33`'s own comment says the hex form is
> *"pinned in golden tests"* — true of the *form*, and it reads fast as the opposite.)

> **Struck again — the narrower claim is false too now, and in the best possible way** (2026-07-16, after
> `db64c30`). **A day-hash *value* is pinned, absolutely.** `M1_ClockworkDeterminismTests.cs:37-53` —
> `Twelve_Townees_Three_Days_Hash_Sequence_Is_Pinned` — hard-asserts the day-1..3 sequence as three
> literals: ~~**`b8d15299d8817639` · `e3478bc4ff7d4848` · `02bc86b987c547c3`**~~ **`2a6a8a3af0a1a81d` ·
> `d615d01daa2c8020` · `619649026a9d8895`**. **Changing what enters `World.ToHashNode` is no longer
> free.** (The *Hash.* paragraph directly above — *"costs nothing … M1 compares run-to-run"* — is the
> same error one level up; read it through this block.)
>
> > **The literals moved, once, later the same day — and NOT for `PNO`** (2026-07-16). Ruled by Panda
> > (`NTD.Q1` + `FBT.Q1`), with the hash consequence stated explicitly before the work started and the
> > `DEV-LOG.md` entry of that date standing as the ruling — i.e. **the obligation this block pre-wrote
> > was discharged exactly as written**, which is the first time in this file that a predicted process
> > actually ran. Cause: `Pressures.BaseDaily`'s `trade` arm stopped being a flat `−0.11/day` countdown
> > and became a restoring force. Two sibling fixes landed in the same change and moved **nothing**,
> > verified by staging them alone and watching the old literals stay green.
> >
> > **`PNO.M2`'s obligation is unchanged and now has a worked precedent:** the `phase` key will redden
> > this test **by design**, and a Panda ruling + a `DEV-LOG.md` entry must land **before** those strings
> > move. Re-baselining to green is still the one forbidden move. Note the bar the `NTD.Q1` move set:
> > the new literals were verified **identical across three fresh CLI processes and the xunit runner**,
> > one of them at `--seed 999999` so `At_Default_Config_Hash_Is_Seed_Independent`'s invariant still
> > holds. *A pin is worthless if it pins a number you got once.*
>
> - **Why it exists.** `Twelve_Townees_Three_Days_Identical_Hash_Sequence` (`:27-34`) only ever compared
>   run A to run B **within a single build** — self-consistency, not stability — so a change that moved
>   every hash *consistently* sailed through it green. It literally did: per `db64c30`, *"It stayed green
>   while a perturbed `ToHashNode` moved the hash."* The determinism contract claims **stability**, and
>   nothing was testing it.
> - **The pin is built on `PNO.D2` — a nice result, not an inconvenience.** The test's own comment: the
>   literals *"were captured from the frozen golden fixture and are the value the contract refers to; the
>   golden town cannot drift (PNO.D2), so nothing but a real change to what enters the hash can move
>   them."* **The ruling this spec records is what makes an absolute pin possible at all** — a hash
>   literal asserted against a town that could be edited underneath it pins nothing, which is precisely
>   the flaw `PNO.D2` cut out. The struck recommendation could never have carried this test.
> - **THE NUANCE — the pin runs against the posting-free frozen fixture.** `RunHashes` (`:68-73`) reaches
>   its town through `TestSupport.LoadGoldenTown()`, and that fixture is posting-free forever. So
>   **authoring `postings.json` / `sites.json` into `data/` cannot move these literals — `PNO.M1` is
>   unaffected**, and the board can land with the test untouched. Only a change to `ToHashNode`'s *shape*
>   moves them — which means **`PNO.M2`'s `phase` key (this spec's own "`ToHashNode` must emit `phase`,
>   not `away`") WILL turn this test red, deliberately, and it is right to.**
> - **The obligation, pre-written by the test itself:** *"If you are here because this test went red: that
>   is the test working. Do not re-baseline it to make it green. Either the change was not supposed to
>   touch the hash — fix the change — or it was, and that needs a ruling in DEV-LOG.md before these
>   strings move."* **Hard `PNO.M2` precondition: a Panda ruling + a `DEV-LOG.md` entry land BEFORE those
>   three literals move.** Re-baselining to green is the one forbidden move.

## Data (JSON, `fishbowl/data/`, `"version": 1`, ints tolerant-parsed)

```jsonc
// sites.json — a fen is a dungeon; so is a mountain (PNO.D7)
{ "id": "the-sedge-fen", "name": "the Sedge Fen", "kind": "fen",
  "distance_slots": 6,                          // travel each way; sets the floor on outing length
  "legs": [                                     // a track, never floors
    { "id": "approach", "slots": 4,  "hazard": 0.10, "activity": "wading in" },
    { "id": "search",   "slots": 10, "hazard": 0.25, "activity": "casting about the reeds" },
    { "id": "the-find", "slots": 3,  "hazard": 0.45, "activity": "at the thing itself" },
    { "id": "return",   "slots": 4,  "hazard": 0.05, "activity": "walking it home" } ] }
```

```jsonc
// postings.json — authored seeds + generator templates. Runtime postings are sim state.
{ "id": "sedgewort-short", "reach": "posting",  // vs "errand" — in-town, courier, no site
  "requester": "petch", "site": "the-sedge-fen",
  "tags": ["fetch", "herbs"], "reward": 0.25,   // paid into the taker's purse on `carried`
  "expires_days": 4,
  "lines": { "hearsay": "Petch wants something out of the fen.",
             "gossip":  "{requester} has a posting up — sedgewort, and not a little of it.",
             "report":  "{requester} filed for sedgewort from {site}; {reward} on delivery, {expires_days}d." } }
```

```jsonc
// storylets/posting-filed.json — Panda's read is right: this is just a storylet
{ "id": "posting-filed", "kind": "posting",
  "predicates": { "copresent": ["A"],
                  "pressure": { "A.trade": { "below": 0.25 } },
                  "phase": { "A": "daily" },
                  "cooldown_days": 3 },
  "effects": [ { "post": { "requester": "A", "template": "sedgewort-short" } },
               { "chronicle": true, "tellability": 0.5 } ] }

// storylets/posting-taken.json — the FIRST rule that cannot be _binding-anchored
{ "id": "posting-taken", "kind": "posting",
  "predicates": { "copresent": ["A"],              // A at the steps; P is NOT co-presence-bound
                  "posting": { "role": "P", "state": "standing" },
                  "phase": { "A": "daily" },
                  "trait": {}, "cooldown_days": 0 },
  "effects": [ { "take": { "adventurer": "A", "posting": "P" } },
               { "chronicle": true, "tellability": 0.7, "mark": ["A"] } ] }
```

```jsonc
// simconfig.json — additions (live knobs, no restart)
{ "posting_rate": 1.0,          // how readily needs become paper
  "posting_expiry_scale": 1.0,
  "outing_hazard_scale": 1.0,   // 0 = nobody is ever routed; 3 = the fen eats everyone
  "outing_pace_scale": 1.0,     // slots per leg
  "cooldown_days": 2,
  "rout_seeds_retrieval": true, // the AGT.12 loop, toggleable for A/B
  "self_select_bias": 1.0 }     // trait/pressure weighting on who takes what (PNO.D4)
```

Also: `dayplans.json` gains `cooldown` variants (+ a shared `cooldown-default`), and
`adventurer-default`'s `away` list is renamed `outing` (keeping `away` as a read alias).

## Observatory additions

> ~~**⚠ The observatory is already out of horizontal room — verified in-engine 2026-07-16 by a GTH
> harness pass, before any `PNO` control was added.** The viewport is **1280** wide, and `btn-storylets`
> **already falls off the right edge the moment a day completes**: the `hash` readout widens by ~103px
> when it goes from `—` to 16 hex digits, shoving the button from x=1173 to x=1276 — a 4px sliver of a
> 90px button. The force-fire debug tool is reachable only *before* you have anything to debug. Right-
> panel bios and summary lines also clip mid-word past x=1280.~~
>
> ~~**So the board panel and outing track cannot simply be appended to the top bar.** Budget the layout
> first; adding two more readouts to a strip that is already overflowing will push `btn-storylets` fully
> off and take the new controls with it. (Note the harness's own caveat: `query_element` reported
> `on_screen:true`/`clickable:true` for a button at x=1276–1366 in a 1280 viewport — **it does not clamp
> to the viewport**, so it will happily tell you an off-screen control is fine. Trust a click, not the
> report.)~~

> **Struck — the hazard is fixed, and the warning had become a trap** (2026-07-16, after `0b0112f`). It
> was true when written; left standing, it would have a future agent budget around a constraint that no
> longer exists. **There is no overflow to budget around.**
>
> - **The overflow dies by construction.** The layout was rebuilt as **two fixed rails** (roster+board /
>   knobs+inspector) around **one fluid reading pane whose width is derived, never declared** —
>   `FISHBOWL.md:27-34` records the rule as load-bearing. The arithmetic comes out right at 1290 *and* at
>   1280 with no second number to keep in sync, so **a new panel no longer has to fight for room**:
>   `PNO`'s board panel has a rail to land in rather than a top bar to overflow. (Also: the **real
>   viewport is 1290×810** — the struck text's 1280 was `project.godot`'s declared figure, not the
>   measured one.)
> - **The fix was `clip_text`, not monospace — this is the reusable lesson, keep it.** Monospace only
>   addresses *a fixed-length string in a proportional font*; **`clip_text` drops a Label's minimum to ~1,
>   so its text stops being a layout demand for any font and any string.** A bare Label's text **is** its
>   minimum width, and a minimum **beats a `FULL_RECT` anchor** — that is how a 108px `hash` once forced
>   the root to 1371px in a 1290 viewport and starved the Dawn Summary. Machine readouts
>   (clock/hash/seed/stats) now go through `_readout()`.
> - **Proven at two different day-hashes producing identical layouts** — the only proof that counts for a
>   **data-dependent** bug. One hash is one string, and one string proves nothing.
> - **The numbers:** Dawn Summary **326px, cut mid-word → 496px, `clipped:[]`**; later **481px**, when the
>   roster rail went 488→503 to buy back legibility — re-verified, not assumed (`visible_fraction 1.0`,
>   `clipped:[]`, at two day-hashes). Narrowing an autowrap pane spends width on *height*, not on
>   clipping.
> - **The `query_element` caveat above is stale too — and its replacement is worse.** `GTH.B1` fixed the
>   viewport clamping: `on_screen` is now **strict** and correctly decoupled from `clickable`, so the
>   report can be trusted (`FISHBOWL.md:225-230`) — "trust a click, not the report" is retired. **But
>   `GTH.B9` is open: `run_scenario` over MCP is a *silent no-op*** — it returns `{}` and executes nothing
>   (`FISHBOWL.md:206-208`). **Every `PNO` gate-check must drive the individual tools directly**, never via
>   a scenario, or it will report success having done nothing at all.

- **Board panel** — standing postings as cards: requester, site, reward, days-to-expiry, taker.
  The board filling and emptying *is* the readout. `test_id`: `board`.
- **Outing track** — for each adventurer not in town: site, leg, slots-in-leg, a four-pip track.
  `test_id`: `outings`.
- **Inspector** — phase chip + current outing + posting history.
- **Stats strip** — postings filed/taken/expired per day · outing outcome mix · **time-to-take**
  (how long paper sits) · the `PNO.Q1` yield split.
- Every new control tagged with a `test_id` per the GTH contract in `FISHBOWL.md`.

## Milestones (each gate-checked in-engine before the next opens)

**Status (2026-07-16):** rulings landed; `PNO.M1` opened. The **`PNO.D2` restructure** (golden → frozen
fixture at `tests/towns/golden-town/`; `data/` → the live all-features town) lands **first, inside
`PNO.M1`** — every later milestone authors into `data/`, so the split has to precede the first posting.
It is a pure move: two lines in `TestSupport`/`ProjectPaths`, no test edited.

- **`PNO.M1` — the board.** ~~◑ **in progress.**~~ ✅ **LANDED 2026-07-16 — accepted, measured.** The
  `PNO.D2` restructure + postings data + `Board` + `post`/expire effects + the `posting` predicate +
  board panel. **Town-side only, no outings.**
  *Accept:* Petch's shortage files a posting; it stands, ages, expires; every transition has a
  because-list; all existing tests green.
  > **Accepted on measurement, not assertion** (live town, `--days 7 --chronicle`, seed 1123):
  > **a posting files on day 2** (`posting-filed`, Petch @ Petch's Simples, slot 16) and **expires on
  > day 6** (`posting-expired`, slot 0 at the Guildhall Steps — 4 days, matching `expires_days: 4`).
  > The full `standing → expired` arc runs in the town you actually play. Suite **53/53**;
  > `--lint --town data` = `errors=0 accepted=14 warnings=70 exit=0`.
  >
  > **The two pieces of real work were real, and one of them was worse than predicted.**
  > - **`expire` synthesizes its own because-list** — 5 facts (`posting`, `filed`, `stood`, `expired`,
  >   `posting_expiry_scale`). As predicted, it is the first chronicle entry ever built outside
  >   `BuildEntry`, because expiry is a **board mechanism, not a rule**.
  > - **`post` was not "work to do" — it was a *silent no-op that was already shipping*.**
  >   `StoryletEffectDto` had no `Post` field, so `System.Text.Json` **dropped the key**, and
  >   `Board.File()` had **zero callers** — *while the chronicle cheerfully printed "filed a posting"*.
  >   The effect union is now enforced at load, so a dropped key is a load error rather than a
  >   plausible nothing. This is the same defect shape the spec already names twice (`copresence_bonus`,
  >   `unknown-drive`): **a number authored in good faith that no engine code ever reads.**
  >
  > **Also fixed, and not on anyone's list: the `place` predicate did not exist.** Before it, prose
  > lied about location — `a-good-catch` could render *"the nets came up heavy off the Long Table"*.
  > `{any:[ids]}` / `{kind:[kinds]}` now gates it. `ApplyMarks` is guarded (it would have **thrown** on
  > `world.TowneeById[id]` if a posting role were ever marked — exactly as the drift check predicted).
  >
  > **What did NOT happen, as predicted:** the three hash literals did not move for `PNO.M1`. The pin
  > runs against the posting-free frozen fixture, so authoring `postings.json` into `data/` cannot reach
  > it. **They did move — for `NTD.Q1`, a Pressures ruling, on the same day.** See *Determinism*.
  - **Why M1 is the right first slice, confirmed by the drift check:** every break the readers found
    (the four `Away` writers, the lossy snapshot, `TownGenerator.cs:44`, the `Pressures` freeze,
    `chronicle_since`'s missing role scope, the `ResetDayStreams` rewind) lands in **`PNO.M2`/`M3`**.
    **None of them touches the board.** M1 is genuinely clean, and it proves the `FinalizeDay`
    insertion point with zero RNG before `M2` needs it to carry draws.
  - **The two pieces of real work hiding in M1**, both from the binder: a posting role must reach the
    chronicle (`BuildEntry` builds `participants` from `Copresent` only), and **expiry needs a
    synthesized because-list** — `post` gets one free from its storylet, but `expire` is a `Board`
    mechanism, not a storylet, so it is the first chronicle entry ever built outside `BuildEntry`.
- **`PNO.M2` — outings.** Phase machine + sites + legs + `take`/`resolve` + cooldown day-plans +
  outing track. *Accept:* a townee takes a standing posting, leaves, is findable at the site at every
  slot, returns, cools down, re-enters daily life — and **`Away`'s one-way trapdoor is gone**.
  Same-seed hash sequence reproduces editor-vs-CLI.
  - **Gated precondition (added 2026-07-16):** M2's `phase` hash key reddens
    `Twelve_Townees_Three_Days_Hash_Sequence_Is_Pinned` **by design** — a **Panda ruling + a `DEV-LOG.md`
    entry must land BEFORE those three literals move**, and re-baselining to green is forbidden. See
    *Determinism*.
- **`PNO.M3` — the loop closes.** Rewards paid; `rout` → gear lost → retrieval posting; the
  `tale-told` retelling reaches a dawn summary. *Accept:* **the Corvo fixture** — Corvo takes a
  paying posting, returns `carried`, pays Marrow, the `debtor` tag clears, and `debt-nagged` stops
  firing. Then the same fixture at `outing_hazard_scale: 3` routs him and puts the axe on the board.
- **`PNO.M4` — instruments.** Stats strip + CLI soak over the posting town (3 seeds × 14 days —
  **14, not 7**: an outing plus cooldown can eat most of a week, so a 7-day soak can't see a full
  cycle). *Accept:* `PNO.Q1`/`Q2` answerable from the report JSON.

## Research questions (what this build exists to answer)

- **`PNO.Q1` — the gossip trade.** Outings **remove bodies from town** — fewer co-present townees,
  fewer beats — then pay it back as a burst on the return day. ~~`VFB.Q1` already sits at **~4.4 distinct
  lines/night with 3/21 nights below 4**.~~ Does the board's own traffic (filed/taken/expired/paid) cover
  the hole while the party is out, or does the town go quiet exactly when two of twelve leave?
  ~~**This could make `VFB.Q1` worse before it makes it better, and that is the thing to measure.**~~
  > **Struck — you cannot measure this with `VFB.Q1`, because `VFB.Q1` cannot go down** (2026-07-16).
  > It is `min(pool, summary_lines)` averaged over a pool of ≥20, reads a flat **5.00**, and holds that
  > with **30 of 46 rules deleted**. **Take two of eighteen out of town and it will still read 5.00** —
  > this question, asked of that metric, is unanswerable and would have come back "no impact" no matter
  > what M2 did to the town. **Ask it of `Api/Variety.cs` instead** (`--soak` / `--report`): distinct
  > rendered *texts*, `rules_fired_but_never_told`, `repeat(any)`. Baseline to measure the hole against,
  > live town, 3 seeds × 14 nights, 2026-07-16 — **47 distinct sentences / 70 delivered lines · novelty
  > 0.67 · repeat(N−1) 0.00 · repeat(any) 0.35 · told/fired 70/319 = 0.22 · 5 of 50 rules never told.**
  > **`told/fired` will not move** — it is `summary_lines × nights / beats fired`, structural, invariant
  > under any reordering. The number that answers `PNO.Q1` is **distinct**, and `repeat(any)` rising is
  > what "the town went quiet" will actually look like.
  > **Also note the board already pays some of this back before M2 exists:** `posting-expired` fires 10×
  > per fortnight and reached a summary once (day 8), at tellability 0.3 — board traffic is already
  > tellable content, so M2's hole opens against a floor that is not zero.
- **`PNO.Q2` — does paper move?** Time-to-take on a standing posting. If postings rot on the board,
  self-selection (`PNO.D4`) is too shy. If they're gone within a slot, the board isn't a board.
- **`PNO.Q3` — is the silence right?** A party is away for days and the summary never mentions them.
  Does that read as *the town not knowing yet* (correct, `AGT.10`) or as *the game forgetting them*?
  Reads-by-feel at M3, and it is the one place `PNO.D5` might have to bend.
- **`PNO.Q4` — does the rout loop land?** Does the axe-on-the-board deepening a debt read as the
  system telling a story, or as a punishment stack?
- **`PNO.Q5` — cooldown length.** Is `cooldown_days: 2` a beat or a lull?

## The asks (rulings that gate the build)

**All nine ruled 2026-07-16.** `PNO.D1` + `PNO.D3`–`PNO.D9` **adopted on the recommendation** (Panda:
*"I accept your recommendations"*). **`PNO.D2` ruled *against* the recommendation** (Panda: *"make
golden separate and secondary. Set the town up to have all features enabled."*) — and the ruling is
the better call; see *Determinism* for why, and what it buys for free.

**The rulings stand as decisions; three of the arguments this spec made *for* them do not.** The
2026-07-16 drift check (21/22 claims confirmed) falsified the phase machine's *"`SetAway` / the `away`
hash key / `departs_day` all work unchanged"* (all three need edits, and the hash key compiling
unchanged **is** the bug), site-as-place's *"zero engine changes"* (three gates + the
`TownGenerator.cs:44` break), and `chronicle_since`'s fitness for `tale-told` (no role scoping at all).
All three are struck in place above. A ruling surviving its own bad argument is the system working —
the argument still had to go, because the next agent would have budgeted from it.

- **`PNO.D1` — vocabulary.** ✅ **Adopted.** posting / request / outing / site; **"quest" struck as a
  type name** — reserved as a word characters say out loud, never a type. The *"new data type for
  standing"* reading is **confirmed**: `standing` is a `Posting.state` and the board is the index of
  standing postings; there is no separate `Standing` record.
- **`PNO.D2` — golden town stays posting-free.** ❌ **Ruled against.** ~~*(recommended: strictly
  additive, every existing test green verbatim, nothing rebaselines)*~~ **Ruled: `data/` is the real
  town with every feature enabled; the golden fixture moves out to its own frozen, secondary town
  directory.** Achieves everything the recommendation wanted (all 22 green verbatim, nothing
  rebaselined — the fixture is still posting-free, just no longer `data/`) *and* fixes a flaw the
  recommendation preserved: **a golden master living inside the live data dir tracks the thing it is
  meant to pin.** It also dissolves a problem the recommendation created — the board panel would have
  rendered permanently empty in the observatory, since `FishbowlBridge._Ready()` hardcodes `res://data`
  and there is no town-switcher. Cost: the fixture is a full copy and will drift. For a frozen master,
  drift is the feature.
- **`PNO.D3` — "or die trying" ⇒ rout, not death.** ✅ **Adopted.** The `AGT.12` read is confirmed:
  gearless respawn, gear persists, **rout seeds a retrieval posting**.
- **`PNO.D4` — who takes a posting.** ✅ **Adopted.** Self-selected by trait + pressure weight; no
  teller in v0, and the desk is shut on purpose. ~~**Note:** this would be the first live consumer of
  `storylet_weight_mods`.~~ **Struck — do NOT wire `storylet_weight_mods`.** Retrofitting it would
  change what fires across the *whole* bank and move `VFB.Q1`'s numbers under Panda mid-measurement.
  Self-selection weighting lives **inside `PNO`'s own code path**; the dead hook keeps its own pending
  ruling.
- **`PNO.D5` — silence while away.** ✅ **Adopted.** No telemetry; the tale on return. The summarizer
  is genuinely untouched — but see the struck caveat above: `chronicle_since` **cannot express**
  "retell *this* party's outing", so `tale-told` keys off the outing record instead. `PNO.M3`.
- **`PNO.D6` — `departs_day` survives** one release as authored sugar, ~~unchanged,~~ as golden-day
  insurance. ✅ **Adopted** — but as a decision, not a free ride: `SetAway(id, false)` is **already** a
  no-op for a `departs_day` townee, so the precedence over `Phase` must be **chosen**, not inherited.
- **`PNO.D7` — site model.** ✅ **Adopted.** Legs not floors; a site is an offscreen place; `kind` is a
  tag, not a structure. Caveat per the struck claim above: it is not free — `haunt:<site-id>` is the
  zero-change path, and `TownGenerator.cs:44` must stop treating `board:false` as "somewhere townees
  live" before a site can exist safely.
- **`PNO.D8` — party size.** ✅ **Adopted.** Solo outings in v1 — keeps townee-role arity at ≤2 and the
  binder's O(n²) search untouched. Parties are the `AGT` floor pillar's eventual business ("what
  adventuring teams go down"); the site-as-place model already supports them when it's time.
- **`PNO.D9` — convergence.** ✅ **Adopted: not now.** The isolation rule holds until both prototypes
  have settled shape; this plan just flags that the collision is real.

## Found while speccing (adjacent, and it bears on `VFB.Q1`)

Two authored hooks are **wired to nothing**, verified by grep across the whole subproject:

- **`copresence_bonus` is a dead knob.** It is declared (`TownDtos.cs:115`), settable
  (`World.cs:126`), projected to the UI (`WorldView.cs:171`), authored in `simconfig.json:8` — and
  **no engine code ever reads it.** Four hits across the whole subproject, not one a consumer.
  `VFB.Q1` is *the* open question on the parent plan and this is one of the instruments pointed at it.
  > **Correction (drift check, 2026-07-16 — the one claim of 22 that drifted).** ~~"and bound to an
  > observatory slider … anyone tuning gossip yield with that dial has been turning a knob connected to
  > nothing."~~ **There is no dial.** `Observatory.gd:167-176` (`_build_knobs`) builds exactly six
  > controls — `actionability`, `storylet_rate`, `pressure_rates.trade`, `summary_lines` sliders +
  > `hearsay_required`, `bio_marks_enabled` checkboxes — and `copresence_bonus` is not among them
  > (`FISHBOWL.md:174-175`'s `test_id` table agrees with the code). The knob is unreachable except via
  > `bridge.SetKnob(...)`. **Nobody was misled by a slider, because nobody could turn one.** The
  > dead-code fact stands; the severity argument was backwards.
  >
  > **Mirror case, worth knowing:** `storylet_cooldown_scale` is the exact inverse — genuinely
  > *consumed* (`StoryletEngine.cs:68`) and settable (`World.cs:125`), but likewise **has no slider**.
  > One live knob with no dial; one dial-less knob with no life.
  >
  > **And the `storylet_rate` slider is half dead.** `FireGate` gates on `rate >= 1.0`
  > (`StoryletEngine.cs:75`), but the slider runs **0.0–3.0** (`Observatory.gd:171`). **The entire
  > 1.0→3.0 half is a no-op** — 2.5 behaves identically to 1.0. Anyone "turning storylet rate up" to
  > move `VFB.Q1` has been moving nothing. Only the 0.0→1.0 half thins.
  >
  > **All three confirmed in-engine 2026-07-16** by a GTH harness pass driving the real observatory —
  > static analysis and behavior agree. `storylet_rate`, fresh boot per arm, seed 1123, day 1 → dawn:
  >
  > | `storylet_rate` | events | day-1 hash |
  > |---|---|---|
  > | 1.00 (default) | 12 | `0ccec96222e31dbe` |
  > | **2.50** | **12** | **`0ccec96222e31dbe`** |
  > | **3.00** | **12** | **`0ccec96222e31dbe`** |
  > | 0.30 | 9 | `6ba1be1c6fc1bb4f` |
  >
  > Byte-identical at 2.5 and 3.0; 0.3 thins 12→9 and diverges the hash, proving the dial is wired at
  > all. The knobs panel was confirmed three ways (snapshot, geometry, source) to hold **exactly six**
  > controls — neither `copresence_bonus` nor `storylet_cooldown_scale` among them.
- **`storylet_weight_mods` is unconsumed.** Every trait in `traits.json` authors it (`gossip-carrier:
  {social: 1.2}`, `curious: {social: 1.25}`, …), `TraitDto` declares it, and nothing reads it —
  `Weight` is used only to *order* candidates in `StoryletEngine.RunSlot`. Trait-flavoured firing
  doesn't exist yet.

Neither is in this plan's scope, and neither should be quietly fixed inside it — changing what fires
would move `VFB.Q1`'s numbers under Panda's feet mid-measurement. Flagged for a ruling of their own.

## Out of scope for `PNO` v0 (say no by list, not by accident)

Desk gameplay (the board is self-served; the desk stays shut) · floor verbs · teller assignment ·
tactical resolution (legs + hazard scalars, never a combat sim) · gear as inventory (gear-lost is a
**flag and a posting**, not an item ledger) · parties (`PNO.D8`) · site pathfinding or any map (a site
is a graph node; travel is slot arithmetic, exactly as places are today) · injuries, morale, XP,
levels · the desk prototype's posting vocabulary (`PNO.D9`) · new drives (four stay four).

## Sync footer (Rule 3)

Advancing this plan touches: this file · `PLAN.md` (index line) ·
[`PLAN-village-fishbowl.md`](./PLAN-village-fishbowl.md) (the out-of-scope line "expedition
resolution (the away-flag knob is the stand-in)" resolves here; `VFB.Q1` gains the `PNO.Q1`
interaction) · `PLAN-adventuring-guild-teller.md` (the floor-economy sketch and `AGR.1`'s request
generator both touch the board) · `../adventuring-guild-teller/fishbowl/FISHBOWL.md` (bridge surface,
data contract, `test_id` table — all currently marked **frozen**; this plan unfreezes them) ·
`../adventuring-guild-teller/fishbowl/README.md` · `../DEV-LOG.md` before every commit.

**Proposal-to-boss tier (not yet written):** the `VFB` pattern is `.md` for the LLMs + HTML pages for
the human. This file is the `.md`. If the rulings land, the read-back page is
`adventuring-guild-teller/postings.html` with claims `PN.1`–`PN.n` — or a section folded into
`fishbowl.html` beneath the `FB.*` claims. Panda's call.
