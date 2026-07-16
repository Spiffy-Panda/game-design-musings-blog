namespace Fishbowl.Core.Data;

/// <summary>
/// Locates the fishbowl project root by walking up from the running assembly until it finds
/// <c>Fishbowl.sln</c> — so the CLI and tests resolve <c>data/</c> from any working directory
/// (CLAUDE.md Rule 1: anchor to a stable marker, never the invocation CWD).
/// </summary>
public static class ProjectPaths
{
    public static string FishbowlRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Fishbowl.sln"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate Fishbowl.sln above the running assembly.");
    }

    public static string DataDir() => Path.Combine(FishbowlRoot(), "data");
}
