using Fishbowl.Core.Engine;

namespace Fishbowl.Core.Text;

/// <summary>
/// The actionability dial (AGR.3): one continuous knob [0,1] that snaps to three authored
/// registers. "Quotable, barely actionable" at one end; a precise report at the other. The
/// prototype exists partly to let Panda pick a shipping default by feel (VFB.Q5).
/// </summary>
public enum Register { Hearsay, Gossip, Report }

public static class Actionability
{
    public static Register Of(double dial) => dial switch
    {
        < 1.0 / 3.0 => Register.Hearsay,
        < 2.0 / 3.0 => Register.Gossip,
        _ => Register.Report,
    };

    public static string Pick(ChronicleEntry e, double dial) => Of(dial) switch
    {
        Register.Hearsay => e.Hearsay,
        Register.Gossip => e.Gossip,
        _ => e.Report,
    };
}
