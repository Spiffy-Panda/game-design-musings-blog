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
