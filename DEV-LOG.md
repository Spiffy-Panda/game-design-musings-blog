# DEV-LOG

Append-only. Newest entry on top. Absolute dates. This log records *why* — the
options weighed, what was tried, what would surprise the next person. Git history
records *what changed*. Write an entry before every commit (Rule 5).

---

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
