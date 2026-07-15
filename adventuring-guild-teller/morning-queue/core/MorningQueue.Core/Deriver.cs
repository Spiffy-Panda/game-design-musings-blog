namespace MorningQueue.Core;

/// <summary>
/// The one home for the "weighed amount vs the claimed standing order's limit" rule.
///
/// This logic was duplicated at ShiftGenerator._limit_result and ReferencePanel's
/// _scale_comparison (~622-666). The derive pass annotates each visit's
/// inspections.scale.verdict with the result so nothing has to recompute it downstream.
///
/// The verdict is judged PURELY from the weighed amount against the claimed order's limit
/// — it never consults the authored `relevant` flag (that stays design-only metadata).
/// </summary>
public static class ScaleVerdict
{
    public const string Within = "within";
    public const string Over = "over";
    public const string Under = "under";
    public const string Meets = "meets";
    public const string NoOrder = "no_order";
}

public static class Deriver
{
    /// <summary>
    /// Derive the scale verdict for a weighed amount against a claimed order.
    ///   * no claimed order, or an order carrying no accept/total limit  -> no_order
    ///   * nothing on the pan (amount null)                              -> no_order
    ///   * a unit that does not match the order's unit                   -> no_order
    ///   * accept window: below min -> under, above max -> over, else within
    ///   * total order:   at-or-above needed -> meets, else under
    /// </summary>
    public static string DeriveScaleVerdict(Posting? order, double? amount, string? unit)
    {
        if (order is null || !order.HasLimit)
            return ScaleVerdict.NoOrder;
        if (amount is null)
            return ScaleVerdict.NoOrder;

        double a = amount.Value;

        if (order.Accept is { } acc)
        {
            if (!string.Equals(acc.Unit, unit))
                return ScaleVerdict.NoOrder;
            if (a < acc.Min) return ScaleVerdict.Under;
            if (a > acc.Max) return ScaleVerdict.Over;
            return ScaleVerdict.Within;
        }

        if (order.Total is { } tot)
        {
            if (!string.Equals(tot.Unit, unit))
                return ScaleVerdict.NoOrder;
            return a >= tot.Needed ? ScaleVerdict.Meets : ScaleVerdict.Under;
        }

        return ScaleVerdict.NoOrder;
    }
}
