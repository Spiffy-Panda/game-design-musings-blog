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

The `ReferencePanel` gains a **second, visually distinct group** above (or clearly separated
from) the Reference tab-list:

```
┌ INSPECTION TOOLS ─────────┐      ← new group header (Loc: tools_head)
│  The Glass                │      ← visitor-dependent; shows THIS visitor's glass.reading
│  The Scale                │      ← shows scale.reading (+ amount/unit)
├ REFERENCE DESK ───────────┤      ← existing header (Loc: reference_head)
│  Reference Book           │      ← the rulebook tables, unchanged
│  Quest Board              │
│  Rank Ledger  … etc.      │
└───────────────────────────┘
```

Make the two groups read as different *kinds*: a header/eyebrow per group, and a distinct
accent — suggested: tools tinted **brass** (`Palette.BRASS`), reference tabs the existing
green-pressed treatment. Tools are visitor-scoped ("this item"); reference is fixed ("the
rules"). Keep the split obvious at a glance.

**The additive contract (do NOT change any frozen signature — ADD this one):**

```gdscript
## ReferencePanel — ADD alongside set_references()/focus(); does not replace either.
func set_inspection_target(visitor: Dictionary) -> void
```

- `Main` calls it in `_on_visitor_changed(v)` — the one place a visitor arrives — right
  after `_card.show_visitor(v)`: `_reference.set_inspection_target(v)`. (Additive line in
  `Main.gd`; the frozen component signatures are untouched.)
- The panel reads `visitor.get("inspections", {})` and refills the two tool pages with this
  visitor's `glass.reading` and `scale.reading`. Guard defensively: a missing/empty
  `inspections` (e.g. the data-error sparse dict) shows the `tool_empty` fallback, never a
  crash.
- **Selection behavior:** switching visitors should not yank the player off a Reference tab
  they are reading. Recommended: refill the tool pages in place; if a tool tab is currently
  selected, keep it selected and just swap its content. Do not auto-jump to a tool.
- Optionally, when a standing-order limit exists for the claimed item, the Scale page may
  render the comparison result (within/over/under/meets) beneath the reading, using the
  `amount_*` Loc keys below. This is presentation sugar over `scale.amount` vs the order
  limit; it must not reveal `relevant`.

**Loc keys the implementation phase must add** (this phase does not own `loc.gd`):

| layer | key | English |
|-------|-----|---------|
| chrome | `tools_head` | `INSPECTION TOOLS` |
| chrome | `tool_empty` | `(nothing to examine)` |
| vocab | `tool_tab.glass` | `The Glass` |
| vocab | `tool_tab.scale` | `The Scale` |
| chrome | `tool_glass_caption` | `Examine — what the item actually is` |
| chrome | `tool_scale_caption` | `Weigh — the measured amount` |
| chrome | `amount_within` | `within the order's limit` |
| chrome | `amount_over` | `over the order's limit` |
| chrome | `amount_under` | `under the order's limit` |
| chrome | `amount_meets` | `meets the order` |
| chrome | `amount_no_order` | `no standing order to measure against` |

(`tool_tab.*` sits in `vocab` so a tool title routes through the same identifier→display
path as `ref_tab.*`; the `amount_*` chrome keys are only needed if the Scale page renders the
computed comparison.)

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
