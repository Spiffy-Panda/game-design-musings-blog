# INSPECTION-TOOLS.md — the examine/weigh loop + the binary desk

This is the **build-to spec** for three linked changes to The Morning Queue. The MODEL +
DATA phase (this doc, `data/visitors.json`, `data/references.json`,
`scripts/autoload/DeckLoader.gd`) is done; the **implementation phase** builds the panel,
verdict, and session behavior to *this* document. Read it top to bottom before touching a
component script.

The through-line is the Papers-Please loop, split cleanly in two:

- **The RULEBOOK** — what a thing *should* be. Static, visitor-independent. That is the
  Reference tables already in `references.json` (Reference Book, Quest Board, Rank Ledger,
  Chapter Ciphers, Completion Archive, Dungeon Drops, Season Wheel, Payout Rule, Active
  Roster). The `ReferencePanel` already renders these. **Keep them; do not fold tools into
  them.**
- **The INSPECTION TOOLS** — what *this visitor's item* actually is. Visitor-dependent.
  These are **new**, they read from the visitor's `inspections` block, and they are the
  other half of every verification: you read a tool, then compare it against the matching
  rulebook page.

The desk's job is the comparison. The Glass says the leaf has a *round* stem; the Reference
Book says kingsfoil's stem is *square*; the mismatch is the reject. Neither half decides
alone.

---

## 1. The inspection tools

Text-based for now. Each tool, given the current visitor, returns that visitor's authored
reading from `inspections`. Two tools ship; the architecture takes more later.

| id | name (`Loc`) | reveals | compare against (rulebook) | data source |
|----|--------------|---------|----------------------------|-------------|
| `glass` | **The Glass** | the item's actual physical characteristics — the herb/feather/seal *tells* ("Silver underside, five lobes, cold to the touch") | Reference **Book** (item tells), **Chapter Ciphers** (seal mark/type), **Completion Archive** (seal genuine/distinct) | `visitor.inspections.glass.reading` |
| `scale` | **The Scale** | the item's measured **amount** (weight or count) — "The jar settles at 3 drams" | the claimed order's **standing-order limit** on the Quest Board (§4) | `visitor.inspections.scale.reading` (+ `amount`/`unit`) |

The Glass generalizes past herbs: it is the *examine any presented physical thing* tool. A
forged transfer card, a completion token, a logbook of seals — all are examined with the
Glass and checked against the Rulebook (Ciphers / Archive), exactly as an herb is checked
against the Book. The Scale is the *measure the amount* tool and is load-bearing only when a
standing-order limit exists to measure against; everywhere else it still returns a reading
(a decoy — §3).

---

## 2. Per-visitor inspection schema

Every visitor carries an `inspections` object. **Every visitor has both readings** — that is
the point (see §3). Shape:

```json
"inspections": {
  "glass": {
    "reading": "Silver underside, five lobes, cold to the touch.",
    "relevant": true
  },
  "scale": {
    "reading": "The jar settles at 3 drams.",
    "amount": 3,
    "unit": "dram",
    "relevant": true
  }
}
```

Fields:

- `glass.reading` — **required**, non-empty string. The examine text. Content prose; lives
  in the data bundle, never in `Loc` (it is authored, per-visitor, like `player_story`).
- `scale.reading` — **required**, non-empty string. The weigh/measure text.
- `scale.amount` — number or `null`. Machine-readable measured value, for the panel to
  compare against a limit (or `null` when there is nothing to weigh — a spoken petition, an
  un-delivered commission).
- `scale.unit` — string slug or `null` (`"dram"`, `"sprig"`, `"petal"`). Must match the
  order's limit `unit` for a comparison to be meaningful.
- `relevant` (on each tool) — **authored design metadata: NEVER render this to the player.**
  `true` = this reading is the load-bearing check for this visitor; `false` = decoy. It
  exists so the implementation and this doc can reason about decoys, and so a future scoring
  pass can ask "did the player consult the tool that mattered." Surfacing it would spoil the
  puzzle. It is consistent with `checks[]` (the truth of what matters) by construction.

`amount`/`unit`/`relevant` are optional to the validator (only the two `reading` strings are
required), but every shipped visitor sets them.

---

## 3. Decoy readings — "weight when it is not needed"

Extra data is the feature. A tool returns a reading even when it is not the relevant check,
so the player must *decide which tool matters*, not blindly run both. The decoy map for the
shipped 16 (which tool is load-bearing; `·` = decoy that still returns a reading):

| # | visitor | task | Glass | Scale | the check that actually decides |
|---|---------|------|:-----:|:-----:|---------------------------------|
| 1 | wren-sixpence | item_check | ✔ identity | ✔ amount | Book match **and** within 2–4 dram → approve |
| 2 | hulbr-odd-eye | rank_gate | · | · | Rank Ledger + posting gate |
| 3 | pell-marrow | item_check | ✔ identity | · (amount meets) | Book: round stem ⇒ yarrow → reject |
| 4 | doss-yellowknife | rank_gate | · | · | posting `rank_min: gold` vs silver |
| 5 | ivy-threnody | completion_claim | · (seal genuine) | · | posting: assigned to another party → reject |
| 6 | bartholomew-quill | item_check | ✔ authenticity | · (no order) | Book forgery tells: dye lifts, solid rachis |
| 7 | sister-coll | quest_file | · | · | Roster: no cleric/water ward registered |
| 8 | ganton-reeve | rank_gate | · | · | Rank Ledger: no card on file |
| 9 | odile-vantry | item_check | · (substance true) | · (within 1–2) | posting `requires: hazard seal` → reject |
| 10 | two-rivers | rank_up | ✔ distinct seals | · | Archive: 3 distinct completions |
| 11 | marn-culpepper | rank_up | ✔ duplicate seal | · | Archive: two seals identical → reject |
| 12 | widow-ashe | quest_file | · | · | Roster: a party is at the barrow |
| 13 | perrick-vane | roster_change | ✔ cipher match | · | Ciphers: Hollowmere triple-notched wave |
| 14 | sable-lorn | roster_change | ✔ cipher mismatch | · | Ciphers: Ironquill four bars/ink → reject |
| 15 | goodwife-tamsin | dungeon_drop | · | · (no delivery) | Drops + Season + reach + Payout |
| 16 | ostler-bram | dungeon_drop | · | · (no delivery) | Season out + reach short → reject |

Two textures worth protecting:

- **Scale-passes-but-identity-fails** (pell, #3): the herb weighs a clean 1 sprig — right
  amount, wrong plant. The number reassures; the Glass reveals the round stem.
- **Both-tools-pass-but-paperwork-fails** (odile, #9): true quicksilver, within the 1–2 dram
  window — and still a reject, because the order needs a hazard seal she never bought. Some
  checks live only on the Rulebook, not on any tool.

---

## 4. Standing orders are LIMITS, measured by the Scale

The old `"amount": "one dram"` string is gone. A standing order now carries a **limit** the
item is measured against, in one of two shapes:

```jsonc
// (a) a per-delivery window — accept anything from min to max, inclusive
"accept": { "min": 2, "max": 4, "unit": "dram" }

// (b) a total request — one delivery must supply exactly/at least `needed`
"total": { "needed": 1, "unit": "sprig" }
```

The three shipped standing orders on the Quest Board (`postings`):

| entry (stable id) | item | limit | extra requirement |
|-------------------|------|-------|-------------------|
| `apothecary-standing-order` | moonwort | `accept { min: 2, max: 4, unit: dram }` | — |
| `infirmary-standing-order` | kingsfoil | `total { needed: 1, unit: sprig }` | — |
| `warded-transport-order` | quicksilver-tears | `accept { min: 1, max: 2, unit: dram }` | `requires: ["hazard seal"]` |

**The AMOUNT check** = compare `scale.amount`/`scale.unit` against the order's limit:

- `accept` window → *within* iff `min <= amount <= max` (units equal). Otherwise *over* / *under*.
- `total` → *meets* iff `amount >= needed` (units equal). (For a strict single-unit order,
  `amount == needed`; the shipped `infirmary` order needs exactly 1 sprig and the visitor
  brings 1.)
- **No limit** (survey proof, un-delivered commission) → *no order to measure against*; the
  Scale reading is pure flavor.

**The worked example (moonwort, visitor 1):** the apothecary accepts **2–4 drams of moonwort
per delivery**; the Glass reads *silver underside, five lobes, cold to the touch* → matches
the Reference Book (identity ✔); the Scale reads **3 drams** → `2 ≤ 3 ≤ 4` → within range
(amount ✔). Both pass → APPROVE.

**A worked reject (illustrative, for the implementer's mental model):** had that jar weighed
**6 drams**, identity still passes but `6 > 4` → *over the order's limit* → REJECT on amount
alone. The schema supports amount-as-the-decisive-fail; no shipped visitor currently trips it
(the 16 verdicts are canon and unchanged this phase), so the Scale's teeth are demonstrated
by the moonwort pass + the decoy that could have failed. If a future shift wants an
amount-fail visitor, add one and add `amount` to `truth.failure.axis`.

---

## 5. An item_check is now two independent checks

For an `item_check` (and any authenticity check), resolution splits:

1. **IDENTITY** — Glass reading vs the Reference **Book** (or Ciphers / Archive) entry for
   the *claimed* item. Match ⇒ identity ✔.
2. **AMOUNT** — Scale reading vs the claimed order's **limit** (§4). In-range/meets ⇒ amount ✔.

A stamp of APPROVE needs *both* to pass (and any Rulebook-only requirement like a hazard
seal). Either failing is a REJECT. The `checks[]` array in `visitors.json` still encodes the
authoritative per-visitor truth and still drives `ReferencePanel.focus()`; the tools are the
player's means of *gathering* the left-hand side of each comparison.

---

## 6. Panel presentation — Tools vs Reference (the additive method)

The `ReferencePanel` keeps a **second, visually distinct group** above the Reference
tab-list for the inspection tools:

```
┌ INSPECTION TOOLS ─────────┐      ← brass-accented group header (Loc: tools_head)
│  The Glass                │      ← visitor-dependent; shows THIS visitor's glass.reading
│  The Scale                │      ← shows scale.reading (+ amount/unit)
├ REFERENCE DESK ───────────┤      ← existing header (Loc: reference_head)
│  Reference Book           │      ← the rulebook tables, unchanged
│  Quest Board (foldouts)   │      ← see §9 for foldout grouping by type
│  Rank Ledger  … etc.      │
└───────────────────────────┘
```

**Clicking a tool spawns a desk tile.** When the player presses The Glass or The Scale, the
panel (a) selects that tool tab (existing content in the scroll area) and (b) emits
`tile_requested` — Main wires this to `VisitorCard.add_tile`, which places a named
reference tile on the main desk **below the claim doc**. The tile carries the reading
plus, for the Scale, the `amount_*` comparison line. Tiles stay until the next visitor.
A brass tint marks the tool tile's left border.

**Clicking a Quest Board row also spawns a desk tile.** Each posting row is made
clickable (pointing-hand cursor); a left-click emits `tile_requested` with the posting's
content and a green tint. Placing the quest details side-by-side with the visitor's claim
is the key flow these tiles support.

**Desk-tile architecture (additive):**

```gdscript
## VisitorCard — ADDITIVE alongside show_visitor(). Does not change the frozen signature.
func add_tile(tile_id: String, title: String, body: String, tint: Color) -> void
func clear_tiles() -> void   # called by Main on visitor_changed

## ReferencePanel — ADDITIVE alongside set_references()/focus()/set_inspection_target().
signal tile_requested(tile_id: String, title: String, body: String, tint: Color)
```

- Main connects `_reference.tile_requested` → `_on_tile_requested` which calls
  `_card.add_tile(...)`.
- `tile_id` is the tool id (`"glass"`, `"scale"`) or the posting id (e.g.
  `"apothecary-standing-order"`) — used as a replace-key so clicking the same source
  twice updates the tile rather than duplicating it.
- `tint` is `Palette.BRASS` for inspection tools, `Palette.GREEN` for postings.
- Main calls `_card.clear_tiles()` on `visitor_changed` so each shift starts fresh.

**`set_inspection_target` unchanged contract:** the selection-stability rule still holds
— switching visitors does not yank the player off a Reference tab. The tool tab content
refills in place; the desk tiles clear via `clear_tiles()`.

**Loc keys added this phase:**

| layer | key | English |
|-------|-----|---------|
| chrome | `desk_tiles_head` | `ON THE DESK` |
| chrome | `desk_tiles_hint` | first-use hint shown in empty tiles area |

**Post-fix additions (usability pass):**
- `VisitorCard._tiles_area` is always visible; starts with a `_tiles_hint` label ("Click a
  tool or quest posting to place a reference here") so the feature is discoverable on day 0.
- `VisitorCard._build_tile` adds a small `×` dismiss Button per tile; calling
  `_dismiss_tile(tile_id)` removes that tile and restores the hint if the area is now empty.
- `VisitorCard._tiles_scroll: ScrollContainer` height-capped at 170px, `SIZE_SHRINK_BEGIN` —
  prevents the VerdictBar from ever being pushed off-screen by tile accumulation.
- Posting rows gain a `mouse_entered` / `mouse_exited` hover tint (subtle GREEN wash via
  `_posting_row_hover_style()`) to signal interactivity before click.
- Foldout section headers use `Palette.GROUND` background + `LINE2` border (previously
  `Color(LINE, 0.4)` — too faint for reliable section boundary scanning).
- Tile title labels 11px → 12px for typographic consistency.
- `Main._unhandled_key_input`: `KEY_G` → `_reference._on_tool_pressed("glass")`,
  `KEY_S` → `_reference._on_tool_pressed("scale")` (guards on `Deck.ok`).

(The `tool_*` and `amount_*` keys from the previous build remain; no key was removed.)

---

## 9. Quest Board foldouts (new)

The Quest Board tab (`postings`) groups its entries by `type` in collapsible foldout
sections. Each type becomes a chevron-headed section (▼ = open, ▶ = collapsed). All
sections start expanded on load. The canonical render order:

| type | label | entries |
|------|-------|---------|
| `bounty` | Bounty | apparition/beast clearing quests — *formerly untyped, now explicit* |
| `survey` | Survey | scouting proofs |
| `retrieval` | Retrieval | bring-back missions |
| `collection` | Collection | gather-from-field orders |
| `rescue` | Rescue | persons-in-peril extractions |
| `standing_order` | Standing Order | recurring supply orders |

**Data fix:** the seven postings that had a `target` field but no `type` now carry
`"type": "bounty"` in `references.json`: `cistern-wisp-swarm`, `barrow-gloam`,
`well-shrine-drowned-saint`, `gloomfen-lurker`, `ember-drake-roost`,
`crypt-of-bells-cleanse`, `saltmarsh-haunt`. Every posting now has an explicit `type`.

The `_POSTING_TYPE_ORDER` const in `ReferencePanel.gd` drives the render order; any
type not in the list is appended at the end. The `focus(consult, entry)` contract is
unchanged — row handles stay the raw posting ids.

---

## 7. The binary desk (change 1)

The desk becomes **APPROVE / REJECT only**. Do this reversibly, via the existing dial — do
**not** delete the four-verdict code paths.

- **Session:** flip `const STRICT_BINARY := false` → `true` in `scripts/autoload/GameState.gd`.
  Correctness then judges against `truth.binary` (already present on every visitor), which
  collapses `hold`/`conditional` to `reject`.
- **VerdictBar:** in binary mode, render **only** the `approve` and `reject` buttons — nothing
  on screen should show HOLD or CONDITIONAL. The `stamp_btn.hold` / `stamp_btn.conditional`
  Loc entries and the four-verdict branches stay in place, unused, so re-enabling is a
  one-line flip back.
- **Data (this phase, done):** every visitor's `truth.binary` is set. The two former
  half-fails keep **both** fields intact so four-verdict mode still works later —
  `ivy-threnody` (`stamp: hold`, `binary: reject`) and `odile-vantry` (`stamp: conditional`,
  `binary: reject`). Under binary they judge as **reject**, which their failure reasons
  already justify (wrong claimant; incomplete/unsealed request). Nothing was renamed or
  removed.

---

## 8. Validator (DeckLoader) — what this phase added

`scripts/autoload/DeckLoader.gd` gained two light, genuinely-required checks (kept green):

1. Every visitor must carry `inspections.glass.reading` and `inspections.scale.reading` as
   non-empty strings (the tools would otherwise have nothing to show).
2. Every `postings` entry of `type: "standing_order"` must carry an `accept` (`min`/`max`/
   `unit`) **or** a `total` (`needed`/`unit`) limit (so the Scale always has something to
   measure against, or the absence is deliberate).

Neither invents optional fields; both guard the new contract. The `checks[].entry` →
`references.json` resolution rule is unchanged, and no id/key was renamed.
