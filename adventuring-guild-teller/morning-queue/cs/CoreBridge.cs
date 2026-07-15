using Godot;

namespace MorningQueue;

/// <summary>
/// Stub bridge proving the game (Godot) assembly compiles with a
/// ProjectReference to MorningQueue.Core. Not yet wired to any scene.
/// </summary>
public partial class CoreBridge : RefCounted
{
    public static string Ping() => "pong";
}
