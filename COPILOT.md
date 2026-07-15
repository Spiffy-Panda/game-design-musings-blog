# COPILOT.md — Copilot CLI entry point

This is the **Copilot CLI** entry point for **Game Design Musings**, a repo of miscellaneous
game-design explorations published as a browsable directory (a small static
"blog"). The conventions here mirror `CLAUDE.md` but are adapted for Copilot workspace
workflows, sessions, and automation.

**Repo shape:** prose / knowledge-base + tooling. The deliverables are **musings** —
written game-design explorations, each authored in its own top-level `<MUSE-SLUG>/`
folder and rendered into the static site in `site/` by a small build step. The code is
mostly tooling: a local preview server and the site build in `utils/`, plus a thin
`build-musing.py` inside each musing folder. **One exception:** `approaches-app/` is a
React/Tailwind app (Vite) that renders the *Minimalist Space Logistics → approaches* hub +
mutation pages — the one framework-built part of the site, with its own `README.md` as its
code-doc (the build stays zero-dependency everywhere else; see `PROJECT-PITCH.md`). There is
still no general `src/` product tier and no `CodeDocs/` / `CODE-DESIGN.md` tier — if broader
source code is added, stand that tier up then.

## Musings — the core deliverable

A **musing** is one self-contained game-design exploration: a design note, a mechanics
experiment, a post-mortem, a half-formed idea worth keeping. Each musing is a top-level
`<MUSE-SLUG>/` folder, authored in Markdown, and published as one page in the `site/`
directory (a card on the landing page links to it).

A musing folder holds:

- `MUSING.md` — the published content; the entry point that gets rendered to HTML.
- `<FOLDER-NAME>.md` — the agent-nav spec (the Rule 2 pair, e.g.
  `MINIMALIST-SPACE-LOGISTICS.md`). **Not** published.
- `build-musing.py` — renders `MUSING.md` → `site/musings/<slug>/`. It lives in the musing
  folder — a sanctioned third script location alongside `utils/` and `scrap_scripts/`,
  because it's part of the deliverable — and still anchors to the repo root per Rule 1.

`MUSING-CONFIG.json` (repo root) registers every musing. The site build
(`utils/python/build_site.py`, run by the preview server and by GitHub Pages) reads it,
runs each `build-musing.py`, and regenerates the landing page from each entry's friendly
name + description. **`musing-tech-notes.md` (repo root) is the canonical "how"** — the
build pipeline, the Markdown subset the renderer supports, the per-folder build contract,
and the gotchas, shared across all musings. Read it before adding or changing a musing,
and append to it whenever you learn something the next musing would want.

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
human at the CLI or (b) a Copilot agent — i.e. it produces a build artifact,
regenerates tracked content, or gets run often enough to justify a stable name —
move it to `utils/<lang>/<descriptive_name>.<ext>`, drop the `NN_` prefix, give it
a human name and a header comment, **and add a row to `utils/README.md`**.

**Pass this rule, verbatim and with a stern warning, into every subagent prompt.**
Larger models have ignored it before.

### Rule 2 — Production hierarchy

| Tier | Where | Pattern | Purpose |
|------|-------|---------|---------|
| Plan | `plans/` | `PLAN-<slug>.md`, indexed by `PLAN.md` | Forward-looking. Session dumps. |
| Design | `PROJECT-PITCH.md` | single narrative + decisions table | Why it is built this way. |
| Deliverable | `<MUSE-SLUG>/` (musings); `site/` | `MUSING.md` + `<FOLDER-NAME>.md` pair (musings); `README.md` + `SITE.md` (site) | Agent-authored deliverables. Musings are authored as top-level folders and rendered into `site/`. |
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

### Rule 4 — Entry-point convention (Copilot)

- **Copilot sessions (from this entry point):** `COPILOT.md` → `PLAN.md` / `PROJECT-PITCH.md` → per-slug plan → deliverable spec → deliverable.
- **Human entry point:** `README.md` → same downstream chain.
- **LLM (Claude/direct chat):** `CLAUDE.md` → same downstream chain (preserved for non-Copilot workflows).

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

### Rule 8 — List enumeration in pages (persistent item handles)

Enumerated items that live in a **repo file** (any page — musing or not) get stable,
referenceable handles. This is the persistent counterpart to the session-local
`_<PREFIX>.<n>` rule: because a page is committed, not throwaway, the leading `_` is
**dropped** and the item takes the page's mnemonic.

- **Form:** `<PREFIX>.<n>` — a short uppercase page mnemonic, a `.`, then the item number.
  The Minimalist Space Logistics page's mnemonic is **`MSL`**, so its items are `MSL.1`,
  `MSL.2`, … Session-local scratch lists still keep the `_` (e.g. `_D.1`); page items never do.
- **Scope is implicit.** Inside that page — or in a Copilot session already scoped to it — the prefix
  is understood: reference items by `.<n>` or the bare number. Qualify with the prefix
  (`MSL.1`) only when crossing scopes (from another page, or an unscoped session).
- **Where the mnemonic lives.** A page declares its mnemonic in its nav spec
  (`<FOLDER-NAME>.md` for musings) so the handle is stable and discoverable.

Applies to **any page**, not just musing-formatted ones.

---

## Copilot workflow notes

### Session management

- Use **local project sessions** (worktree or in-place) for sustained work on musings or tooling.
- **Branch per task:** create a feature branch in each session (`copilot-port`, `add-musing-foo`, etc.).
- **PR review:** use `create_pull_request` to push changes upstream and solicit review.
- **Commit discipline:** write descriptive commit messages; include a `DEV-LOG.md` entry before committing (Rule 5).

### Session-local tools

- **`plan.md`** in session artifacts (not committed): Use for session-specific task tracking, pseudo-todos, and exploration notes. Keep it short; move important findings to repo files (Rule 3).
- **Background agents** (`task` agent type, `mode: "background"`): Delegate long-running work (builds, tests, lints) to agents; you'll be notified on completion.
- **Parallel work:** use the `create_session` or `orchestrate` tool to spawn child sessions for independent branches or workstreams.

### Integration with CLAUDE.md

- **COPILOT.md is not a replacement.** Both files coexist; `CLAUDE.md` remains the canonical entry point for direct LLM workflows.
- **Cross-reference:** When Copilot sessions encounter LLM-specific guidance, they refer back to `CLAUDE.md` for detailed rules or examples.
- **Shared rules:** Rules 1–8 apply identically to both Copilot and LLM workflows.

---

## Where to look first

- **`PLAN.md`** — index of active/completed plans (`plans/PLAN-<slug>.md`).
- **`PROJECT-PITCH.md`** — why this project exists + decisions table.
- **`DEV-LOG.md`** — append-only decision log (newest on top).
- **`MUSING-CONFIG.json`** — registry of musings the site build reads (folder, slug, name, description, hidden).
- **`musing-tech-notes.md`** — the *how* of musings: build pipeline, Markdown subset, per-folder build contract. Canonical, shared across musings.
- **`site/SITE.md`** — spec for the published site output (structure, the build step, how a musing becomes a page).
- **`utils/README.md`** — catalog of durable tooling (the preview server + the site build).
- **`approaches-app/README.md`** — the React/Tailwind sub-site (MSL approaches hub + mutation pages); the one framework-built part of the site.
- **`scrap_scripts/README.md`** — scratch-script convention + promotion rule.
- **`CLAUDE.local.md`** — *(gitignored)* machine paths + identity rules.
- **`CLAUDE.md`** — LLM entry point (preserved for non-Copilot workflows).
- **`README.md`** — human entry point.
