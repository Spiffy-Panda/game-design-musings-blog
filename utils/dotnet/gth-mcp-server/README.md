# gth-mcp-server — the live driver for the GTH Godot test harness

The **MCP server** half of the Godot test harness (spec: `../../../plans/PLAN-godot-test-harness.md`,
mnemonic `GTH`). It speaks **MCP over stdio** to the model and bridges each tool call to the in-Godot
**Bridge** (`addons/gd_test_harness/bridge.gd`) over a **loopback WebSocket**. Dependency-free .NET 8
(framework `System.Net.WebSockets` + `System.Text.Json` — no NuGet), matching the repo's zero-dependency
ethos. It is **project-agnostic**: the Godot project it drives is named entirely by config, so the same
server drives any project that ships the `gd_test_harness` addon.

```
  Claude ──MCP/stdio──▶ gth-mcp-server ──ws://127.0.0.1:PORT──▶ Bridge autoload (in Godot)
                        · tools/list, tools/call                · command API (input/inspect/capture)
                        · launch|attach lifecycle
```

## Build & self-test

```bash
dotnet build utils/dotnet/gth-mcp-server                      # builds; zero external packages

# live round-trip WITHOUT MCP registration (launches the game, drives it, stops it):
#   set the machine-specific values first (kept OUT of the repo — Rule 7):
#   GTH_MODE=launch  GTH_GODOT_EXE=<godot.exe>  GTH_PROJECT=<godot-project-dir>  GTH_PORT=8787
dotnet run --project utils/dotnet/gth-mcp-server -- --selftest
```

A green self-test prints `OK — live round-trip works (the click advanced the sim)`.

## Config (environment)

| Var | Default | Meaning |
|-----|---------|---------|
| `GTH_MODE` | `attach` | `attach` = connect to a bridge already serving; `launch` = start the game with `--gth-serve` and stop it on exit. |
| `GTH_GODOT_EXE` | — | Godot executable (launch mode only). **Machine-specific — never commit it.** |
| `GTH_PROJECT` | — | Godot project dir to drive (launch mode only). **Machine-specific.** |
| `GTH_HOST` / `GTH_PORT` | `127.0.0.1` / `8787` | Bridge address; must match the game's `--gth-port`. |

## Registered in the repo (`../../../.mcp.json`)

Registered **project-scoped** as **`gth-fishbowl`** (launch mode → the fishbowl). The committed entry is
**Rule-7-clean** — it carries no machine path:

- `args` and `GTH_PROJECT` are **relative to the repo root** (the CWD of a project-scoped MCP server); the
  server resolves `GTH_PROJECT` to absolute at startup.
- `GTH_GODOT_EXE` is omitted — the server defaults to `%ProgramFiles%\godot\godot.exe` (this machine's mono
  build; set the env var to override if yours is elsewhere).

**To activate:** `bin/` is gitignored, so run `dotnet build utils/dotnet/gth-mcp-server` once (the entry
points at the built DLL), then **restart the client** and **approve** the new server when prompted. The
model can then call `session_start`, `snapshot`, `click_element`, `capture`, `run_scenario`, ….

To drive a *different* project (the harness is generic), copy the entry and change `GTH_PROJECT`
(+ `GTH_GODOT_EXE` if needed), or use `attach` mode against an already-running game.

## Tools (each maps to one Bridge command)

`session_start` · `session_stop` · `snapshot` · `query_element` · `read_element` · `hit_test` ·
`click_at` · `click_element` · `press_key` · `send_action` · `capture` · `wait_for` · `run_scenario`.

Captures come back as **file paths + metadata** (not inline bytes) — read the path only when you need
pixels. Element handles resolve by `{test_id}` / `{text}` / `{path}` / `{contains}` / `{group}`.

## Files

`Program.cs` (entry + UTF-8 stdio + `--selftest`) · `McpServer.cs` (JSON-RPC loop, tool registry,
dispatch) · `BridgeClient.cs` (WebSocket client) · `GodotLauncher.cs` (launch/attach) · `Config.cs`
(env) · `SelfTest.cs` (live round-trip check).
