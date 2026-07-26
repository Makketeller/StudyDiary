namespace StudyDiary.Domain.Scheduling;

/// <summary>
/// A waiting period expressed in calendar terms (e.g. 7 days, 6 months).
/// Calendar-aware on purpose: "1 month" means AddMonths(1), not "30 days",
/// so review dates land on sensible days instead of drifting over years.
/// </summary>
public sealed record ReviewInterval(int Count, IntervalUnit Unit)
{
    /// <summary>Advance a whole-day date by this interval.</summary>
    public DateOnly AddTo(DateOnly date) => Unit switch
    {
        IntervalUnit.Day   => date.AddDays(Count),
        IntervalUnit.Month => date.AddMonths(Count),
        IntervalUnit.Year  => date.AddYears(Count),
        _ => throw new ArgumentOutOfRangeException(nameof(Unit), Unit, "Unknown interval unit.")
    };
}

