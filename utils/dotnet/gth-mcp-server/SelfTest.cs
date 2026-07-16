using System.Text.Json.Nodes;

namespace GthMcp;

/// End-to-end check of the LIVE path WITHOUT MCP registration: connect the BridgeClient to the
/// in-Godot Bridge and exercise the command API (snapshot → query → click → read → capture). Proves
/// the WebSocket transport + the in-engine harness round-trip. In launch mode it starts/stops the
/// game itself. All output to stderr; exit 0 on success.
///
/// Two lessons from GTH.B2/B3 are built into the shape of this file:
///
///  1. Calls go through McpServer.Map(), NOT straight at the bridge. The old self-test talked to
///     the bridge directly, which meant the tool-surface layer — where BOTH bugs actually lived —
///     was untested by construction. Even a self-test that had passed a `region` would have sailed
///     past B2: it would have handed the bridge a proper array and gone green, while a real MCP
///     call was still being mangled one layer up. Testing the wire is not testing the surface.
///
///  2. It exercises the arguments a real caller reaches for — region, repeat, an unknown one — and
///     not just the happy path it was written from. Both bugs shipped because nobody ever passed
///     them; a self-test only covers the imagination of whoever wrote it.
public static class SelfTest
{
    public static async Task<int> RunAsync(Config cfg)
    {
        void L(string s) => Console.Error.WriteLine("[selftest] " + s);
        var fails = new List<string>();

        // --- static: schema vs honoured-args, no engine required. This is the check that makes
        // the whole B2/B3 class impossible rather than merely fixed. ---
        var contract = McpServer.ContractErrors();
        if (contract.Count > 0) { foreach (var e in contract) L("CONTRACT " + e); fails.AddRange(contract); }
        else L("contract OK — every tool's schema matches exactly what it honours");

        var bridge = new BridgeClient(cfg.Host, cfg.Port);
        var launcher = new GodotLauncher();
        try
        {
            if (cfg.Mode == "launch") { L("launching game with --gth-serve…"); launcher.Launch(cfg); }
            await bridge.ConnectAsync(TimeSpan.FromSeconds(cfg.Mode == "launch" ? 30 : 8));
            L($"connected to ws://{cfg.Host}:{cfg.Port}");

            // Route every call the way a model's tool call actually travels: through Map().
            async Task<JsonNode?> Tool(string name, JsonObject args)
            {
                var (method, bp) = McpServer.Map(name, args);
                return await bridge.CallAsync(method, bp);
            }

            // In serve mode the bridge listens from autoload time — give the main scene a moment to build.
            await Tool("wait_for", new JsonObject { ["ms"] = 700 });

            var snap = await Tool("snapshot", new JsonObject());
            L($"snapshot → {snap?["count"]} interactable elements");

            var q = await Tool("query_element", new JsonObject { ["test_id"] = "btn-step" });
            L($"query btn-step → clickable={q?["clickable"]} on_screen={q?["on_screen"]} rect_px={q?["rect_px"]?.ToJsonString()}");

            var before = await Tool("read_element", new JsonObject { ["test_id"] = "clock" });
            await Tool("click_element", new JsonObject { ["test_id"] = "btn-step" });
            var after = await Tool("read_element", new JsonObject { ["test_id"] = "clock" });
            L($"click btn-step → clock {before?["text"]}  ⇒  {after?["text"]}");
            if (after?["text"]?.GetValue<string>() == before?["text"]?.GetValue<string>())
                fails.Add("click_element did not advance the sim");

            var hits = await Tool("hit_test", new JsonObject { ["x"] = 0.15, ["y"] = 0.5 });
            L($"hit_test (0.15,0.5) → viewport={hits?["viewport"]} consumer={hits?["consumer"]}");

            var cap = await Tool("capture", new JsonObject { ["label"] = "mcp-selftest", ["settle_ms"] = 300 });
            L($"capture → changed={cap?["changed"]} path={cap?["path"]}");

            // GTH.B2 — `region`: the argument that used to arrive at capturer.gd as a String and
            // throw. Crossing the real wire, through the real mapping, with the real shape.
            var rcap = await Tool("capture", new JsonObject
            {
                ["label"] = "mcp-selftest-region",
                ["region"] = new JsonArray(0, 0, 320, 100),
                ["if_changed"] = false,
            });
            if (rcap?["error"] is not null) fails.Add($"capture region → {rcap["error"]}");
            else
            {
                var w = rcap?["w"]?.GetValue<int>() ?? -1;
                L($"capture region [0,0,320,100] → {w}x{rcap?["h"]}");
                if (w != 320) fails.Add($"region crop returned w={w}, wanted 320");
            }

            // GTH.B3 — `repeat`: dropped by Pick() and reported as success for the whole of v0.
            var keyed = await Tool("press_key", new JsonObject { ["keys"] = "F9", ["repeat"] = 2 });
            var got = keyed?["repeat"]?.GetValue<int>() ?? -1;
            if (got != 2) fails.Add($"press_key repeat=2 → the engine reports {got}");
            else L("press_key repeat=2 → engine confirms 2 presses");

            // The silent-drop guard itself: an argument we do not honour must come back named.
            var unknown = McpServer.Unknown("capture", new JsonObject { ["label"] = "x", ["bogus_arg"] = 1 });
            if (!unknown.Contains("bogus_arg")) fails.Add("an unrecognised argument was NOT reported back");
            else L("unknown-arg guard OK — 'bogus_arg' is named, not swallowed");

            // GTH.B5/B6 — the window guard.
            var win = await Tool("window_state", new JsonObject());
            L($"window_state → mode={win?["mode"]} size={win?["size_px"]?.ToJsonString()} resize_locked={win?["resize_locked"]}");
            if (win?["resize_locked"]?.GetValue<bool>() != true)
                fails.Add("the window is not resize-locked while the harness is active");

            if (fails.Count == 0)
            {
                L("OK — live round-trip works (the click advanced the sim), contract holds, "
                  + "region/repeat/window verified");
                return 0;
            }
            foreach (var f in fails) L("FAIL " + f);
            return 1;
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
}
