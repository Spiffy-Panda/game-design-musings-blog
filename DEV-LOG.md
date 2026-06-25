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
