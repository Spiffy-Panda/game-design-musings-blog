#!/usr/bin/env python3
"""Build the whole site: render every musing + (re)generate the landing page.

This is the site's single build step. It:

1. Reads ``MUSING-CONFIG.json`` (the registry of musings).
2. Runs each musing's ``build-musing.py`` (which renders that folder's ``MUSING.md``
   into ``site/musings/<slug>/index.html``).
3. Regenerates ``site/index.html`` with one directory card per visible musing, using the
   ``name`` + ``description`` from the config.

Usage::

    python utils/python/build_site.py            # build published (non-hidden) musings
    python utils/python/build_site.py --drafts   # also build hidden musings, for local preview

The local preview server (``serve_site.py``) calls this with drafts ON, so you can preview
work-in-progress locally. The GitHub Pages workflow calls it WITHOUT ``--drafts``, so a
``"hidden": true`` musing is never built into the public deploy. (Hidden is a draft gate,
not a privacy guarantee — see ``musing-tech-notes.md``.)

``site/index.html`` and ``site/musings/`` are generated output: they are gitignored and
rebuilt from the tracked sources. Anchored to the repo root via ``__file__``; stdlib only.
"""
from __future__ import annotations

import argparse
import html
import json
import shutil
import subprocess
import sys
from pathlib import Path

# utils/python/build_site.py -> parents[0]=python, [1]=utils, [2]=repo root
REPO_ROOT = Path(__file__).resolve().parents[2]
CONFIG = REPO_ROOT / "MUSING-CONFIG.json"
SITE_DIR = REPO_ROOT / "site"
MUSINGS_OUT = SITE_DIR / "musings"

# The one framework-built section of the site: a React + Tailwind app (Vite) that renders
# the approaches hub + the HAND-lineage mutation pages. Its output is copied over the
# Python-rendered approaches/ folder. This is the documented exception to the otherwise
# zero-dependency build (see PROJECT-PITCH.md / musing-tech-notes.md).
FRONTEND_APP = REPO_ROOT / "approaches-app"
FRONTEND_MUSING_SLUG = "minimalist-space-logistics"
FRONTEND_TARGET = MUSINGS_OUT / FRONTEND_MUSING_SLUG / "approaches"


def _load_musings() -> list[dict]:
    data = json.loads(CONFIG.read_text(encoding="utf-8"))
    return data.get("musings", [])


def build(include_hidden: bool = False, with_frontend: bool = True) -> int:
    """Render all musings, regenerate the index, and build the approaches app.

    Returns the number of failures. ``with_frontend`` also builds the React approaches app
    (``approaches-app/``) and copies it into the site; set it False for a fast
    Markdown-only build. A frontend failure is counted but never aborts the rest.
    """
    musings = _load_musings()

    # site/musings/ is fully generated + gitignored, so wipe and rebuild it.
    if MUSINGS_OUT.exists():
        shutil.rmtree(MUSINGS_OUT)
    MUSINGS_OUT.mkdir(parents=True, exist_ok=True)

    cards: list[dict] = []
    failures = 0

    for m in musings:
        folder = m["folder"]
        slug = m.get("slug") or folder.lower()
        hidden = bool(m.get("hidden", False))
        if hidden and not include_hidden:
            continue

        script = REPO_ROOT / folder / "build-musing.py"
        if not script.is_file():
            print(f"error: {folder}/build-musing.py not found", file=sys.stderr)
            failures += 1
            continue

        result = subprocess.run(
            [sys.executable, str(script), "--out", str(MUSINGS_OUT / slug)],
            capture_output=True,
            text=True,
        )
        if result.returncode != 0:
            print(f"error: build failed for {folder}:\n{result.stderr.rstrip()}", file=sys.stderr)
            failures += 1
            continue
        if result.stdout.strip():
            print(result.stdout.strip())

        # Optional per-musing landing-row theming: an emblem SVG (inlined into the
        # row, so it can use the row's --m-* CSS vars) + light/dark token maps.
        emblem_svg = ""
        emblem_name = m.get("emblem")
        if emblem_name:
            emblem_path = REPO_ROOT / folder / emblem_name
            if emblem_path.is_file():
                emblem_svg = emblem_path.read_text(encoding="utf-8").strip()
            else:
                print(f"warning: {folder}/{emblem_name} not found; row renders without emblem",
                      file=sys.stderr)

        cards.append(
            {
                "slug": slug,
                "name": m.get("name", folder),
                "description": m.get("description", ""),
                "draft": hidden,
                # Optional per-musing sublinks (e.g. Approaches / Explorations hubs),
                # each {"label", "href"} with href relative to the musing dir.
                "links": m.get("links", []),
                "emblem_svg": emblem_svg,
                "theme": m.get("theme") or {},
            }
        )

    _write_index(cards)

    if with_frontend and (MUSINGS_OUT / FRONTEND_MUSING_SLUG).is_dir():
        failures += _build_frontend()

    summary = f"site: {len(cards)} musing(s) built, index.html regenerated"
    if failures:
        summary += f", {failures} failed"
    print(summary)
    return failures


def _build_frontend() -> int:
    """Build the approaches React app (Vite) and copy it into the site.

    Returns a failure count; never raises. A missing Node toolchain or a failed build is
    reported and counted as one non-fatal failure, so the Markdown site and the local
    preview still work. On success, copies ``approaches-app/dist/`` over
    ``site/musings/<slug>/approaches/`` — adding the hub (index.html), shared assets/, and
    the mutation pages alongside the Markdown approach pages already rendered there.
    """
    if not (FRONTEND_APP / "package.json").is_file():
        return 0  # no app to build
    npm = shutil.which("npm")
    if not npm:
        print(
            "warning: npm not found on PATH; skipping the approaches app "
            "(install Node.js to build it). Markdown pages still built.",
            file=sys.stderr,
        )
        return 1

    if not (FRONTEND_APP / "node_modules").is_dir():
        installer = "ci" if (FRONTEND_APP / "package-lock.json").is_file() else "install"
        print(f"approaches-app: installing dependencies (npm {installer})...")
        res = subprocess.run([npm, installer], cwd=str(FRONTEND_APP), capture_output=True, text=True)
        if res.returncode != 0:
            print(f"error: approaches-app deps install failed:\n{res.stderr.rstrip()}", file=sys.stderr)
            return 1

    res = subprocess.run([npm, "run", "build"], cwd=str(FRONTEND_APP), capture_output=True, text=True)
    if res.returncode != 0:
        print(f"error: approaches-app build failed:\n{res.stderr.rstrip()}", file=sys.stderr)
        return 1

    dist = FRONTEND_APP / "dist"
    if not dist.is_dir():
        print("error: approaches-app build produced no dist/", file=sys.stderr)
        return 1

    FRONTEND_TARGET.mkdir(parents=True, exist_ok=True)
    shutil.copytree(dist, FRONTEND_TARGET, dirs_exist_ok=True)
    print(f"approaches-app: built + copied dist/ -> {FRONTEND_TARGET}")
    return 0


# --------------------------------------------------------------------- index template

_INDEX = """<!doctype html>
<!-- GENERATED by utils/python/build_site.py from MUSING-CONFIG.json. Do not edit by hand:
     change the config (cards) or a musing's MUSING.md (pages), then rebuild. -->
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Game Design Musings</title>
  <meta name="description" content="Miscellaneous game-design musings and exploration — a browsable directory.">
  <link rel="stylesheet" href="./style.css">
  <style>
{theme_css}
  </style>
</head>
<body>
  <nav class="crumbs" aria-label="Breadcrumb">
    <ol>
      <li><a href="https://spiffy-panda.github.io/">Panda's Portfolio</a></li>
      <li><span aria-current="page">Game Design Musings</span></li>
    </ol>
  </nav>
  <header class="site-header">
    <h1>Game Design Musings</h1>
    <p class="tagline">Miscellaneous game-design notes, mechanics experiments, and half-formed ideas — collected and explored.</p>
  </header>

  <main>
    <section aria-labelledby="musings-heading">
      <h2 id="musings-heading">Musings</h2>
      <ul class="musing-list">
{cards}
      </ul>
    </section>
  </main>

  <footer class="site-footer">
    <p>A static directory · generated by <code>utils/python/build_site.py</code> · previewed locally with <code>utils/python/serve_site.py</code> · published via GitHub Pages.</p>
  </footer>
</body>
</html>
"""

_PLACEHOLDER = """        <li class="musing-row placeholder">
          <div class="row-body">
            <h3>No musings yet</h3>
            <p class="row-desc">Explorations will appear here as they're written. Each musing is a folder listed in <code>MUSING-CONFIG.json</code>.</p>
          </div>
        </li>"""


def _row_html(card: dict) -> str:
    """One landing-page row: emblem (inlined SVG, themed by the row's --m-* vars)
    beside the musing's name, description, and sublinks."""
    draft = ' <span class="draft">draft</span>' if card["draft"] else ""
    slug = html.escape(card["slug"], quote=True)
    font = (card.get("theme") or {}).get("font", "sans")
    font_cls = " font-serif" if font == "serif" else ""

    emblem_html = ""
    if card.get("emblem_svg"):
        # Trusted repo-authored SVG, inlined so it inherits the row's CSS vars.
        emblem_html = (
            '          <div class="emblem" aria-hidden="true">\n'
            + card["emblem_svg"]
            + "\n          </div>\n"
        )

    links_html = ""
    links = card.get("links") or []
    if links:
        anchors = "\n".join(
            '              <a href="./musings/{slug}/{href}">{label}</a>'.format(
                slug=slug,
                href=html.escape(link.get("href", ""), quote=True),
                label=html.escape(link.get("label", "")),
            )
            for link in links
        )
        links_html = '\n            <p class="row-links">\n{anchors}\n            </p>'.format(anchors=anchors)

    return (
        '        <li class="musing-row row-{slug}{font_cls}">\n'
        "{emblem}"
        '          <div class="row-body">\n'
        '            <h3><a href="./musings/{slug}/">{name}</a>{draft}</h3>\n'
        '            <p class="row-desc">{desc}</p>{links}\n'
        "          </div>\n"
        "        </li>"
    ).format(
        slug=slug,
        font_cls=font_cls,
        emblem=emblem_html,
        name=html.escape(card["name"]),
        draft=draft,
        desc=html.escape(card["description"]),
        links=links_html,
    )


def _theme_css(cards: list[dict]) -> str:
    """Per-row theme variables generated from each musing's config `theme` block.

    Every key in `theme.light` / `theme.dark` is emitted as `--m-<key>` on
    `.row-<slug>`, so the row chrome AND the inlined emblem SVG pick up the
    musing's own palette in both color schemes. Structure/layout stays in
    site/style.css; only colors live here.
    """
    light_rules: list[str] = []
    dark_rules: list[str] = []
    for card in cards:
        theme = card.get("theme") or {}
        slug = card["slug"]
        for mode, bucket in (("light", light_rules), ("dark", dark_rules)):
            tokens = theme.get(mode) or {}
            if tokens:
                decls = " ".join(f"--m-{k}: {v};" for k, v in tokens.items())
                bucket.append(f"    .row-{slug} {{ {decls} }}")
    css = "\n".join(light_rules)
    if dark_rules:
        css += "\n    @media (prefers-color-scheme: dark) {\n"
        css += "\n".join("  " + r for r in dark_rules)
        css += "\n    }"
    return css


def _write_index(cards: list[dict]) -> None:
    body = "\n".join(_row_html(c) for c in cards) if cards else _PLACEHOLDER
    SITE_DIR.mkdir(parents=True, exist_ok=True)
    (SITE_DIR / "index.html").write_text(
        _INDEX.format(cards=body, theme_css=_theme_css(cards)), encoding="utf-8"
    )


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    ap.add_argument(
        "--drafts",
        action="store_true",
        help="Also build musings marked \"hidden\": true (local preview only)",
    )
    ap.add_argument(
        "--no-frontend",
        action="store_true",
        help="Skip the approaches React app (Markdown pages + index only; fast iteration)",
    )
    args = ap.parse_args()

    if not CONFIG.is_file():
        print(f"error: {CONFIG} not found", file=sys.stderr)
        return 1
    return build(include_hidden=args.drafts, with_frontend=not args.no_frontend)


if __name__ == "__main__":
    raise SystemExit(main())
