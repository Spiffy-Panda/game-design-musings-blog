# musing-tech-notes.md — the *how* of musings

Cross-musing engineering notes: the mechanics every musing shares — the build pipeline,
the Markdown the renderer understands, the per-folder build contract, and the gotchas.
When you build one musing and learn something the next one would want, write it **here**,
not buried in a single musing's folder. This file is the durable home for the "how"; each
musing folder only holds its own "what."

> **What is a musing?** A musing is one self-contained game-design exploration — a design
> note, a mechanics experiment, a post-mortem, a half-formed idea worth keeping. It lives
> in its own top-level folder (`<MUSE-SLUG>/`), is authored in Markdown (`MUSING.md`), and
> is published as a single page in the directory site. The landing page is a list of
> themed rows, one per musing (emblem + name + description + sublinks).

---

## Anatomy of a musing folder

```
<MUSE-SLUG>/                 e.g. Minimalist-Space-Logistics/
  MUSING.md                  the published content (the entry point; rendered to HTML)
  <FOLDER-NAME>.md           agent-nav spec, e.g. MINIMALIST-SPACE-LOGISTICS.md (NOT published)
  build-musing.py            renders MUSING.md -> site/musings/<slug>/index.html
  emblem.svg                 (optional) the landing-row emblem: a small square SVG inlined
                             into this musing's row on the landing page; colors via the
                             row's --m-* vars (see "Landing rows" below)
  assets/                    (optional) images/files copied verbatim into the page
  <pages>/                   (optional) sub-pages for a multi-page musing (e.g. approaches/)
```

`MUSING.md` + `<FOLDER-NAME>.md` are the deliverable pair from Rule 2 (`CLAUDE.md`):
`MUSING.md` is the content/human entry point, `<FOLDER-NAME>.md` is the agent map. Only
`MUSING.md` is rendered into the site — the nav file stays internal.

> **Variant:** an *HTML-first musing* has no `MUSING.md` at all — its pages are
> hand-authored, self-contained `.html` files copied verbatim by its `build-musing.py`.
> The nav spec is still required. See "HTML-first musings" below.

## The build pipeline

```
MUSING-CONFIG.json ──┐
                     ▼
   utils/python/build_site.py        (orchestrator; the ONLY build step)
     ├─ for each listed musing: run <folder>/build-musing.py --out site/musings/<slug>/
     │     └─ build-musing.py reads MUSING.md, calls utils/python/musing_render.py,
     │        writes site/musings/<slug>/index.html (+ copies assets/)
     └─ regenerates site/index.html  (one themed row per visible musing: inlined emblem
        SVG + name + description + sublinks + per-musing --m-* theme vars)
```

Two callers run the same `build_site.build()`:

- **Local preview** — `utils/python/serve_site.py` builds on startup (`--drafts` on, so
  hidden musings render for preview), then serves `site/`.
- **GitHub Pages** — `.github/workflows/pages.yml` runs `python utils/python/build_site.py`
  (no `--drafts`) before uploading `site/`.

### Generated vs. tracked (important)

| Tracked (source) | Generated (gitignored) |
|------------------|------------------------|
| `MUSING-CONFIG.json`, every `<MUSE-SLUG>/`, `site/style.css`, the build scripts | `site/index.html`, `site/musings/**` |

The output is rebuilt from sources, so it is **not committed**. A fresh clone has no
`site/index.html` until you build (run the server, or `python utils/python/build_site.py`).
If you'd rather commit the output instead, un-ignore those paths in `.gitignore` and drop
the build step from the Pages workflow — but the default keeps git focused on sources.

## `build-musing.py` contract

Each musing owns its build script, so a musing *can* be bespoke (custom layout, an
interactive canvas, multiple pages). The default script is thin and delegates to the
shared renderer. The contract the orchestrator relies on:

- Accept `--out DIR`; write `DIR/index.html` (default `site/musings/<slug>/`).
- Render `MUSING.md` (and any sub-pages the musing defines — see "Multi-page musings").
  **Never render `<FOLDER-NAME>.md`** (it's internal; rendering it would publish agent notes).
- Anchor to the repo root via `__file__` — **never assume the CWD** (Rule 1, `CLAUDE.md`):

  ```python
  _HERE = Path(__file__).resolve()
  MUSING_DIR = _HERE.parent          # this musing's folder
  REPO_ROOT = _HERE.parents[1]       # repo root
  sys.path.insert(0, str(REPO_ROOT / "utils" / "python"))
  import musing_render
  ```

- Exit non-zero on failure (the orchestrator isolates a failed musing and keeps building
  the rest, then exits non-zero overall).

`build-musing.py` lives in the deliverable folder rather than `utils/` or `scrap_scripts/`
because it's part of the musing. It's the one sanctioned third location for a script — keep
it thin and let `musing_render.py` carry the shared logic.

## Multi-page musings (sub-pages)

A musing can publish more than one page — an *approaches* tree, an appendix, a playable
sketch. The contract above already allows it ("renders `MUSING.md` *and any sub-pages*"); the
mechanics are just relative-link bookkeeping, and `musing_render.render_page` has the hooks:

- **Depth-aware asset links.** `css_href`/`home_href` are relative because Pages serves the
  site under a sub-path. Pass the right `../` count for where the page lands: the musing root
  (`site/musings/<slug>/index.html`) is **depth 2** (`../../style.css`); a sub-page one folder
  deeper is depth 3; a sub-sub-page is depth 4. Get this wrong and the stylesheet 404s on
  Pages while looking fine locally.
- **Breadcrumb trail.** `render_page(crumbs=[(label, href), …])` emits the site-wide
  `.crumbs` breadcrumb nav (see "Navigation: the breadcrumb standard" below). The last crumb
  is the current page (`href=None`); earlier crumbs point up the tree with depth-aware `../`
  hrefs. The legacy single `back_href`/`back_text` backlink is still accepted as a fallback
  when `crumbs` is omitted, but new pages should pass `crumbs`.

The worked example is *Minimalist Space Logistics*, and it now spans **both** rendering paths.
Its `build-musing.py` renders `MUSING.md` at depth 2 and the three *original* Markdown approach
pages (`the-invisible-hand.md`, `the-tide-line.md`, `dead-reckoning.md`) at depth 4 (back-link
→ the hub). The approaches **hub** and the newer HAND-lineage **mutation** pages are *not*
Markdown — they're a React app (next section). It **also** copies the repo-root `explorations/`
gallery (a standalone overview + 16 self-contained interactive HTML pages) into `…/explorations/`
— copied verbatim, **not** rendered; internal docs there (`README.md`, `RUN-LOG.md`, `_research/`,
none of which have an `index.html`) are skipped, so they never reach the public surface. Keep the
per-musing script the only place the Markdown layout logic lives; `musing_render.py` stays generic.
Markdown sub-pages are still bound by the same subset and the Rule 6 gate.

## Framework-built sub-pages (the approaches app)

The approaches **hub** + the HAND-lineage **mutation** pages are a **React + Tailwind app**
(Vite) in `approaches-app/` at the repo root. This is the one **documented exception** to the
zero-dependency build (see `PROJECT-PITCH.md`): the MSL landing page and the original three
approaches stay pure-stdlib Markdown renders for portability; only the hub and the mutation
pages get the framework.

How the two builds compose into one site:

- `approaches-app/` is a **multi-page Vite build** with `base: "./"` (relative asset URLs, so
  pages work at `/` locally and under the Pages sub-path). One HTML entry per page: the hub
  (`index.html`) + one folder per mutation (`two-ledgers/`, `known-war/`, `glass-cockpit/`),
  wired in `vite.config.ts` (`rollupOptions.input`).
- `utils/python/build_site.py` is the single orchestrator. After rendering the Markdown pages
  it calls `_build_frontend()`: `npm ci` (first run) + `npm run build`, then copies
  `approaches-app/dist/` over `site/musings/minimalist-space-logistics/approaches/` — adding
  the hub `index.html`, shared `assets/`, and the mutation pages **alongside** the Markdown
  approach folders already there. `serve_site.py` (which calls `build_site.build()`) and the
  Pages workflow (which has a Node step) both get the whole site from one entry point.
- A missing Node toolchain is **non-fatal**: `_build_frontend()` warns and skips, so the
  Markdown site + preview still work. `build_site.py --no-frontend` skips it on purpose for
  fast Markdown-only iteration.
- Author pages from the shared kit in `approaches-app/src/components/kit.tsx`;
  `src/pages/Hub.tsx` is the worked example. The app has its own `README.md`. To add a
  framework page: new HTML entry + `src/pages/*.tsx` + a `rollupOptions.input` line — it lands
  under `approaches/` on the next build.

Gotcha learned the hard way: `background-attachment: fixed` and `backdrop-filter`/`backdrop-blur`
can stall headless screenshot capture (and hurt paint perf). Avoid them on these pages.

## HTML-first musings (verbatim-copy sets)

Some musings are authored directly as **self-contained HTML pages** rather than Markdown —
design-system-heavy sets where the page *is* the deliverable. Worked examples:
`thaumodynamics/` (gallery + monograph + worksheet + duel chronicle) and `logical-magic/`
(gallery + system-pitch page). The pattern:

- **No `MUSING.md`.** The published entry point is a hand-authored gallery `index.html`
  (the "hub"), with one card per content page. The Rule 2 pair becomes
  `index.html` + `<FOLDER-NAME>.md` — the agent-nav spec is **still required** (it's where
  the Rule 8 mnemonic lives); a human-facing `README.md` is conventional too. Neither is
  published.
- **`build-musing.py` copies, it doesn't render.** Every top-level `*.html` is copied
  verbatim to `site/musings/<slug>/` (same treatment as the MSL `explorations/` gallery);
  `.md` files are never copied. The contract is otherwise unchanged: `--out`, repo-root
  anchoring via `__file__`, non-zero exit on failure.
- **Pages must stay self-contained** — inline CSS/JS, no external assets, light + dark
  themes carried per page. They don't use `site/style.css` and take no chrome from
  `musing_render.py`, so they hand-author their own copy of the breadcrumb (see
  "Navigation: the breadcrumb standard" below). The page body still renders standalone from
  a raw `file://` open; the one nav link that does **not** resolve on a raw disk open is the
  "Game Design Musings" landing crumb (it is site-relative — correct on the served site and
  the local preview, where the landing exists at the right depth). That is an accepted,
  deliberate relaxation of the old "no landing back-link" rule: coherent wayfinding rooted
  at the portfolio won the trade, and the served site (or `serve_site.py`) is the canonical
  way to view these pages. The **portfolio** crumb is an absolute cross-site URL and works
  everywhere, including disk.
- **Cross-musing links** use the shape `../<other-slug>/<page>.html`. That resolves
  identically in the repo (folder beside folder) and on the site (slug beside slug under
  `site/musings/`) — keep folder name == slug (lowercase) for these musings so the
  equivalence holds.
- **Landing-page sublinks:** list card sublinks in `MUSING-CONFIG.json` `links` with hrefs
  relative to the musing dir (e.g. `"pitch.html"`, `"mdyn101-worksheet3.html"`).
- **Rule 6 with teeth:** registering the musing is the moment "committed" becomes
  "published" — every `*.html` in the folder deploys on the next Pages build. Gate any
  edit accordingly.
- **SVG text gotcha:** a CSS `font:` shorthand in a shared utility class overrides SVG
  `font-size` presentation attributes. Size one-off SVG text with an inline
  `style="font:…"` instead.

## Landing rows: themes + emblems

The landing page gives each musing **one full-width row, themed after its content**: an
emblem on the left, then name / description / sublinks — set in the musing's own palette
and typeface so the directory reads like a shelf of different books. Two optional config
fields per musing drive it (both degrade gracefully when absent):

- **`"emblem": "emblem.svg"`** — a small square SVG (`viewBox="0 0 200 200"`, stroke-led,
  `aria-hidden`) living in the musing folder, **inlined** into the row by `build_site.py`.
  Color exclusively with `var(--m-<token>, <light-fallback>)` — never hard-code — so the
  emblem follows the row's light/dark palette for free. Keep it self-contained (no
  external refs); it is *not* copied into `site/musings/` — it exists only inside the
  generated landing. Worked examples: the LoMa proof-seal (`logical-magic/emblem.svg`,
  textPath glyph ring + inference rule), the THAU mirror-fields, the MSL lane web, the
  Space Feudal system roundel.
- **`"theme"`** — `{"font": "serif"|"sans", "light": {…}, "dark": {…}}`. Every key in the
  `light`/`dark` maps is emitted as `--m-<key>` on `.row-<slug>` (dark inside a
  `prefers-color-scheme` block) in a `<style>` generated into `index.html`. Conventional
  keys: `bg`, `ink`, `muted`, `line`, `accent` (+ `accent2`, `accent3`, or bespoke ones
  like MSL's `front`) — but the set is open: whatever the emblem needs. Pull the values
  from the musing's own pages so the row genuinely matches.

Row **layout** (flex, emblem width, mobile stacking, link styling) lives once in
`site/style.css` (`.musing-list` / `.musing-row`); the generated CSS carries only colors.

## Navigation: the breadcrumb standard

Every published page carries the **same breadcrumb trail**, rooted at the portfolio, so the
whole site navigates coherently regardless of which of the three rendering paths built it.
The trail is:

```
Panda's Portfolio  ›  Game Design Musings  ›  <Musing>  ›  <sub-page…>
```

- **First crumb — `Panda's Portfolio`** → the **absolute** URL `https://spiffy-panda.github.io/`.
  This is a deliberate cross-site link (the portfolio is a *separate* Pages deploy at the org
  root; the musings site lives at `…github.io/game-design-musings-blog/`). It is not a
  self-link, so the "relative links only / no absolute github.io" rule doesn't apply, and it
  resolves everywhere — disk, preview, Pages.
- **Second crumb — `Game Design Musings`** → the musings landing, **site-relative** (depth-aware
  `../…/index.html`). Correct on the served site and the local preview; the one link that does
  not resolve on a raw `file://` open (accepted — see the HTML-first note above).
- **Then** the musing and any parents (relative), ending with the **current page** as a
  non-link `aria-current="page"`.

**Markup** (identical everywhere) — a `<nav class="crumbs" aria-label="Breadcrumb"><ol>` of
`<li>`s, each an `<a>` except the current page (`<span aria-current="page">`); the `›`
separators are drawn by CSS (`li + li::before`), not in the markup.

**Three implementations, one look:**

- **Markdown pages** (`musing_render.render_page(crumbs=[(label, href), …])`) — emits the nav;
  styled by `.crumbs` in `site/style.css`. `PORTFOLIO_HREF`/`PORTFOLIO_LABEL` constants live in
  `musing_render.py`. The per-musing `build-musing.py` builds the trail (see MSL's `_root_crumbs`).
- **The generated landing** (`utils/python/build_site.py`) — hard-codes the two-crumb trail
  (`Panda's Portfolio › Game Design Musings`, current) into its `_INDEX` template.
- **Self-contained HTML musings** (thaumodynamics, logical-magic, space-feudal) **and** the
  copied explorations pages — hand-author the same `<nav class="crumbs">` inline, with a local
  copy of the `.crumbs` CSS using **that page's own palette tokens** (never `site/style.css`,
  never hard-coded colors). The **React** approaches app renders it via the shared `Page`
  component's `crumbs` prop (`approaches-app/src/components/kit.tsx`).

**Sizing/a11y (all paths):** links ≥ `.92rem` with real padding (tap target ≈ 34px), a visible
`:focus-visible` outline, `aria-label="Breadcrumb"` on the nav, wraps at narrow widths. This
replaced the old grab-bag of tiny back-links (a `.72rem` explorations backlink, small per-page
`.crumb` bars, and two musings with *no* back-nav at all).

## Markdown the renderer supports

`utils/python/musing_render.py` is a small, pure-stdlib renderer (no `pip install`, so the
Pages build stays zero-dependency). It is intentionally **a subset, not CommonMark**:

| Supported | Syntax |
|-----------|--------|
| Headings | `#` … `######` |
| Paragraphs | blank-line separated |
| Bold / italic | `**bold**`, `*italic*`, `_italic_` |
| Inline code | `` `code` `` |
| Links / images | `[text](url)`, `![alt](src)` |
| Lists | unordered `- * +`, ordered `1.` (single level — **no nesting**) |
| Blockquote | `>` (renders its contents recursively) |
| Code block | fenced ```` ``` ```` (optional language) |
| Rule | `---`, `***`, `___` on their own line |

**Not supported** (by design): tables, nested lists, footnotes, reference links, setext
headings, raw-HTML passthrough. If a musing needs more, either extend `musing_render.py`
deliberately (add the rule, update this table) or give that musing a bespoke
`build-musing.py` that renders however it likes. Don't reach for a Markdown dependency
without revisiting the zero-dependency decision in `PROJECT-PITCH.md` first.

## Hidden / draft musings

`"hidden": true` in `MUSING-CONFIG.json` means **draft**: the musing is skipped by the
default build, so it is not listed *and not deployed*. `serve_site.py` builds with
`--drafts`, so you still get a local preview (its card shows a "draft" badge). Pages builds
without `--drafts`, so a draft never reaches the public surface.

This is a draft gate for the build, **not** a privacy guarantee — the source `MUSING.md` is
still in the repo. Don't put anything that fails the Rule 6 public-surface gate into a
musing folder just because it's hidden.

## Adding a new musing (checklist)

1. `mkdir <MUSE-SLUG>/` at the repo root (PascalCase-with-hyphens, e.g. `Trade-Wind-Economy`;
   HTML-first musings use lowercase == slug, e.g. `logical-magic/`).
2. Write `<MUSE-SLUG>/MUSING.md` (the content) and `<MUSE-SLUG>/<FOLDER-NAME>.md` (agent nav)
   — or, for an HTML-first musing, the gallery `index.html` + pages instead of `MUSING.md`.
3. Copy an existing `build-musing.py` into the folder (the default one needs no edits — it
   derives its slug from the folder name; HTML-first musings copy the verbatim-copy variant
   from `thaumodynamics/` or `logical-magic/`).
4. Add an entry to `MUSING-CONFIG.json` (`folder`, `slug`, `name`, `description`, `hidden`;
   optionally `emblem` + `theme` for a themed landing row — see "Landing rows" above).
5. Preview: `python utils/python/serve_site.py` (builds on startup) and open the page.
6. Public-surface check (Rule 6), DEV-LOG entry (Rule 5), then commit.

## Gotchas

- **Slugs are lowercase.** Pages serves on case-sensitive Linux; Windows is
  case-insensitive. Keep `slug` lowercase so `./musings/<slug>/` resolves identically in
  both places. The folder name can be PascalCase; the URL slug should not.
- **Relative links only.** The site is served at `/` locally but under
  `/game-design-musings-blog/` on Pages. Musing pages link to `../../style.css` and
  `../../index.html`. Never a leading `/` or an absolute `…github.io/…` self-link.
- **Rebuild to see changes.** The server builds once at startup. After editing a `MUSING.md`
  or the config, restart `serve_site.py` (or rerun `build_site.py`). A live-rebuild/watch
  mode is a possible future enhancement.
- **Config errors fail loudly.** `MUSING-CONFIG.json` must be valid JSON (no comments, no
  trailing commas). A missing `build-musing.py` or a failing render is reported per-musing;
  the rest of the site still builds.

## Where the pieces live

- `MUSING-CONFIG.json` — the registry (repo root).
- `utils/python/build_site.py` — orchestrator + index generator.
- `utils/python/musing_render.py` — shared Markdown→HTML + page chrome.
- `utils/python/serve_site.py` — local preview server (builds, then serves).
- `<MUSE-SLUG>/build-musing.py` — per-musing render.
- `site/SITE.md` — spec for the published-site output side of this pipeline.
