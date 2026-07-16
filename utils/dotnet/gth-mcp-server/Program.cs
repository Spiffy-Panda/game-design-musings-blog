using System.Text;
using GthMcp;

// GTH MCP server entry. `--selftest` runs the live round-trip check and exits; otherwise it speaks
// MCP over stdio (UTF-8, no BOM) and bridges tool calls to the in-Godot GTH Bridge.

var cfg = Config.FromEnv();

if (args.Contains("--selftest"))
    return await SelfTest.RunAsync(cfg);

// Own the standard streams explicitly as UTF-8 without BOM — stdout is the JSON-RPC channel.
var stdin = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false };

var bridge = new BridgeClient(cfg.Host, cfg.Port);
var launcher = new GodotLauncher();
var server = new McpServer(cfg, bridge, launcher, stdin, stdout);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try { await server.RunAsync(cts.Token); }
finally { await bridge.DisposeAsync(); launcher.Stop(); }
return 0;
