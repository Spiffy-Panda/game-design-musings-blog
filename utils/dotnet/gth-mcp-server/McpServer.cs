using System.Text.Json.Nodes;

namespace GthMcp;

/// Minimal MCP server over stdio (JSON-RPC 2.0, newline-delimited). Implements initialize /
/// tools/list / tools/call / ping. Each tool maps to one in-Godot Bridge method; the Bridge's JSON
/// reply is returned verbatim as text content (token-frugal — images come back as file paths, not
/// inline bytes). All diagnostics go to stderr; stdout is the protocol channel only.
public sealed class McpServer
{
    private readonly Config _cfg;
    private readonly BridgeClient _bridge;
    private readonly GodotLauncher _launcher;
    private readonly TextReader _in;
    private readonly TextWriter _out;
    private readonly object _wlock = new();

    public McpServer(Config cfg, BridgeClient bridge, GodotLauncher launcher, TextReader input, TextWriter output)
    {
        _cfg = cfg; _bridge = bridge; _launcher = launcher; _in = input; _out = output;
    }

    private static void Log(string s) => Console.Error.WriteLine("[gth-mcp] " + s);

    public async Task RunAsync(CancellationToken ct)
    {
        Log($"ready (mode={_cfg.Mode} bridge=ws://{_cfg.Host}:{_cfg.Port})");
        string? line;
        while (!ct.IsCancellationRequested && (line = await _in.ReadLineAsync(ct)) != null)
        {
            if (line.Length == 0) continue;
            JsonNode? msg;
            try { msg = JsonNode.Parse(line); } catch { continue; }
            var id = msg?["id"];
            var method = msg?["method"]?.GetValue<string>();
            if (method is null) continue;  // a response or malformed — ignore
            try
            {
                switch (method)
                {
                    case "initialize": Respond(id, Initialize(msg!["params"])); break;
                    case "notifications/initialized": break;                 // notification, no reply
                    case "ping": Respond(id, new JsonObject()); break;
                    case "tools/list": Respond(id, ToolList()); break;
                    case "tools/call": Respond(id, await ToolCall(msg!["params"], ct)); break;
                    default: if (id is not null) Error(id, -32601, $"method not found: {method}"); break;
                }
            }
            catch (Exception ex)
            {
                Log($"error in {method}: {ex.Message}");
                if (id is not null) Error(id, -32000, ex.Message);
            }
        }
        Log("stdin closed — exiting");
    }

    private JsonObject Initialize(JsonNode? p)
    {
        var pv = p?["protocolVersion"]?.GetValue<string>() ?? "2025-06-18";  // echo the client's version
        return new JsonObject
        {
            ["protocolVersion"] = pv,
            ["capabilities"] = new JsonObject { ["tools"] = new JsonObject { ["listChanged"] = false } },
            ["serverInfo"] = new JsonObject { ["name"] = "gth-mcp-server", ["version"] = "0.1.0" },
        };
    }

    // ---- tools ----------------------------------------------------------------------------------

    // The schema IS the contract. GTH.B2/B3 both trace back to this table: `region`,
    // `annotate` and `repeat` were never declared, so a caller had nothing to shape them
    // against — it guessed a String where GDScript wanted an Array, and invented a `repeat`
    // that no layer had ever implemented. Anything a tool honours must be declared here, and
    // anything not declared here is reported back as ignored (see Unknown/Accepts below)
    // rather than dropped in silence.
    private static readonly (string Name, string Desc, string Schema)[] Tools =
    {
        ("session_start", "Connect to the Godot GTH bridge (launching the game first in launch mode). Returns element count.",
            """{"type":"object","properties":{}}"""),
        ("session_stop", "Disconnect and, if launched, stop the game.",
            """{"type":"object","properties":{}}"""),
        ("snapshot", "Compact interactable-element tree (test_id/path/type/text/rect/on_screen/clickable). The token-cheap 'what's on screen' — prefer it over a capture. `on_screen` is strict (FULLY inside the viewport); a partly-clipped control still appears, carrying visible_fraction + the clipped edges.",
            """{"type":"object","properties":{"interactable":{"type":"boolean","description":"only buttons/ranges/inputs and test_id-tagged nodes (default true)"},"offscreen":{"type":"boolean","description":"also include wholly-offscreen controls (default false)"}}}"""),
        ("query_element", "Clickability + hit-box/layer/offscreen/screen-size report for one element. No click. `clickable` means a click aimed at this would actually land on it; `on_screen` means FULLY on screen — read visible_fraction/clipped for partly-visible controls.",
            """{"type":"object","properties":{"test_id":{"type":"string"},"text":{"type":"string"},"path":{"type":"string"},"contains":{"type":"string"},"group":{"type":"string"}}}"""),
        ("read_element", "Read an element's text/value.",
            """{"type":"object","properties":{"test_id":{"type":"string"},"text":{"type":"string"},"path":{"type":"string"},"contains":{"type":"string"},"group":{"type":"string"}}}"""),
        ("hit_test", "Elements under a point (normalized 0..1 by default) with predicted consumption order. No click. If an embedded Window (a popup dialog) covers the point, the report is that window's contents — main-viewport controls beneath it cannot receive the click.",
            """{"type":"object","properties":{"x":{"type":"number"},"y":{"type":"number"},"normalized":{"type":"boolean"}},"required":["x","y"]}"""),
        ("click_at", "Click a point; returns the hit-stack + which control consumed it. `trace` adds Mode B: it watches every Control's gui_input during the click and reports who ACTUALLY received it, then scores that against Mode A's prediction.",
            """{"type":"object","properties":{"x":{"type":"number"},"y":{"type":"number"},"normalized":{"type":"boolean"},"button":{"type":"string","enum":["left","right","middle"]},"report_hits":{"type":"boolean"},"clicks":{"type":"integer","minimum":1,"description":"1 = single, 2 = double-click"},"trace":{"type":"boolean","description":"Mode B observed trace: who really got the event, vs who Mode A predicted"}},"required":["x","y"]}"""),
        ("click_element", "Click an element by handle; refuses if not clickable unless force=true. Returns clickability + hit report. The click lands on the element's anchor CLAMPED into its on-screen part, so a partly-clipped control is clicked where it is actually reachable.",
            """{"type":"object","properties":{"test_id":{"type":"string"},"text":{"type":"string"},"path":{"type":"string"},"contains":{"type":"string"},"group":{"type":"string"},"button":{"type":"string","enum":["left","right","middle"]},"force":{"type":"boolean"},"index":{"type":"integer","description":"which match to click when the handle is ambiguous"},"anchor":{"type":"array","items":{"type":"number"},"minItems":2,"maxItems":2,"description":"relative point in the element's rect, default [0.5,0.5]"},"clicks":{"type":"integer","minimum":1},"trace":{"type":"boolean","description":"Mode B observed trace: who really got the event, vs who Mode A predicted"}}}"""),
        ("press_key", "Inject a key (e.g. 'F9','ENTER', or a single character). `repeat` presses it N times; the result echoes back the count actually injected.",
            """{"type":"object","properties":{"keys":{"type":"string"},"shift":{"type":"boolean"},"ctrl":{"type":"boolean"},"alt":{"type":"boolean"},"meta":{"type":"boolean"},"repeat":{"type":"integer","minimum":1,"description":"press the key this many times (default 1)"},"repeat_frames":{"type":"integer","description":"frames to wait between repeats (default 1)"}},"required":["keys"]}"""),
        ("send_action", "Inject an InputMap action (tap by default).",
            """{"type":"object","properties":{"action":{"type":"string"},"pressed":{"type":"boolean"},"strength":{"type":"number"}},"required":["action"]}"""),
        ("capture", "Token-frugal capture: settle, sha-dedup, write to the artifacts dir, return {path,changed,...}. Read the path only if you need pixels. similar_ok enables perceptual dedup. A minimized window is restored first — it would otherwise hand back a stale frame that dedup calls 'unchanged'.",
            """{"type":"object","properties":{"label":{"type":"string"},"if_changed":{"type":"boolean"},"settle_ms":{"type":"integer"},"max_dim":{"type":"integer","description":"long-edge cap in px (default 1280)"},"format":{"type":"string","enum":["png","jpg","jpeg"]},"similar_ok":{"type":"boolean","description":"allow perceptual-hash dedup, not just exact-frame (default false)"},"phash_threshold":{"type":"integer"},"allow_minimized":{"type":"boolean","description":"shoot even if the window is minimized; the frame may be stale/blank and `changed` becomes untrustworthy (default false: the window is restored first)"},"region":{"type":"array","items":{"type":"number"},"minItems":4,"maxItems":4,"description":"crop to [x,y,w,h] in pixels of the full frame; clamped to the frame"},"annotate":{"type":"object","description":"draw a marker on the shot","properties":{"point":{"type":"array","items":{"type":"number"},"minItems":2,"maxItems":2},"rect":{"type":"array","items":{"type":"number"},"minItems":4,"maxItems":4}}}}}"""),
        ("wait_for", "Wait: {ms} sleep, {settled_ms} until the frame stops changing, {element_visible handle}, or {element_clickable handle}. Prefer element_clickable before a click — a control can be visible while still disabled, and clicking it then is refused. A timeout is returned as an error.",
            """{"type":"object","properties":{"ms":{"type":"integer"},"settled_ms":{"type":"integer"},"element_visible":{"type":"object","description":"an element handle, e.g. {\"test_id\":\"btn-step\"}"},"element_clickable":{"type":"object","description":"an element handle; waits until a click aimed at it would land"},"timeout_ms":{"type":"integer"}}}"""),
        ("window_state", "Window mode/size/focus + whether the resize lock is on. While the harness is active the window cannot be resized or maximized (a resize would silently invalidate every coordinate already issued). Pass minimize/restore to drive the window — the only way to exercise the minimize path.",
            """{"type":"object","properties":{"minimize":{"type":"boolean"},"restore":{"type":"boolean"}}}"""),
        ("run_scenario", "Run a prescripted scenario (array of steps, or {steps:[...]}) in one round-trip.",
            """{"type":"object","properties":{"scenario":{}}}"""),
    };

    // Every argument each tool actually honours. Kept beside the schemas on purpose: when the
    // two disagree, an argument silently does nothing, which is how B2 and B3 shipped.
    private static readonly string[] HandleKeys = { "test_id", "text", "path", "contains", "group" };

    private static readonly Dictionary<string, string[]> Accepts = new()
    {
        ["snapshot"] = new[] { "interactable", "offscreen" },
        ["query_element"] = HandleKeys,
        ["read_element"] = HandleKeys,
        ["hit_test"] = new[] { "x", "y", "normalized" },
        ["click_at"] = new[] { "x", "y", "normalized", "button", "report_hits", "clicks", "trace" },
        ["click_element"] = HandleKeys.Concat(new[] { "button", "force", "index", "anchor", "clicks", "trace" }).ToArray(),
        ["press_key"] = new[] { "keys", "shift", "ctrl", "alt", "meta", "repeat", "repeat_frames" },
        ["send_action"] = new[] { "action", "pressed", "strength" },
        ["capture"] = new[] { "label", "if_changed", "settle_ms", "max_dim", "format", "similar_ok", "phash_threshold", "allow_minimized", "region", "annotate" },
        ["wait_for"] = new[] { "ms", "settled_ms", "element_visible", "element_clickable", "timeout_ms" },
        ["window_state"] = new[] { "minimize", "restore" },
        ["run_scenario"] = new[] { "scenario" },
    };

    /// Arguments the caller sent that this tool does not honour. Returning these is the whole
    /// point: an argument that vanishes without a word reads as "done", and the caller then
    /// trusts a result for work that never happened.
    internal static List<string> Unknown(string name, JsonObject a) =>
        Accepts.TryGetValue(name, out var ok)
            ? a.Select(kv => kv.Key).Where(k => !ok.Contains(k)).ToList()
            : new List<string>();

    private JsonObject ToolList()
    {
        var arr = new JsonArray();
        foreach (var (name, desc, schema) in Tools)
            arr.Add(new JsonObject { ["name"] = name, ["description"] = desc, ["inputSchema"] = JsonNode.Parse(schema) });
        return new JsonObject { ["tools"] = arr };
    }

    private async Task<JsonObject> ToolCall(JsonNode? p, CancellationToken ct)
    {
        var name = p?["name"]?.GetValue<string>() ?? "";
        var args = p?["arguments"] as JsonObject ?? new JsonObject();
        try
        {
            if (name == "session_start") return TextResult(await SessionStart(ct));
            if (name == "session_stop") { await SessionStop(); return TextResult("""{"stopped":true}"""); }
            await EnsureConnected(ct);   // bridge tools auto-connect so callers can skip session_start
            var (method, bp) = Map(name, args);
            var result = await _bridge.CallAsync(method, bp, ct);
            var ignored = Unknown(name, args);
            if (ignored.Count > 0)
            {
                var warn = $"ignored unrecognised argument(s): {string.Join(", ", ignored)} — not part " +
                           $"of {name}'s schema, so nothing was done with them. Check the tool schema.";
                Log($"{name}: {warn}");
                if (result is JsonObject ro) ro["gth_warning"] = warn;
                else result = new JsonObject { ["result"] = result?.DeepClone(), ["gth_warning"] = warn };
            }
            return TextResult(result?.ToJsonString() ?? "null");
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = "error: " + ex.Message }),
                ["isError"] = true,
            };
        }
    }

    private static JsonObject TextResult(string text) =>
        new() { ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text }) };

    // Every tool that just forwards its arguments falls through to the generic arm, which picks
    // exactly what Accepts lists. Before, each tool repeated its key list here as well — a THIRD
    // copy of the contract, free to drift from the other two in silence, which is precisely what
    // it did. Two statements now, and ContractErrors() proves they agree.
    internal static (string method, JsonNode bp) Map(string name, JsonObject a) => name switch
    {
        "snapshot" => ("snapshot", new JsonObject { ["filter"] = Pick(a, Accepts["snapshot"]) }),
        "query_element" => ("query_element", new JsonObject { ["element"] = Handle(a) }),
        "read_element" => ("read_element", new JsonObject { ["element"] = Handle(a) }),
        "click_element" => ("click_element",
            Merge(new JsonObject { ["element"] = Handle(a) }, a, Accepts["click_element"].Except(HandleKeys))),
        "run_scenario" => ("run_scenario", new JsonObject { ["scenario"] = a["scenario"]?.DeepClone() ?? new JsonArray() }),
        _ when Accepts.ContainsKey(name) => (name, Pick(a, Accepts[name])),
        _ => throw new InvalidOperationException($"unknown tool '{name}'"),
    };

    /// The schema and Accepts are two statements of one contract, and B2/B3 are what happens when
    /// they drift apart quietly: an argument declared but not honoured does nothing; an argument
    /// honoured but not declared cannot be discovered, so a caller guesses its shape. Neither
    /// announces itself at runtime — the tool just returns success. Checked at startup instead.
    internal static List<string> ContractErrors()
    {
        var errs = new List<string>();
        foreach (var (name, _, schema) in Tools)
        {
            if (name is "session_start" or "session_stop") continue;
            if (!Accepts.TryGetValue(name, out var accepted))
            {
                errs.Add($"{name}: in Tools but absent from Accepts — every argument would be dropped");
                continue;
            }
            var declared = (JsonNode.Parse(schema)?["properties"] as JsonObject)?
                .Select(p => p.Key).ToHashSet() ?? new HashSet<string>();
            foreach (var d in declared.Where(d => !accepted.Contains(d)))
                errs.Add($"{name}.{d}: declared in the schema but not honoured — Pick() would drop it silently");
            foreach (var acc in accepted.Where(acc => !declared.Contains(acc)))
                errs.Add($"{name}.{acc}: honoured but not declared — a caller cannot know it exists, and will guess its shape");
        }
        foreach (var name in Accepts.Keys.Where(k => !Tools.Any(t => t.Name == k)))
            errs.Add($"{name}: in Accepts but exposed by no tool");
        return errs;
    }

    private static JsonObject Handle(JsonObject a)
    {
        var h = new JsonObject();
        foreach (var k in HandleKeys)
            if (a[k] is { } v) h[k] = v.DeepClone();
        return h;
    }

    private static JsonObject Pick(JsonObject a, IEnumerable<string> keys)
    {
        var o = new JsonObject();
        foreach (var k in keys) if (a[k] is { } v) o[k] = v.DeepClone();
        return o;
    }

    private static JsonObject Merge(JsonObject baseObj, JsonObject a, IEnumerable<string> keys)
    {
        foreach (var k in keys) if (a[k] is { } v) baseObj[k] = v.DeepClone();
        return baseObj;
    }

    private async Task EnsureConnected(CancellationToken ct)
    {
        if (_bridge.Connected) return;
        if (_cfg.Mode == "launch" && !_launcher.Launched) _launcher.Launch(_cfg);
        await _bridge.ConnectAsync(TimeSpan.FromSeconds(_cfg.Mode == "launch" ? 30 : 5), ct);
    }

    private async Task<string> SessionStart(CancellationToken ct)
    {
        await EnsureConnected(ct);
        var snap = await _bridge.CallAsync("snapshot", new JsonObject { ["filter"] = new JsonObject() }, ct);
        var count = snap?["count"]?.GetValue<int>() ?? -1;
        return $$"""{"connected":true,"mode":"{{_cfg.Mode}}","port":{{_cfg.Port}},"elements":{{count}}}""";
    }

    private async Task SessionStop()
    {
        await _bridge.DisposeAsync();
        _launcher.Stop();
    }

    // ---- JSON-RPC framing -----------------------------------------------------------------------

    private void Respond(JsonNode? id, JsonNode result)
    {
        if (id is null) return;
        Write(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id.DeepClone(), ["result"] = result });
    }

    private void Error(JsonNode id, int code, string message) =>
        Write(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id.DeepClone(),
            ["error"] = new JsonObject { ["code"] = code, ["message"] = message } });

    private void Write(JsonObject o)
    {
        lock (_wlock) { _out.Write(o.ToJsonString()); _out.Write('\n'); _out.Flush(); }
    }
}
