namespace GthMcp;

/// Runtime config, read from environment (set these in the MCP registration's `env`, or in the
/// shell for `--selftest`). Nothing is repo- or project-specific at build time — the server is a
/// generic driver; the project it drives is named entirely by config.
public sealed record Config(string Mode, string Host, int Port, string? GodotExe, string? ProjectPath)
{
    public static Config FromEnv()
    {
        // GTH_PROJECT may be relative (to CWD — the repo root when launched from a committed .mcp.json),
        // so a project-scoped config needs no private absolute path. Resolve it to absolute for Godot.
        var project = Environment.GetEnvironmentVariable("GTH_PROJECT");
        if (!string.IsNullOrEmpty(project)) project = Path.GetFullPath(project);

        // Default the Godot exe so the committed config carries no machine path either.
        var godot = Environment.GetEnvironmentVariable("GTH_GODOT_EXE");
        if (string.IsNullOrEmpty(godot)) godot = DefaultGodot();

        return new(
            Mode: Env("GTH_MODE", "attach"),             // "attach" (connect to a running bridge) | "launch"
            Host: Env("GTH_HOST", "127.0.0.1"),
            Port: int.TryParse(Env("GTH_PORT", "8787"), out var p) ? p : 8787,
            GodotExe: godot,
            ProjectPath: project);
    }

    // The GTH addon runs inside the game process; the fishbowl is .NET (mono), so this wants the mono
    // build (`godot.exe`). Standard Windows install location; override with GTH_GODOT_EXE if elsewhere.
    private static string? DefaultGodot()
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var guess = Path.Combine(pf, "godot", "godot.exe");
        return File.Exists(guess) ? guess : null;
    }

    private static string Env(string k, string d) =>
        Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : d;
}
