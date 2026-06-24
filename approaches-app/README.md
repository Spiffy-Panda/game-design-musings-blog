# approaches-app

The **framework-built** part of the Game Design Musings site: a Vite + React 19 + Tailwind v4
multi-page app that renders the *Minimalist Space Logistics → approaches* **hub** and the
**HAND-lineage mutation pages**. Everything else on the site (the landing page, the original
three approaches, every other musing) stays pure-stdlib Markdown — this app is the one
deliberate exception to the zero-dependency build (see `../PROJECT-PITCH.md`).

## How it fits the site

You almost never build this directly. `utils/python/build_site.py` is the single
orchestrator: it renders the Markdown pages, then runs this app's build and copies
`dist/` into `site/musings/minimalist-space-logistics/approaches/` — so the React hub +
mutation pages land **alongside** the Markdown approach pages. `serve_site.py` (local
preview) and the GitHub Pages workflow both go through `build_site.py`, so both get the
whole site from one command. Missing Node is non-fatal there (it warns and skips);
`build_site.py --no-frontend` skips this app on purpose.

Full pipeline + rationale: `../musing-tech-notes.md` ("Framework-built sub-pages").

## Layout

```
approaches-app/
  index.html                 hub entry        -> approaches/index.html
  two-ledgers/index.html     M1 entry         -> approaches/two-ledgers/index.html
  known-war/index.html       M2 entry         -> approaches/known-war/index.html
  glass-cockpit/index.html   M3 entry         -> approaches/glass-cockpit/index.html
  vite.config.ts             multi-page input map; base: "./" (relative assets)
  src/
    entries/*.tsx            one mount per page (imports the page + the stylesheet)
    pages/                   Hub.tsx + the three mutation pages (the content)
    components/kit.tsx       the shared "ops console" design system — build pages from this
    styles/index.css         Tailwind import + theme tokens (ink/fog palette, --accent)
```

## Develop / build

- `npm install` once. `npm run dev` for the live Vite server while editing.
- `npm run build` produces `dist/` (what `build_site.py` copies). `npm run typecheck` for TS.
- To preview the *assembled* site as Pages serves it, run `python ../utils/python/serve_site.py`
  from the repo root (it builds everything, including this app, then serves `site/`).

## Conventions (keep pages consistent + portable)

- **Build pages from `kit.tsx` only** (plus React). `Hub.tsx` is the worked example. The kit
  gives you `Page`, `Section`, `DecisionList`, `Callout`, `StatGrid`, `Figure`, `CompareTable`,
  `Tabs`, `Handle`, `Tag`, etc. Per-page accent: `<Page accent="amber|rose|cyan|violet|...">`.
- **`base: "./"` is load-bearing** — assets must stay relative so pages work both locally (`/`)
  and under the Pages sub-path. Don't introduce absolute (`/…`) asset URLs.
- **Avoid `background-attachment: fixed` and `backdrop-filter`/`backdrop-blur`** — both stall
  the headless screenshot tooling and hurt paint perf. (Learned the hard way; see the DEV-LOG.)
- Public surface (Rule 6): these pages deploy publicly. No dead names, real last names, private
  paths, or secrets.

## Add a page

1. New `src/pages/Foo.tsx` (default-export a component, built from the kit).
2. New `src/entries/foo.tsx` (mount it, import `../styles/index.css`).
3. New `foo/index.html` entry pointing at `/src/entries/foo.tsx`.
4. Add `"foo": r("foo/index.html")` to `rollupOptions.input` in `vite.config.ts`.
5. Link it from `Hub.tsx`. Rebuild — it lands at `approaches/foo/` on the next site build.
