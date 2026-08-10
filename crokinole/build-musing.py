#!/usr/bin/env python3
"""Build THIS musing (Crokinole) — an HTML-first set, copied verbatim, with the
board geometry inlined from ``layouts.json``.

Per-musing build script for the "HTML-first musing" variant (see
``musing-tech-notes.md``): there is no ``MUSING.md`` — the deliverable is a set of
hand-authored, self-contained HTML pages (a hub ``index.html`` + the playable
board), each of which opens straight from disk. Publishing = copying them into the
site, the same verbatim-copy treatment ``logical-magic/``, ``thaumodynamics/``
and ``midi-drum/`` get::

    site/musings/<slug>/index.html    <- index.html  (the hub: the musing prose)
    site/musings/<slug>/board.html    <- the playable simulator, geometry INLINED
    site/musings/<slug>/layouts.json  <- the authored geometry, copied as-is
    site/musings/<slug>/<page>.html   <- future pages (the statistics curves, ...)

One page is not copied verbatim. **``layouts.json`` is the single authored source
of truth for board geometry**; ``board.html`` carries it inside a

    <script type="application/json" id="board-layouts"> ... </script>

block, which this script REGENERATES from ``layouts.json`` on the way out. The
page parses that block with ``JSON.parse`` rather than fetching the file, because
the page must open straight from ``file://`` and a fetch of a sibling file is
blocked there by CORS. So: authored in JSON, inlined at build, and the JSON is
shipped alongside the page so the two can be diffed.

Because the repo copy of ``board.html`` also has to open from ``file://``, its
block is kept in sync too — run::

    python crokinole/build-musing.py --sync

after editing ``layouts.json``. A normal build never writes to the source tree;
it warns loudly if the repo copy has gone stale, and the published page is
always built from ``layouts.json``.

Only ``*.html`` plus ``layouts.json`` are copied: ``README.md`` (human doc) and
``CROKINOLE.md`` (agent-nav spec) are internal and never reach the public
surface.

The site build (``utils/python/build_site.py``) runs this for every musing listed
in ``MUSING-CONFIG.json``, passing ``--out``. Also runnable on its own::

    python crokinole/build-musing.py            # -> site/musings/<slug>/
    python crokinole/build-musing.py --out DIR
    python crokinole/build-musing.py --sync     # refresh the repo copy's block

Anchored to the repo root via ``__file__`` so it runs from any CWD. Stdlib only.
Exits non-zero if ``layouts.json`` is missing, unparseable, or the block it is
supposed to fill cannot be found in ``board.html``.
"""
from __future__ import annotations

import argparse
import json
import re
import shutil
import sys
from pathlib import Path

# <repo>/<musing>/build-musing.py -> parent = musing folder, parents[1] = repo root
_HERE = Path(__file__).resolve()
MUSING_DIR = _HERE.parent
REPO_ROOT = _HERE.parents[1]
SLUG = MUSING_DIR.name.lower()

LAYOUTS_JSON = MUSING_DIR / "layouts.json"
BOARD_PAGE = "board.html"

# The generated block. Matched on the opening tag's id so the page can move it.
_BLOCK_RE = re.compile(
    r'(<script[^>]*\bid="board-layouts"[^>]*>)(.*?)(</script>)',
    re.DOTALL,
)


def load_layouts():
    """The authored geometry, re-serialised. Fails loudly rather than shipping a
    page whose board data is missing or half-written."""
    if not LAYOUTS_JSON.is_file():
        raise SystemExit(f"error: {LAYOUTS_JSON} not found — it is the authored source of board geometry")
    try:
        data = json.loads(LAYOUTS_JSON.read_text(encoding="utf-8"))
    except json.JSONDecodeError as err:
        raise SystemExit(f"error: {LAYOUTS_JSON} is not valid JSON — {err}")
    if not isinstance(data, dict) or not data:
        raise SystemExit(f"error: {LAYOUTS_JSON} must be a non-empty object of {{ name: layout }} entries")
    # Re-serialised, not passed through: this guarantees the inlined block is
    # valid JSON by construction and normalises whitespace, so a build is
    # reproducible and a diff against the shipped layouts.json is meaningful.
    # `</` cannot appear in the values here, but escape it anyway — an author
    # who ever writes one into a label must not be able to close the tag early.
    return json.dumps(data, indent=2, sort_keys=False).replace("</", "<\\/"), len(data)


def inline(page_html: str, payload: str) -> str:
    """Replace the board-layouts block's contents. Raises if it is not there."""
    if not _BLOCK_RE.search(page_html):
        raise SystemExit(
            'error: board.html has no <script type="application/json" id="board-layouts"> block '
            "to inline layouts.json into"
        )
    # A JSON block cannot carry a comment, so the "this is generated" note lives
    # in the HTML comment that precedes it in the page. Keep the payload pure JSON.
    return _BLOCK_RE.sub(lambda m: m.group(1) + "\n" + payload + "\n" + m.group(3), page_html, count=1)


def current_block(page_html: str) -> str:
    m = _BLOCK_RE.search(page_html)
    return m.group(2).strip() if m else ""


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
    ap.add_argument(
        "--sync",
        action="store_true",
        help="Also rewrite crokinole/board.html's inlined block from layouts.json, "
             "so the repo copy opens from file:// with the current geometry.",
    )
    args = ap.parse_args()
    out: Path = args.out

    board_src = MUSING_DIR / BOARD_PAGE
    if not (MUSING_DIR / "index.html").is_file():
        print(f"error: {MUSING_DIR / 'index.html'} not found (the hub page)", file=sys.stderr)
        return 1
    if not board_src.is_file():
        print(f"error: {board_src} not found (the simulator page)", file=sys.stderr)
        return 1

    payload, n_layouts = load_layouts()  # raises SystemExit(non-zero) on any failure
    board_html = board_src.read_text(encoding="utf-8")
    inlined = inline(board_html, payload)

    if args.sync:
        if inlined != board_html:
            board_src.write_text(inlined, encoding="utf-8")
            print(f"synced {board_src.relative_to(REPO_ROOT)} <- layouts.json")
        else:
            print(f"{board_src.relative_to(REPO_ROOT)} already matches layouts.json")
    elif current_block(board_html) != payload.strip():
        # Loud, but not fatal: the PUBLISHED page is always built from
        # layouts.json, so the site is never wrong. What is stale is the repo
        # copy you open from file://.
        print(
            f"warning: {board_src.relative_to(REPO_ROOT)}'s inlined board-layouts block is STALE "
            f"relative to layouts.json — the published page uses layouts.json; "
            f"run `python {Path(__file__).relative_to(REPO_ROOT).as_posix()} --sync` to refresh the repo copy",
            file=sys.stderr,
        )

    out.mkdir(parents=True, exist_ok=True)
    pages = sorted(MUSING_DIR.glob("*.html"))
    for page in pages:
        if page.name == BOARD_PAGE:
            (out / page.name).write_text(inlined, encoding="utf-8")
        else:
            shutil.copy2(page, out / page.name)
    shutil.copy2(LAYOUTS_JSON, out / LAYOUTS_JSON.name)

    print(f"built {SLUG}: {len(pages)} page(s) -> {out} "
          f"({n_layouts} board layouts inlined into {BOARD_PAGE}, {LAYOUTS_JSON.name} copied)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
