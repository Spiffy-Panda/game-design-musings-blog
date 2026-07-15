#!/usr/bin/env python3
"""Build THIS musing (MIDI Drum Coach) — an HTML-first set, copied verbatim.

Per-musing build script for the "HTML-first musing" variant (see
``musing-tech-notes.md``): there is no ``MUSING.md`` — the deliverable is a set of
hand-authored, self-contained HTML pages (a hub ``index.html`` + the interactive
coach page), each of which opens straight from disk. Publishing = copying them
into the site unchanged, the same verbatim-copy treatment ``logical-magic/`` and
``thaumodynamics/`` get:

    site/musings/<slug>/index.html   <- index.html  (the hub: the musing prose)
    site/musings/<slug>/coach.html   <- the interactive coach (Web MIDI + WebAudio)
    site/musings/<slug>/<page>.html  <- future pages (fill director, ...)

Only ``*.html`` is copied: ``README.md`` (human doc) and ``MIDI-DRUM.md``
(agent-nav spec) are internal and never reach the public surface.

The site build (``utils/python/build_site.py``) runs this for every musing listed
in ``MUSING-CONFIG.json``, passing ``--out``. Also runnable on its own::

    python midi-drum/build-musing.py            # -> site/musings/<slug>/
    python midi-drum/build-musing.py --out DIR

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
        print(f"error: {MUSING_DIR / 'index.html'} not found (the hub page)", file=sys.stderr)
        return 1

    out.mkdir(parents=True, exist_ok=True)
    for page in pages:
        shutil.copy2(page, out / page.name)

    print(f"built {SLUG}: {len(pages)} page(s) copied verbatim -> {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
