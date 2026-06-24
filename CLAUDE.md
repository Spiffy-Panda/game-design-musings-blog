# CLAUDE.md — agent entry point

This is the LLM entry point for **Game Design Musings**, a repo of miscellaneous
game-design explorations published as a browsable directory (a small static
"blog"). Read this file first, then follow the entry-point chain at the bottom.

**Repo shape:** prose / knowledge-base + tooling. The deliverables are written
explorations surfaced through the static site in `site/`. The only code is
tooling (a local preview server in `utils/`). There is **no** `src/` product code
and therefore **no** `CodeDocs/` / `CODE-DESIGN.md` tier — if real source code is
ever added, stand that tier up then.

---

## Rules

### Rule 1 — No inline interpreter calls (shell one-liners are fine)

Hard rule for **interpreters**: no `python -c`, `python3 -c`, `py -c`, `node -e`, etc.

Trigger: if `import` (or `require`, `using`, `#include`) appears in a command line
you are about to send to a shell, **stop**. Create
`scrap_scripts/<lang>/<NN>_<slug>.<ext>` and run that file instead.

Shell is different. Short one-liners in PowerShell / bash / cmd are allowed
(`git status`, a single `grep`, `ls | head`). Escalate to a file the moment the
one-liner grows loops, variables, conditionals, or more than a couple of pipes —
then it goes in `scrap_scripts/<lang>/`.

Every script — scrap or util — must anchor to the repo root so it runs from any
CWD: `Path(__file__).resolve().parents[N]` (Python) or the language equivalent.
Never assume the invocation directory.

Promotion: the moment a scrap file is depended on by anything other than (a) a
human at the CLI or (b) an LLM agent — i.e. it produces a build artifact,
regenerates tracked content, or gets run often enough to justify a stable name —
move it to `utils/<lang>/<descriptive_name>.<ext>`, drop the `NN_` prefix, give it
a human name and a header comment, **and add a row to `utils/README.md`**.

Pass this rule, **verbatim and with a stern warning**, into every subagent prompt.
Sonnet-tier models have ignored it before.

### Rule 2 — Production hierarchy

| Tier | Where | Pattern | Purpose |
|------|-------|---------|---------|
| Plan | `plans/` | `PLAN-<slug>.md`, indexed by `PLAN.md` | Forward-looking. Chat dump. |
| Design | `PROJECT-PITCH.md` | single narrative + decisions table | Why it is built this way. |
| Deliverable | `<folder>/` | `README.md` + `<FOLDER-NAME>.md` pair | LLM-authored deliverables (e.g. `site/`). |
| Code-doc | *(n/a — no `src/`)* | — | Stand up `CodeDocs/` + `CODE-DESIGN.md` only if real source is added. |

`PLAN.md` is an *index only* — one line per slug.

### Rule 3 — Sync discipline

Touching one tier means updating the matching files in the others.

- Change a deliverable folder (`site/...`) → update its `<FOLDER-NAME>.md` spec
  (e.g. `site/SITE.md`) and tick off matching items in the relevant `PLAN-<slug>.md`.
- Change `PROJECT-PITCH.md` → if a decision conflicts with what's already built,
  flag it in `DEV-LOG.md`.
- Change `PLAN-<slug>.md` → update root `PLAN.md` if the change adds or completes a
  top-level item.

If you find an unexpected desync: flag it to the user, say docs need a resync, and
re-raise it at the start of every subsequent phase until handled. Force a sync check
before any `git push`.

### Rule 4 — Entry-point convention

- LLM: `CLAUDE.md` → `PLAN.md` / `PROJECT-PITCH.md` → per-slug plan → deliverable spec → deliverable.
- Human: `README.md` → same downstream chain.

### Rule 5 — DEV-LOG vs git commits

Git commits = *what changed*. `DEV-LOG.md` = *why we chose this, what we tried
first, what would surprise the next person*. Append-only, newest on top, absolute
dates. **Write an entry before every commit** — minimum one line; a short paragraph
for any non-obvious change.

### Rule 6 — Public-surface gate

Everything under `site/` is deployed to GitHub Pages and becomes publicly
redistributed content. Before pushing changes that touch a public surface, run a
content review:

- **Third-party material** (game data, decompiled code, scraped text, copyrighted
  prose): only small, transformative excerpts. No verbatim bulk. For redistributed
  creative work, walk the four fair-use factors — purpose/transformation, nature of
  the work, amount/substantiality, market effect — and note which factor(s) you
  weighed if it isn't obvious.
- **Identity / private data:** no dead names, real last names, private absolute
  paths, secrets, or local-only material (Rule 7) in anything that reaches a public
  surface — and that includes the git commit author identity.

If a deliverable drifts toward "comprehensive reproduction of the source," pull it
back before pushing.

### Rule 7 — Identity & naming rules live in CLAUDE.local.md, never committed

Identity rules that gate public output live in the **gitignored** `CLAUDE.local.md`,
not here. They are applied to every public surface under Rule 6.

---

## Where to look first

- **`PLAN.md`** — index of active/completed plans (`plans/PLAN-<slug>.md`).
- **`PROJECT-PITCH.md`** — why this project exists + decisions table.
- **`DEV-LOG.md`** — append-only decision log (newest on top).
- **`site/SITE.md`** — spec for the published site (structure, how to add a musing).
- **`utils/README.md`** — catalog of durable tooling (the preview server).
- **`scrap_scripts/README.md`** — scratch-script convention + promotion rule.
- **`CLAUDE.local.md`** — *(gitignored)* machine paths + identity rules.
- **`README.md`** — human entry point.
