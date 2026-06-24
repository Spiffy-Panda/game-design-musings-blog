#!/usr/bin/env python3
"""Local preview server for the Game Design Musings static site.

Serves the published ``site/`` directory over HTTP so you can preview the landing
page exactly as GitHub Pages will serve it. Static files only — this mirrors the
Pages deployment (see ``.github/workflows/pages.yml``); it is NOT a dynamic backend.

Anchored to the repo root via ``__file__``, so it runs correctly from any CWD::

    python utils/python/serve.py
    py utils/python/serve.py --port 9000

Standard library only — no dependencies.
"""
from __future__ import annotations

import argparse
import http.server
import webbrowser
from functools import partial
from pathlib import Path

# utils/python/serve.py -> parents[0]=python, [1]=utils, [2]=repo root
REPO_ROOT = Path(__file__).resolve().parents[2]
SITE_DIR = REPO_ROOT / "site"


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("--port", type=int, default=8000, help="Port to listen on (default: 8000)")
    parser.add_argument("--host", default="127.0.0.1", help="Host/interface to bind (default: 127.0.0.1)")
    parser.add_argument("--no-browser", action="store_true", help="Do not open a browser window on start")
    args = parser.parse_args()

    if not SITE_DIR.is_dir():
        print(f"error: site directory not found at {SITE_DIR}")
        return 1

    # Serve SITE_DIR regardless of the current working directory.
    handler = partial(http.server.SimpleHTTPRequestHandler, directory=str(SITE_DIR))

    # ThreadingHTTPServer keeps the page responsive while assets load.
    with http.server.ThreadingHTTPServer((args.host, args.port), handler) as httpd:
        url = f"http://{args.host}:{args.port}/"
        print(f"Serving {SITE_DIR} at {url}")
        print("Press Ctrl+C to stop.")
        if not args.no_browser:
            webbrowser.open(url)
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nStopped.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
