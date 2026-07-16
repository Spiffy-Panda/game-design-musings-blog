using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace GthMcp;

/// Loopback WebSocket client to the in-Godot GTH Bridge (addons/gd_test_harness/bridge.gd).
/// Sends line JSON-RPC {id, method, params}; awaits the {id, result} reply. Calls are serialized
/// (the Bridge processes one at a time), so send-then-receive per call under one gate is enough.
public sealed class BridgeClient : IAsyncDisposable
{
    private readonly Uri _uri;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ClientWebSocket? _ws;
    private int _id;

    public BridgeClient(string host, int port) => _uri = new Uri($"ws://{host}:{port}");
    public bool Connected => _ws is { State: WebSocketState.Open };

    public async Task ConnectAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var ws = new ClientWebSocket();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                await ws.ConnectAsync(_uri, cts.Token);
                _ws = ws;
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                try { await Task.Delay(250, ct); } catch (OperationCanceledException) { throw; }
            }
        }
        throw new InvalidOperationException($"could not connect to {_uri} within {timeout.TotalSeconds:0}s", last);
    }

    public async Task<JsonNode?> CallAsync(string method, JsonNode? @params, CancellationToken ct = default)
    {
        if (_ws is not { State: WebSocketState.Open })
            throw new InvalidOperationException("bridge not connected — run session_start (or launch the game with --gth-serve)");
        await _gate.WaitAsync(ct);
        try
        {
            var id = Interlocked.Increment(ref _id);
            var req = new JsonObject { ["id"] = id, ["method"] = method, ["params"] = @params ?? new JsonObject() };
            await _ws.SendAsync(Encoding.UTF8.GetBytes(req.ToJsonString()), WebSocketMessageType.Text, true, ct);
            var reply = JsonNode.Parse(await ReceiveAsync(ct));
            return reply?["result"];
        }
        finally { _gate.Release(); }
    }

    private async Task<string> ReceiveAsync(CancellationToken ct)
    {
        var buf = new byte[16 * 1024];
        using var ms = new MemoryStream();
        while (true)
        {
            var res = await _ws!.ReceiveAsync(buf, ct);
            if (res.MessageType == WebSocketMessageType.Close)
                throw new InvalidOperationException("bridge closed the connection");
            ms.Write(buf, 0, res.Count);
            if (res.EndOfMessage) break;
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public async ValueTask DisposeAsync()
    {
        if (_ws is { State: WebSocketState.Open })
            try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); }
            catch { /* ignore */ }
        _ws?.Dispose();
    }
}
