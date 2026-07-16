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

    private static readonly (string Name, string Desc, string Schema)[] Tools =
    {
        ("session_start", "Connect to the Godot GTH bridge (launching the game first in launch mode). Returns element count.",
            """{"type":"object","properties":{}}"""),
        ("session_stop", "Disconnect and, if launched, stop the game.",
            """{"type":"object","properties":{}}"""),
        ("snapshot", "Compact interactable-element tree (test_id/path/type/text/rect/on_screen/clickable). The token-cheap 'what's on screen' — prefer over a capture.",
            """{"type":"object","properties":{"interactable":{"type":"boolean"},"offscreen":{"type":"boolean"}}}"""),
        ("query_element", "Clickability + hit-box/layer/offscreen/screen-size report for one element. No click.",
            """{"type":"object","properties":{"test_id":{"type":"string"},"text":{"type":"string"},"path":{"type":"string"},"contains":{"type":"string"}}}"""),
        ("read_element", "Read an element's text/value.",
            """{"type":"object","properties":{"test_id":{"type":"string"},"text":{"type":"string"},"path":{"type":"string"},"contains":{"type":"string"}}}"""),
        ("hit_test", "Elements under a point (normalized 0..1 by default) with predicted consumption order. No click.",
            """{"type":"object","properties":{"x":{"type":"number"},"y":{"type":"number"},"normalized":{"type":"boolean"}},"required":["x","y"]}"""),
        ("click_at", "Click a point; returns the hit-stack + which control consumed it.",
            """{"type":"object","properties":{"x":{"type":"number"},"y":{"type":"number"},"normalized":{"type":"boolean"},"button":{"type":"string"},"report_hits":{"type":"boolean"}},"required":["x","y"]}"""),
        ("click_element", "Click an element by handle; refuses if not clickable unless force=true. Returns clickability + hit report.",
            """{"type":"object","properties":{"test_id":{"type":"string"},"text":{"type":"string"},"path":{"type":"string"},"contains":{"type":"string"},"button":{"type":"string"},"force":{"type":"boolean"}}}"""),
        ("press_key", "Inject a key (e.g. 'F9','ENTER', or a single character).",
            """{"type":"object","properties":{"keys":{"type":"string"},"shift":{"type":"boolean"},"ctrl":{"type":"boolean"},"alt":{"type":"boolean"}},"required":["keys"]}"""),
        ("send_action", "Inject an InputMap action (tap by default).",
            """{"type":"object","properties":{"action":{"type":"string"},"pressed":{"type":"boolean"}},"required":["action"]}"""),
        ("capture", "Token-frugal capture: settle, sha-dedup, write to the artifacts dir, return {path,changed,...}. Read the path only if you need pixels. similar_ok enables perceptual dedup.",
            """{"type":"object","properties":{"label":{"type":"string"},"if_changed":{"type":"boolean"},"settle_ms":{"type":"integer"},"max_dim":{"type":"integer"},"format":{"type":"string"},"similar_ok":{"type":"boolean"}}}"""),
        ("wait_for", "Wait: {ms} sleep, {settled_ms} until the frame stops changing, or {element_visible handle}.",
            """{"type":"object","properties":{"ms":{"type":"integer"},"settled_ms":{"type":"integer"}}}"""),
        ("run_scenario", "Run a prescripted scenario (array of steps, or {steps:[...]}) in one round-trip.",
            """{"type":"object","properties":{"scenario":{}}}"""),
    };

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

    private static (string method, JsonNode bp) Map(string name, JsonObject a) => name switch
    {
        "snapshot" => ("snapshot", new JsonObject { ["filter"] = Pick(a, "interactable", "offscreen") }),
        "query_element" => ("query_element", new JsonObject { ["element"] = Handle(a) }),
        "read_element" => ("read_element", new JsonObject { ["element"] = Handle(a) }),
        "hit_test" => ("hit_test", Pick(a, "x", "y", "normalized")),
        "click_at" => ("click_at", Pick(a, "x", "y", "normalized", "button", "report_hits", "clicks")),
        "click_element" => ("click_element", Merge(new JsonObject { ["element"] = Handle(a) }, a, "button", "force", "index")),
        "press_key" => ("press_key", Pick(a, "keys", "shift", "ctrl", "alt")),
        "send_action" => ("send_action", Pick(a, "action", "pressed")),
        "capture" => ("capture", Pick(a, "label", "if_changed", "settle_ms", "max_dim", "format", "similar_ok", "region", "annotate")),
        "wait_for" => ("wait_for", Pick(a, "ms", "settled_ms", "element_visible", "timeout_ms")),
        "run_scenario" => ("run_scenario", new JsonObject { ["scenario"] = a["scenario"]?.DeepClone() ?? new JsonArray() }),
        _ => throw new InvalidOperationException($"unknown tool '{name}'"),
    };

    private static JsonObject Handle(JsonObject a)
    {
        var h = new JsonObject();
        foreach (var k in new[] { "test_id", "text", "path", "contains", "group" })
            if (a[k] is { } v) h[k] = v.DeepClone();
        return h;
    }

    private static JsonObject Pick(JsonObject a, params string[] keys)
    {
        var o = new JsonObject();
        foreach (var k in keys) if (a[k] is { } v) o[k] = v.DeepClone();
        return o;
    }

    private static JsonObject Merge(JsonObject baseObj, JsonObject a, params string[] keys)
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
