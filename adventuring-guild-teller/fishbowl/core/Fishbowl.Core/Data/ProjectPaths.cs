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

    /// <summary>The live town: every feature enabled, and where postings/outings are authored
    /// (PNO.D2). The observatory and the CLI both default here.</summary>
    public static string DataDir() => Path.Combine(FishbowlRoot(), "data");

    /// <summary>
    /// The frozen golden fixture — a posting-free town kept deliberately apart from <see cref="DataDir"/>
    /// so it cannot drift with the live data it exists to pin (PNO.D2, ruled 2026-07-16).
    /// <para>
    /// It is the sole town behind the seed-independence invariant, the 12/6/2 count pins, and the
    /// golden day's 7 beats. <b>Do not add postings, sites, or cast to it</b> — its whole value is
    /// that it stopped changing. New features are exercised against <see cref="DataDir"/>.
    /// </para>
    /// </summary>
    public static string GoldenTownDir() => Path.Combine(FishbowlRoot(), "tests", "towns", "golden-town");
}
