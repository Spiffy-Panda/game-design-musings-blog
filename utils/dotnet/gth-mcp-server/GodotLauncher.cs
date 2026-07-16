using System.Diagnostics;

namespace GthMcp;

/// Optional lifecycle: in "launch" mode the server starts the game with `--gth-serve` and stops it
/// on shutdown. In "attach" mode this is unused (the bridge is already serving).
public sealed class GodotLauncher
{
    private Process? _proc;
    public bool Launched => _proc is { HasExited: false };

    public void Launch(Config cfg)
    {
        if (string.IsNullOrEmpty(cfg.GodotExe) || string.IsNullOrEmpty(cfg.ProjectPath))
            throw new InvalidOperationException("launch mode needs GTH_GODOT_EXE and GTH_PROJECT");
        var psi = new ProcessStartInfo(cfg.GodotExe) { UseShellExecute = false };
        psi.ArgumentList.Add("--path");
        psi.ArgumentList.Add(cfg.ProjectPath);
        psi.ArgumentList.Add("--");                       // engine args end; user args follow
        psi.ArgumentList.Add("--gth-serve");
        psi.ArgumentList.Add($"--gth-port={cfg.Port}");
        _proc = Process.Start(psi);
    }

    public void Stop()
    {
        try { if (_proc is { HasExited: false }) _proc.Kill(entireProcessTree: true); }
        catch { /* ignore */ }
        _proc = null;
    }
}
