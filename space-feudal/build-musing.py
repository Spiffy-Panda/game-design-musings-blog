#!/usr/bin/env python3
"""Build THIS musing (Space Feudal) — an HTML-first set, copied verbatim.

Per-musing build script for the "HTML-first musing" variant (see
``musing-tech-notes.md``): there is no ``MUSING.md`` — the deliverable is a set of
hand-authored, self-contained HTML pages, each of which opens straight from disk.
Publishing = copying them into the site unchanged, the same verbatim-copy treatment
``thaumodynamics/`` and ``logical-magic/`` get:

    site/musings/<slug>/index.html   <- index.html  (the brief; the entry point)
    site/musings/<slug>/ledger.html  <- the Ledger of Correspondences (SF.1-SF.27)
    site/musings/<slug>/sieges.html  <- The Long Patience: siegecraft + the Harrow map
    site/musings/<slug>/loom.html    <- The Loom: pulled threads + counterweights (LOOM.1-6)

Only ``*.html`` is copied: ``README.md`` (human doc) and ``SPACE-FEUDAL.md``
(agent-nav spec) are internal and never reach the public surface.

The site build (``utils/python/build_site.py``) runs this for every musing listed
in ``MUSING-CONFIG.json``, passing ``--out``. Also runnable on its own::

    python space-feudal/build-musing.py            # -> site/musings/<slug>/
    python space-feudal/build-musing.py --out DIR

Anchored to the repo root via ``__file__`` so it runs from any CWD. Stdlib only.
"""
from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

# <repo>/<musing>/build-musing.py -> parent = musing folder, parents[1] = repo root
_HERE = Path(__file__).resolve()
MUSING_DIR = _HERE.parent
REPO_ROOT = _HERE.parents[1]
SLUG = MUSING_DIR.name.lower()


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    ap.add_argument(
        "--out",
        type=Path,
        default=REPO_ROOT / "site" / "musings" / SLUG,
        help="Output directory for the copied pages (default: site/musings/<slug>/)",
    )
    args = ap.parse_args()
    out: Path = args.out

    pages = sorted(MUSING_DIR.glob("*.html"))
    if not (MUSING_DIR / "index.html").is_file():
        print(f"error: {MUSING_DIR / 'index.html'} not found (the entry page)", file=sys.stderr)
        return 1

    out.mkdir(parents=True, exist_ok=True)
    for page in pages:
        shutil.copy2(page, out / page.name)

    print(f"built {SLUG}: {len(pages)} page(s) copied verbatim -> {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
