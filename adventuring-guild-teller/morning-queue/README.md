# The Morning Queue — desk-shift paper prototype (code-doc)

A small Godot 4.6 prototype of **one morning shift** at the Adventuring Guild Teller's
desk: sixteen visitors queue up, you check each claim against the reference book / ledger
/ drop table, and stamp a verdict. It exists to answer one question fast — *does the
checking loop actually flow?* (the musing's `AGT.5` / `AGR.1`).

This is the repo's **first source-code tier**. The rest of the repo is prose + a
zero-dependency static site; this folder is a self-contained Godot project and does **not**
ship to GitHub Pages (the musing build copies only top-level `*.html`).

## Run

Open `project.godot` in **Godot 4.6** (GDScript-only; a `.mono` editor is fine, we don't
use C#) and press Play, or use the godot MCP `run_project`. Main scene: `scenes/Main.tscn`.

## Layout

```
morning-queue/
  project.godot         GDScript-only, gl_compatibility renderer, autoloads Deck + Session
  data/
    visitors.json       the 16-visitor queue
    references.json     the rulebook every check resolves against
  scenes/               Main + one scene per component
  scripts/
    Main.gd             integrator (builds layout, wires signals — no game rules)
    autoload/           DeckLoader.gd (Deck), GameState.gd (Session)
    components/          VisitorCard / ReferencePanel / VerdictBar / Scoreboard
  MORNING-QUEUE.md      architecture + frozen interfaces + sub-agent allocation (read this)
```

## For agents / contributors

**`MORNING-QUEUE.md` is the spec** — the data schema, the frozen component interfaces, and
the file-ownership map that keeps parallel work conflict-free. Read it before touching
anything. The two autoloads (`Deck`, `Session`) and the four component scenes are stubs
with working plumbing; build the bodies, keep the signatures.

## Why GDScript / why Compatibility renderer

The installed editor is a `.mono` build, but Godot's .NET target **cannot Web-export**;
GDScript can. To keep open the option of embedding a Web build in the *local* site preview
(not Pages — Godot 4 Web needs COOP/COEP headers Pages can't serve), the project is pure
GDScript on the `gl_compatibility` renderer from day one.
