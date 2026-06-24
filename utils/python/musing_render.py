#!/usr/bin/env python3
"""Markdown -> HTML for musings (pure stdlib, zero dependencies).

Shared rendering library imported by every ``<musing>/build-musing.py``. It does two
things:

* ``markdown_to_html(md)`` — convert the small Markdown subset musings are authored in
  into an HTML fragment.
* ``render_page(...)`` — wrap an HTML fragment in the standard site page chrome so every
  musing page matches the landing page's styling.

This is deliberately a *subset*, not CommonMark — see ``musing-tech-notes.md`` for the
exact supported syntax and how to extend it. Standard library only, so the GitHub Pages
build needs no ``pip install`` (it mirrors the zero-dependency preview server).
"""
from __future__ import annotations

import html
import re

__all__ = ["markdown_to_html", "render_page"]


# --------------------------------------------------------------------------- inline

_CODE_SPAN = re.compile(r"`([^`]+)`")
_IMAGE = re.compile(r"!\[([^\]]*)\]\(([^)\s]+)\)")
_LINK = re.compile(r"\[([^\]]+)\]\(([^)\s]+)\)")
_BOLD = re.compile(r"\*\*(.+?)\*\*")
_ITALIC_STAR = re.compile(r"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)")
_ITALIC_USCORE = re.compile(r"(?<!\w)_(.+?)_(?!\w)")


def _render_inline(text: str) -> str:
    """Apply inline Markdown to a run of text, HTML-escaping everything else.

    Order matters: code spans are pulled out first (so nothing rewrites their contents),
    then the remaining text is escaped, then links/images/emphasis are applied, then the
    code spans are restored.
    """
    spans: list[str] = []

    def _stash(m: "re.Match[str]") -> str:
        spans.append("<code>" + html.escape(m.group(1)) + "</code>")
        return "\x00%d\x00" % (len(spans) - 1)

    text = _CODE_SPAN.sub(_stash, text)
    text = html.escape(text, quote=False)  # brackets/parens/asterisks survive for the rules below

    text = _IMAGE.sub(
        lambda m: '<img src="%s" alt="%s">'
        % (html.escape(m.group(2), quote=True), html.escape(m.group(1), quote=True)),
        text,
    )
    text = _LINK.sub(
        lambda m: '<a href="%s">%s</a>' % (html.escape(m.group(2), quote=True), m.group(1)),
        text,
    )
    text = _BOLD.sub(r"<strong>\1</strong>", text)
    text = _ITALIC_STAR.sub(r"<em>\1</em>", text)
    text = _ITALIC_USCORE.sub(r"<em>\1</em>", text)

    return re.sub(r"\x00(\d+)\x00", lambda m: spans[int(m.group(1))], text)


# ---------------------------------------------------------------------------- blocks

_HEADING = re.compile(r"^(#{1,6})\s+(.*)$")
_HR = re.compile(r"^(?:-{3,}|\*{3,}|_{3,})$")
_UL_ITEM = re.compile(r"^[-*+]\s+(.*)$")
_OL_ITEM = re.compile(r"^\d+[.)]\s+(.*)$")


def _is_block_start(line: str) -> bool:
    s = line.strip()
    return (
        s == ""
        or line.startswith("```")
        or line.startswith(">")
        or bool(_HEADING.match(line))
        or bool(_HR.match(s))
        or bool(_UL_ITEM.match(line))
        or bool(_OL_ITEM.match(line))
    )


def _collect_list(lines: list[str], i: int, n: int, item_re: "re.Pattern[str]") -> "tuple[list[str], int]":
    """Collect consecutive list items, folding in lazily-wrapped continuation lines.

    A list item may soft-wrap across several source lines; any non-blank line that isn't
    itself a new block start is folded into the current item. Single level only — an
    indented sub-bullet is treated as continuation text, not a nested list.
    """
    items: list[str] = []
    while i < n and item_re.match(lines[i]):
        text = item_re.match(lines[i]).group(1).rstrip()
        i += 1
        while i < n and not _is_block_start(lines[i]):
            text += " " + lines[i].strip()
            i += 1
        items.append("<li>%s</li>" % _render_inline(text))
    return items, i


def markdown_to_html(md: str) -> str:
    """Render a Markdown string to an HTML fragment (the supported subset only)."""
    lines = md.replace("\r\n", "\n").replace("\r", "\n").split("\n")
    out: list[str] = []
    i, n = 0, len(lines)

    while i < n:
        line = lines[i]

        # Fenced code block --------------------------------------------------
        if line.startswith("```"):
            lang = line[3:].strip()
            i += 1
            buf: list[str] = []
            while i < n and not lines[i].startswith("```"):
                buf.append(lines[i])
                i += 1
            i += 1  # consume the closing fence
            cls = ' class="language-%s"' % html.escape(lang, quote=True) if lang else ""
            out.append("<pre><code%s>%s</code></pre>" % (cls, html.escape("\n".join(buf))))
            continue

        # Blank line ---------------------------------------------------------
        if line.strip() == "":
            i += 1
            continue

        # Heading ------------------------------------------------------------
        m = _HEADING.match(line)
        if m:
            level = len(m.group(1))
            out.append("<h%d>%s</h%d>" % (level, _render_inline(m.group(2).strip()), level))
            i += 1
            continue

        # Thematic break -----------------------------------------------------
        if _HR.match(line.strip()):
            out.append("<hr>")
            i += 1
            continue

        # Blockquote (consume consecutive `>` lines, render recursively) -----
        if line.startswith(">"):
            buf = []
            while i < n and lines[i].startswith(">"):
                buf.append(re.sub(r"^>\s?", "", lines[i]))
                i += 1
            out.append("<blockquote>%s</blockquote>" % markdown_to_html("\n".join(buf)))
            continue

        # Unordered list -----------------------------------------------------
        if _UL_ITEM.match(line):
            items, i = _collect_list(lines, i, n, _UL_ITEM)
            out.append("<ul>%s</ul>" % "".join(items))
            continue

        # Ordered list -------------------------------------------------------
        if _OL_ITEM.match(line):
            items, i = _collect_list(lines, i, n, _OL_ITEM)
            out.append("<ol>%s</ol>" % "".join(items))
            continue

        # Paragraph (consume until a blank line or the next block starter) ---
        buf = [line.strip()]
        i += 1
        while i < n and not _is_block_start(lines[i]):
            buf.append(lines[i].strip())
            i += 1
        out.append("<p>%s</p>" % _render_inline(" ".join(buf)))

    return "\n".join(out)


# ------------------------------------------------------------------------ page chrome

_PAGE = """<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{title} — Game Design Musings</title>{meta_desc}
  <link rel="stylesheet" href="{css_href}">
</head>
<body>
  <main class="prose">
    <p class="backlink"><a href="{back_href}">{back_text}</a></p>
{body}
  </main>
</body>
</html>
"""


def render_page(
    *,
    title: str,
    body_html: str,
    css_href: str = "../../style.css",
    home_href: str = "../../index.html",
    description: "str | None" = None,
    back_href: "str | None" = None,
    back_text: str = "← All musings",
) -> str:
    """Wrap a rendered HTML fragment in the standard musing page shell.

    ``css_href``/``home_href`` are relative because the site is served under a
    sub-path on Pages (see ``musing-tech-notes.md``); pick the right depth of
    ``../`` for where the page lands. ``back_href``/``back_text`` control the
    top-of-page backlink: they default to the home link ("← All musings"), but a
    sub-page (e.g. an approaches page nested under a musing) can point one level
    up to its parent instead — e.g. ``back_href="../", back_text="← Approaches"``.
    """
    meta_desc = ""
    if description:
        meta_desc = '\n  <meta name="description" content="%s">' % html.escape(description, quote=True)
    if back_href is None:
        back_href = home_href
    return _PAGE.format(
        title=html.escape(title, quote=False),
        meta_desc=meta_desc,
        css_href=html.escape(css_href, quote=True),
        back_href=html.escape(back_href, quote=True),
        back_text=html.escape(back_text, quote=False),
        body=body_html,
    )
