#!/usr/bin/env python3
"""Build THIS musing (Minimalist Space Logistics) to HTML — main page + sub-pages.

Per-musing build script. Renders ``MUSING.md`` to the musing's main page and, if an
``approaches/`` folder is present, renders that sub-tree too:

    site/musings/<slug>/index.html                     <- MUSING.md           (depth 2)
    site/musings/<slug>/approaches/index.html          <- approaches/APPROACHES.md (depth 3)
    site/musings/<slug>/approaches/<a>/index.html       <- approaches/<a>.md   (depth 4)
    site/musings/<slug>/explorations/index.html         <- explorations/index.html  (copied)
    site/musings/<slug>/explorations/<e>/index.html      <- explorations/<e>/index.html (copied)

This is the "bespoke, multi-page musing" case sanctioned in ``musing-tech-notes.md``:
the shared renderer (``utils/python/musing_render.py``) still does the Markdown→HTML and
page chrome; this script just decides which files become which pages and at what depth.

The site build (``utils/python/build_site.py``) runs this for every musing listed in
``MUSING-CONFIG.json``, passing ``--out``. It is also runnable on its own::

    python Minimalist-Space-Logistics/build-musing.py            # -> site/musings/<slug>/
    python Minimalist-Space-Logistics/build-musing.py --out DIR

Contract (see ``musing-tech-notes.md``):
  * Renders ``MUSING.md`` -> ``<out>/index.html`` and the optional ``approaches/`` tree.
  * The ``<FOLDER-NAME>.md`` agent-nav file is NOT rendered (internal, not published).
  * Copies an optional ``./assets/`` folder into ``<out>/assets/``.
  * Copies the repo-root ``explorations/`` gallery (the overview ``index.html`` + each
    exploration's own ``index.html``) into ``<out>/explorations/``. These are standalone,
    self-contained interactive HTML — copied, not rendered. Internal docs there
    (``README.md``, ``RUN-LOG.md``, ``_research/``) lack an ``index.html`` and are skipped.

Anchored to the repo root via ``__file__`` so it runs from any CWD. Standard library only.
"""
from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

# <repo>/<musing>/build-musing.py -> [0]=musing folder, parents[1]=repo root
_HERE = Path(__file__).resolve()
MUSING_DIR = _HERE.parent
REPO_ROOT = _HERE.parents[1]
SLUG = MUSING_DIR.name.lower()

# Import the shared renderer without assuming the CWD or an installed package.
sys.path.insert(0, str(REPO_ROOT / "utils" / "python"))
import musing_render  # noqa: E402


def _first_h1(md: str) -> "str | None":
    for raw in md.splitlines():
        s = raw.strip()
        if s.startswith("# "):
            return s[2:].strip()
    return None


def _emit(out_dir: Path, *, depth: int, md: str, title: str, back_href: str, back_text: str) -> Path:
    """Render one Markdown string to ``out_dir/index.html``.

    ``depth`` is how many folders deep the page sits below ``site/`` (2 for the musing
    root, 3 for ``approaches/``, 4 for an approach page); it sets the ``../`` count for
    the stylesheet and home links so they resolve under the Pages sub-path.
    """
    up = "../" * depth
    page = musing_render.render_page(
        title=title,
        body_html=musing_render.markdown_to_html(md),
        css_href=up + "style.css",
        home_href=up + "index.html",
        back_href=back_href,
        back_text=back_text,
    )
    out_dir.mkdir(parents=True, exist_ok=True)
    dest = out_dir / "index.html"
    dest.write_text(page, encoding="utf-8")
    return dest


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    ap.add_argument(
        "--out",
        type=Path,
        default=REPO_ROOT / "site" / "musings" / SLUG,
        help="Output directory for the rendered page (default: site/musings/<slug>/)",
    )
    args = ap.parse_args()
    out: Path = args.out

    src = MUSING_DIR / "MUSING.md"
    if not src.is_file():
        print(f"error: {src} not found", file=sys.stderr)
        return 1

    # --- main page (depth 2: site/musings/<slug>/index.html) --------------------
    main_md = src.read_text(encoding="utf-8")
    muse_title = _first_h1(main_md) or MUSING_DIR.name.replace("-", " ")
    _emit(out, depth=2, md=main_md, title=muse_title,
          back_href="../../index.html", back_text="← All musings")
    built = 1

    # --- approaches sub-tree -----------------------------------------------------
    # The approaches HUB (index.html) and the HAND-lineage mutation pages are built by the
    # React app (approaches-app/) and copied in by utils/python/build_site.py — NOT here.
    # This script renders only the original Markdown approach pages (one folder each, depth
    # 4: site/musings/<slug>/approaches/<slug>/). The React copy lands alongside them.
    approaches_dir = MUSING_DIR / "approaches"
    if approaches_dir.is_dir():
        for md_file in sorted(approaches_dir.glob("*.md")):
            if md_file.name == "APPROACHES.md":
                continue  # retired — the hub is now the React app's job
            aslug = md_file.stem.lower()
            a_md = md_file.read_text(encoding="utf-8")
            _emit(out / "approaches" / aslug, depth=4, md=a_md,
                  title=_first_h1(a_md) or md_file.stem.replace("-", " "),
                  back_href="../", back_text="← Approaches")
            built += 1

    # --- explorations sub-tree (static HTML, copied — no render step) ------------
    # The explorations gallery is standalone, self-contained interactive HTML at the
    # repo root (explorations/). Copy the overview (index.html) and every exploration
    # folder that has an index.html; internal docs (README.md, RUN-LOG.md, _research/)
    # have no index.html and are skipped, so they never reach the public surface.
    explor_dir = REPO_ROOT / "explorations"
    if (explor_dir / "index.html").is_file():
        explor_out = out / "explorations"
        explor_out.mkdir(parents=True, exist_ok=True)
        shutil.copy2(explor_dir / "index.html", explor_out / "index.html")
        n_explor = 0
        for child in sorted(explor_dir.iterdir()):
            if child.is_dir() and (child / "index.html").is_file():
                shutil.copytree(child, explor_out / child.name, dirs_exist_ok=True)
                n_explor += 1
        print(f"  explorations: overview + {n_explor} page(s) -> {explor_out}")

    # --- assets (copied verbatim) -----------------------------------------------
    assets = MUSING_DIR / "assets"
    if assets.is_dir():
        shutil.copytree(assets, out / "assets", dirs_exist_ok=True)

    print(f"built {SLUG}: {built} page(s) -> {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
