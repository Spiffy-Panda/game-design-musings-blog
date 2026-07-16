using Fishbowl.Core.Model;

namespace Fishbowl.Core.Engine;

/// <summary>
/// The single-threaded tick driver. The engine steps only via <see cref="StepSlot"/> /
/// <see cref="RunToDawn"/> — never wall-clock or a _process loop (PLAN "Determinism"). Order
/// within a slot: pressures drift, then storylets fire, then the slot signal. At the day
/// boundary the day-hash is taken and the summary's hearsay gate is sealed (both over the
/// just-finished day's state and occupancy) before the next day resolves. The hash is taken
/// first and the summary is not in it, so the summary is causally downstream of determinism.
/// </summary>
public sealed class Simulation
{
    public World World { get; }

    public event Action<int, int>? SlotTicked;                          // (day, slot)
    public event Action<ChronicleEntry>? EventLogged;
    public event Action<int, IReadOnlyList<SummaryLine>>? DawnReady;     // (day, summary)
    public event Action<int, string>? HashReady;                        // (day, hash)

    public Simulation(Town town) => World = World.Build(town);
    public Simulation(World world) => World = world;

    public void StepSlot()
    {
        int day = World.Day, slot = World.Slot;
        Pressures.DriftSlot(World, slot);
        StoryletEngine.RunSlot(World, slot, e => EventLogged?.Invoke(e));
        World.RecordPressures();
        SlotTicked?.Invoke(day, slot);

        World.Slot++;
        if (World.Slot >= World.SlotsPerDay) FinalizeDay(day);
    }

    /// <summary>Run the current day to its end, returning that day's dawn summary.</summary>
    public IReadOnlyList<SummaryLine> RunToDawn()
    {
        int day = World.Day;
        while (World.Day == day) StepSlot();
        return Summarizer.Render(World, day);   // derived, not stored — see Summarizer's remarks
    }

    public void RunDays(int n) { for (int i = 0; i < n; i++) RunToDawn(); }

    private void FinalizeDay(int day)
    {
        string hash = World.ComputeDayHash();
        World.DayHashes[day] = hash;
        HashReady?.Invoke(day, hash);

        // Dawn does the one occupancy-dependent phase and stops: seal hearsay-lite onto the day's
        // entries while this day's occupancy is still loaded (the next ResolveDay below replaces
        // it). Filter/take/pick are derived on read, so the rendering knobs re-present a finished
        // day live instead of being deferred to a dawn that changes the events underneath them.
        Summarizer.SealDay(World, day);
        DawnReady?.Invoke(day, Summarizer.Render(World, day));

        // Advance. ResolveDay re-seeds the day's streams, applies departure schedule, and
        // rebuilds occupancy for the new day.
        World.Day = day + 1;
        World.Slot = 0;
        Clockwork.ResolveDay(World);
    }
}
