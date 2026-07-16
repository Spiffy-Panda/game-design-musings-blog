using System.Text.Json.Nodes;

namespace GthMcp;

/// End-to-end check of the LIVE path WITHOUT MCP registration: connect the BridgeClient to the
/// in-Godot Bridge and exercise the command API (snapshot → query → click → read → capture). Proves
/// the WebSocket transport + the in-engine harness round-trip. In launch mode it starts/stops the
/// game itself. All output to stderr; exit 0 on success.
public static class SelfTest
{
    public static async Task<int> RunAsync(Config cfg)
    {
        void L(string s) => Console.Error.WriteLine("[selftest] " + s);
        var bridge = new BridgeClient(cfg.Host, cfg.Port);
        var launcher = new GodotLauncher();
        try
        {
            if (cfg.Mode == "launch") { L("launching game with --gth-serve…"); launcher.Launch(cfg); }
            await bridge.ConnectAsync(TimeSpan.FromSeconds(cfg.Mode == "launch" ? 30 : 8));
            L($"connected to ws://{cfg.Host}:{cfg.Port}");

            // In serve mode the bridge listens from autoload time — give the main scene a moment to build.
            await bridge.CallAsync("wait_for", new JsonObject { ["ms"] = 700 });

            var snap = await bridge.CallAsync("snapshot", new JsonObject { ["filter"] = new JsonObject() });
            L($"snapshot → {snap?["count"]} interactable elements");

            var q = await bridge.CallAsync("query_element", Elem("btn-step"));
            L($"query btn-step → clickable={q?["clickable"]}, rect_px={q?["rect_px"]?.ToJsonString()}");

            var before = await bridge.CallAsync("read_element", Elem("clock"));
            await bridge.CallAsync("click_element", Elem("btn-step"));
            var after = await bridge.CallAsync("read_element", Elem("clock"));
            L($"click btn-step → clock {before?["text"]}  ⇒  {after?["text"]}");

            var hits = await bridge.CallAsync("hit_test", new JsonObject { ["x"] = 0.15, ["y"] = 0.5 });
            L($"hit_test (0.15,0.5) → consumer={hits?["consumer"]}");

            var cap = await bridge.CallAsync("capture", new JsonObject { ["label"] = "mcp-selftest", ["settle_ms"] = 300 });
            L($"capture → changed={cap?["changed"]} path={cap?["path"]}");

            var ok = after?["text"]?.GetValue<string>() != before?["text"]?.GetValue<string>();
            L(ok ? "OK — live round-trip works (the click advanced the sim)" : "WARN — clock did not change");
            return ok ? 0 : 1;
        }
        catch (Exception ex)
        {
            L("FAIL: " + ex.Message);
            return 1;
        }
        finally
        {
            await bridge.DisposeAsync();
            launcher.Stop();
        }
    }

    private static JsonObject Elem(string testId) =>
        new() { ["element"] = new JsonObject { ["test_id"] = testId } };
}
