#!/usr/bin/env python3
"""sync_gth_addon.py -- fan the canonical GTH addon out to every Godot project that uses it.

Why this exists (`GTH.Q4`, ruled 2026-07-16 -- full convergence): Godot resolves addons under
`res://`, so two projects physically cannot share one directory. Symlinks would manage it, but
they need Developer Mode on Windows and do not survive a clean clone -- so the canonical copy
lives in `utils/godot/` (shared tooling, alongside the MCP server that already lives in
`utils/dotnet/`) and this script copies it into each registered project.

`--check` is the entire point of the arrangement. On 2026-07-16 six harness bugs were fixed in
what was then the fishbowl's *private* copy of the addon; any other project that had copied it
would have been six bugs stale, and nothing anywhere would have said so. Now there is one
canonical copy and drift is an error you can run.

**Edit the canonical copy. Never edit a project's `addons/` copy** -- it is generated, and the
next sync overwrites it.

Usage:
    python utils/python/sync_gth_addon.py            # copy canonical -> every project
    python utils/python/sync_gth_addon.py --check    # exit 1 if any copy has drifted
    python utils/python/sync_gth_addon.py --list     # show canonical + its targets
"""

from __future__ import annotations

import argparse
import filecmp
import shutil
import sys
from pathlib import Path

# Anchor to the repo root so this runs from any CWD (CLAUDE.md Rule 1).
ROOT = Path(__file__).resolve().parents[2]
CANONICAL = ROOT / "utils" / "godot" / "gd_test_harness"
ADDON_REL = Path("addons") / "gd_test_harness"

# Every Godot project that ships the harness. Add a row when a project adopts it.
TARGETS = [
    Path("adventuring-guild-teller") / "fishbowl",
    Path("adventuring-guild-teller") / "morning-queue",
]


def _files(root: Path) -> set[Path]:
    return {p.relative_to(root) for p in root.rglob("*") if p.is_file()}


def check_one(dst: Path) -> list[str]:
    """Every way a project copy can disagree with canonical, named individually."""
    if not dst.is_dir():
        return ["not present at all -- run without --check to install it"]
    src_files, dst_files = _files(CANONICAL), _files(dst)
    problems = [f"missing: {r}" for r in sorted(src_files - dst_files)]
    problems += [f"extra (absent from canonical): {r}" for r in sorted(dst_files - src_files)]
    problems += [
        f"DRIFTED: {r}"
        for r in sorted(src_files & dst_files)
        if not filecmp.cmp(CANONICAL / r, dst / r, shallow=False)
    ]
    return problems


def sync_one(dst: Path) -> None:
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(CANONICAL, dst)


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    ap.add_argument("--check", action="store_true", help="report drift and exit 1; change nothing")
    ap.add_argument("--list", action="store_true", help="show the canonical copy and its targets")
    args = ap.parse_args()

    if not CANONICAL.is_dir():
        print(f"canonical addon not found: {CANONICAL}", file=sys.stderr)
        return 2

    if args.list:
        print(f"canonical: {CANONICAL.relative_to(ROOT)}")
        for t in TARGETS:
            print(f"  -> {t / ADDON_REL}")
        return 0

    rc = 0
    for t in TARGETS:
        proj = ROOT / t
        if not (proj / "project.godot").is_file():
            print(f"!! {t}: no project.godot -- is the TARGETS list stale?", file=sys.stderr)
            rc = 2
            continue
        dst = proj / ADDON_REL
        if args.check:
            problems = check_one(dst)
            if problems:
                rc = max(rc, 1)
                print(f"DRIFT {t}")
                for p in problems:
                    print(f"      {p}")
            else:
                print(f"ok    {t}")
        else:
            sync_one(dst)
            print(f"synced {t / ADDON_REL}")

    if args.check and rc == 1:
        print(
            "\nThe canonical copy is utils/godot/gd_test_harness/ -- edit THAT, then re-run this\n"
            "script without --check. A project's addons/ copy is generated; editing it is exactly\n"
            "the drift GTH.Q4 was ruled on.",
            file=sys.stderr,
        )
    return rc


if __name__ == "__main__":
    sys.exit(main())
