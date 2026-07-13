# CONTENT-BANKS.md — the build-to contract for the week-of-shifts expansion

This is the **Phase-1 design + skeleton** spec for four data-driven additions to The
Morning Queue, all requested by Panda:

1. **Generalize the Glass** — make the examine tool work on *any* physical subject, not
   just herbs. Every examinable subject carries an authored examined-description and the
   generator derives a visit's Glass reading from it, compared against the matching
   rulebook page.
2. **Broaden the reference material** to a week's worth (7 shifts × ~12–16 visits) so a
   generated week doesn't obviously repeat.
3. **A Townee Directory** (who posts work; pays **dues** to keep posting rights) mirrored
   by an **Adventurer Directory** (the rank/dues/chapter/logbook pool).
4. **A procedural visit generator** that composes a shift from the banks and emits visits
   in the *existing* `visitors.json` schema, unchanged — the card/panel/verdict/scoreboard
   consume them without edits.

Read `MORNING-QUEUE.md` (frozen interfaces, the Loc layer, the class_name cache gotcha)
and `INSPECTION-TOOLS.md` (the examine/weigh loop, the standing-order limit model) first —
this doc extends both and never contradicts them.

> **Repo Rule 1 (verbatim, stern):** *No inline interpreter calls — no `python -c`,
> `py -c`, `node -e`, etc. If `import`/`require` would appear in a shell command, STOP and
> write a file under `scrap_scripts/<lang>/` anchored to the repo root, then run that.
> Shell one-liners (git, ls, one grep) are fine.* GDScript authored **inside** this Godot
> project is the deliverable, not a scrap script. Sonnet-tier models have ignored this
> before; do not.

---

## 0. Phase boundaries — who owns what

| Phase | Owns (writes) | Must NOT touch |
|---|---|---|
| **1 — this doc + skeletons** | `CONTENT-BANKS.md`, `data/townees.json`, `data/adventurers.json`, `data/generation.json` | `references.json`, `visitors.json`, any script |
| **2 — Banks** | `references.json` (ADD the ids §7 lists), enrich the three new JSON files' prose | `visitors.json` (frozen — the curated 16), the generator |
| **3 — Generator + integration** | `scripts/autoload/ShiftGenerator.gd` (new), `DeckLoader.gd` (additive), `loc.gd` (additive tab titles) | the curated `visitors.json`, the frozen component signatures |

**Hard invariants (all phases):** never rename or remove an existing id/key. The curated
16 in `visitors.json` resolve against `references.json` by exact string — ADD, never
disturb `moonwort` / `kingsfoil` / the existing postings / ciphers / drops / archive /
roster. Godot 4.6, GDScript only, `gl_compatibility`. Colors via `Palette`; user-facing
strings via `Loc`. Randomness via a seeded `RandomNumberGenerator` (seed = day). All
invented names fictional — no real people, and no dead names or real surnames (Rule 7).

---

## 1. Bank schemas

Five banks feed the generator. Three already live in `references.json` (extended in place,
additively) and two are new files. Every bank is a **dict keyed by id** (except `roster`,
an array) so a `checks[].entry` string resolves by exact key — the existing lookup
contract.

### 1a. Reference **Book** — items by their tells (`references.json.book`)

Two fields are ADDED to every book item (existing 5 included — additive, non-breaking):

| field | type | meaning |
|---|---|---|
| `category` | string | one of `herb` \| `beast_part` \| `reagent` \| `mineral` \| `relic` |
| `tells` | string[] | the rulebook tells (unchanged; what the Book *page* shows) |
| `glass` | string | **NEW** — the authored examined-description of a *genuine* specimen (the prose the Glass returns). This is the derivation source for a valid visit's `inspections.glass.reading`. |
| `forgery_glass` | string? | **NEW, optional** — the Glass reading a *forged/faked* version gives (drives `authenticity` fails). Present only where `forgery_tells` exists. |
| `sure_tell`, `confusable_with`, `forgery_tells`, `unit`, `hazard` | as today | `confusable_with` drives `identity` fails; `unit` is required for any standing-order item |

`glass` is content prose (lives in the data bundle, never in `Loc`), exactly like
`inspections.glass.reading`. The Banks phase authors `glass` for all 24 items (§7) and
adds `glass` + `category` to the existing 5.

### 1b. **Postings** — gates + standing orders (`references.json.postings`)

Unchanged schema (`MORNING-QUEUE.md` / `INSPECTION-TOOLS.md §4`): gate postings carry
`rank_min` / `ward_required` / `type` / `assigned_to`; standing orders carry `type:
"standing_order"`, `item`, and a limit (`accept {min,max,unit}` **or** `total
{needed,unit}`), optional `requires`. The Banks phase ADDS the postings §7 lists. A townee
in `townees.json` `owns` a subset of these ids.

### 1c. **Chapter Ciphers** (`references.json.cipher_table`)

One field ADDED to every cipher (existing 2 included):

| field | type | meaning |
|---|---|---|
| `mark`, `seal` | string | as today — the genuine transfer-card mark + seal medium |
| `glass` | string | **NEW** — the examined-description of a *genuine* seal from this chapter (Glass derivation source for a valid `roster_change`) |

A forged transfer-seal reading is **composed** by the generator (borrow another chapter's
`mark`, or swap `seal` wax→ink) — see §4. 6 chapters total (§7).

### 1d. **Dungeon Drops** (`references.json.drop_table`) + **Season / Payout / Roster**

`drop_table` schema unchanged (`is_drop`, `floor`, `season`, `base_bounty`); the Banks
phase ADDS the drops §7 lists. `season.current` stays `summer` (day 0); generated days set
the season from `generation.json.season_schedule`. `payout` formula and `roster` schema
are unchanged; the Banks phase may add 1–2 roster parties so reach/ward interlocks stay
live across a week (listed §7, optional).

### 1e. **Completion Archive** (`references.json.archive`)

Schema unchanged (token entries `{seal, posting}`; logbook entries `{entries,
distinct_seals, all_owner}`). The Banks phase ADDS: one `<adv-id>-logbook` per directory
adventurer (mirrors `adventurers.json.logbook`), and ~6 completion tokens for
`completion_claim` visits (§7).

### 1f. **Townee Directory** (`data/townees.json`) — NEW

Keyed by townee id:

| field | type | meaning |
|---|---|---|
| `name` | string | display name (content) |
| `profession` | string | flavor job title |
| `dues` | `current` \| `owing` | **the posting-rights gate** (§2) |
| `owed` | number | gold owed when `owing`, else 0 |
| `owns` | string[] | posting/order ids this townee has posted — each must resolve in `references.json.postings` |
| `blurb` | string | light prose (Banks phase) |

Injected by `DeckLoader` into `references["townee_directory"]` so the ReferencePanel renders
it as an ordinary tab and `consult: "townee_directory"` resolves like any other table.

### 1g. **Adventurer Directory** (`data/adventurers.json`) — NEW

Keyed by adventurer id — the expanded rank ledger and the rank_gate/rank_up/roster_change
actor pool:

| field | type | meaning |
|---|---|---|
| `name`, `profession` | string | identity + flavor |
| `rank` | one of `rank_order` | copper→platinum |
| `dues` | `current` \| `owing` | gates rank_gate / rank_up (§2) |
| `owed` | number | gold owed when `owing` |
| `chapter` | string | home chapter — resolves in `cipher_table` |
| `wards` | string[] | ward capabilities (roster-write / fieldability lever) |
| `logbook` | object | `{ archive_id, entries, distinct_seals, from, to }` — mirrored into `archive` by the Banks phase; drives `rank_up` |
| `blurb` | string | light prose |

Injected into `references["adventurer_directory"]` (same mechanism as the townee tab).

### 1h. **Generation config** (`data/generation.json`) — NEW

`shift` (visit-count range, `id_prefix`, `seed_basis: "day"`), `task_weights`,
`invalid_rate`, `failure_axis_weights`, `per_task` (composition contract per task_type —
`actor_pool`, `subject`, `failure_axes`, `checks_template`, `glass_subject_kind`,
`scale_relevant_when`), `name_pools` (one-off walk-ins), `season_schedule`. Consumed only
by `ShiftGenerator`; never rendered as a tab.

---

## 2. The DUES mechanic

**Townees pay dues to post work.** A townee whose `dues` is `owing` has lost posting
rights: the teller must reject any *new* posting or commission from them until dues are
cleared. This adds one failure axis: **`dues`** (extends the `truth.failure.axis` enum,
additive).

- **Who carries dues:** every townee (`townees.json.dues`) and every adventurer
  (`adventurers.json.dues` — guild membership dues, already a concept in the curated
  `rank_ledger` where each card reads `dues: current`).
- **Which task_types check it:**
  - `quest_file` — a townee **posting a new quest**. Primary dues gate. Owing ⇒ `dues`
    reject before any fieldability check.
  - `dungeon_drop` — a townee **commissioning a fetch**. Owing ⇒ `dues` reject, short-
    circuiting the quote pipeline (before is_drop/season/reach/payout).
  - `rank_gate` / `rank_up` — an adventurer **owing** guild dues fails on `dues` (a card
    on file but membership lapsed). The existing curated rank_gates read `dues: current`,
    so this is the same column, now able to fail.
- **Which do NOT check it:** `item_check` (delivering against an *already-posted, already-
  paid* standing order — the order's existence is the authorization, not the deliverer's
  dues), `completion_claim` and `roster_change` gate on seal/cipher, not dues (though
  `roster_change` admits a secondary `dues` fail via the transfer card's dues stamp).
- **How the teller verifies:** look the actor up in the **Townee Directory** /
  **Adventurer Directory** tab; the `dues` field is authoritative. A generated dues-fail
  visit emits a `{consult: "townee_directory"|"adventurer_directory", entry: <id>,
  compare: ["dues current"], result: "owing — <owed>g outstanding"}` check so the
  ReferencePanel `focus()` deep-links to the row.

`dues` reject reasons are authored prose on `truth.failure.reason` (e.g. *"Posting rights
lapsed — 15g in dues outstanding; clear them before filing."*).

---

## 3. The Glass generalization

The Glass becomes the **examine-any-physical-thing** tool. Every visit whose subject is a
physical object has its `inspections.glass.reading` **derived** by the generator from an
authored source in the banks, and that reading is compared against a specific rulebook
page. Six subject kinds, one derivation rule each:

| `glass_subject_kind` | Glass reading derived from | compared against (rulebook page) | fails it can carry |
|---|---|---|---|
| `book_item` | `book[item].glass` (valid) · confusable item's `glass` (identity fail) · `book[item].forgery_glass` (authenticity fail) | Reference **Book** `tells` for the *claimed* item | identity, authenticity |
| `rank_card` | authored card reading (usually a decoy; the Ledger/Directory is authoritative) | **Adventurer Directory** rank + dues | (decoy — rank/dues decide) |
| `transfer_seal` | `cipher_table[chapter].glass` (valid) · composed forged reading (authenticity fail) | **Chapter Ciphers** `mark` + `seal` | authenticity |
| `completion_token` | `archive[token]` seal reading (valid) · composed forged-seal reading | **Completion Archive** seal genuine | authenticity |
| `logbook` | reading of the logbook's seals (distinct vs duplicated) | **Completion Archive** distinct-seal count | duplicate |
| `filing` | *nothing to examine* — spoken petition / un-delivered commission → the `tool_empty` decoy reading | (no Glass page; Roster / Drop / Season carry the check) | — |

This is exactly the existing loop generalized: an herb's Glass-vs-Book check (`moonwort`),
a seal's Glass-vs-Ciphers check (`perrick-vane`), a logbook's Glass-vs-Archive check
(`marn-culpepper`) are all instances of *read the tool, compare to the matching page*. The
new work is (a) authoring `glass` on every Book item / cipher, (b) the generator picking
the right derivation source per valid/invalid branch.

**Scale generalization is unchanged** (`INSPECTION-TOOLS.md §4`): the Scale is load-bearing
only when a standing-order limit exists (item_check `amount` checks); everywhere else it
returns a decoy reading. Every visit still carries **both** readings.

---

## 4. The visit-generation algorithm

`ShiftGenerator.generate_shift(day)` composes one shift. Determinism: seed a
`RandomNumberGenerator` with `rng.seed = day`. Same day ⇒ identical shift.

**Top loop:**
1. `rng.seed = day`. Read banks (references + townees + adventurers + generation).
2. `season = season_schedule.by_day[str(day)]` (fallback: walk the wheel by `day`).
3. `N = rng.randi_range(visits_min, visits_max)`.
4. For `n` in `1..N`:
   a. **task_type** = weighted pick from `task_weights`.
   b. **actor** = draw from `per_task[task].actor_pool` (directory pick, or a `name_pools`
      walk-in). Avoid drawing the *same directory actor twice in one shift* (sample without
      replacement per shift; walk-ins are unbounded).
   c. **valid?** = `rng.randf() >= invalid_rate` (true ⇒ valid).
   d. if invalid: **axis** = weighted pick from `per_task[task].failure_axes`
      (renormalized subset of `failure_axis_weights`).
   e. **compose** subject + `claim` + `checks[]` + `truth` + `inspections` per the task
      recipe below.
   f. `id = "gen-d%d-%d" % [day, n]`, `order = n`, `portrait = null`, author `player_story`
      + `notes` from templates.
5. Return the array (already in `order`).

**Every emitted visit sets `truth.binary`** (approve/reject) so `STRICT_BINARY` judges it;
where a 4-verdict feel is wanted, `truth.stamp` may be `hold`/`conditional` with `binary:
reject` (as the curated `ivy-threnody` / `odile-vantry` do). Default generated invalids use
`stamp == binary == reject`.

### Per-task recipes

**`item_check`** — deliverer (walk-in townee) presents an item against a standing order.
- Pick a `standing_order` posting → `item` = its `item`, `order` = its id.
- Glass source = `book_item`. Scale source = the order limit.
- **valid:** Glass = `book[item].glass`; Scale amount within `accept`/meets `total`;
  `stamp = approve`. Checks: `book:item` (match), `posting:order` (within/meets).
- **identity fail:** pick `c ∈ book[item].confusable_with`; Glass = `book[c].glass`; add
  check `book:c` (match) + `book:item` (mismatch). Scale still within (decoy). `reject`.
- **authenticity fail** (item has `forgery_glass`): Glass = `book[item].forgery_glass`;
  check `book:item` result "mismatch: forgery tells". `reject`.
- **amount fail:** Glass = genuine; Scale amount pushed outside window (`over`/`under`, or
  `< needed`); check `posting:order` result over/under. `reject`. (Uses the NEW `amount`
  axis; `INSPECTION-TOOLS.md §4` sanctions adding it.)
- **paperwork fail** (order has `requires`, e.g. hazard seal): Glass + Scale both pass;
  check `posting:order` result "requirement absent". `reject`. (Mirrors `odile-vantry`.)

**`rank_gate`** — directory adventurer takes a gate posting.
- Pick a posting with `rank_min` → actor with `rng`-chosen rank relative to the gate.
- Glass = `rank_card` (decoy). Scale decoy.
- **valid:** actor `rank >= rank_min` AND `dues == current`; `approve`. Checks:
  `adventurer_directory:adv` (rank+dues), `posting:p` (meets).
- **rank fail:** actor `rank < rank_min`; check result "below". `reject`.
- **dues fail:** actor `dues == owing`; check `adventurer_directory:adv` result "owing —
  Ng outstanding". `reject`.
- **unverifiable fail:** actor presents no card (a walk-in claiming a rank with no directory
  entry); check "no card on file". `reject`. (Mirrors `ganton-reeve`.)

**`quest_file`** — townee posts a quest they own. **Dues gate.**
- Actor = a townee with a non-empty `owns`; posting = one of their owned ids (a
  `ward_required` posting where possible, for fieldability).
- Glass = `filing` (empty). Scale empty.
- **valid:** `dues == current` AND a roster party satisfies `ward_required`/reach;
  `approve`. Checks: `townee_directory:t` (dues current), `posting:p` (ward), `roster:*`
  (eligible party).
- **dues fail:** `dues == owing`; check `townee_directory:t` result "owing"; short-circuit.
  `reject`.
- **fieldability fail:** dues current but no roster party carries the required ward;
  check `roster:*` result "no eligible party". `reject`. (Mirrors `sister-coll`.)

**`completion_claim`** — a claimant brings a completion token.
- Pick an archive token → its `posting` (assigned to a party). Claimant may or may not be
  the assigned party.
- Glass = `completion_token`. Scale decoy.
- **valid:** seal genuine AND claimant == `assigned_to`; `approve`.
- **claimant fail:** seal genuine but claimant ≠ assigned party; check `posting` result
  "payee ≠ assigned party". `binary: reject` (`stamp: hold` optional). (Mirrors
  `ivy-threnody`.)
- **authenticity fail:** composed forged-seal Glass reading; check `archive:token` result
  "seal broken/forged". `reject`.

**`rank_up`** — adventurer submits a logbook.
- Actor = directory adventurer; `threshold = rankup_thresholds[from->to]`.
- Glass = `logbook` (reads distinct vs duplicated seals). Scale decoy.
- **valid:** `logbook.distinct_seals >= threshold`; `approve`. Checks: `archive:logbook`
  (distinct seals), `adventurer_directory:adv` (threshold met).
- **duplicate fail:** `distinct_seals < entries` (a reused seal); check result "two seals
  identical". `reject`. (adv-castor-thorn 3/2, adv-sable-thorn 8/7 are pre-seeded.)
- **rank fail:** `entries < threshold` (too few completions); adv-lysa-ironquill (1),
  adv-tessa-hollow (2) pre-seeded. `reject`.
- **dues fail:** `dues == owing`. `reject`.

**`roster_change`** — adventurer presents a transfer card.
- Actor = directory adventurer; `chapter` = their chapter.
- Glass = `transfer_seal`. Scale decoy.
- **valid:** card `mark`+`seal` match `cipher_table[chapter]` AND dues current; `approve` +
  `roster_write {party, rank, wards}`. Checks: `cipher:chapter` (match),
  `adventurer_directory:adv` (dues).
- **authenticity fail:** composed forged reading — borrow another chapter's `mark`, or swap
  `seal` medium (wax→ink); check `cipher:chapter` result "mismatch: N bars / ink not wax".
  No roster_write. `reject`. (Mirrors `sable-lorn`.)
- **dues fail (secondary):** cipher matches but the card's dues stamp is stale;
  `adventurer_directory:adv` "owing". `reject`.

**`dungeon_drop`** — townee commissions a fetch. **Dues gate, then the quote pipeline.**
- Actor = a townee (commissioner); drop = a `drop_table` item.
- Glass = `filing` (empty). Scale empty.
- **valid:** dues current AND `is_drop` AND `season == current` AND a roster party's
  `reach_floor >= floor`; `approve` + `truth.quote` (base × depth_multiplier + in_season
  premium, per the `payout` formula). Checks: `townee_directory:t` (dues), `drop_table:item`
  (is_drop + floor), `season:item`, `roster:*` (reach).
- **dues fail:** `dues == owing`; short-circuit before the pipeline. `reject`.
- **season fail:** drop's `season != current`; check `season:item` "out of season".
  `reject`. (Mirrors `ostler-bram`.)
- **reach fail:** `floor >` deepest roster `reach_floor`; check `roster:*` "beyond reach".
  `reject`.
- **identity fail:** item not a genuine drop (shop-craftable); check `drop_table:item`
  "not a drop". `reject`.

### Inspection derivation (every visit, both readings)

- `glass.reading`: per §3 by `glass_subject_kind`. `filing` ⇒ an authored "nothing to
  examine" string. `relevant = true` iff the axis in play is Glass-decided (identity /
  authenticity / duplicate); else `false` (decoy).
- `scale.reading` + `amount`/`unit`: for `book_item` subjects, `amount` = the delivered
  quantity (within-window for valid/decoy; outside for an `amount` fail), `unit` =
  `book[item].unit`. For non-item subjects, an authored decoy reading with `amount: null`,
  `unit: null`. `relevant = true` only for the `amount` axis.
- **Never emit `relevant` to the player** — authored design metadata only
  (`INSPECTION-TOOLS.md §2`).

---

## 5. Deck integration, public API, validator additions

**Shift selection (DeckLoader, additive):**
- `day == 0` → load `visitors.json` (the curated tutorial shift, unchanged).
- `day > 0` → `ShiftGenerator.generate_shift(day)` populates `Deck.visitors`.
- The current day is a Session/Deck field (default 0); a later hub increments it.

**New directory tabs (DeckLoader, additive):** load `townees.json` + `adventurers.json`
and inject them into `references` under `townee_directory` / `adventurer_directory`, plus
mirror each adventurer `logbook` into `archive`. The ReferencePanel then renders them as
ordinary tabs with **zero** signature change (add `Loc.ref_tab("townee_directory")` /
`ref_tab("adventurer_directory")` titles only). `consult: "townee_directory" |
"adventurer_directory"` join the `check.consult` enum.

**Public API (new autoload `scripts/autoload/ShiftGenerator.gd`, sibling to Deck):**
```gdscript
## ShiftGenerator (autoload) — composes a shift from the banks. Deterministic by day.
func generate_shift(day: int) -> Array          # Array[Dictionary], visitors.json schema, in `order`
func _compose_visit(day, n, task, rng) -> Dictionary   # one visit (internal)
```
It reads `Deck.references`, the two directories, and `generation.json`; it writes nothing.

**Validator additions (DeckLoader `_validate*`, kept green):**
1. `townees.json`: `dues ∈ {current, owing}`; every `owns` id resolves in
   `references.postings`; `owed` is a number.
2. `adventurers.json`: `rank ∈ rank_order`; `chapter` resolves in `cipher_table`;
   `logbook.archive_id` resolves in `archive`; `dues ∈ {current, owing}`.
3. `generation.json`: `task_weights` keys ⊆ task_type enum; every `per_task[*].failure_axes`
   entry ∈ the extended axis enum; `season_schedule.by_day` values ∈ `wheel`.
4. **Generated-shift self-check** (dev/debug): a generated visit passes the *same*
   `_validate_inspections` (both readings non-empty) and every `checks[].entry` resolves in
   `references` (now including the two injected directories). Run over day 1 at boot in a
   debug build.

The existing `checks[].entry → references.json` resolution rule is unchanged; no id/key is
renamed.

---

## 6. Loc additions (impl phase — this phase does not own `loc.gd`)

| layer | key | English |
|---|---|---|
| vocab | `ref_tab.townee_directory` | `Townee Directory` |
| vocab | `ref_tab.adventurer_directory` | `Adventurer Directory` |
| vocab | `failure_axis.dues` | `Dues` |
| vocab | `failure_axis.amount` | `Amount` |
| chrome | `dues_current` | `dues current` |
| chrome | `dues_owing` | `dues owing` |

(`failure_axis.*` only if the verdict surfaces the axis name; the reason prose is content.)

---

## 7. The pinned id roster — every id to exist, with cross-references

Parallel authoring drifts unless every id is fixed now. **Counts:** 24 Book items / 5
categories · 20 postings (14 gate+quest, 6 standing) · 6 chapters · 10 drops · 16 townees ·
16 adventurers. Ids marked **(exists)** are already in the data — do not disturb; all
others are **references.json ADDITIONS** the Banks phase supplies (except townee/adventurer
ids, which live in the Phase-1 skeletons).

### Book items (24 · adds `category` + `glass`; existing 5 gain both fields)
- **herb (6):** `moonwort` (exists), `kingsfoil` (exists), `yarrow` (exists), `sunwort`,
  `gravebloom`, `marsh-ivy` — confusable pair: `sunwort ↔ gravebloom`.
- **beast_part (5):** `griffon-feather` (exists), `cockatrice-plume`, `basilisk-claw`,
  `direwolf-fang`, `wyvern-scale` — confusable pairs: `griffon-feather ↔ cockatrice-plume`,
  `basilisk-claw ↔ direwolf-fang`; `wyvern-scale` carries `forgery_glass` (painted
  fish-scale).
- **reagent (5):** `quicksilver-tears` (exists), `sulfur-bloom` (hazard), `nightshade-oil`,
  `aqua-vitae`, `troll-bile` — confusable pair: `aqua-vitae ↔ nightshade-oil`.
- **mineral (4):** `star-iron-ore`, `bog-iron`, `cinnabar` (hazard), `moonstone` —
  confusable pair: `star-iron-ore ↔ bog-iron` (an amount/heft texture; ore units in dram).
- **relic (4):** `ashford-signet`, `hollow-king-coin`, `drowned-saints-medal`, `wax-effigy`
  — confusable pair: `ashford-signet ↔ hollow-king-coin`; `wax-effigy` carries
  `forgery_glass`.
- **standing-order items need `unit`:** `moonwort`(dram, exists), `kingsfoil`(sprig,
  exists), `quicksilver-tears`(dram, exists), `star-iron-ore`(dram), `sunwort`(sprig),
  `troll-bile`(dram).

### Postings (20)
- **Gate/quest (14):** `cistern-wisp-swarm` (exists), `barrow-gloam` (exists),
  `mana-wyrm-sheddings` (exists), `cliff-nest-survey` (exists), `well-shrine-drowned-saint`
  (exists), `sunken-barrow-blade` (exists), `ashford-mill-ledger` (exists),
  `gloomfen-lurker` (rank_min silver), `ember-drake-roost` (rank_min gold),
  `tanglewood-survey` (survey, proof_item `cockatrice-plume`, rank_min bronze),
  `crypt-of-bells-cleanse` (ward_required cleric, rank_min silver), `millpond-retrieval`
  (retrieval, rank_min bronze), `collapsed-mine-rescue` (ward_required earth, rank_min
  silver), `saltmarsh-haunt` (ward_required water, rank_min bronze).
- **Standing orders (6):** `apothecary-standing-order` (moonwort, exists),
  `infirmary-standing-order` (kingsfoil, exists), `warded-transport-order`
  (quicksilver-tears + hazard seal, exists), `forge-standing-order` (star-iron-ore,
  `accept` dram), `temple-standing-order` (sunwort, `total` sprig), `tannery-standing-order`
  (troll-bile, `accept` dram).

### Chapters (6 · adds `glass`)
`hollowmere` (exists), `ironquill` (exists), `greenhollow`, `saltcrag`, `emberwatch`,
`thornwatch`.

### Drops (10)
`phoenix-ember-resin` (F4/summer, exists), `frost-lily-petal` (F7/winter, exists),
`shadow-moss` (F2/spring), `ember-drake-scale` (F5/summer), `glacier-pearl` (F6/winter),
`mandrake-root` (F3/autumn), `basilisk-eye` (F8/autumn), `sunfire-opal` (F5/summer),
`wraithsilk` (F4/autumn), `deeproot-amber` (F3/spring).

### Archive additions (Banks phase)
- 16 `<adv-id>-logbook` entries mirroring `adventurers.json.logbook` (e.g.
  `adv-brek-hollow-logbook` {entries:3, distinct_seals:3, all_owner:"adv-brek-hollow"}).
- ~6 completion tokens for `completion_claim`: `saltmarsh-haunt-token`,
  `millpond-retrieval-token`, `crypt-of-bells-token`, `tanglewood-survey-token`,
  `gloomfen-lurker-token`, `ashford-mill-ledger-token` (exists) — each `{seal, posting,
  assigned_to}`.

### Roster additions (Banks phase, optional but recommended)
Keep the existing 3 parties; optionally add 1–2 (e.g. a cleric/earth-warded party) so
`crypt-of-bells-cleanse` / `collapsed-mine-rescue` are *sometimes* fieldable and
`saltmarsh-haunt` (water) flips fieldable only after an `adv-oona-ember` roster_change —
preserving the enrol-then-field seam.

### Townees (16 · `data/townees.json`, Phase-1 skeleton)
`townee-hessa-brightwater` [apothecary-standing-order], `townee-orlin-pethwick`
[infirmary-standing-order], `townee-sarai-quillon`* [warded-transport-order],
`townee-maud-cinderhand` [forge-standing-order], `townee-linnet-orrery`
[temple-standing-order], `townee-garrick-tallow`* [tannery-standing-order],
`townee-brother-anselm` [crypt-of-bells-cleanse], `townee-wenna-fisk` [saltmarsh-haunt],
`townee-old-perrin`* [millpond-retrieval], `townee-dorcas-veil` [sunken-barrow-blade],
`townee-sister-mabel` [well-shrine-drowned-saint], `townee-cuthbert-lyle` [],
`townee-goodman-fenn`* [], `townee-elga-thorne` [], `townee-piers-danglow` [],
`townee-mother-ashby` [].  (`*` = `dues: owing`.)

### Adventurers (16 · `data/adventurers.json`, Phase-1 skeleton)
`adv-brek-hollow` (bronze/hollowmere), `adv-yria-saltcrag` (silver/saltcrag),
`adv-torvald-ember` (gold/emberwatch), `adv-nima-green`* (silver/greenhollow),
`adv-castor-thorn` (bronze/thornwatch · dup-seal logbook), `adv-lysa-ironquill`
(copper/ironquill · under-threshold), `adv-brand-hollow` (gold/hollowmere),
`adv-perrin-saltcrag`* (silver/saltcrag), `adv-oona-ember` (silver/emberwatch · water
ward), `adv-huld-green` (bronze/greenhollow), `adv-sable-thorn`* (gold/thornwatch ·
dup-seal), `adv-miro-ironquill` (platinum/ironquill), `adv-tessa-hollow` (copper/hollowmere
· under-threshold), `adv-garret-saltcrag` (bronze/saltcrag), `adv-vesk-ember`
(silver/emberwatch), `adv-wynn-green` (gold/greenhollow · cleric ward).  (`*` = `dues:
owing`.)

---

## 8. Skeleton status (Phase 1 — this delivery)

- `data/townees.json` — **written.** 16 ids, dues + owed + `owns` cross-refs filled;
  blurbs empty for Banks.
- `data/adventurers.json` — **written.** 16 ids, rank/dues/chapter/wards/logbook filled;
  blurbs light.
- `data/generation.json` — **written.** Full knob set + per-task composition contract;
  weights are first-pass defaults to tune.
- `references.json`, `visitors.json`, scripts — **untouched** (later phases per §0).

**Sync (repo Rule 3):** when the Banks phase adds to `references.json`, tick the matching
ids off §7; when the Generator phase lands `ShiftGenerator.gd`, update
`MORNING-QUEUE.md`'s architecture table + this doc's §5, and log the decision in the repo
`DEV-LOG.md` (Rule 5).
