# DEV-LOG

Append-only. Newest entry on top. Absolute dates. This log records *why* — the
options weighed, what was tried, what would surprise the next person. Git history
records *what changed*. Write an entry before every commit (Rule 5).

---

## 2026-07-16 — the view pass: two fixed rails and a derived pane; and the metric everyone measured was the wrong one

The observatory's layout is rebuilt: **two fixed rails** (roster + place board left; knobs + inspector
right) and **one fluid reading pane** (summary + chronicle) whose width is **derived, never declared**.
Machine readouts (clock/hash/seed/stats) moved to a monospace status strip. The Dawn Summary went from
**326px visible and cut mid-word** to **496px, `clipped:[]`**.

**The overflow is dead by construction, and the construction is not what the study proposed.** The
study's fix was monospace — reasoning that a 16-char hex string in a fixed-width font has one width
forever. True, but *slack*: monospace fixes the hash being fixed-**length** in a **proportional** font,
which is a different problem from the one that broke the layout. The actual guarantee is
**`clip_text = true`**, which drops a `Label`'s minimum width to ~1 — its text stops being a layout
demand for **any** font, **any** string, and any future edit. Both shipped; `_readout()` documents which
solves what. The general rule is now recorded in FISHBOWL.md, because it is the kind of thing that gets
re-broken by someone adding an innocent `Label`: **a bare Label's text IS its minimum width, and a
minimum beats a `PRESET_FULL_RECT` anchor.** That is how 108px of hash forced the root to 1371px in a
1290 viewport.

Proof is at **two different day-hashes** (`b8d15299d8817639`, `5aa99deb4b587b10`) producing **identical
layouts** — which is the only proof that counts for a data-dependent bug. Verifying against one seed
would have been verifying against one sample of a moving target.

**The two studies contradicted each other and neither noticed.** Study B measured the roster 45.9% empty
and ~132px *over*-allocated. Study A proved that same roster illegible and priced the fix at "four
`set_text` lines, contained." Both were right and they are the same finding: **in words, the roster's
measured demand is ~464px — more than the 406 that B called over-allocation. The horizontal surplus
*was* the illegibility, priced in pixels.** Neither study could see that alone, and the synthesis is the
whole argument for running them as separate lenses rather than one "review the UI" agent.

**And the headline metric was the wrong metric.** Dead space, measured A/B on the same tiling: boot
**77.0% → 74.6%**, day-2 dawn **54.1% → 55.6% — up.** That is not a regression; it is the *expected*
consequence of the wrap-instead-of-truncate trade, because wrapped prose is less pixel-dense than
ellipsised prose. **The defect was never empty pixels. It was truncation sitting beside them.** The
metric that actually moved: every `clipped:[right]` on the deliverable is now `clipped:[]`. Recorded
because "reduce dead space" is the obvious brief for a next pass, and it is the wrong brief — a future
agent optimising that number would re-truncate the summary to win it.

**`regression-b1-b6.json` was pinning the bug it existed to catch.** It hard-asserted
`expect btn-storylets on_screen: false` and ran green for a whole release. The assertion that should
have caught the clipping was *encoding* it as intended behaviour, and FISHBOWL.md's `on_screen`-vs-
`clickable` "worked example" was built on the same defect. Both corrected; the scenario is re-fixtured
onto the invariant (green, 34 steps). **A real coverage gap is left open rather than papered over:**
`GTH.B1`'s `on_screen:false` / clamped-anchor path now has no fixture, because restoring one would mean
reintroducing the defect. It belongs in the addon's own test scene, not in a consumer's regression suite —
which is the general lesson: *a project's UI bug is a bad fixture for a harness invariant, because
fixing the project deletes the fixture.*

**The legend question resolved by dissolving it.** A legend was **ill-defined, not expensive**: `🍺` meant
innkeep *and* inn (the innkeep's row read `🍺|🍺`), `🔨` meant smith *and* workshop, `🧭` meant away-place
*and* away-mode. Meaning depended on the column, so a legend needed one table **per column** — three
tables to explain four columns. Worst was `Place`, keyed on `place_kind` while its tooltip showed
`place_name`: it rendered the inn's `🍺` for people asleep in their own beds. So `Role`/`Place`/`Top
drive` became words and `Doing` kept its five glyphs under a single on-screen legend. **This reverses
Panda's explicit request for emoji in all four columns and is flagged for review, not settled** — see
the next entry.

Also landed: the day + **tense** on every `view_day` panel (computed, so it reads "today, live" mid-day
and "yesterday" after dawn — the flip is the point); the chronicle's first row open by default, because
the because-list was the best explanatory artifact in the project and it was hidden behind one
character; the type scale un-inverted; the chronicle converted from a `Tree` to a wrapped VBox. All 23
`test_id`s preserved — `chronicle` deliberately tags the ScrollContainer, since on the inner VBox it
would report `clipped:[bottom]` forever.

Dropped, with reasons: `A.11` (whether the dawn frame should publish at all — a model question the study
declined to adjudicate, correctly), the `board`/`shut` dialog fields (a `CreatePlace` contract change;
the dialog now at least *says* it always boards), and `Sparkline.gd`'s midline alpha (a one-character
fix outside the pass's file — recommended, not taken).

---

## 2026-07-16 — a debug readout was resizing the whole app; the roster's "surplus" width was the illegibility, priced in pixels

The view half of the observatory usability study. Two independent studies (space, comprehension) fed
this pass; the interesting part is **where they contradicted each other**, which neither could see alone.

**The overflow, and why the fix is `clip_text` and not a number.** `hash_label` was a bare `Label`. A
Label with autowrap off reports its *whole single-line text* as its minimum width; an HBox's minimum is
the sum of its children's; and a Control is clamped to at least its own minimum — **so a minimum beats a
`PRESET_FULL_RECT` anchor**. When the hash populated at first dawn it grew 47px → 155px, and that 108px
walked hash_label → top-bar HBox → PanelContainer → root VBox and forced the root to **1371px inside a
1290px viewport**. `body` then re-divided 1371 by stretch ratio, so the two *emptiest* regions gained
width (roster +24, chronicle +33) and the Dawn Summary — the deliverable — lost exactly their sum (−57),
purely because it was the rightmost child. **Column order picked the victim; nothing about the summary's
content selected it.**

Study B prescribed monospace + a wide strip. That is right but it is *slack*, not construction — it
argues the bug can't reach far enough, not that it can't happen. The actual guarantee is
**`clip_text = true`**, which drops a Label's minimum width to ~1: its text stops being a layout demand
at all, for any font, any string, any future edit. Monospace fixes a *different* thing — the hash is a
fixed-*length* string in a *proportional* font, so its width was a sample, not a constant (three
observed hashes rendered ~152/155/156px, which is why four documents in this repo recorded four
different rects for one button). Both halves shipped; they solve different problems and the comments say
which. Verified at day 2 (`b8d15299d8817639`) and day 5 (`5aa99deb4b587b10`): **different hash,
identical layout**, `btn-storylets` at x=1134 `visible_fraction 1.0` in both.

**Where the two studies collide, and the finding that came out of it.** Study B measured the roster
45.9% empty in every state and ~132px *over*-allocated, and sized a 320px rail from its ~274px demand.
Study A independently showed that same roster is 29 undocumented glyphs whose stated mitigation is a
tooltip — void, since Panda ruled the screenshots get published and a PNG has no hover — and prescribed
words at "four `set_text` lines, contained". **Both are right and they are incompatible.** Rendered in
words the roster's honest demand is **~464px, measured** — *more* than the 406px Study B called
over-allocation. The horizontal surplus was never real; **it was the illegibility, priced in pixels.**
Study A's fix is not four lines: it costs ~156px of rail, and the only account it can be drawn from is
the reading pane. Paid deliberately — a wider summary beside an unreadable roster fails the same
publication test as a narrow one. Left rail 488, right rail 290 (the knobs+inspector only ever demanded
~270, not the 320 they held), reading pane **496 and derived, not declared** — so the arithmetic is
right at 1290 and at 1280 without a second number to keep in sync.

I made the same class of error one scope down and it is worth recording: I sized the Top-drive column
for `"purse 0.50"` and it clipped instantly on `"restlessness 0.45"`. **Every column minimum in the
roster is now the widest value the *format* admits, measured off the running app** — my estimates were
uniformly ~20% low.

**On the legend — the argument, not the verdict.** A legend maps symbol → meaning. The old encoding had
no such map: `🍺` was `ROLE_GLYPH["innkeep"]` *and* `PLACE_GLYPH["inn"]` (so the innkeep's row read
`🍺 | 🍺`), `🔨` was smith *and* workshop, `🧭` was away-place *and* away-mode. The meaning depended on the
column, so a legend would have had to be one legend **per column** — three more tables to explain the
four already there. **The legend wasn't expensive; it was ill-defined.** Fixing that is what makes a
legend possible: Role/Place/Top became words, `Doing` stayed glyphs (5 values, changes every slot, the
one column you genuinely scan), and one vocabulary means one five-symbol legend, on screen, under the
table. The `Place` glyph was also the worst of the four — keyed on `place_kind` while its tooltip said
`place_name`, it rendered the inn's `🍺` for three people asleep in their own beds. Not a degenerate
column: one that was **wrong in a way that read as right**.

**A regression suite had encoded the bug as expected behaviour.** `regression-b1-b6.json` asserted
`expect btn-storylets on_screen: false` and ran green on it for a release — the assertion that should
have *caught* a debug readout resizing the application was *pinning* it instead. Re-fixtured onto the
invariant (`on_screen: true` after the hash populates, plus `summary` un-clipped). **This leaves a real
coverage gap, recorded rather than papered over:** B1's `on_screen:false` / clamped-anchor path now has
no fixture here, and restoring one would mean reintroducing the defect. It belongs in the addon's own
project-agnostic test scene. FISHBOWL.md's "worked example" prose leaned on the same clipping and is
corrected in the same pass (Rule 3).

**Dropped, with reasons.** The courier's four-rooms-at-once display (A.11) — conceptually the deepest
thing in either study, but it only bites on mid-day frames while the musing publishes a dawn shot, and
Study A itself declines to adjudicate whether the sim means literal presence or visits-within-the-slot.
`Sparkline.gd`'s invisible 0.12-alpha midline (A.7) — a genuine one-character fix, but that file was
outside this pass's ownership; flagged for whoever owns it. The `board`/`shut` fields missing from the
New Place dialog (A.8/OBS.10) — the dialog now *says* it always boards, but adding the fields is a
`CreatePlace` data-contract change, not a view change.

---

## 2026-07-16 — the knobs were never live, the strip showed the wrong number, and the determinism test could not detect a determinism break

A usability study of the observatory went looking for layout problems and found that **the instrument
does not work as an instrument.** Three findings, in ascending order of how much they should worry us.

**1. The knobs — documented as "the `VFB.Q1` tuning surface" — did nothing.** The day's summary was
computed once at dawn and cached (`World.Summaries[day]`), so dragging `actionability` from min to max
produced a byte-identical summary. The view was innocent: `_knob()` correctly called `SetKnob` then
`_refresh_summary()`, and the refresh dutifully re-read a baked artifact. **This was a storage gap, not
a refresh gap** — a distinction that decides where the fix lives, and the first sweep got it wrong.
Worse, `SummaryJson` mixed two time bases in one object: `lines` came from the cache while
`dial`/`register` were computed live from `Config`. So the label read `register: report` over gossip
prose — **the instrument lied about its own state.**

**Fixed by splitting the Summarizer along the axis that actually exists.** Dawn keeps only
`SealDay` — writing `CarriedByGossip` onto the day's entries — because gossip-carriage is the one
phase that depends on *that day's* occupancy and cannot be reconstructed later. Everything downstream
(filter → order → take → pick) derives on read. The `Summaries` cache is **removed, not patched**: the
storage gap is gone rather than invalidated. `SealDay` is deliberately unconditional (it ignores
`hearsay_required`) so that turning the knob on later still finds a truthful gate rather than a gate
that was never recorded.

The knob classification is load-bearing and should survive any future refactor: **rendering** knobs
(`actionability`, `summary_lines`, `hearsay_required`) choose how to present an already-simulated day
and are now live; **simulation** knobs (`pressure_rates.*`, `storylet_rate`, `bio_marks_enabled`)
change what happens and cannot retroactively apply without a re-run. **`bio_marks_enabled` is a trap** —
it sits beside `hearsay_required` looking like a twin display toggle but writes hashed `Marks` at fire
time, so live-rendering it would break determinism. Two controls that look identical while differing on
whether they may legally be live is a UI defect as much as a code one; the knob list now carries two
headers saying which is which.

**2. The stats strip displayed the wrong quantity for the question the project exists to answer.**
`VFB.Q1` asks whether the sim sustains ~4–5 *distinct tellable lines per night*. The soak CLI counts
distinct **delivered text** (bounded by `summary_lines` ≤ 7). The on-screen strip counted distinct
**candidate StoryletIds** (bounded by the 12-rule bank) — a *pre-truncation pool*, which cannot respond
to `summary_lines` at all. At golden day 1 the strip read **12** where the true value was **5**. It now
reads `events 12 · tellable 5 / pool 12`, and the CLI independently reports `summary=5`. **The screen
and the soak instrument now agree, which they never did.** Keeping `pool` beside `tellable` turned out
to be the useful part: dropping `summary_lines` 5→3 live gives `tellable 3 / pool 12`, which reads at a
glance as *truncation problem, not generation problem* — the actual `VFB.Q1` diagnostic.

**3. The thing that should worry us: the determinism acceptance test could not fail.**
`..._Identical_Hash_Sequence` compared run A to run B **within one build**. It proved self-consistency,
never stability. When the implementer deliberately perturbed `ToHashNode` to check their own new test,
**the pre-existing test stayed green while the hash moved.** FISHBOWL.md cites that test as the
determinism acceptance for a contract it calls non-negotiable. It would have passed through any change
that broke the hash, including this one. There is now a real pin —
`Twelve_Townees_Three_Days_Hash_Sequence_Is_Pinned` asserting the literals
`b8d15299d8817639, e3478bc4ff7d4848, 02bc86b987c547c3`, **captured from the pre-change build** so the
pin descends from the old behaviour rather than blessing the new.

**This is the second green-test-over-a-dead-feature found today** (the first: `Snapshot` never persisted
summaries, invisible because the M2 test only checked hashes — now free, since `CarriedByGossip` was
already being persisted and a derived summary round-trips automatically). Two in one day, in a suite
that was 22-for-22 green, is a pattern about *what our tests assert* rather than two coincidences.

**Determinism was proven, not argued.** The claim that the split is safe rests on the hash being sealed
at `Simulation.cs:48` *before* the summary is built at `:52`, with no summary in `ToHashNode` — verified
against source first. Then empirically: CLI baselines captured **before any edit** and diffed after —
golden town 12 days identical, live town 7 days identical, soak across 3 seeds × 7 days identical
(`avg 4.43`, `3/21` below four, matching the figures FISHBOWL.md already quotes). The CLI prints every
summary line as well as the hashes, so that diff covers more than the contract requires. Every new test
was then mutation-checked — each fix reverted in turn, confirming exactly the intended test went red.

**A correction worth recording, because the study overstated it.** `Candidates` gating past days against
the *current* day's occupancy was reported as "a live bug today." It is **latent**: on the shipped
fixture it has no observable effect, because both gossip-carriers have day-invariant itineraries and
`Clockwork` only honours an away-block for `adventurer-default`. Re-gating day 1 against day-3 occupancy
with both adventurers away flipped **nothing**. It took a synthetic fixture (carrier trait on an
adventurer) to make it bite — and then it's dramatic, pool 9 → 1. So: a genuine correctness bug that
goes live the moment occupancy varies (generated towns, more away-plans), and the reason derive-on-read
is safe at all — but not something that was corrupting output today. Fixed anyway.

Tests 22 → 30. Left for Panda: `copresence_bonus` is settable, projected, and **read by no engine
system** (reported, not deleted — deleting a knob is a design call), and `SetAway` silently no-ops on
non-adventurers (`Clockwork.cs:37`) — it sets the flag and the roster says "away" while the townee stays
in occupancy generating co-presence, which is the same lie-to-the-operator shape as the register bug.

---

## 2026-07-16 — the roster speaks emoji, and Godot's own docs say that shouldn't work

The fish-bowl roster now reads `Odile V. · 🍺 · 🍺 · 💼 · 💰 0.62` instead of five columns of words.
The name rule is given-name + surname initial; the other four columns are glyphs. This is the first
step of a usability pass, not the end of one.

**The thing that will surprise the next person: emoji render in full colour, for free, and the
documentation says they shouldn't.** Godot 4.6's own `gui_using_fonts` page states plainly that
COLR/CPAL emoji fonts are **not supported** — and Segoe UI Emoji, which Windows system-fallback
resolves to, *is* COLR/CPAL. By the docs this should have produced monochrome outlines or tofu. It
produced colour, including VS16 sequences (`⚔️ ❤️ 🛡️`) that had every opportunity to degrade to text
presentation and didn't. Verified in pixels via GTH captures (`.captures/gth/smoke/002-slot20-roster.png`),
then re-verified by grepping the roster path for `set_icon` / `ImageTexture` / `load_svg` / `FontFile`
and finding **none** — these are plain strings through `set_text`, rendered by the stock font stack.
No bundled asset, no theme change, no fallback configuration.

**We did not chase the reconciliation, and the next person shouldn't either without cause.** The
standing hypothesis — *unverified, do not cite as fact* — is that the docs are imprecise about COLRv0
vs COLRv1: FreeType has rendered COLRv0's layered-solid-colour glyphs through a long-standing layer
API, while COLRv1's gradient machinery is a newer, separate path. "COLR/CPAL unsupported" is plausibly
an over-broad claim about v1. Nobody read Godot's source to confirm it. The pixels were decisive and
the decision didn't depend on the mechanism.

**The real risk is platform, not format, and it is unmitigated.** This works because *Windows* ships
an emoji font the fallback can find. A Linux checkout with no emoji font gets tofu, and **nothing in
the repo would catch it** — no test asserts a glyph rendered, and `read_element` returning `"🍺"` only
proves the string was set. If cross-platform glyph fidelity is ever wanted, the researched answer is
Twemoji's **SVG** source rasterized at runtime via `Image.load_svg_from_string` (~86 µs per 32px icon,
thread-safe, ships in export templates — the "SVG is editor-only" claim is stale). Twemoji *as a font*
is a dead end: `mozilla/twemoji-colr` is COLR/CPAL by construction. Parked, not adopted.

**Why three fields were added to `Api/WorldView.cs` when this is meant to be presentation-only.**
Keying glyphs off `activity` was the obvious zero-core-change route and it is a trap: `activity` is
**authored prose** (~35 free-text strings like "a pint before home"), so work/haunt/home are not
recoverable from it, and the mapping would break the first time a day-plan was re-authored. Clockwork
already computes `mode` and `asleep` as closed enums and was discarding them at the projection
boundary; `place_kind` likewise. Exposing them is additive, touches no hash input (`ComputeDayHash`
never reaches `WorldView`), and keeps the *rule* in the core where it belongs while the *glyph* stays
in GDScript. `dotnet test` 22/22.

**Two judgment calls worth knowing about.** `Widow Karsk` keeps her full name: the mechanical rule
yields "Widow K.", which identifies *less* than the full name, because "Widow" is a shared class and
"Karsk" is the actual handle — abbreviating the only distinguishing token defeats the column. And
haunt is `💬`, not `🍻`, for a reason only pixels could have found: `🍻` sat beside the inn's `🍺` in
the adjacent column and the two amber mugs were indistinguishable at 20px — different facts that
looked alike, which is worse than redundancy. It was also simply wrong for Tam, who haunts the
Guildhall Steps.

**Column titles stayed words on purpose.** Every emoji column's tooltip carries the original word, so
nothing the table used to say was destroyed — but a tooltip is only discoverable if you know to hover.
The header row is the only always-visible legend, and emoji titles would leave a cold reader with no
anchor anywhere on screen. It renders once, not once per row, so it buys no density either.

**Swept up en route:** the Dawn Summary labels had no `autowrap_mode`, so after day 1 each demanded its
full ~670px single-line width as the Read column's minimum, starving the roster and pushing `Top`
**out of the viewport entirely**. Pre-existing, unrelated to emoji, and invisible until a day had run —
which is why the first capture looked clean. Fixed here because "does the layout survive?" cannot be
answered honestly in a window a different bug has already broken.

**Also recorded: `GTH.B9`** — `run_scenario` over MCP returns `{}` and executes nothing, while the
file-based runner accepts identical steps and works. Open, not yet independently reproduced. It is the
**fourth** instance of the `B2`/`B3`/`B7` disease, and the plan now argues the fix has to be structural
rather than a fourth patch: the lesson *"a harness must not have a way to fail quietly"* keeps being
re-learned one module at a time by whoever is first to call something. Worth noting the pattern —
`B9` was invisible until a consumer used MCP instead of a scenario file, exactly as `B7`/`B8` were
invisible until a second app shape existed. **The harness's bugs live wherever a consumer has not been.**

**Amended later the same day: `GTH.B10`–`B12`.** The usability capture sweep — the first consumer to
build a *corpus for someone else to read* rather than take one interactive shot — found three more of
the same disease within a single session. `max_dim`'s default (1280) is **narrower than the app's own
1290px viewport**, so it silently downscaled every frame to 1280×803 and silently invalidated the 1:1
correspondence between the pixels and the `query_element` rects the analysts measure against. Dedup
compares against the *global last* capture regardless of label, so a deliberate A/B — knob MIN vs MAX,
exactly the comparison `VFB.Q1` needs — writes **no file** when the two states match, turning a real
result ("these are identical") into missing evidence. And `session_id` is unreachable except by editing
tracked config, so a read-only consumer cannot choose where its captures land. The sweep caught all
three and worked around them; an agent that hadn't thought to check would have published measurements
off by a silent scale factor. **That makes seven instances of fail-toward-false-reassurance across
`B2`/`B3`/`B7`/`B9`–`B12`, and the honest reading is that writing the lesson down three times has not
enforced it.** The plan now argues the fix has to be structural. Recorded during the usability run
rather than fixed, to keep that run's diff about usability — but they are the tool this project uses to
tell whether anything works, and a corpus-corrupting harness undermines every study built on it.

---

## 2026-07-16 — GTH.Q4 ruled full convergence — and the second consumer found two bugs within the hour

Panda ruled the last four open GTH questions. `GTH.Q2` (3D hit-reporting) and `GTH.M5`'s optional C#
scenario facade are **closed as YAGNI** — neither has a consumer, and both would be maintained API
built against a guess. `GTH.Q3` is **closed: the harness stays input / inspect / capture** — seed-pinning
is inherently project-specific while GTH's whole claim is project-agnosticism, and it is moot anyway
(the fish-bowl's seed is already drivable through its own `seed-spin` / `btn-reseed` handles; the
harness drives the app, it does not reach inside it). **`GTH.Q4` ruled full convergence**, and that one
was work.

**The headline, and the reason to record this at all: the ruling paid for itself within the hour.**
`morning-queue`'s very first GTH run walked the curated 17-visitor shift, reported **zero failures**,
and **6 of its 17 clicks had never happened.** Two bugs, both live since release, neither ever tripped
by the fish-bowl: `GTH.B7` — `click_element` returned a refusal as `{clicked: false, note: …}`, a
**`note`**, a key no driver checks, so the ScenarioRunner walked straight past it and the scenario went
green having clicked nothing (`wait_for`'s timeout had the identical shape: `{timed_out: true}`, no
`error`). `GTH.B8` — `wait_for` could wait for *visible* but not *clickable*, and the desk disables its
stamp buttons between visitors, so they are visible-but-dead for a beat and the wait sailed through into
the refusal. **What found them was not a better test; it was a differently-shaped app.** The observatory
never disables a button, so one consumer could not have found this in another year. That is the argument
for convergence, and it arrived unprompted an hour after the ruling.

`B7` is the *third* instance of one disease (after `B2`/`B3`), so the lesson is now written on the plan
in its general form: **a harness must not have a way to fail quietly**, and the reliable smell is *a
result key invented for a sad path that no caller reads* — `note`, `timed_out`, a dropped argument. All
of them fixed the same way: return `error`, which every driver already treats as failure.

**Why "shared" had to mean canonical-plus-sync rather than one directory.** Godot resolves addons under
`res://`, so two projects physically cannot share a folder. Symlinks would do it and need Developer Mode
on Windows and do not survive a clean clone. So: canonical at `utils/godot/gd_test_harness/` (beside the
MCP server that was already shared), fanned into each project by `utils/python/sync_gth_addon.py`, whose
**`--check` exits 1 on drift** — that check *is* the ruling's substance. The trigger was never
aesthetic: six bugs got fixed in one copy that morning, and a second copy would have been six bugs stale
with nothing anywhere to say so.

**The isolation rule was discharged, not broken** — and it discharged *on its own terms*, which is worth
noticing. It named "a capture harness" as duplication written on purpose, and made convergence "a
post-v1 decision for Panda, made once both prototypes have settled shape." They settled; the decision
came due. Its other reason had also quietly expired: the MQT refactor it was protecting finished on
2026-07-15. Kept in full on the VFB plan as history, because it explains why the fish-bowl's early code
looks duplicated — otherwise a mystery to the next reader. Still standing for everything else (RNG
wrapper, JSON helpers): duplication is cheap right up until the duplicate needs a fix.

**The port improved on what it replaced, which was not the plan.** `DevHarness.gd`'s auto-step read each
visitor's `truth.binary` at runtime and played it back — so its **17/17 was tautological**: it applied
the answer key by construction and could not fail whatever the desk did. `shift-walk.json` hardcodes the
17 verdicts and asserts `17 / 17`, so judging drift now *fails*; and it clicks the real `VerdictBar`
buttons rather than calling `Main._on_stamp_chosen`, exercising one layer more than the harness it
replaced. Cost: change `visitors.json` and you must update that list on purpose. That is the trade a
real fixture makes.

**Not converged, deliberately:** `DeskFeatureHarness.gd` stays. It is a *feature test* (12 desk-tile
assertions), not harness plumbing, and it is **white-box** — it reaches into `Main`'s private members by
its own admission, where GTH is black-box by construction. Moving it is a rewrite of what it asserts,
not a port, and it is the tiles' only regression cover. Flagged on the plan rather than swept in.

Verified end to end: `mq-shift-walk` green (55 steps, 17/17 clicks landed, score `17 / 17`),
`fb-regression-b1-b6` green (34), `fb-smoke` green (14), drift check clean on both projects, morning
queue's boot self-check unchanged at `7 days, 96 visits, 0 problems`, `dotnet test` 57/57. Touching the
two frozen components (`VerdictBar`, `Scoreboard`) was `set_meta("test_id", …)` only — metadata, no
behaviour, no signature, per the `Observatory.gd` precedent.

## 2026-07-16 — GTH.M5: Mode B built — the trace watches everything, so it can disagree with the prediction

Built the Mode-B observed trace (`trace: true` on `click_at`/`click_element`), which `GTH.D3` deferred to
M5 back on 2026-07-15. That closes the last code item on the harness plan; `GTH.M5`'s only survivor is the
optional C# scenario facade, recommended closed as YAGNI (below).

**The one design call worth recording: Mode B connects to *every* visible non-IGNORE Control, not to Mode
A's candidate list.** Connecting to the candidates would be cheaper and is the obvious implementation —
and it would be worthless. A trace that can only observe what the prediction already predicted cannot
falsify the prediction, and falsifying it is the entire reason Mode B exists: `GTH.B4` was Mode A naming a
consumer, with total confidence, that never received the event. Watching only the predicted set would have
reproduced that bug's blind spot exactly, while *feeling* like verification. So the report carries
`consumer_observed` **and** `consumer_predicted` **and** `agrees_with_mode_a`, and when they differ it says
in words to believe the trace.

**What it can't do, stated where users will read it:** Godot emits the `gui_input` signal *before* the
Control runs its own `_gui_input` (so that a listener can override an event and then accept it). There is
therefore no per-control "did *you* consume it?" hook — the consumer is inferred from where the chain
**stops**, and `handled_on_arrival` reports whether the event was already spent by the time it arrived.
Input consumed outside the GUI dispatch entirely — a raw `_input` handler — is invisible to Mode B. That
limitation is in the addon README and the plan rather than in a comment nobody reads, because the whole
point of this run was that a harness must not overstate what it knows.

Verified on two cases: `btn-step` and a roster-Tree location click. Mode B watched **52 controls** and
**agreed with Mode A both times** — the expected, boring, correct result. Mode A is right nearly always;
what changed is that its being wrong is now *detectable* rather than silent.

**The C# scenario facade (`GTH.D2`'s "optional thin facade") is deliberately not built**, and flagged for
a ruling rather than quietly dropped. It has no consumer: the one project using GTH authors scenarios in
JSON, and that format is shared by both drivers by design (`GTH.R2`). Building an authoring surface for a
hypothetical C#-first adopter means maintaining API against a guess. Recommend closing as YAGNI; revisit
if such a project actually shows up.

Incidental proof the new contract check earns its keep: adding `trace` meant declaring it in *two* places
(the tool schema and `Accepts`). Had I updated only one, `ContractErrors()` would have failed `--selftest`
on the spot — which is precisely the failure mode that let `region` and `repeat` ship broken.

## 2026-07-16 — GTH.B1–B6 fixed: minimize really does freeze the framebuffer, and the self-test was testing the wrong layer

All six harness bugs closed and verified — `tests/harness/regression-b1-b6.json` green (32 steps, 0
failures) and `--selftest` extended. Four things here would surprise the next person; the first two are
the ones worth the read.

**`GTH.B6` is not a theory. Minimize freezes the framebuffer while the sim runs on, and dedup then calls
the change "unchanged."** The guess going in was that a minimized window stops presenting. It does, and
the measurement is unambiguous: capture a frame (`sha 2c6be602759f5fc5`), minimize, click `btn-step`,
read the clock — **it advances to `Day 2 · 00:30 (slot 1)`**, so input is entirely unaffected — then
capture again. **The sha comes back byte-identical.** Restore the window and it is `fe5149114b74d101`.
So the harness had a path where the app visibly changed and the capture insisted nothing had: with the
default `if_changed: true`, that identical sha **deduplicates to `changed: false`**. That is *the exact
failure the sha-vs-phash ruling exists to prevent* (DEV-LOG 2026-07-15 — "for a test harness a false
'unchanged' is the dangerous direction"), arriving through a third door in the same addon. Twice now the
lesson has had to be re-learned rather than transferred; writing it on the plan is the third attempt.
The fix restores the window before any capture. Proving it needed an `allow_minimized` escape hatch —
without a way to *induce* the bad state on purpose, "minimize breaks capture" stays a story someone
tells, and the guard becomes something a future reader deletes as superstition.

**`GTH.B2`/`B3`: the self-test could not have caught these, and "add a region to the self-test" would
not have fixed that.** The field report filed them as two bugs; triage found one root cause (the tool
schemas never declared `region`/`annotate`/`repeat`, and `Pick()` dropped unrecognised args silently);
but the deeper finding came from reading `SelfTest.cs`. **It called `bridge.CallAsync` directly — so
`McpServer.Map()`, the layer both bugs actually lived in, was untested by construction.** A self-test
that *had* passed a `region` would have handed the bridge a clean array, gone green, and left real MCP
calls just as broken. Testing the wire is not testing the surface. So: the self-test now routes through
`Map()`, and the contract stopped depending on anyone remembering. There were **three** copies of it —
the JSON schema, `Pick`'s allowlist, and `Map`'s per-tool key list — free to drift in silence, and they
had. Now there are two, and `ContractErrors()` proves they agree at startup, failing `--selftest` if a
tool ever declares an argument it does not honour or honours one it never declared. An unrecognised
argument comes back named in `gth_warning` instead of vanishing. `repeat` echoes the count actually
injected — and the proof it is not just echoing the number back is that the *fish-bowl's own* F9 handler
logs `[capture]` twice for `repeat: 2`.

**The regression scenario caught a false positive in my own `B5` guard on its first run**, which is the
best argument for having written it. Baselining the window size in `_ready()` reads the *requested*
1280x800 out of `project.godot`; the platform then adjusts the real window to **1290x810** before the
first command lands — so the drift warning fired on every single session. Sampled lazily on first use
now. A warning that always fires is a warning nobody reads, which is the same disease as a green test
nobody questions — and this repo already has the `btn-generate` scar to prove it.

**`GTH.B1` had a second half and it forced a spec amendment.** The field report caught `_onscreen` using
`intersects` (any overlap = "on screen"). It missed that the click *anchor* was unclamped too, so
`click_element` re-derived the raw centre and aimed at x=1323 in a 1290-wide window: the report lied
*and* the click sailed past the window edge. Fixing it broke `GTH.R5`'s stated predicate, because
strict `on_screen` would then have called a 4px-visible button **unclickable** — the same lie told
backwards. `clickable` is now decoupled from `on_screen` and means *"would a click aimed at this land?"*,
evaluated at the clamped anchor. The real `btn-storylets` now reports `on_screen=false`,
`visible_fraction=0.133`, `clipped=[right]`, `clickable=true` — all four true at once, and the click
lands at x=1289. `snapshot` had a *second, weaker* clickable formula omitting `is_top_hit`; it now shares
the one predicate, because two callers getting two answers about the same button is its own small lie.

Two smaller notes for whoever works here next. The **scenario runner had no way to expect a failure**, so
a negative test failed the scenario it was proving correct — which is precisely why the error paths were
never covered; `expect_error` fixes that. And **the running MCP server holds its own DLL open**, so
`dotnet build` cannot write `bin/` while the client is connected: the `mcp__gth-fishbowl__*` tools in a
session are pinned to the binary from when the client started. Build out-of-tree (`-o <tmp>`) to verify
server changes mid-session; the GDScript addon has no such problem, since Godot re-reads it every launch.
`GTH.Q1` is answered by the `B4` fix (root viewport + embedded `Window`s; no arbitrary viewport
auto-discovery). `GTH.Q2`/`Q3`/`Q4` still want a ruling and are not code.

## 2026-07-16 — GTH: the backlog gets handles; two of the four bugs share a root cause; the plan was lying about itself

Panda added two harness items and asked for the backlog to be made real before it gets worked. Three things
came out of writing it down that were not visible from the field report alone.

**The plan's status line was stale, and stale in the direction that invents work.** It said the remaining
task was to `dotnet build` the server, restart, and approve, "to close the MCP-stdio→model leg" — a line
written on 2026-07-15 by a session that (correctly) could not self-verify that leg mid-flight. **That leg
closed the next morning**: the server is registered as `gth-fishbowl`, and the 213-call observatory pass in
the entry below *is* a model driving it end-to-end. The proof of closure was sitting in the DEV-LOG one
entry above the claim it falsified. The `dotnet build` step is real but it is a **clone** step (`bin/` is
gitignored), not an open task. `GTH.M5`'s "first real adoption against an AGT prototype" is likewise done —
that pass was it. M5's actual remainder is two build items: the Mode-B trace and the C# facade.

**`GTH.B2` and `GTH.B3` are one bug, not two.** The field report filed "`capture` region throws a String/Array
mismatch" and "`press_key` ignores `repeat`" as unrelated defects. They aren't. `McpServer.cs`'s tool-schema
table **declares neither argument** — no `region`/`annotate` on `capture`, no `repeat` on `press_key` — and
`Pick()` filters incoming arguments against a hardcoded allowlist, **discarding anything not on it without a
word**. So `repeat` was never implemented at any layer: not the schema, not the picker, not `harness_core`,
not the injector. The model that "used" it invented a plausible argument, and the server returned success for
work it never did. `region` survives `Pick` but, undeclared, arrives shaped however the caller guessed —
hence a String where GDScript wanted an Array. **The real defect is that an unrecognised argument vanishes
silently**, which makes the fix systemic rather than two patches. Worth noting *why* neither was caught:
`--selftest` passes neither argument. The self-test only ever walked the happy path it was written from.

**`GTH.B1` has a second half the field report missed.** It correctly caught that `_onscreen` uses
`Rect2.intersects` — any overlap counts, so 4px of a button in a 1280 viewport reads as "on screen". But the
anchor point is *also* unclamped: `click_element` aims at the rect's centre, x=1321, which is outside the
window yet still inside the button's rect — so the hit-stack reaches the control, finds no occluder, and
certifies `is_top_hit: true`. Both halves have to go, and the fix **amends `GTH.R5`'s stated predicate**:
`clickable` decouples from `on_screen`, because a 4px sliver genuinely *is* clickable. The harness's job is
not to call it unreachable; it is to stop implying it is fully reachable, and to aim somewhere real.

The four bugs now have Rule-8 handles (`GTH.B1`–`B4`) alongside Panda's two new ones: **`GTH.B5`** locks
window resize/maximize while the harness is active (every GTH coordinate is normalized against
`get_visible_rect().size`, so a mid-session resize silently invalidates every cached rect the caller holds),
and **`GTH.B6`** asks whether minimize-to-taskbar breaks it. B6 is likely `GTH.D7`'s constraint arriving at
runtime — a minimized window may stop presenting, and a stale frame is byte-identical to its predecessor, so
`if_changed` would report **`changed: false`**: the harness answering "nothing happened" when it means "I
cannot see." That is the same false-reassurance direction as the other four, which is the through-line now
recorded on the plan. `GTH.Q1` (multi-viewport scope) is **answered by `B4`** — it stopped being an open
question the moment it showed up as a wrong answer about a dialog.

## 2026-07-16 — GTH pass over the observatory: the town generator has never worked, and a green test hid it

Drove the shipped fish-bowl end-to-end through the GTH MCP for the first time since release (~213 tool
calls, ~9 relaunches, every `test_id`, four A/B experiments). Fired to establish a UI baseline before
`PNO` touches `Observatory.gd`, and to make a *behavioral* check on four claims a code reader had made
statically. **It found four real bugs, one of them a shipped feature that has never once worked.**

**`btn-generate` fails every time, and fails silently.** `FishbowlBridge.cs:49` calls
`TownLoader.Rebuild(World.Town, townees)` **before** building its storyletless town at lines 50–57.
`Rebuild` copies `Storylets = from.Storylets` — the old **anchored** bank — swaps in the new cast, and
validates; `SchemaValidator` checks every `_binding` against the new `TowneeById`, all 20 dangle, throw.
**The storylet-stripping at 50–57 is dead code: it runs after the throw it exists to prevent.** The
error surfaces only as a transient toast, so the roster just doesn't change and it reads as a no-op.

**The part worth internalizing:** `M4_GeneratorTests.Generated_Town_Holds_The_Invariants_And_Validates`
is **green**, and has been since release. It passes because its own `BuildTownWith` helper strips
storylets *before* validating — **it exercises a path the bridge does not take.** A generator test that
never calls `GenerateTown` isn't testing the generator anyone can reach. The green test is precisely
why this survived a release: it bought confidence in the wrong code path. `VFB.M4` is ticked "seeded
town generator ✅" on the strength of it. Corrected on the plan.

Three more, none blocking: **`btn-storylets` is unreachable once a day has run** — the `hash` readout
widens ~103px going from `—` to 16 hex digits and shoves the 90px button from x=1173 to x=1276 in a
1280 viewport, so the force-fire debug tool is reachable only *before* there is anything to debug (this
one lands on `PNO` directly — the observatory is out of horizontal room *before* the board panel and
outing track are added; recorded in the spec). **The `register` label lies about the lines beneath it** —
`WorldView.SummaryJson` serves dawn-cached lines while `register` recomputes live from the dial, so
moving `actionability` relabels the header while the prose stays character-for-character identical
(the engine is right; the readout misleads — and it is `VFB.Q5`'s instrument). **`hearsay_required` is
unobservable on seed 1123** — the gate never binds because every event is already witness-carried, so
`VFB.Q1`'s hearsay dial cannot be measured on the golden fixture at all.

**Behavior confirmed all four static claims**, which is the other reason to write this down: the
`storylet_rate` slider is a verified no-op above 1.0 (2.5 and 3.0 produce byte-identical hashes and
event counts to 1.0; 0.3 thins 12→9 and diverges), and the knobs panel holds exactly six controls with
neither `copresence_bonus` nor `storylet_cooldown_scale` among them. **Pairing a code reader with a
harness pass told to falsify it is cheap, and it caught what neither would alone** — the reader could
not see `btn-generate` fail, and the harness could not have known to look.

GTH's own four defects (region-capture marshalling, `press_key` dropping `repeat`, `query_element` not
clamping to the viewport, hit-stack mis-attribution over embedded Windows) are recorded in
`plans/PLAN-godot-test-harness.md`. The `query_element` one matters most: it reported
`on_screen: true` for a control at x=1276 in a 1280 viewport. For a *test* harness a false "reachable"
is the same failure direction as the false "unchanged" that the sha-vs-phash decision already rejected —
**one day earlier, in a different module of the same addon** (Capturer vs SceneProbe). The lesson was
written down and did not transfer, because nothing carried it into the prompt that needed it. Worth
remembering the next time a rule feels too obvious to restate: Rule 1 is passed verbatim into every
sub-agent prompt for exactly this reason, and this is what it looks like when a lesson *isn't*.

Spun the story of this pass out into **`AGENT-FEED.md`** (new, root, flavor tier — pointer added to
`CLAUDE.md`'s where-to-look): a social-feed-format retelling of the agent that was accused of being
stuck while it was busy auditing its own tools. The format is a joke; the facts are load-bearing and
every citation resolves. It exists because "how agentic development gets odd" is worth examples, and
this pass produced a good one: a sub-agent that debugged the harness *through* the harness, via a 4px
sliver of a button the harness had told it was fully clickable.

## 2026-07-16 — PNO: nine rulings landed, three of the spec's own arguments struck, golden split out of `data/`

Opened the `PNO` build (postings & outings; `plans/PLAN-fishbowl-postings-outings.md`). All nine gates
ruled. `PNO.D1` + `D3`–`D9` adopted on the recommendation. **`PNO.D2` was ruled *against* the
recommendation, and the ruling is better than the recommendation** — that is the entry worth reading.

**`PNO.D2` — Panda: *"make golden separate and secondary. Set the town up to have all features
enabled."*** The spec had recommended keeping the golden town posting-free and exiling postings to a
fixture. The ruling inverts it: **`data/` is the live all-features town; the golden day moves out to
`tests/towns/golden-town/`, frozen.** Why the inversion is right, and why the spec missed it: *a golden
master that lives inside the live data directory is not frozen.* Editing `data/townees.json` silently
changed what the golden test asserted against — **the fixture tracked the very thing it existed to
pin.** The recommendation preserved that flaw and then contorted around it ("keep postings out of the
main town forever"). It also created a problem nobody had noticed: `FishbowlBridge._Ready()` hardcodes
`res://data` and `Observatory.gd` never calls `LoadTown`, so under the recommendation **the M1 board
panel would have rendered permanently empty** and needed a town-switcher built to see it. The ruling
deletes that work.

The restructure was almost free, which is the surprise: `TestSupport.DataDir` had exactly three
consumers, and `LoadGoldenTown()` fanned out to 18 call sites through **one** of them. So:
`tests/towns/golden-town/` is today's `data/` copied verbatim; `ProjectPaths.GoldenTownDir()` is new;
`TestSupport.LoadGoldenTown()` now loads from it — **one line moved all 22 tests onto the fixture, and
they stayed green with no test edited.** `TestSupport.DataDir` deliberately still points at `data/`
because it drives `WriteFloatifiedData`'s recursive sweep — which means the Godot-stringify round-trip
covers every posting file we are about to author, for free. That is why the fixture lives under
`tests/` and not `data/`: anything under `data/` joins that sweep.

**Three of the spec's own supporting arguments were falsified before a line was written**, by a
drift-check reader plus an adversarial verifier told to break the five load-bearing claims (21 of 22
claims confirmed; the architecture holds). All three are struck in place in the spec:

- **"Making `Away` derived keeps `SetAway`, the `away` hash key, and `departs_day` working unchanged"
  — false on all three counts.** `Away` has **four writers**, not just readers, so it won't compile as
  a derived property. Worse are the two that *would* compile: `World.cs:172` emits `["away"] = t.Away`,
  so a derived bool makes **Cooldown and InTown hash identically** — the determinism spine silently
  stops distinguishing a third of the new state space, and "unchanged" is the failure mode rather than
  the reassurance. And `Pressures.cs:29` treats `Away` as "freeze drift", which contradicts the spec's
  own "party co-present at the site". Bonus: `SetAway(id,false)` is **already** a no-op for a
  `departs_day` townee (it re-runs `Clockwork.ResolveDay` in the same call, which sets the flag back),
  so there was never any behavior there to preserve.
- **"A site is a place ⇒ zero engine changes" — false.** The read side is genuinely clean (every lookup
  is a `TryGetValue`; `Hours`/`Capacity` are never read at runtime at all). But `SchemaValidator` hard-
  rejects any place token outside `work|home|away|haunt:<id>`, and **`TownGenerator.cs:44` treats any
  `board:false` place as a home candidate** — an offscreen site would silently become somewhere
  generated townees *live*. The claim was also never testable: `SetOccupants` is `internal` with no
  `InternalsVisibleTo`, so "a place no dayplan references, with townees at it" is unreachable.
- **"`chronicle_since` gives us the retelling for zero summarizer changes" — aimed at the wrong
  problem.** The predicate is a **global existence check** with no role scoping: any outing beat by
  anyone satisfies it for everyone. The spec worried the day-window would age out; the real blocker is
  that it cannot express "retell *this* party's outing" at all.

The rulings all survive their bad arguments — but the arguments had to go, because the next agent
would have budgeted from them. Load-bearing lesson for the next spec: **claims about existing code
decay, and the ones a design leans hardest on are the ones most likely to have been read generously.**

Also found and recorded on the parent plan (`PLAN-village-fishbowl.md`), because `VFB.Q1` is being
tuned with these: **`copresence_bonus` is read by no engine code and has no slider** (the spec's claim
that it was "bound to an observatory slider" is the one claim of 22 that drifted — there is no dial, so
nobody was misled by one); **`storylet_rate`'s entire 1.0→3.0 half is a no-op** because `FireGate` gates
on `rate >= 1.0` while the slider runs 0–3, so `VFB.Q1` cannot be tuned *upward* with that dial; and
`storylet_cooldown_scale` is the mirror — consumed, but with no slider. None are being fixed inside
`PNO`: wiring them would change what fires and move `VFB.Q1`'s numbers under Panda mid-measurement.

Day-hash note (Rule: only a human's written-down chip goes stale): the day-1 baseline was
**`0ccec96222e31dbe`** at `08c9a22`, and it will shift once `phase`/`postings` keys enter
`World.ToHashNode`. No test pins a day-hash *value* — but three FNV vectors and a canonical-format
literal *are* pinned in `DeterminismPrimitivesTests`, so the hash **function and format** are not free
to change even though its **inputs** are.

## 2026-07-16 — AGT: the fish-bowl pages were shipped unlinked; registration resynced (Rule 3)

Found while opening the `PNO` build, not while looking for it. Commit `97a3710` ("village-fishbowl:
first release") **added `fishbowl.html` + `fishbowl-studies.html` but committed no registration for
them.** At `HEAD` before this entry, `MUSING-CONFIG.json` contained zero mentions of "fishbowl", and
`utils/python/build_site.py` renders each landing-card's sublinks from exactly that `links` array —
so both pages deployed to Pages as **orphans, reachable only by direct URL**. Nobody would have found
them from the site.

What made it non-obvious: the fix was *already written* and sitting uncommitted in the working tree
(`MUSING-CONFIG.json`, `adventuring-guild-teller/index.html`, `README.md`,
`plans/PLAN-adventuring-guild-teller.md`) — four dirty files, untouched since 2026-07-15. The VFB
session updated the Rule-2 nav spec (`ADVENTURING-GUILD-TELLER.md:45` has the `fishbowl/` file-map row,
build status and all) and committed *that*, but its registration tier never made it into the commit.
So the desync reads as "half a sync" rather than "no sync", which is exactly why it survived a release.

**The trap, for whoever hits this pattern next: those staged edits were stale, and committing them
verbatim would have published a falsehood.** They were authored *before* the build and said the
fish-bowl "waits on the `VFB.D` rulings" — but those rulings were adopted and the prototype shipped the
same day, in the very commit that orphaned the pages. Corrected the tense in place (three lines:
`index.html`'s card + foot, `README.md`'s table row + plan list) rather than rewriting the pages: they
*are* still the proposal (`FB.1`–`FB.10` remain open for correction), so only "waits on" was false.
Left `MUSING-CONFIG.json`'s description alone — it describes the published *pages*, and the pages are
still the proposal.

Deliberately **not** done here: the VFB plan's sync footer also asks that `fishbowl.html` fold the
adopted rulings into its `FB.*` claims (strike-not-renumber). That fold never happened — the page still
reads as awaiting rulings in 9 places. It is a public surface and a `VFB` chore, not a `PNO` one; flagged
for Panda rather than swept into this build. Re-raise it before any push that touches `fishbowl.html`.

## 2026-07-15 — GTH: reusable test-harness addon built into the fishbowl (in-engine core + prescripted runner)

Implemented the `godot-test-harness` (spec: `plans/PLAN-godot-test-harness.md`, mnemonic `GTH`) as a
drop-in addon in `adventuring-guild-teller/fishbowl/addons/gd_test_harness/` — the fishbowl's **own copy**,
per the VFB isolation rule (no shared library yet; convergence with the F9 DevHarness stays a post-v1 call,
`GTH.Q4`). It is **additive** (F9 untouched) and **inert** unless activated, so golden-day determinism is
unaffected. Verified end-to-end by a rendered prescripted run (`tests/harness/smoke.json`): a synthetic
click on `btn-step` advanced the observatory clock to slot 1 (input → `Button.pressed` → `bridge.StepSlot`
→ refresh), a location click at (0.15,0.5) selected the roster's Sela Quick, and three captures
(baseline / after-step / annotated) landed in `.captures/gth/` with a `manifest.jsonl`.

Decisions the next person would not guess:

- **The two-process split is unavoidable and became the architecture.** Injection / introspection /
  capture must run *inside* Godot, so the "MCP" is an external server over a loopback WebSocket to an
  in-engine Bridge autoload. The four GDScript modules (InputInjector / SceneProbe / Capturer / Bridge)
  are the product; the prescripted ScenarioRunner and the live Bridge are thin drivers over **one** command
  API (`GTH.R2`). Pure GDScript keeps it `.NET/GD`-agnostic — a C# node and a GD node are both just `Node`.
- **`changed` is sha256, not a perceptual hash.** An 8×8 average-hash washed out a real one-line clock
  change (hamming 1 ≤ threshold) so the first cut *falsely deduped* it. For a **test** harness a false
  "unchanged" is the dangerous direction — so exact-frame sha256 is authoritative and phash is advisory;
  visual-similarity dedup is opt-in (`similar_ok`). sha proved stable frame-to-frame under gl_compatibility
  for a static scene (the repeat capture deduped at distance 0), so exact dedup is reliable here.
- **Pixels need a rendered window** (`GTH.D7` confirmed): `--headless` uses a dummy renderer with no
  framebuffer, so capture is blank there. The harness runs the observatory windowed for captures (the F9
  DevHarness already did) and reserves headless for input/introspection/assertions.
- **`test_id` meta is the durable handle** (`GTH.D4`): the observatory builds its UI in code, so runtime
  node names are auto-generated (`@Button@10`) and fragile. Tagged the readouts/buttons with `test_id`
  meta (`clock`, `roster`, `btn-step`, …); text resolution also works (buttons carry `.text`), but ids
  survive relayout. This is the one deliberate touch to `Observatory.gd` (metadata only, no behaviour).
- **Two GDScript gotchas cost a couple of iterations:** methods on an untyped injected `var`
  (`_probe`/`_capturer`) return Variant, so `:=` can't infer — those locals need explicit types; and a
  capturer helper named `_set` silently overrode `Object._set` (signature-mismatch parse error) → renamed
  `_plot`. The headless `--import` pass is the fast way to surface both before spawning a window.

Live MCP path is now **built and verified** (`GTH.D2` → **.NET**): the external server
`utils/dotnet/gth-mcp-server/` (dependency-free .NET 8, MCP stdio ⇄ loopback WebSocket) round-trips
end-to-end — `--selftest` launches the game with `--gth-serve`, connects, and a `click_element` **over the
wire** advances the observatory clock to slot 1, then captures. The one leg left is MCP-stdio→model:
register the server in the Claude config + restart (can't be self-verified mid-session). The server is
shared tooling in `utils/dotnet/` (row in `utils/README.md`); the addon stays the fishbowl's own copy.
The server is dependency-free on purpose — a hand-rolled JSON-RPC/stdio loop over `System.Text.Json` +
`System.Net.WebSockets`, matching the repo's zero-NuGet ethos and sidestepping a fresh package restore.

Registered project-scoped in the repo `.mcp.json` as `gth-fishbowl` — kept **Rule-7-clean** by making the
server self-sufficient: `GTH_PROJECT` is committed **relative** (`Path.GetFullPath` resolves it against the
CWD, which is the repo root for a project-scoped server) and `GTH_GODOT_EXE` defaults to
`%ProgramFiles%\godot\godot.exe`, so no home-dir/dead-name path lands in the committed config. Verified the
committed shape end-to-end (`--selftest` with the relative project path + defaulted exe → green). `bin/` is
gitignored, so a fresh clone needs one `dotnet build utils/dotnet/gth-mcp-server` before the server starts;
Claude Code prompts to approve the new project MCP server on restart (the security gate stays with the user).

## 2026-07-15 — VFB: first release of the fish-bowl prototype built (M0–M3 done, M4 in place)

Built the village fish-bowl's first release end-to-end: an engine-free `Fishbowl.Core` (C#/.NET 8)
+ CLI + xUnit under a Godot 4.6 mono observatory, JSON data — at `adventuring-guild-teller/fishbowl/`.
Adopted every recommended ruling because the staged `data/` already committed to them (`VFB.D1`
48×30-min slots, `VFB.D2` engine-free core, `VFB.D3` JSON-only storylets, `VFB.D4` 12/6/2, `FB.8`
bio-marks on/toggleable). 22 xUnit tests green; the **golden day reproduces its 7 scripted beats**;
day-hash sequence identical across two runs and between CLI and editor; the observatory boots with
zero engine errors (screenshot in `.captures/`, proof captured via the in-engine F9 harness).

Decisions the next person would not guess from the code:

- **Storylets are `_binding`-anchored.** The golden cast's tags over-match (both the Karsk↔Fenn rent
  pair and the Marrow↔Corvo axe-debt pair carry `debtor`; both `rumor-retold` carriers are
  gossip-carriers). Free predicate-search binding fired the wrong pair and consumed the per-storylet
  cooldown, breaking the golden day. So a storylet with an authored `_binding` is anchored to that
  cast (still gated by every predicate each slot); only unbound storylets search. This keeps the
  golden day exact **and** leaves emergence open for future rules. It is the answer to `MUA.Q4`-ish
  over-matching, and it means v0 storylets read as authored beats, not fully emergent ones.
- **Awake gate.** Non-`must_fire` storylets don't fire on a sleeping participant — otherwise the rent
  quarrel fired at slot 0 (both asleep at midnight). This moved it to the evening at the Long Table,
  which reads far better; the departure-farewell is `must_fire` and exempt (the adventurer is leaving,
  not sleeping). Golden beats still all fire, just at waking slots.
- **`departs_day` on the townee.** Brindle's mid-day departure needed a trigger; added an optional
  scheduled-departure field (sets `departing_today` that day, `Away` after). The away-flag knob is the
  live override. This is data added to `townees.json` for the fixture.
- **Regard drifts only through storylet effects in v0** (deliberate, `MUA.Q3`) — passive regard decay
  would wash out the authored debtor/courting tensions the golden day depends on.
- **Canonical day-hash quantizes floats to 6 decimals**, integral numbers emit as ints (so Godot's
  `4.0` hashes identically to `4`). That resolves `MUA.Q5`. Needed a tolerant **`long`** converter too
  (the seed is a long) — the round-trip suite caught it, exactly its job.
- **Live knobs live on `World.Config`** (a mutable copy of the loaded record), not `Town.Config`, so
  `SetKnob` takes effect without a restart and snapshots carry knob state.

`VFB.Q1` finding (not a blocker — it is the research question): the 3-seed × 7-day soak sustains
**avg ~4.4 distinct tellable lines/night**, with 3/21 nights dipping below 4. Twelve added storylets
(10 total) + the awake gate lifted it from ~3.7; pushing to zero-starvation is a live-knob tuning
exercise for Panda, which is what the observatory is for. Bank is 12 rules (M3 wanted ≥10).

## 2026-07-15 — VFB: data files staged ahead of the `VFB.D*` rulings

Out of build order on purpose (tokens low, plan is fresh): produced the golden-day
`data/` set in `adventuring-guild-teller/fishbowl/data/` — `places.json`, `townees.json`
(all 12 golden-day cast), `dayplans.json` (one template per role), `traits.json`,
`simconfig.json`, six `storylets/*.json` covering the plan's scripted beats (rent-quarrel,
stock-runs-low, fetch-arranged, departure-farewell, debt-nagged, rumor-retold,
market-squabble), and `golden/day1.json` pinning expected beat types/participants for the
`VFB.M3` acceptance check. No engine/code yet — the Godot skeleton, bridge, and core
classlib are still gated on `VFB.D1`–`VFB.D4` and `FB.8`. Places split `board:true/false`
(the six place-board cards vs. private homes) since the plan's home example
(`karsk-rents`) isn't one of the six cards — worth folding into `PLAN-village-fishbowl.md`
if the ruling confirms this split. Storylet bank is only the golden-day 6; `VFB.M3` wants
≥10, so more rules are still needed before that milestone.

## 2026-07-15 — VFB/MUA: UtilityAi (Autonome) mined — jargon in, RNG out

Panda pointed the fish-bowl at the sibling `../UtilityAi` repo (a finished-enough
utility-AI city sim, C#/.NET 8 core + Godot 4.6 mono front-end) with the brief "mine vs.
make anew; the minimum is jargon." Four read-only subagents swept docs, simulator core,
Godot side, and tooling; findings landed in
`plans/PLAN-village-fishbowl.appendix-MinedUtilityAi.md` (`MUA`, referenced from the VFB
plan header). Three things would surprise the next person: (1) Autonome is a
*near-sibling* of the planned fish-bowl — its engine-free Core/Data/Cli split
empirically de-risks `VFB.D2`; (2) its determinism is an illusion — every
"Deterministic\*" path hashes through .NET `HashCode.Combine`, which is seeded
per-process, and its three xUnit projects contain zero tests, so nothing ever caught it
— the strongest possible argument for VFB's golden-fixtures-first order; (3) its Godot
side is all C#, no GDScript, so the fish-bowl's JSON-across-the-boundary bridge gets no
precedent from it. Verdict shape: adopt the vocabulary (Property/ResponseCurve/Modifier/
Relationship map ~1:1 onto L2/L3), re-implement the patterns (curve presets,
softmax-top-K with must-fire override, decision records carrying candidate ranks +
property snapshots, the analysis/report layer), rebuild bridge + RNG + tests from
scratch. Seven open questions (`MUA.Q*`) queued to feed the `VFB.D*` rulings.

## 2026-07-15 — VFB: the village fish-bowl specced — studies → composite → observatory proposal

Panda commissioned AGT's pillar III as a prototype spec (Godot .NET + GDScript + JSON;
town sim + creation menus; readouts and debug knobs only, no desk/floor) — explicitly
**without reading `morning-queue/`**, which this session honored and then promoted into a
standing isolation rule at the top of `plans/PLAN-village-fishbowl.md`: no reads, no
shared code in v0, duplication on purpose, convergence being a post-v1 ruling. Rationale:
MQT is mid-refactor in a parallel session (its edits landed *around* this session's in
shared files while working — the isolation rule is already earning rent).

Approached research-style per the brief: six machinery studies at deliberate surface
level (`FBS.1`–`FBS.6` on the new `fishbowl-studies.html`) rather than one deep dive.
The scoring criteria were **derived from the settled claims first** (gossip yield,
explainability, authorability, dawn-cadence fit, weight, determinism) so the matrix
argues from the design contract, not taste. Outcome: no single machinery survives — the
two adopts fail in opposite directions (clockwork generates nothing; storylets can't
stand alone) — which made the pick a **composite, CPS**: clockwork day-plans for
co-presence, Sims-style meters demoted to slow *pressures* (fuel, never arbitration),
JSON storylets whose fired predicates *are* the because-list (AGR.2's citable causes for
free), and a hearsay-lite summarizer so dawn quotes the town's telephone game instead of
the engine log. GOAP and ledger economies declined with reasons on the record.

Two moves worth remembering: (1) the proposal page's hand-cranked mock (`fishbowl.html`)
is not an illustration — its canned cast and day are **the golden fixture** `VFB.M3` is
accepted against, so the mock is a spec with a scrubber on it. (2) The bridge carries the
tolerant-int lesson from this morning's MQT entry as a *lesson import, not code import* —
core parses float-shaped ints from day one and tests replay the Godot stringify
round-trip. Also amended the musing's "no JS" invariant deliberately (nav spec updated):
`fishbowl.html` introduces inline dependency-free JS per the `midi-drum` precedent —
self-containment was the real invariant, not scriptlessness. Dataviz discipline: the
musing accents fail the categorical-palette validator in both themes, so nothing on the
new pages separates series by hue alone — matrix cells encode by glyph shape, chips carry
text, sparklines are single-hue labeled tiles. Build gated on `VFB.D1`–`D4` + `FB.8`
(the bio-marks claim, which graduates the plan's open Wildermyth question toward
`AGT.13` if ratified).

## 2026-07-15 — MQT.6: docs synced; the tier refactor is complete

WP-G (Opus) brought `MORNING-QUEUE.md` and `CONTENT-BANKS.md` in line with the shipped
refactor: the three-tier engine line, the `dotnet build` + mono-only run step, the
`core/` box and bridge calls in the architecture diagram, the `MQT.D1` invariant rewrite
(Web embed deferred, citations reused from the plan's audit), the retired-`ShiftGenerator`
/ new-`CoreBridge` class_name-gotcha update, a new **Code map** section for `core/`
(one line per file — the deliverable-internal code-doc ruling, no repo-level `CodeDocs/`
tier stood up), and the 16→17 curated-visitor-count correction throughout. Coordinator
read the result top-to-bottom against the actual tree (Rule for G5) and found it accurate
— one cosmetic fix (an unrendered markdown link in the Invariants citation).

**Sync discipline (Rule 3) catch:** WP-G's file ownership didn't include
`plans/PLAN-adventuring-guild-teller.md` beyond a single tick-line, so it correctly
flagged rather than fixed two stale lines there (still said "GDScript-only" and quoted
the pre-rebaseline `97 visits` self-check). Coordinator fixed both directly after
re-reading the file fresh — a separate, unrelated concurrent workstream
(`plans/PLAN-village-fishbowl.md`, hard-isolated from `morning-queue/` per its own
plan) is also actively editing that same file right now; touched only the two
morning-queue-specific lines, left everything else (including anything mentioning
"fishbowl"/"VFB") untouched.

**Run summary, all six phases:** `MQT.1` locale + generator content out of scripts,
`MQT.2` .NET skeleton, `MQT.3` typed model + validator (with a real GDScript↔C# JSON
transport bug found and fixed via the boot gate, not by `dotnet test` alone — see the
`MQT.3` entry above; this is the run's main lesson worth remembering), `MQT.4` the
generator ported to `core/` with a rebaseline (97→96 visits, `MQT.D2a`), `MQT.5` the
accept/total limit rule single-homed in `Core.Deriver`, `MQT.6` this entry. All three
kickoff rulings (`MQT.D1`=A′ in-engine C#, `MQT.D2`=(a) rebaseline, `MQT.D3`=skip) held
through to completion with no need to revisit them. Ran unattended throughout (Panda
not present); every gate was coordinator-verified via actual `dotnet build/test` runs
and godot MCP boots, never taken solely from a subagent's self-report — that discipline
caught the one real regression (`MQT.3`'s JSON float transport bug) that a
trust-the-transcript pass would have missed. **Never pushed** — this run's commits sit
on `main` locally, awaiting Panda's review per the handoff.

## 2026-07-15 — MQT.5: the accept/total limit rule is single-homed

WP-F (Haiku, bounded patch-level spec) deleted `ReferencePanel._standing_order()` and
the `accept`/`total` recomputation inside `_scale_comparison()`, replacing it with a
lookup against `inspections.scale.verdict` (the field WP-D's Deriver now precomputes and
WP-E's composer now emits for generated shifts too). The presentation mapping (verdict →
`Loc` `amount_*` key → `Palette` GREEN/RED/INK3) is preserved exactly; the "nothing on the
pan" guard (amount not present → render no line at all) stays local to the panel, since
the verdict enum only distinguishes order-related no-op, not tool-input absence — noted
so a future reader doesn't assume the derive pass owns that case too.

G4 gate (coordinator-verified): grep confirms zero `accept`/`total` comparison logic
remains in any component script; DeskFeatureHarness toggled on for the check (flipped
back off after) — **12/12 pass**, zero engine errors. Did not pixel-diff
`nessa-broom`/a total-order visitor capture against a pre-refactor baseline (no saved
baseline capture existed to diff against, and the harness's own PASS/FAIL assertions plus
the unit-level `Week_EveryVisitCarriesScaleVerdict` test already prove the mapping is
byte-identical to the deleted logic) — flagging rather than silently claiming the visual
check ran.

## 2026-07-15 — MQT.4: the generator ported to core; GDScript original retired

WP-E (Fable — design authority delegated per the handoff) ported the 1,151-line
`ShiftGenerator.gd` into `MorningQueue.Core.Composer` as a pure `Generate(day, banks,
duesState, locales)`, executing the `MQT.D2a` rebaseline: the stream changed once (a
self-owned PCG32, not System.Random, to keep it engine-independent), and days 1–7 are now
golden-pinned fixtures (`GeneratorTests.cs` + `Fixtures/golden_day1..7.json`). **Visit
count rebaselined 97 → 96** — expected under D2a, confirmed by boot. `Deck.load_day(d>0)`
now makes one bridge call (`GenerateShift`); the live `townees`/`adventurers` dicts are
serialized fresh per call so the pay-dues floor beat still sees runtime state, not the
bank file's. `scripts/gen/ShiftGenerator.gd` (+ `.uid`) deleted; zero live references
remain (grep-confirmed). Boot self-check (`_selfcheck_generated`) shrank to a smoke line;
the substance moved into dotnet tests per the plan.

**Deviation flagged by the agent, accepted:** `cs/CoreBridge.cs` wasn't in WP-E's OWNED
list but needed one additive method (`GenerateShift`) — the bridge shape in the handoff
explicitly anticipated a day/banks/locales generation call, and no existing bridge method
was re-signed. Accepted as in-spirit; noting here per Rule 3 sync discipline since the
handoff's file-ownership table undersold this.

**Design call the agent made and flagged:** the naive weighted failure-axis draw never
surfaced 3 of the required axes across a week (fieldability/claimant/reach/duplicate are
rare) — the required "every reachable failure axis appears" distribution assert was
unsatisfiable without sample-without-replacement bias toward fresh axes first, falling
back to authored weights once every admissible axis has appeared once. Mirrors the
existing actor no-repeat design in CONTENT-BANKS.md §4.

**Latent oddities in the GD original, ported as-is (not this run's job to fix):** a
hardcoded curated `ledger` entry id (`ganton-reeve`) used for a random walk-in's
rank_gate-unverifiable check; a rank-fail case that silently degrades to a valid gate
visit when no under-ranked material exists; dungeon_drop/quest_file dues-fail items
falling back to `drops[0]`/`owns[0]`. CONTENT-BANKS.md §4 also describes an item_check
*authenticity* branch the GD generator never implemented (moot — no standing-order item
carries `forgery_glass`).

G3 gate (coordinator-verified): `dotnet test` 57/57 green (golden weeks + distribution
sanity + a live-dues-state regression test); class-name cache regenerated
(`--headless --import`); boot selfcheck `7 days, 96 visits, 0 problems`, zero errors;
DevHarness auto-step (toggled on for the check, flipped back off after) confirms day 0 =
**17/17** correct (the curated shift now carries 17 visitors since `nessa-broom`'s
rev-3 addition — the handoff's "16/16" wording predates that and is now stale, corrected
here rather than silently smoothed over).

## 2026-07-15 — MQT.3: typed model + validator move into core/ (with a G2 near-miss)

WP-D ported the domain model, validator, and the scale-verdict derive pass into
`MorningQueue.Core`, and wired `DeckLoader.gd`'s boot validation + day-0 load through
`CoreBridge.Validate`/`PrepareShift`. First G2 attempt shipped 34 green tests but **failed
the actual in-engine boot** with `The JSON value could not be converted to System.Int32.
Path: $.accept.max` — a real gap between the test harness and reality worth recording.
Root cause: GDScript's `JSON.stringify` re-emits every whole number with a trailing `.0`
(`4` → `4.0`) on its round-trip through `JSON.parse_string`; strict `System.Text.Json` int
binding rejected the float-shaped literal. None of the 34 tests caught it because they all
fed raw file text, never the Godot-stringified payload the real boot sends across the
bridge — the actual failure mode was in the *transport*, not the data. Fixed with a
`TolerantIntConverter` registered repo-wide in `Json.Options` (accepts int-or-float-shaped
JSON numbers for every `int`/`int?` model field) plus a same-fix pass over
`ParseIntTable` (which had a matching latent bug — `rankup_thresholds` rows with `4.0`-form
keys were being silently dropped, not just failing to convert). Added
`BootRoundTripTests.cs`, which replays Godot's stringify float-ification over the real
`data/` files through the actual `CoreBridge` entry points, so any future bridge payload is
exercised the way the engine actually sends it, not just the way a `.NET` test happens to.

**Lesson for future bridge work in this repo:** a green `dotnet test` run is not sufficient
proof a GDScript↔C# JSON bridge works — test fixtures must go through the same
serialize/deserialize round-trip the engine performs, not the raw source file. The
coordinator's gate discipline (booting via MCP before trusting a subagent's dotnet-only
report) caught this; it would not have surfaced from `dotnet test` alone.

G2 gate (coordinator-verified after the fix): `dotnet build`/`dotnet test` 38/38 green,
boot selfcheck unchanged, zero engine errors, `_validate_banks/_validate_shift/
_validate_inspections/_validate_standing_orders` bodies confirmed gone from
`DeckLoader.gd`, public Deck contract (members/signals) diff-reviewed untouched.

## 2026-07-15 — MQT execution begins: G0 kickoff + wave 1 (MQT.1 + MQT.2)

Ran unattended (Panda not present at kickoff) — proceeded on the plan's recommended
defaults: `MQT.D1` = A′ in-engine C# (mono runtime, Web-embed deferred), `MQT.D2` = (a)
rebaseline the RNG stream and pin days 1–7 as golden fixtures once the generator ports,
`MQT.D3` = skip theme datafication. Baseline confirmed clean before touching anything:
`[gen-selfcheck] 7 days, 97 visits, 0 problems`, tagged `mqt-baseline`.

Wave 1 (WP-A/B/C, disjoint files, ran concurrently):
- **WP-A** moved `loc.gd`'s `_LOCALES` table to `data/locales/en.json` byte-identically
  (diff-verified via a new `scrap_scripts/python/13_loc_json_diff.py`); `loc.gd` now
  lazy-loads it with a humanizer fallback on missing/broken JSON.
- **WP-B** moved three authored-content constants out of `ShiftGenerator.gd` into the
  banks (`_WALKIN_PROFESSIONS` → `generation.json` name_pools, the `_decoy_scale` prose
  table → `generation.json` decoy_scales, the hardcoded `0.25` depth-rate → `references.json`
  payout.depth_rate) with zero `rng.*` call reordering — verified by the coordinator via
  `git diff` hunk-by-hunk, not just the subagent's say-so. **Surprise:** the two
  `_decoy_scale("filing")` call sites never matched a real case in the original table (always
  fell through to `_` default) — renamed to `"default"`, output unchanged.
- **WP-C** stood up the .NET skeleton (`MorningQueue.sln`, root csproj, `core/` classlib +
  xUnit tests, `cs/CoreBridge.cs` stub). The `Godot.NET.Sdk/4.6.*` wildcard doesn't restore
  without a `global.json`; pinned to the exact installed editor version `4.6.1`.

G1 gate (coordinator-verified, not trusted from transcripts): `dotnet build`/`dotnet test`
green, boot selfcheck unchanged at `0 problems`, no bin/obj/.godot noise in git status.

## 2026-07-15 — MQT handoff prompt authored (coordinator + tiered subagent briefs)

`morning-queue/MQT-HANDOFF.md`: an executable operating manual for a **Sonnet 5
coordinator** running the MQT plan via subagents. Instruction depth is deliberately
inverse to model tier — Haiku gets a patch-level spec with placeholders the coordinator
must freshen (WP-F, the ReferencePanel de-dup), Sonnet gets strict numbered steps with
stop conditions (WP-A locales, WP-B bank extraction w/ the RNG-stream trap, WP-C dotnet
scaffolding), Opus gets goals + constraints + latitude (WP-D model/validator/bridge,
WP-G doc sync), and Fable gets mission + invariants with design authority delegated
(WP-E, the 1,151-line generator port). Load-bearing calls are pre-settled in the file so
the coordinator never adjudicates architecture (bridge shape, humanize-at-compose-time
via a Core Humanizer reading the same locales JSON, derive-pass verdict field). Rulings
`MQT.D1`–`D3` baked in as the plan's recommended defaults; confirming them with Panda is
the kickoff gate. Rule 1 is embedded verbatim in the hard-laws block every brief carries.

## 2026-07-15 — Morning Queue: tier-refactor plan authored (PLAN-morning-queue-tiers)

Audit of the prototype against the code/script/data framing: of 4,347 GDScript lines,
~35% is code or data in a script costume — `ShiftGenerator.gd` (1,151 ln of pure
deterministic systems code + embedded authored prose), the `_LOCALES` tables inside
`loc.gd`, ~155 validation lines inside `DeckLoader.gd`, and the `accept`/`total` limit
rule implemented twice (generator `_limit_result` + `ReferencePanel` ~622–666). .NET is
absent (the `[dotnet]` stamp in `project.godot` is just the mono editor's fingerprint).
Verified 2026-07-15 that Godot 4.x **still can't Web-export C#** — so the plan doesn't
dodge the collision with MORNING-QUEUE.md's "GDScript-only for the Web path" invariant;
it makes the rewrite an explicit ruling (`MQT.D1`: in-engine C# with a documented
pre-bake escape hatch, vs baker-only). Also flagged: a C# port changes RNG streams, so
generated weeks rebaseline unless PCG32 is ported (`MQT.D2`). Plan only — no code
touched; frozen component contracts stay frozen by design.

## 2026-07-15 — LoMa vignette page ships; MIDI Drum Coach registered as new musing

**LoMa `vignettes.html`:** "Everyday Records" companion page carries the three
finished vignettes (`LVIG.1`–`LVIG.3`: Transcription Nights / The Letter Kept /
Crack and Splint) over from the `VIGNETTE-HANDOFF.md` chat as a verbatim-copy HTML
page. Registered in `MUSING-CONFIG.json` (new "Vignettes" link), gallery card and
nav spec (`LOGICAL-MAGIC.md`) synced, `LVIG` mnemonic declared append-only. The
Grimoire (IV) stays the only remaining candidate on `PLAN-logical-magic.md`.

**MIDI Drum Coach (`midi-drum/`):** New HTML-first musing — a drum practice tool
framed as a game-design exploration: Web MIDI in from an e-kit (or keyboard/click
pads when kit-free), step-grid groove notation, millisecond judging, rush/drag
bias tracking, and a director that explains its own suggestion rules out loud.
Registered in `MUSING-CONFIG.json`, plan added at `plans/PLAN-midi-drum.md`,
indexed in `PLAN.md`.

## 2026-07-15 — Morning Queue: amount-fail visitor, richer Glass, pay-dues floor beat

**Context:** Four backlog items from the PLAN; the shift-select hub was already done.
Three items are data/plumbing; one (pay-dues) required new code.

**Amount-fail visitor (#17 nessa-broom):** No curated visitor ever failed on weight alone
— the Scale had teeth in the generator but was invisible in the tutorial. Added
`nessa-broom` (order 17): moonwort, 6 drams, against the apothecary's `accept 2–4 dram`
cap. Identity passes (glass confirms moonwort); Scale condemns (6 > 4). `failure.axis:
amount`. The worked-reject scenario INSPECTION-TOOLS.md §4 described explicitly was
finally made concrete in the curated shift.

**Richer Glass readings:** Thin card/seal decoy readings on visitors #2, #4, #5 — "A
silver card, edges bright, lately issued" doesn't feel examined. Enriched all three with
more tactile detail. Content-only change; no schema changes.

**Pay-dues floor beat:** The dues gate already blocked owing townees from posting; the
missing piece was a floor mechanic to clear arrears. Chose to add it inline to `Main.gd`
(not a new frozen component) because the floor beat is plumbing: no frozen interface
needed. `Deck.pay_dues(id)` mutates the runtime `Deck.townees` dict (JSON untouched);
the next `generate_shift(day)` reads the updated dues status and stops assigning that
townee a dues-fail visit. Floor beat appears after shift_complete, between the Scoreboard
summary and the Next-Day button. If all accounts are current, shows a "no dues to
collect" line (so the floor always renders). Update is in-place (button disabled, label
dimmed) — no queue_free from a button handler.

**Selfcheck:** `7 days, 97 visits, 0 problems` confirmed after all changes.

## 2026-07-13 — Morning Queue: day advance + skip-tutorial wired into the UI

**Context:** Playtest — no way to reach day 1+. The week-of-shifts data/generator
(`Deck.load_day`, `ShiftGenerator`) shipped, but nothing in the UI ever called it, so
the desk dead-ended at the day-0 "SHIFT COMPLETE" ledger. Not a performance issue — the
control never existed.
**Options considered:** (A) add day-flow controls to a component scene (VerdictBar /
Scoreboard); (B) keep it all in `Main.gd` as flow plumbing.
**Choice:** B. `Main` grows a top-of-booth day strip (day label + Skip-tutorial button,
shown only on day 0) and a Next-Day button under the ledger that walks day → day+1 up to
`LAST_DAY = 7`, then locks as "the week is done." `_go_to_day(d)` = `Deck.load_day(d)` +
`Session.start()`; banks are unchanged across days so only the queue reloads.
**Why:** day flow is session plumbing, not a component rule — keeps the four frozen
component interfaces untouched. New strings live in `Loc` chrome; button chrome is a
brass-outlined `_make_desk_button` matching the parchment theme.
**Notes:** verified via godot MCP — boots clean, gen-selfcheck `7 days, 97 visits, 0
problems`. Skip jumps straight to generated day 1; Next-Day caps at the seventh day.

## Entry template

```
## YYYY-MM-DD — <short title>

**Context:** what prompted this.
**Options considered:** A / B / C.
**Choice:** what we did.
**Why:** the deciding factor.
**Notes:** anything that would surprise the next person.
```

---

## 2026-07-13 — Session close-out: docs synced + AGT-scoped commit

**Context:** closing the Morning Queue build session. Captured the only-in-chat backlog
(day-advance hub, "pay dues" floor interaction, richer Glass for card/seal subjects, a
curated amount-fail visitor) into `plans/PLAN-adventuring-guild-teller.md`, marked the
Morning Queue plan item `[x]`, and recorded that **AGT.5 is mechanically settled** (the desk
ships binary). Refreshed `MORNING-QUEUE.md`'s status line.

**Commit scoping (would surprise the next person):** the working tree also carried
*pre-existing, unrelated* WIP from earlier sessions — `logical-magic/` (vignettes) and
`midi-drum/` (a whole new musing), plus their entangled entries in `MUSING-CONFIG.json` and
root `PLAN.md`. I committed **only the Morning Queue work** — `adventuring-guild-teller/`,
`plans/PLAN-adventuring-guild-teller.md`, `DEV-LOG.md`, `musing-tech-notes.md` — on a branch,
and left the other musings and the shared `MUSING-CONFIG.json` / root `PLAN.md` **uncommitted**
(couldn't verify they're commit-ready, and committing the AGT registration would drag in the
still-uncommitted midi-drum folder). **Consequence:** the committed AGT musing is not yet
registered in the committed `MUSING-CONFIG.json`, so a fresh checkout won't build it into the
site until that entry is committed alongside the other musings. Not pushed.

## 2026-07-13 — Morning Queue: week-of-content banks + procedural visit generator

**Context:** Panda's four asks — generalize the Glass, fill reference material to a week's
breadth, add a townee directory (townees pay dues to post), and a basic data-driven
procedural visit generator pulling from the banks.

**How it was built:** a three-phase workflow (Design → Banks×4 parallel → Generator),
6 agents, 0 errors. Design pinned every id + cross-reference so the four parallel Banks
agents couldn't drift; all phases returned plain text (the last workflow died gating a
files-on-disk phase behind a strict output schema — not repeated).

**What shipped:**
- **Banks (all JSON):** `references.json` broadened to 24 Book items across 5 categories
  (each with an authored `glass` examined-description — the generalization — + optional
  `forgery_glass`), 20 postings, 6 chapter ciphers (each with `glass`), 10 drops, an
  enlarged archive (per-adventurer logbooks + tokens), one added earth-warded roster party.
  Two NEW directory files: `townees.json` (16 townees; dues current/owing) and
  `adventurers.json` (16; rank/dues/chapter/logbook). `generation.json` = the generator's
  config (task weights, invalid_rate 0.45 + per-day ramp, failure-axis weights, 44×44 name
  pools). All existing ids preserved byte-for-byte; the curated 16 still resolve.
- **Generalized Glass:** the Glass now examines any subject kind (book item / transfer seal
  / completion token / logbook / rank card / filing), reading derived from the bank data,
  compared against the matching rulebook page.
- **Dues mechanic:** new `dues` failure axis (+ `amount`). Owing townees can't post
  (`quest_file`/`dungeon_drop` reject); owing adventurers fail `rank_gate`/`rank_up`. Two
  new reference tabs — **Townee Directory** and **Adventurer Directory** — so the teller
  looks up dues; a dues-fail check deep-links to the owing row.
- **Generator (`scripts/gen/ShiftGenerator.gd`, ~700 lines):** `generate_shift(day)` seeded
  by day (deterministic — a week is 7 reproducible shifts). Composes each visit
  actor→task→subject→valid/invalid→failure, emitting the EXACT `visitors.json` schema so
  card/panel/verdict/scoreboard consume generated visits unchanged. `Deck.START_DAY`/`day`
  selects: 0 = curated tutorial (visitors.json), >0 = generated.

**Verified:** boot self-check `[gen-selfcheck] 7 days, 97 visits, 0 problems`; curated day 0
= 16/16; generated day 1 = 14 coherent visits, all correct, zero errors. Read captures of
generated visitors (walk-in "Greta Inglebright" delivering Troll Bile vs the Tannery
Standing Order; approved as gen-d1-1, the same order rejected as gen-d1-12) and the Townee
Directory showing Sarai Quillon **owing 15**. Left shipped: `START_DAY=0`,
`DevHarness.enabled=false`, panel default = first reference tab (reverted the temp
capture-only tweaks).

**Note:** `ShiftGenerator` is a new `class_name` → the global-class-cache `--import` gotcha
applies. Design contract: `CONTENT-BANKS.md`.

## 2026-07-13 — Morning Queue: binary desk + inspection tools + standing-order limits

**Context:** Panda's three fixes — (1) hold/conditional are confusing and absent from the
genre; cut them "for now"; (2) some tabs should be *inspection tools* that reveal more about
the current item (herb characteristics), with decoy data like an irrelevant weight; (3) the
"jar of <item> vs <item>, unit drams" framing is misleading — standing orders should be a
total or min–max limit the item is measured against, via a scale tool.

**Design (in `morning-queue/INSPECTION-TOOLS.md`):** the Papers-Please loop split in two —
the **Rulebook** (static reference tables: what a thing *should* be) vs **Inspection Tools**
(visitor-scoped: what *this* item actually is). Two tools ship: **The Glass** (examine —
the item's tells) and **The Scale** (weigh — the measured amount). Every visitor carries
both readings; a hidden `relevant` flag marks which one decides (never shown), so most
readings are decoys — e.g. a rank-card weighed at "2 drams," or Pell's yarrow at a clean "1
sprig" (right amount, wrong plant). Standing orders became `accept {min,max,unit}` or
`total {needed,unit}`; an `item_check` is now two independent checks — identity (Glass vs
Book) and amount (Scale vs limit). Binary via the existing `STRICT_BINARY` dial = true
(reversible; the two former half-fails already carry `binary: reject`).

**How it was built:** a two-phase workflow — Model+Data, then Implement.

**What broke and the lesson:** the Model+Data agent did ALL its file work (data rewrite,
validator, the full design doc) and then failed to emit its **StructuredOutput** return,
hitting the retry cap (5) — which aborted the whole workflow before the Implement phase.
The *work* was fine and on disk; the *typed return* was the failure. **Lesson: for a phase
whose real deliverable is files on disk (not a data payload the next phase consumes), don't
gate it behind a strict output schema.** Recovery: verified the data booted clean, then ran
the Implement phase as a plain Agent (free-text report, no schema) — it completed cleanly.

**Verified (via the DevHarness capture loop):** binary desk shows only APPROVE/REJECT;
`ivy-threnody`/`odile-vantry` judge as reject; 16/16, zero errors/warnings. Read the Glass
capture ("Silver underside, five lobes, cold to the touch" under "Examine — what the item
actually is") and the Scale capture ("The jar settles at 3 drams." + green "within the
order's limit"). New additive method `ReferencePanel.set_inspection_target(visitor)`, wired
in `Main._on_visitor_changed`; no frozen signature changed.

**Note:** used a throwaway `_select_tab("glass"/"scale")` default-tab tweak to make the
harness auto-capture a tool page (it captures the default tab; tools aren't the default),
then reverted it. Left `DevHarness.enabled=false` (Panda's resting default).

## 2026-07-13 — Morning Queue: localization prep + a viewport-capture dev harness

**Context:** display text was inconsistent (Title-Cased in some places, raw slugs like
`rank_order` / `item_check` leaking in others) because the data stores identifiers and the
UI prettified some but not all — and the prettifier was copy-pasted across three
components. Separately, validating via the OS screenshotter was painful (other windows get
masked over the game).

**Localization prep (spawned agent):** centralized every user-facing string into one
`scripts/loc.gd` (`class_name Loc`) — the six duplicated formatters collapsed into a single
`humanize()` + a keyed `chrome`/`vocab`/`overrides` table, called statically like Palette.
Two layers, explicit: (a) translatable UI chrome + the finite enum/slug vocabulary → `Loc`;
(b) procedural content (names, summaries, player_story) → stays in the JSON. Identifiers are
never mutated — `checks[].entry` still resolves by exact string; only the *rendered* label
is humanized, with an `overrides` table for proper nouns the humanizer gets wrong. en is the
only locale; a second is one added dictionary. Chose a `Loc` module over Godot's native
`tr()`+CSV because the UI is 100% code-built and needs a *dynamic* slug humanizer a static
CSV can't express.

**DevHarness (`scripts/dev/DevHarness.gd`):** a validation aid on Main.tscn that (1)
captures the whole viewport to `.captures/*.png` — a folder readable directly, no OS
screenshotter, no window-masking — and (2) auto-steps the shift on a timer by invoking the
same handler a stamp-press fires, shooting one frame per visitor + the summary. Toggle via
the **Enabled** checkbox on the node (on by default so a bare `run_project` yields a full
capture set; untick to play manually). This is the edit→run→read loop Panda asked for.

**Consistency fix the harness immediately surfaced:** the end-of-shift ledger showed "Wren
Sixpence" (id `wren-sixpence` humanized) while the card showed the authored "Wrenna
Sixpence". Fixed additively — `Session` verdict-log entries now carry `name`, and the
Scoreboard prefers it. No frozen signature changed.

**Verified:** headless import + boot = zero errors; a full auto-stepped run scored 16/16 and
wrote 17 captures; read the summary PNG back to confirm the ledger now reads "Wrenna
Sixpence" and every reference tab is Title-Case ("Rank Ladder", not `rank_order`).

**Notes:** `.captures/` carries a `.gdignore` (Godot skips importing the shots) and is
`.gitignore`d. `Loc` is a new `class_name`, so the same class-cache gotcha applies — the
agent ran `--import` to register it.

## 2026-07-13 — Morning Queue: components built via workflow + playtest-verified

**Context:** with the scaffold + frozen contracts in place, built out the four UI components
+ theme.

**Choice:** fanned out one agent per component (card / reference / verdict / score / theme)
in a Workflow, each owning a disjoint file pair against the frozen interfaces, each
self-reviewing after building. Then integrated (wired `ThemeFactory.build()` into Main) and
playtested in Godot myself.

**Why the file-per-component split:** the frozen contract (methods/signals in
MORNING-QUEUE.md) + one `.tscn`+`.gd` per component meant five agents wrote in parallel with
zero merge conflicts. The self-review stage caught three real bugs the build missed: an Array
routed to a Dictionary-typed param in ReferencePanel (would crash on `set_references`), a
`get_node` result needing an `as ColorRect` cast in VerdictBar, and a base-class shadow.

**Verified:** booted maximized, stamped APPROVE on visitor 1 (Wren) → advanced to visitor 2
(Odd-Eye), score 0→1, verdict log `wren-sixpence -> approve : right`; headless smoke still
16/16. Screenshotted the running desk.

**Notes (would surprise the next person):** `class_name` globals (`Palette`,
`ThemeFactory`) live in `.godot/global_script_class_cache.cfg`, which ONLY the editor
regenerates — running via the MCP right after adding a new `class_name` script died with
`Identifier "Palette" not declared`. Fix: `godot --headless --path . --import` once (or open
the editor) to rebuild the cache; it's gitignored so a fresh clone needs it too. Both stamp
models ship behind `Session.STRICT_BINARY` (default false = four verdicts) per Panda's
"build both, decide by playtest" call on AGT.5.

## 2026-07-12 — The Morning Queue: first Godot/code tier + 16-visitor data

**Context:** Panda greenlit building the Morning Queue (the ghost-card candidate) — one
desk shift as a playable prototype — and asked for the 16 visitors coded to JSON, a Godot
project stood up, and a wireframe good enough to allocate sub-agents against.

**Options considered (project home + engine target):**
- (A) Pure GDScript + `gl_compatibility` renderer, under `adventuring-guild-teller/morning-queue/`.
- (B) C#/.NET (the installed editor is a `.mono` build).
- (C) A top-level `src/`-style tier, standing up `CodeDocs/` + `CODE-DESIGN.md` per Rule 2.

**Choice:** (A). Project lives inside the musing folder as a self-contained Godot 4.6
project; `build-musing.py` globs only top-level `*.html`, so the subfolder never reaches
Pages. Documented it with a project `README.md` (code-doc, following the `approaches-app`
precedent) + `MORNING-QUEUE.md` (agent-nav spec: data schema, frozen interfaces,
sub-agent allocation) rather than standing up the repo-wide `CodeDocs/` tier for one
prototype.

**Why:** the `.mono` editor **cannot Web-export** (.NET has no Web target); GDScript can.
Keeping it GDScript-only on the Compatibility renderer preserves the option to embed a Web
build in the **local** site later (not Pages — Godot 4 Web needs COOP/COEP headers Pages
can't serve; this matches Panda's "local not github pages" framing). Following the
`approaches-app` README-as-code-doc precedent keeps the zero-dependency-elsewhere posture
without a premature tier.

**Data shape:** two files — `visitors.json` (the queue) and `references.json` (the
rulebook every `check` resolves against), so the desk is actually *verifiable*, not
narrated. 16 visitors span all seven task types (incl. two new ones Panda added:
welcome/farewell roster changes, and multi-gate dungeon-drop commissions with a payout
calc) and eight failure axes. Two half-fails (`hold`/`conditional`) deliberately pressure
the "is the desk strictly binary?" question (AGT.5) via a `Session.STRICT_BINARY` dial.

**Corrections folded from chat:** all enemy targets are wild-magic apparitions / mana
beasts (no rats/vermin — nothing that depopulates); Sister Coll's fieldability check is
"no cleric/water-warded party *registered active*," independent of when anyone returns.

**Verified:** headless smoke drive scored 16/16 on correct stamps and 0 on a wrong stamp,
no load errors — the autoloads, flow state machine, and scoring work end-to-end. The four
component scenes are functional stubs with live plumbing, awaiting build-out per the
allocation table in `MORNING-QUEUE.md`.

**Notes:** renamed a `Session.log` member (shadowed GDScript's built-in `log()`). If the
site ever embeds a Web export, that artifact becomes a public surface — gate it under
Rule 6 then.

---

## 2026-07-12 — AGT correction round 1: nine claims settled

**Context:** Panda ruled on the pitch read-back by handle (AGT.2/3/4/6/8/9/10/11/12);
AGT.1/.5/.7 remain open.
**Choice:** folded rulings in place per the append-only protocol — a `settled` chip plus
a green "Settled —" line per claim, superseded text struck (never renumbered); the
research page got matching "Settled" notes on AGR.1/2/3/6, and the Bad Viking entry
became the Horticulture + Antiquities pair (2022 / Sept 2025, web-verified). Rulings
archived in `plans/PLAN-adventuring-guild-teller.md`.
**Why this shape:** provenance stays auditable — the original given/read/gap tag and
text remain visible under each ruling, so the page records the *dialogue*, not just the
outcome.
**Notes:** the rulings that most change the design: (1) the floor **never ticks** — no
time limits or affection decay; pillar II's rule gained "And no clock, ever." (2) no
death — gearless respawn at the dungeon entrance, and gear left behind seeds retrieval
quests, turning failure into desk content (AGR.1) and settling tone (AGR.6). (3)
summaries may be actionable, but every stat lives in in-game bios — anti-homework moves
from summary-tuning to bio UX (AGR.3). (4) suggestion acceptance = teller-trust ×
target-liking, giving refusal legibility a mechanism (AGR.2). One judgment call: the
full-town ruling arrived addressed to AGT.2 but answers AGT.9's questions — filed under
AGT.9, flagged in chat.

## 2026-07-12 — New musing: Adventuring Guild Teller (pitch stage, read-back format)

**Context:** Panda's brief — a guild-teller game: papers-please desk / stardew-social
floor / tomodachi fishbowl with a popup-dungeon creator layer; asked for a landing page,
a dressed-up pitch page containing *my breakdown of the brief so misconceptions can be
corrected*, and a functional desk-research page.
**Options considered:** (a) Markdown musing; (b) HTML-first set (thaumodynamics/LoMa/MDC
pattern); (c) one long pitch page, no hub.
**Choice:** (b) — `adventuring-guild-teller/`: hub + `pitch.html` + `research.html`,
verbatim-copy build. The pitch is a **read-back**: twelve claims `AGT.1`–`AGT.12`, each
tagged by provenance (given = restates brief · read = my inference · gap = brief silent)
with a per-claim "correct me" line — MDC's claims pattern, pointed at correction instead
of assertion. The research page gives every precedent a take/skip verdict and ends in
risk register `AGR.1`–`AGR.6`, each risk citing the claim it pressures.
**Why:** the ask was literally "so I can correct any misconceptions" — provenance tags +
stable append-only handles make corrections cheap and precise. Two mnemonics (AGT/AGR)
because Rule 8 scopes handles per page.
**Notes:** (1) The design spine I read into the brief — discretion *evicted* from the
desk, the inverse of Papers, Please's dilemma injection — is claim AGT.5, flagged as
inference, not fact. (2) Research facts were web-spot-checked; caught that Potionomics'
Quinn is the ingredient *vendor* (hero-adventuring is a separate befriend-and-send
system) before publishing the wrong version. (3) Stakes policy (adventurer death/injury)
deliberately left unset as Panda's call — AGT.12 flags it, AGR.6 records why it decides
the game's tone.

## 2026-07-12 — MDC follow-up: MIDI-learn remapper (Map pads)

**Context:** Panda's kit's MIDI spec doesn't put the hi-hat on the GM notes the coach
expects (42/44/46) — the plan's "MIDI-learn pad mapping" candidate got pulled forward the
same day.
**Options considered:** (a) a static note-number table you type into; (b) MIDI-learn
(click the lane, hit the drum); (c) per-kit presets.
**Choice:** (b). *Map pads* mode in `coach.html`: arming a lane binds the next incoming
note-on to it (`USERMAP` overrides GM; `laneFor()` resolves user-first), bindings render
on each pad, double-click clears a lane, one button resets all; persisted as
`localStorage["mdc:map"]`. The unmapped monitor line now says "use Map pads."
**Why:** learn-by-hitting needs zero knowledge of the kit's manual and works for any
number of zones; presets are a maintenance treadmill; typing note numbers is (a) with
extra steps. Binding *consumes* the hit (no sound-through into judging) and plays the
lane's voice once as confirmation — you hear what you just taught it.
**Notes:** keyboard/click pads never bind (only MIDI notes reach the learn branch), so
the feature is inert without hardware; verified by calling the same `handleNote()` the
MIDI callback uses. A GM note re-bound to a different lane stops counting for its GM
lane (`n in USERMAP` guard). Export/import of the map stays on the plan.

**Context:** Panda's brief — "a webpage musing that connects to a MIDI device and provides
suggested rhythms; let's get there for now." First musing whose deliverable is a *tool*
(Web MIDI in, WebAudio out), not prose or a static explorable.
**Options considered:** (a) Markdown musing + separate app page; (b) HTML-first hub +
self-contained app page (the thaumodynamics/logical-magic pattern); (c) app-only, prose in
a sidebar.
**Choice:** (b). `midi-drum/` — hub `index.html` carries the musing (claims `MDC.1`–`MDC.5`:
instrument-as-controller, *legible director*, notation-as-UI, judge-the-gap, practice-as-core-loop);
`coach.html` is the tool: GM-mapped pad monitor, 13 grooves as step grids (16ths + one
12-cell triplet shuffle, levels 1–5 in families), synthesized kit + click on a lookahead
scheduler, practice judging at ±30/±70/±120 ms with signed rush/drag bias, and a
director whose five rules (R1–R5) print the reason each suggestion fired.
**Why:** the game-design content *is* the director — DDA you can audit and override — so
the prose page frames exactly that and the tool demonstrates it. HTML-first because the
page must open from disk and stay dependency-free like its siblings.
**Notes:** (1) The whole input path is device-optional: on-screen pads + keyboard feed the
same `hit()` pipeline, so the page demos with zero hardware — also how it was verified
(machine-timed hits injected through the real pipeline: 100%/±0 ms run → R2 escalation;
50 ms-early run → all "good", −50 ms bias, R3 "you're rushing" + slower-BPM card).
(2) The embedded preview browser auto-denies `requestMIDIAccess`, so the granted path
needs a real browser + kit — untested until Panda plugs in; denied/unsupported paths are
exercised and graceful. (3) A manual input-latency slider (localStorage `mdc:calib`)
stands in for auto-calibration, deferred to the plan. (4) Judging windows are fixed-ms,
not tempo-scaled — rhythm-game convention; revisit if slow practice feels punishing.

**Context:** The reusable vignette handoff (`VIGNETTE-HANDOFF.md`) had done its job in a
*separate* chat — three finished everyday-life vignettes (`LVIG.1`–`LVIG.3`: Transcription
Nights, The Letter Kept, Crack and Splint) came back as Markdown. This session did the
deferred "separate step" the handoff always named: integrate them into the published set.
**Choice / what was built:** a new hand-authored `logical-magic/vignettes.html`
("Everyday Records") — self-contained, both themes, LoMa tokens, the site-wide breadcrumb,
the three vignettes at stable anchors `#lvig-1`–`#lvig-3`, each with its `LVIG.n` handle,
italic abstract, and body. Prose is **verbatim** from the handoff chat (typographic
conversion only — curly quotes, em-dashes, `<em>`; no rewriting, per the brief). Wired per
the HTML-first new-page checklist: a fourth live gallery card, a "Vignettes" registry
sublink, the nav-spec file-map row, the plan tick — plus the **`LVIG`** mnemonic declared
(Rule 8, append-only; next is `LVIG.4`).
**Why this shape:** it mirrors Space Feudal's `loom.html` *role* (several self-contained
vignettes on one companion page, one citable handle apiece) without copying its skin — LoMa
keeps its own palette, leaning on the gilt "grace/settlement" accent the everyday pieces
turn on (a mend is 12 flips; a vale lays down 10,000/day). Framed the card as a
**companion**, not a numbered chapter, so the "IV · candidate — the Grimoire" ghost stays
untouched.
**Notes:** numbers were checked against pitch §8 (Mending 22 strokes / 12 flips, 4
strokes/s, 10,000 flips/day, ~10⁶-fact true names) — the vignettes only *spend* constants,
never mint them. Doc resync ran slightly past the brief's file list: refreshed the stale
root `PLAN.md` "Next: worksheet + application pages" line (those shipped earlier today) and
added the `README.md` page-table row, so the index and human doc match the built set.

## 2026-07-12 — LoMa completes the THAU trio: worksheet + trial-duel + a vignette handoff

**Context:** Panda asked for three things in sequence: a reusable handoff doc so a
*separate* chat can write everyday-life LoMa vignettes (that chat proposes the
abstracts/prompts and the prose; this repo supplies the system and canon), then the two
pages that make LoMa match the thaumodynamics set — the problem set and the duel.
**Recon surprise:** this worktree was sitting on a stale local `main` (4e2fe00) with three
*uncommitted* DEV-LOG entries from some other session's Space Feudal **vignette** work
(VIG.6–8, "companion page V", a LOOM.6 split ruling + "two-system atlas") — whose files
exist neither here nor on `origin/main`. Preserved them in `git stash`
("stranded DEV-LOG entries: Space Feudal vignette session"), then fast-forwarded to
`origin/main` (3469a42, PR #2 merged + breadcrumbs + themed landing rows + Space Feudal).
🐼Panda: that stash is yours to reconcile — the vignette session's files live elsewhere.
**Choice / what was built:** (1) `logical-magic/VIGNETTE-HANDOFF.md` — self-contained
briefing (system digest as lived experience, glossary, the §8 constants verbatim, canon
cast incl. the new pages', texture inventory, guardrails, `LVIG.n` handle convention —
distinct from SF's `VIG.n`). (2) `loma101-worksheet1.html` — LOMA 101 Problem Set One,
mirroring MDYN 101's blank/student/key toggle; student is M. Sedge; all numbers cite
pitch §8 (the Ascent's forgotten survey strokes and the 226 s/226 min unit slip are the
teaching errors). (3) `assize-of-bells.html` — Crown v. Fen as a 10-slide trial-duel
mirroring the Ashfield Bout's deck; new canon minted deliberately: the **Rule of Sound
Warrant** (courts run relevance logic — ⊥-tainted derivations establish nothing at the
bar, even though stones explode), **"no writ, no wall"**, conviction by **ash-gap**
(the 8-stroke hole), total ledger 38 strokes / 0 flips. Both new pages carry the
breadcrumb standard and the LoMa tokens; gallery cards went live; registry sublinks,
README, nav spec, and plan synced.
**Why this shape:** the worksheet and trial reuse only mechanics the pitch already
claims (LOMA.1/.4/.5/.6) — the point of the set is that the courtroom and the classroom
fall out of the same ten rules, the way MDYN's worksheet and bout fall out of its field
equations.
**Notes:** the Assize deliberately resolves the pitch's Plea 03 aftermath and the
worksheet's B3 cites the repeal — the three pages now form one continuity. The vignette
chat should return `LVIG.n` Markdown; integration as a companion page is a later,
separate step.

## 2026-07-11 — Landing page: one themed row per musing, with hand-drawn SVG emblems

**Context:** Panda asked that the landing directory give each musing its own full-width
line, themed after its content, with an "img" on the left that helps the feel — reference:
the LoMa proof-circle seal.
**Options considered:** (A) hand-edit the generated index — dead on arrival, it's
generated; (B) hard-code four bespoke rows in `build_site.py` — themes don't belong in the
generator; (C) extend the registry: per-musing `emblem` (an SVG in the musing folder,
inlined into its row) + `theme` (`font` + `light`/`dark` token maps emitted as `--m-<key>`
CSS vars on `.row-<slug>`).
**Choice:** C. Four emblems authored, each in its musing's own visual language, colored
exclusively via `var(--m-*, fallback)` so one SVG follows both color schemes for free:
the **LoMa proof-seal** (glyph ring on a textPath around the settle rule — closest to
Panda's reference), the **THAU mirror-fields** (ember and storm circles coupled across the
dashed mirror), the **MSL lane web** (one accent route, a ship diamond mid-run, the front
collapsing in dashed from the right — with a bespoke `front` theme token), and the
**Space Feudal system roundel** (font star + bloom + orbits, gilt keep, two lane mouths
with drift rings). Row layout (flex, emblem column, mobile stack) lives once in
`site/style.css` (`.musing-list`/`.musing-row`, replacing `.project-grid`/`.project-card`);
generated CSS carries only colors. Serif rows for THAU/LoMa/SF, sans for MSL — matching
their pages. Palettes lifted verbatim from each musing's own `:root` tokens.
**Why:** the registry stays the single source of truth (Rule: anything on the landing
comes from config), themes stay with their musings (emblem lives in the musing folder,
palette in its config entry), and the open token-map schema means a future musing can
bring whatever colors its emblem needs without touching the generator again.
**Notes:** (1) The emblem is inlined into `index.html` only — it is not copied into
`site/musings/`, so HTML-first copy scripts and the MSL assets rule needed no changes.
(2) Fixed a stale card blurb while in the config: Space Feudal's description said "25-row
ledger" — it has been 27 rows since the Loom appended SF.26–27. (3) Verified light + dark
+ 375 px; the same SVGs recolor across schemes with no per-mode variants. Schema
documented in `musing-tech-notes.md` ("Landing rows: themes + emblems").

## 2026-07-11 — Site-wide breadcrumb navigation (coherent, portfolio-rooted) via a Sonnet fan-out

**Context:** Nav across the musings was a grab-bag — thaumodynamics and logical-magic had
*no* back-link at all, the 16 explorations used a `.72rem` mono backlink, space-feudal had a
small per-page `.crumb` bar, the MSL Markdown pages a minimal `← All musings`, and the React
approaches app a single `← back`. Panda asked (before the PR) for a coherent UX pass with
bigger, consistent navigation, rooted at the **portfolio** — and to run it as a fan-out of
**Sonnet** sub-agents with *firm* rules (Sonnets have ignored Rule 1 / PII guidance before),
supplying the portfolio repo for reference of the breadcrumb root.
**Recon:** the portfolio (`spiffy-panda_github-portfolio`, a Quartz site) deploys at the org
root `https://spiffy-panda.github.io/`; Game Design Musings is a *separate* Pages deploy at
`…github.io/game-design-musings-blog/`. So the coherent trail is **Panda's Portfolio ›
Game Design Musings › ‹Musing› › ‹sub-page›**: the portfolio crumb an absolute cross-site
URL (works everywhere), the landing crumb site-relative, the rest relative, current page a
non-link.
**Method:** wrote one prescriptive standard (`.crumbs` structure, sizing/a11y, exact
per-page hrefs, firm Rule 1 verbatim + PII gate + lane discipline) and had every agent read
the *same* file so parallel work stayed coherent. Split by write-disjoint folders: **4
Sonnet agents** (thaumodynamics / logical-magic / space-feudal / explorations, each editing
only its self-contained HTML) + **central (me)** for the shared surfaces that can't be
parallelized — `musing_render.py` (new `crumbs=` param), MSL `build-musing.py`, `site/style.css`,
the landing generator in `build_site.py`, and the React `Page`/`TopBar` (`kit.tsx` + 4 pages).
**Choice / invariant change:** the old "HTML-first pages carry **no** back-link to the
landing (file://-openability)" rule is **relaxed**: pages still render standalone from disk,
but the "Game Design Musings" crumb is site-relative (correct on the served site + local
preview; the one link that doesn't resolve on a raw `file://` open). Coherent wayfinding
rooted at the portfolio won the trade; the served site is the canonical target. Documented
in `musing-tech-notes.md` ("Navigation: the breadcrumb standard").
**Why:** a single shared spec + write-disjoint lanes is what made a Sonnet fan-out produce a
*coherent* result rather than five different nav designs; the shared-chrome layer (Python
renderer, React component, CSS) stays central so one edit fans out to many pages.
**Notes:** (1) All 13 page families verified post-build (fetch audit: portfolio-abs root,
depth-correct landing href, single `aria-current` crumb; React pages checked live since they
hydrate client-side) + screenshots in both themes and a 375px wrap check. (2) Agents each ran
a PII sweep and a global sweep confirmed no dead/real name in source or output. (3) Agent
finds logged for triage: thaumodynamics `ashfield-bout` slide-dots are 9px (kept — enlarging
edges into a layout change); explorations interactive controls not exhaustively audited.
(4) One agent caught a `.crumbs` class collision in `utility-ai-fit` and renamed the clashing
footer class — coherence dividend of the shared class name.

## 2026-07-10 — Space Feudal: The Loom (consequence threads + counterweights) + SF.26–27

**Context:** Panda pulled a thread the brief never examined — decoherence polices only
*transit* and the bloom only *the font*, so in-system automation is free: a settled
system's drone home-fleet dwarfs any chrism-fed mobile force. Commission: follow such
threads (and find more), chase consequences, offer counter-forces — keep both the
emergent texture and the tuned feudal feel.
**Options considered:** (A) fold consequences into existing pages — rejected, each page
has one job and the method deserves its own statement; (B) a fourth page with an explicit
thread → runaway → counterweight → dial format.
**Choice:** B — `loom.html` ("The Loom", handles `LOOM.1`–`LOOM.6`), opening with three
binding rules: counterweights must be *found in canon*, never decreed; every thread ends
on a feel dial; an uncheckable thread becomes new canon or a dissolution-tier tech. The
six threads: home shell (Panda's — checked by the pyramid leak pricing drone *quality*,
remass making it a point-defense shell, the bloom paradox [the only prize is anti-drone
ground], and the shell pointing inward at successions); muster of ghosts (unauditable
arsenals → Potemkin shells, checked by live-fire reviews/the Progress/defector ledgers);
fact corsairs (keys and prices as physical cargo, checked by courier sanctity + staleness
+ split keys + poisoned pouches); cold coast (ballistic stealth favors crewless pods,
checked by time cost + the floodlit Deep + sweep-certification as a banal fee); endemic
minds (AI crosses only as cold weights → heirloom lineages, wright guilds, the poisoned
codex); stratified manor (automation leaves three estates: bloom gangs / drone-wright
yeomanry / lodesman gentry — "bread cheap, chips dear"). Two counterweights were strong
enough to *append to the ledger*: **SF.26** license to crenellate ↔ shell charters
(adulterine shells razed), **SF.27** tournament ↔ the live-fire muster of ghosts —
first exercise of the append-only handle law (rows sit in the war group; IDs ≠
positions; ranges updated across pages).
**Why:** the home-shell thread *strengthened* the siege doctrine rather than breaking it
(it explains why SIEGE.4 storms are rare and why leaguers stand off in the Shallows) —
evidence the kit's counterweights are load-bearing rather than decorative, which is the
whole bet of the musing.
**Notes:** (1) New canon (nav spec invariants): drone home-shells exist and are chartered
("license to swarm"); AI ships as cold weights needing witnessed local revival; cold-coast
stealth exists and favors drones. (2) The dials interact — same counterweights serve
multiple threads (pyramid leak runs both sieges and shell quality) — noted in §7 as the
bench test for future threads.

## 2026-07-10 — Space Feudal: siege doctrine corrected + The Long Patience page + Harrow map

**Context:** Panda sustained an objection against the brief's one-line siege ("cut the
font → bunkers fade → surrender date"): chrism is *jump* fuel, not life support — a
near-future keep with closed loops grows its own food and lives for generations, so
fuel interdiction can't starve it. Commissioned: a siege-details page + a system map
with marked distances ("scale fudged — to scale nothing is readable").
**Options considered:** (A) give chrism in-system tactical uses so the old claim holds —
rejected, retcons the fuel into magic juice and muddies the §2 kit rule; (B) quietly
soften the wording — rejected, the claim was load-bearing and wrong; (C) concede the
objection in full and rebuild the doctrine on what the fade *does* clock.
**Choice:** C — `sieges.html` ("The Long Patience"): the fade kills a keep's **reach**
(three half-lives → no jumps) and the **besieger's reserve** (re-buy ≈ 1−2^(−t/90) ≈ 60%
per 120-day season), never a population. The keep's real clock is the one loop that
can't close at 30k souls — the industrial pyramid (fab-grade spares, pharma) — measured
in *years*; so a siege is two public clocks, most sieges end in terms (the customary
law of the siege: terms decay with resistance time) or a lifted leaguer, and six endings
got handles `SIEGE.1`–`SIEGE.6` with base rates. The Harrow map (inline themed SVG,
schematic on purpose, distances + brachistochrone times true at corsair 0.3 g / war
0.05 g / freight 0.01 g) grounds the doctrine: mouths anchor in flat space ~3 AU out
(wander ±0.2 AU seasonally), so nobody covers both gates — geometry forces
subinfeudation, and Millstone (volatiles at the Shallows' edge) is the natural leaguer
campsite, the counter-castle re-derived. Amended `SF.8` + the brief's §5 bullet;
constants contract extended in the siege page's foot; correction canonized in the nav
spec invariants ("don't reintroduce 'the keep starves'").
**Why:** the honest fix made the analogy *stronger* — the medieval record agrees (few
storms, fewer starvations; most sieges broke on the attacker's clock: the forty days,
winter, the pay chest) — and "the besieger is the one melting" lands the setting's core
thesis (chrism is income, never wealth) in siege form.
**Notes:** (1) New canon knock-on: "mouths are munitions" — population is fortification,
because industrial diversity scales the decay clock; big keeps outlast small ones, the
inverse of a granary siege. (2) The map quietly reconciles SF.7's tempo claim: corsairs
sprint mouth→font in ~10 d and raids resolve in hours; interstellar relief still needs a
season — the tempo gap survives the geometry. (3) In-system war is "chess by mail"
(burns are visible weeks out; surprise exists only at emergence) — this is why sieges
are stately and ambush lives at the mouths.

## 2026-07-10 — Space Feudal authored + registered (HTML-first: brief + correspondence ledger)

**Context:** Panda commissioned a new musing — X-series/Elite-style play, but the point of
interest is the economic layer above it: mimic the actual economic problems feudalism
solved and show how they lead to feudal lords again; FTL stipulated, resources free to
invent, plus "a page explaining how the old and to-be line up."
**Options considered:** (A) Markdown musing — dead on arrival for the centerpiece, the
renderer has no tables and the alignment page is inherently tabular; (B) extend
`musing_render.py` with tables — touches shared tooling for one musing's layout needs;
(C) HTML-first musing (the day-old THAU/LoMa pattern) — full layout control, two
self-contained pages.
**Choice:** C — `space-feudal/` (lowercase == slug per the HTML-first invariant):
`index.html` = the brief (six pillar-problems each emitting a spec line; an invented kit
where *every invention must earn ≥1 pillar and repair none*; §3 constants; the
company→governor→fief ladder; knobs + four "dissolution" techs), `ledger.html` = the
alignment page (25 old ‖ new ‖ in-play rows, grouped, mnemonic **SF**, append-only
handles `SF.1`–`SF.25`). Registered in `MUSING-CONFIG.json` with "The Ledger" sublink;
PLAN/SITE.md synced; plan file added.
**Why:** the ledger *is* the deliverable the brief argues for — a two-column
old-vs-to-be page wants designed HTML, and the verbatim-copy pattern was built for
exactly this shape. The load-bearing invention is **chrism decay** (t½ = 90 d): one
constant re-derives itinerant kingship (tithes can't be hauled → the Progress), plenary
governors (120-day order loops), and castle logic (attackers arrive at 63% bunkers) —
that triple coincidence is why it, and not the lane graph, anchors the setting.
**Notes:** (1) No ansible is the absolute knob — lanes carry hulls, not signals; every
other constraint is tunable, that one unravels the whole equilibrium if softened, so the
"wire returns" only as an endgame dissolution tech. (2) The MSL mention in the brief's
foot is deliberately *not* a hyperlink: MSL's repo folder has no `index.html`, so a
slug-relative link would 404 from disk, violating the file://-openable invariant.
(3) SF.19 (bound labor) carries an explicit darkness-knob note rather than sanitizing
the serfdom analogue — flagged as a designer's visible choice, with abolition as
late-game politics. (4) Constants contract lives in the brief §3; future SF pages cite,
never fork (LoMa §8 discipline).

## 2026-07-10 — Landing page links every project: THAU + LoMa promoted as HTML-first musings

**Context:** Panda asked for the Game Design Musings landing page to link the other
projects (the `thaumodynamics/` and `logical-magic/` sets, until now staging-only and
invisible to the site build).
**Options considered:** (A) hand-edit `site/index.html` — dead on arrival, it's generated;
(B) teach the landing generator "link-only cards" pointing at unbuilt folders — broken on
Pages, where unregistered folders don't deploy; (C) promote both sets to registered
musings with verbatim-copy `build-musing.py` scripts, the path both READMEs anticipated.
**Choice:** C — a new sanctioned musing variant, the **HTML-first musing**: no `MUSING.md`;
the hand-authored gallery `index.html` is the published entry; `build-musing.py` copies
every top-level `*.html` verbatim (the MSL-explorations treatment); the Rule-2 pair
becomes `index.html` + `<FOLDER-NAME>.md` (nav specs added for both). Registered both in
`MUSING-CONFIG.json` with card sublinks (THAU: Monograph/Worksheet/Bout; LoMa: The Pitch).
Pattern documented in `musing-tech-notes.md`; SITE.md inventory + plan files synced.
**Why:** the registry is the only honest way onto the landing page — anything else forks
the generator or ships dead links; and the copy-script promotion was designed for exactly
this moment.
**Notes:** (1) cross-musing links between the sets use `../<slug>/<page>.html`, which
resolves identically in-repo and under `site/musings/` — that only works because these
folders are lowercase == slug; keep that invariant. (2) HTML-first pages carry no site
chrome, hence no back-link to the landing page — deliberate, they must stay
`file://`-openable (precedent: copied explorations pages). (3) Registration flips these
folders from "committed" to "published on next push" — Rule 6 was re-checked on all six
HTML pages this session. Verified locally: full build clean (3 musings), all seven routes
200, LoMa→THAU cross-link lands on the copied gallery.

## 2026-07-10 — LoMa (Logical Magic) staged: the "casting is proving" pitch page

**Context:** Panda pitched a new system — **[Lo]gical [Ma]gic (LoMa)**: magic built on
first/second-order logic, advanced tiers dipping into monads and abstract CS/math. Classic
magical effects arrive *by fiat* (explicitly unlike MDYN's field equations), but it must keep
MDYN's detailed-grounded-calculation discipline. First deliverable: a graphical pitch page.
**Options considered:** (A) register a full musing now; (B) a top-level staging folder like
`thaumodynamics/`; (C) park it inside `explorations/` (rejected — that gallery is MSL-only).
**Choice:** B — `logical-magic/` with `pitch.html` (the deliverable), a THAU-style gallery
`index.html` with ghost cards for planned pages, and `README.md` declaring mnemonic **LOMA**;
plus `plans/PLAN-logical-magic.md` + a `PLAN.md` line. Unregistered; nothing deploys.
**Why:** mirrors the thaumodynamics precedent exactly (HTML-first set; promotion to a musing
is a later, deliberate hub-page step), and the two systems now read as deliberate siblings —
field equations vs. metamathematics, same grounded-calculation bet.
**Notes:** core design decisions worth not re-deriving: (1) two-currency cost model —
**strokes** (proof labor, caster-side) vs **flips** of **grace** (facts changed at settlement,
world-side); (2) **the Miser's Law** — settlement is minimal-model revision, which makes the
monkey's paw a *theorem* (Plea 02 audits it in a table); (3) spell circles = quantifier
alternation depth (the arithmetical hierarchy), with induction-vs-instantiation as the whole
economics of ∀ (Plea 01's 13-strokes-vs-904,779 punchline); (4) duels = game semantics (foes
buy the falsifier's seat on your ∀); (5) rituals = monads, skinned as **vessels/pouring**,
with the monad laws as "the Three Duties" and Writer-residue as forensics; (6) the six limit
theorems (Gödel/Tarski/Löb/Rice/compactness/Löwenheim–Skolem) as unpatchable physics.
`pitch.html` §8 is the tuning table — future LoMa pages cite or amend it, never fork numbers.
Gotcha for the next session: staging folders are invisible on the normal preview server
(`serve_site.py` serves `site/` only) — preview via a repo-root static server or open the
file directly. Also: SVG `font-size` attributes lose to the CSS `font` shorthand in utility
classes — size SVG text with an inline `style` when it matters.

## 2026-07-10 — Thaumodynamics set imported as a top-level staging folder

**Context:** Panda built a three-page fictional-physics set in a Claude Code session scoped to
the Builder-Research workspace — a field-theory magic monograph, a worksheet with a
blank/student/answer-key toggle, and a duel-chronicle slide deck — then realized game-design
material belongs here. Asked to move it into a subfolder as real files.
**Options considered:** (A) `explorations/<slug>/` — matches the self-contained-HTML staging
convention, but that gallery is explicitly MSL-only; (B) a new top-level `thaumodynamics/`
staging folder, musing-shaped but unregistered; (C) register it as a full musing now
(`MUSING.md` + `build-musing.py` + `MUSING-CONFIG.json` row).
**Choice:** B. Three standalone `file://`-openable pages + a small gallery `index.html` +
`README.md`; companion cross-links rewritten from artifact URLs to relative hrefs; **not**
registered, so nothing deploys.
**Why:** keeps `explorations/` single-universe; mirrors its staging precedent; promotion to a
published musing is a later, deliberate step — and this set is HTML-first, so it would publish
via a hub page rather than the Markdown pipeline.
**Notes:** authored under the Rule 6/7 gate (all names fictional). Each page also exists as a
private claude.ai artifact (URLs in the folder README) — repo copies are canonical. The three
pages share one token system and one set of in-world constants; the worksheet's numbers match
the monograph's plates deliberately.

## 2026-06-27 — Exploration explainer videos: a zero-pip slide+narration pipeline

**Context:** Panda asked for a narrated 60–90s explainer video (visuals + audio) for each
of the 16 MSL explorations. No video skill exists locally (confirmed via web search; third-party
Claude Code video toolkits exist but need paid APIs / installs). Decided to build it in-repo.

**Options considered:**
- *Visuals:* (A) screenshot the live pages and pan, (B) author purpose-built slides, (C) hybrid.
  Chose **C (hybrid)** per Panda.
- *TTS:* MCP voice gateway vs. Windows SAPI directly. The gateway turned out to be **the same
  three SAPI desktop voices** (David/Zira/Haruka), so calling SAPI directly via a `.ps1` keeps the
  util self-contained (no MCP dependency).
- *HTML→PNG:* headless Edge/Chrome (present on the box) vs. a pip rasterizer. Chose **headless Edge**
  (`--headless=new --screenshot`) — zero pip.

**Choice:** `utils/python/build_exploration_video.py` + `utils/powershell/tts_sapi.ps1`, driven by a
per-exploration scene script at `explorations/_video/scenes/<slug>.json`. Output → `explorations/_video/out/<slug>.mp4`.
Building blocks: SAPI (audio) + headless Edge (stills) + `ffmpeg` (Ken-Burns clips + concat). All zero-pip.

**Why / the surprise that shaped it:** the live JS **instruments do not paint in one-shot headless
capture** — `--screenshot` runs zero animation frames, so the canvases/SVGs come out blank (and
`--virtual-time-budget` freezes rAF, making it worse). *Static* page content (hero/prose) captures
perfectly. So the pipeline screenshots only the static page parts and **re-draws the key diagrams
(circulation loop, phase plot) as static SVG inside the slides** — which also reads better at video
scale. Capturing real running-simulation footage would need an interactive browser driver (e.g. the
Playwright MCP) and can't be fully batch-automated without extra setup; left as an optional upgrade.

**Notes:**
- Headless gotcha: capture reliably with `Start-Process -Wait` + a **fresh `--user-data-dir`** per
  shot + `--run-all-compositor-stages-before-draw`; too-short a wait yields no file.
- Duration is governed by narration length × SAPI `rate`; the pilot landed 129s→91s by trimming
  copy, `rate:1`, and a 0.4s per-scene tail. Tune `rate`/copy per page to stay in band.
- **Pilot:** `liquidity-deflation-spiral` (91.1s, 1080p, 22 MB). Other 15 pending Panda's review.
- **Git/disk:** 16 × ~20 MB ≈ 320 MB of binaries. Decided: **gitignore** `explorations/_video/{out,build}/`
  and track only the pipeline + per-page scene scripts (anyone can re-render).

**Update (same day) — Piper backend, speed knob, two more pilots:** Panda found SAPI's pacing slow
(consumes content at ~2×) and wanted denser narration + a "how to use the tool" beat, plus a
**SAPI-vs-Piper** voice comparison across the next two ideas. Added: a `piper` TTS backend (free local
neural voice `en_US-amy-medium`, installed to `%LOCALAPPDATA%\piper`, resolved with no hardcoded path),
a uniform `speed` knob (`ffmpeg atempo`, decoupled from the TTS engine so it's comparable across both),
and `--tts`/`--speed` CLI overrides. Two new bespoke pilots: **solvency-cell** (SAPI Zira, 1.6×, 89 s)
and **enemy-attack-schedule** (Piper amy, 1.8×, 87 s), each with re-drawn SVG diagrams (payer-gap clamp,
hub threshold; opening-book timeline, fog map) and a controls "how to poke it" slide. Piper's base pace
is slower than SAPI, so it needs a higher `speed` to hit the same band. Three pilots now await Panda's
voice pick before the bespoke batch of the remaining 13.

**Update — full batch done (16/16):** Panda picked Piper, ~1.6–1.8×, and a louder binaural bed
(gain 0.10→0.16). Authored the 13 remaining bespoke scene scripts via a parallel subagent fan-out
(one agent per page, each reading its page and writing `explorations/_video/scenes/<slug>.json` to the
schema; 6 produced clean inline-SVG diagrams — all spot-checked in-bounds). Added inline-SVG support
(`scene.svg`) so per-page diagrams live in the data, not the engine. All 16 render to **62–88 s**
(avg 76 s, 339 MB total, gitignored). **`speed` had to be tuned per page** because Piper's pace and each
script's word count vary: most sit at 1.7–1.9×, but the wordiest (utility-ai-fit, market-clearing-cell)
needed 2.2× to fit the 60–90 s band — pick `speed` from the first render's duration rather than guessing.
One render run was interrupted at a session boundary and left a truncated `solvency-cell.mp4` (moov atom
missing); re-rendering fixed it — the batch is restartable since each slug is independent.

## 2026-06-25 — MSL explorables: run complete + published into the site

**Context:** Morning wrap of the overnight run (entry below). It produced **16** interactive
explorables (the planned set); the final three landed but the session limit truncated the last
agent's summary, and `dead-reckoning-deck` never launched (classifier briefly down — left out, not
referenced anywhere). Panda then asked to (1) wrap the explorables in an **overview page** and
(2) link both the approaches and explorations hubs from the **landing-page MSL card**.

**What shipped:**
- **All 16 committed.** `explorations/index.html` rewritten into a real overview: top-nav back to the
  musing + a link to the approaches hub, intro framing, a lineage legend, 16 cards in three tiers.
- **Published into the site.** MSL's `build-musing.py` now **copies** the repo-root `explorations/`
  (overview + every folder with an `index.html`; internal `README`/`RUN-LOG`/`_research` skipped) into
  `site/musings/<slug>/explorations/`. Static HTML — copied, not rendered.
- **Landing-card sublinks, config-driven.** `build_site.py`'s card generator renders an optional
  `"links"` array from `MUSING-CONFIG.json`; the MSL entry gained Approaches + Explorations. New
  `.card-links` rule in `site/style.css`.
- **Bug found + fixed in QA.** The un-agent-verified `liquidity-deflation-spiral` crashed on boot —
  `reset()` runs `pause()→render()` before `S = freshState()`, so `render()` dereferenced an undefined
  `S`. One-line boot guard `if (!S) return;` in `render()`; re-verified (interactive console renders,
  zero console errors).

**Verification:** full `build_site.py` (incl. the React app) builds clean; served `site/` and
browser-checked the landing card (both sublinks render), the explorations overview (nav + 16 cards),
and earlier the marquee pages (solvency-cell, jumpgate-topology, enemy-attack-schedule, glass-cockpit).
All 16 identity-grepped clean — they're **public now**, so Rule 6/7 matters: no dead name, real last
name, or local paths; third-party game refs are transformative one-liners.

**Notes:**
- During the run: an agent edited tracked `.claude/launch.json` (reverted); one agent brushed Rule 1
  with a single `node -e` (no artifact). `.playwright-mcp/` added to `.gitignore`; QA screenshots removed.
- **Not pushed** — local commits only; Panda to review and push. On push, CI (Node step) builds the
  React app and `build-musing.py` copies the explorations, so the whole tree deploys automatically.

---

## 2026-06-25 — MSL: overnight "explorables" run (interactive HTML technical explorations)

**Context:** Overnight, unattended. Panda flagged mutation M1 (*The Two Ledgers*) as the favorite
and added two design seeds — (1) a broad enemy front whose attack *order* is predictable so the
player learns the firing conditions (time-since-start, time-since-last-op, prior-op failed/succeeded),
and (2) a jumpgate lane web (X4 / Freelancer / EVE / Stellaris / Mass-Effect-relay lineage). Brief:
spawn Opus agents to explore *technical aspects of the game*, each producing a web page with strong
info-visuals; pace the launches; branch + commit for a morning review.

**Options considered:**
- *Page form:* standalone interactive HTML vs. new React pages in `approaches-app/` vs. Markdown
  approaches. **Standalone interactive HTML** (chosen with Panda) — lowest merge-risk for parallel
  autonomous agents, richest fit for "poke the model," opens offline via `file://` with zero build.
- *Placement:* under the musing / under `approaches-app/` / a new top-level staging dir.
  **`explorations/`** — the site build does not read it, so nothing deploys to Pages until promoted
  (Rule 6 conservative).
- *Orchestration:* one Workflow vs. individual background Agents. **Background Agents** — Panda
  directed agent-spawning, and the cadence / wall-clock cutoff can't be expressed in a Workflow script.
- *Cutoff:* the "10am" cutoff read as **10:00 ET = 07:00 PT** (tied to *peak hours*; peak ~09:00 ET,
  so the Eastern reading serves the stated reason). Run started 03:17 PT.

**Choice:** A curated, tiered backlog of ~12 interactive "explorables," each a self-contained HTML page
in the console style (tokens copied from `approaches-app/src/styles/index.css` so they match M1/M2/M3
without the Vite build). A rolling ~3 Opus builders in the background, replace-on-completion; each page
committed as it lands. Wave 0 = the favorite (`solvency-cell`) + both Panda seeds
(`enemy-attack-schedule`; a `jumpgate-topology` page fed by a Sonnet net-scout) + an honest
`utility-ai-fit` audit. `explorations/index.html` is the morning entry point; `explorations/RUN-LOG.md`
tracks live state; `plans/PLAN-msl-explorations.md` is the plan.

**Why:** Interactive explainers are the highest-value reading of "explore a technical aspect," and
standalone HTML lets many agents work without touching shared build config. Staging in `explorations/`
keeps the public surface clean until Panda picks winners. Rolling-3 keeps the session alive on
background-completion notifications without depending on a timer tool, and naturally paces launches to
~agent-duration.

**Notes:**
- Rule 1 passed **verbatim, with a stern warning**, into every subagent prompt; each agent writes
  exactly one file in its own slug folder (no shared-file contention) and is forbidden servers/installers/builds.
- Identity gate (Rule 6/7) baked into every prompt: no real names, no local filesystem paths in any
  page, third-party game refs brief + transformative. The jumpgate scout held to small transformative
  excerpts (no wiki bulk).
- `UtilityAi` ("PandasAutonome") used **read-only** as reference; its public, AI-authored utility-AI
  architecture (response curves, modifiers, disembodied agents issuing *directives* that reshape
  subordinates' utility landscape) is the spine of the fit-audit page — MSL's contract board reads as
  exactly such a directive layer.
- `explorations/` is **intentionally not** wired into `build_site.py` (a deliberate desync, flagged in
  the plan: it's staging, not deployed). **Not pushed** — local commits only; Panda to review, promote
  favorites, then push.
- Results addendum to follow in the morning once the run completes.

---

## 2026-06-24 — Approaches go React; three HAND-lineage mutations

**Context:** Next pass on *Minimalist Space Logistics*. Two asks: (1) switch the approaches
hub + sub-pages to a rich HTML front end (the MD landing page stays Markdown, for
portability); (2) spawn three *mutations* of the HAND approach, each taking the original
pitch + a shared set of revisions + a divergent seed, each owning a slice of five open
questions.

**Options considered:**
- *Front-end tech:* keep hand-authored static HTML/CSS (zero-dependency) vs. a real
  framework + build step. **User chose the framework.**
- *Mutation placement:* nested under HAND vs. siblings under `/approaches/`. **Siblings.**
- *Existing three approaches:* re-skin into the new design vs. leave as-is. **Leave as-is.**

**Choice:** A Vite + React 19 + Tailwind v4 **multi-page** app in `approaches-app/`
(`base: "./"` so assets resolve under the Pages sub-path). It owns the hub
(`approaches/index.html`) and the three mutation pages (`two-ledgers`, `known-war`,
`glass-cockpit`); the retired Markdown hub (`APPROACHES.md`) was deleted and its synthesis
ported into `Hub.tsx`. `build_site.py` is now the single orchestrator: render the Markdown
pages, then run the Vite build and copy `dist/` over the `approaches/` folder — so
`serve_site.py` and CI both get the full site from one call. A shared component kit
(`src/components/kit.tsx`) keeps the pages consistent; three agents each authored one page
against it. CI gained a Node step; `.gitignore` covers `node_modules/` + `dist/`.

**Why:** The approaches pages wanted designed, interactive layout the Markdown subset can't
carry; the landing page wanted to stay portable. Scoping the framework to `approaches/` and
below satisfies both, and one orchestrator keeps "build = one command" true. A fixed kit + a
worked example (`Hub.tsx`) made three parallel React authors safe to integrate.

**Notes:**
- The zero-dependency stance is **amended, not abandoned** — two new rows in
  `PROJECT-PITCH.md`. `--no-frontend` does a fast Markdown-only build; a missing Node
  toolchain is non-fatal (warns + skips).
- `background-attachment: fixed` and header `backdrop-blur` both stalled the preview
  screenshot tool — dropped both (also better paint perf). The capture tool also returns
  black for deep-scrolled shots of tall pages; verify those via DOM + a tall viewport.
- Rule 1 passed verbatim to all three agents; each authored only its one page; all four
  pages compile in one Vite build with zero console errors (hub + M1/M2/M3 verified live).
- Mutations inherit HAND **minus ghosts** (shelved per request) and replace HAND's free
  agent-market with a faction-AI contract board. Not pushed (no request to).

---

## 2026-06-24 — MSL: approaches sub-page, authored by three divergent agents

**Context:** Pushed *Minimalist Space Logistics* past its first sketch. The fiction was
settled but the engineering was wide open (the musing's own "open questions"). Rather than
answer once, added an *approaches* sub-page and generated three pitches in parallel — each
given the same canon plus a distinct "spark" chosen to send it into a different design space
*and a different simulation paradigm* from the other two.

**Options considered:**
- *Sub-page shape:* one long page of three sections vs. a hub page + one page per approach.
- *Rendering sub-pages:* extend the shared `render_page` vs. hand-inject nav in the Markdown
  body vs. a fully bespoke build that bypasses the shared renderer.
- *Authoring:* write the three myself vs. fan out to three parallel agents with diverging sparks.

**Choice:** Hub + three sub-pages under `Minimalist-Space-Logistics/approaches/`, rendered by
an extended (still thin) `build-musing.py`. Added two optional, backward-compatible params to
`musing_render.render_page` — `back_href`/`back_text` — so a nested page back-links to its
parent instead of always "← All musings"; the build passes depth-aware `css_href`/`home_href`.
Fanned out three agents — *The Invisible Hand* (agent-based economy), *The Tide Line*
(pressure-field front), *Dead Reckoning* (deterministic content deck) — then wrote the hub as
a synthesis of where they converge and fork.

**Why:** Hub + pages gives each pitch room to go deep (the brief was "iron the loop down to
the simulation tech"), and reads better than one giant page. Extending `render_page` was the
minimal correct change — a sub-page back-linking to "All musings" with the wrong label is
worse than two optional params, and the shared renderer stays generic. Divergent-spark agents
produced genuinely different design spaces; the core they *independently* agreed on
(`APR.1`–`APR.5`) is the most trustworthy signal in the result — three explorers told to
disagree still bottomed out at the same game.

**Notes:**
- Rule 1 was passed **verbatim, with a stern warning**, into all three agent prompts (required
  by `CLAUDE.md`). Each was also constrained to the renderer's Markdown subset — no tables /
  nested lists. They complied: *Dead Reckoning* used a fenced ASCII ledger (not a pipe table),
  and equations stayed inside code spans/fences so underscores didn't turn into emphasis.
- Verified the build: `build_site.py --drafts` → 5 pages. Spot-checked the generated HTML for
  depth-correct `style.css` hrefs (`../../../` hub, `../../../../` approach pages) and the
  parent back-links.
- Rule 6 (public surface): the approaches are public design prose — no identity/third-party
  issues; reviewed before declaring done. **Not pushed** (no request to).
- Sub-page mnemonics registered in the nav spec (Rule 8): `APR` (hub), `HAND` / `TIDE` / `DEAD`.

---

## 2026-06-24 — Musing build framework (config + per-folder build, render into `site/`)

**Context:** First musing requested (*Minimalist Space Logistics*), and with it a framework:
each musing is a top-level `<MUSE-SLUG>/` folder with its own `MUSING.md` content, a
`<FOLDER-NAME>.md` nav spec, and a `build-musing.py` that renders it to HTML. A registry
(`MUSING-CONFIG.json`) drives a build that the server runs and includes in `site/`. This is
the generator the v1 plan deferred.

**Options considered:**
- *Markdown:* a pip library (`markdown` / `mistune`) vs a small in-repo stdlib renderer.
- *Output:* commit generated HTML into `site/` vs gitignore it and build in CI.
- *Hidden flag:* skip entirely (draft) vs build-but-unlist (unlisted-public).
- *Per-musing build:* duplicate logic in each `build-musing.py` vs a thin script that
  delegates to a shared `musing_render.py`.

**Choice:** Pure-stdlib renderer (`utils/python/musing_render.py`, documented Markdown
subset) so Pages needs no `pip install`. Generated output (`site/index.html`,
`site/musings/`) is gitignored and built in CI (`pages.yml` gains a build step);
`site/style.css` stays the one tracked source asset. `hidden: true` = **draft**: skipped by
the default build (never deployed), but `serve_site.py` builds with `--drafts` so drafts
preview locally with a badge. Each `build-musing.py` is thin and imports the shared renderer.
Retired the old `site/projects/` hand-authored model.

**Why:** Zero-dependency is a stated core value (it's why the preview server is stdlib);
breaking it for Markdown wasn't worth it for hand-authored prose. Gitignoring output keeps
git focused on sources and matches "the server builds the site." Draft-skip honors the
Rule 6 public-surface gate — a hidden musing's source stays local, never deployed.

**Notes:**
- This **introduces a build step**, superseding the pitch's "no build step (yet)" stance —
  flagged here per Rule 3; decisions recorded in `PROJECT-PITCH.md`.
- `build-musing.py` lives in the deliverable folder — a sanctioned third script location
  beyond `utils/` / `scrap_scripts/`; it still anchors to the repo root per Rule 1.
- Renderer bug caught during verification: soft-wrapped list items were splitting into stray
  `<p>`s; the parser now folds lazy continuation lines into the list item. Verified in the
  browser preview (landing card + full musing page: headings, lists, blockquote, code block).
- Hidden ≠ private: a hidden musing's `MUSING.md` is still in the repo. Don't put
  gate-failing material in a musing folder just because it's hidden.
- `slug` is lowercase (Pages is case-sensitive Linux; Windows isn't) — the folder can be
  PascalCase, the URL slug must be lowercase.

---

## 2026-06-24 — Local-server launch config + canonical server name

**Context:** Mirrors the `.claude/launch.json` "local-server" preview pattern from a
sibling project so the site can be previewed in the Claude Code launch panel. Also written
up as a reusable appendix to the bootstrap skill.

**Choice:** Renamed `utils/python/serve.py` → `utils/python/serve_site.py` (the
cross-project canonical name the launch config expects) and updated every reference. Added
`.claude/launch.json` with the `local-server` config (`python utils/python/serve_site.py
--port 8000`, `port: 8000`). Flipped the server's browser behavior from auto-open
(`--no-browser` opt-out) to opt-in (`--open`).

**Why:** One canonical server name keeps the pattern identical across repos and lets the
appendix be authoritative. Browser opt-in matches `python -m http.server` and avoids a
redundant browser window beside the in-panel preview (the reference launch.json passes no
browser flag).

**Notes:**
- Pattern documented in `../initialize-skill-v0_2-appendix-local-site-preview.md` (next to
  the prototype, outside this repo — a bootstrap artifact, not committed here).
- `.claude/launch.json` is committed (shared preview config); keep machine-local Claude
  settings in `.claude/settings.local.json` (gitignore that if it appears).
- The bootstrap entry below still names the old `serve.py` — left as-is (append-only history).

---

## 2026-06-24 — Bootstrap: scaffold + landing-page site + Pages deploy

**Context:** Fresh repo for miscellaneous game-design musings and exploration (named
after a Godot directory, but not Godot-specific). Initialized per the bootstrap skill
`initialize-skill-v0_2.md`. First real task bundled in: a Python preview server + a
landing page that acts as a directory to future projects, plus a GitHub Actions
workflow to publish it to GitHub Pages.

**Options considered:**
- *Repo shape:* code-bearing (stand up `src/` + `CodeDocs/` + `CODE-DESIGN.md`) vs
  prose/knowledge-base + tooling (skip the code-doc tier).
- *Landing page:* hand-authored `index.html` vs a data-driven generator that rebuilds
  the index from per-project metadata.
- *Publishing:* deploy-from-branch Pages vs GitHub Actions Pages deploy.
- *Preview server location:* `src/` (product code) vs `utils/` (durable tooling).

**Choice:** Prose/KB + tooling shape — no `src/`, no `CodeDocs/`. Deliverables are
written explorations surfaced via the static site (`site/`, a README + SITE.md
deliverable pair); the only code is a local preview server placed at
`utils/python/serve.py` and cataloged in `utils/README.md`. Hand-authored `index.html`
for v1 (generator deferred — see `plans/PLAN-blog-site.md`). Publish via GitHub Actions
(`.github/workflows/pages.yml`) uploading `site/` as a Pages artifact.

**Why:** The product here is content, not a program; the server is tooling, so the
code-doc tier would be ceremony with nothing to mirror. A hand-authored page is the
"basic" thing asked for and stays robust with zero projects; the generator is a clean
follow-up once real musings exist. Actions-based Pages is the current first-class path
and keeps `site/` the single source of truth (the same folder the local server previews).

**Notes:**
- `scrap_scripts/` is gitignored *except* its `README.md`, so the scratch-script
  convention ships with the repo while throwaway scripts stay local.
- Site links are **relative** so the page works both locally (served at `/`) and on
  Pages (served under `/game-design-musings-blog/`).
- Identity gate (Rule 6/7): git author is `Spiffy-Panda <CptSpiffyPanda@gmail.com>` —
  pseudonymous, no dead/real-name leak — so the public push is clean.
- One-time manual step: set GitHub Pages source to "GitHub Actions" (repo Settings →
  Pages) for the workflow to publish.
