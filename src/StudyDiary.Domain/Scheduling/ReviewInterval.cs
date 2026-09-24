// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Domain.Scheduling;

/// <summary>
/// A waiting period expressed in calendar terms (e.g. 7 days, 6 months).
/// Calendar-aware on purpose: "1 month" means AddMonths(1), not "30 days",
/// so review dates land on sensible days instead of drifting over years.
/// </summary>
public sealed record ReviewInterval(int Count, IntervalUnit Unit)
{
    private readonly int _count = ValidatedCount(Count);
    private readonly IntervalUnit _unit = ValidatedUnit(Unit);

    public int Count
    {
        get => _count;
        init => _count = ValidatedCount(value);
    }

    public IntervalUnit Unit
    {
        get => _unit;
        init => _unit = ValidatedUnit(value);
    }

    private static int ValidatedCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return count;
    }

    private static IntervalUnit ValidatedUnit(IntervalUnit unit)
    {
        if (!Enum.IsDefined(unit))
            throw new ArgumentOutOfRangeException(
                nameof(unit), unit, "Unknown interval unit.");
        
        return unit;
    }

    /// <summary>Advance a whole-day date by this interval.</summary>
    public DateOnly AddTo(DateOnly date) => Unit switch
    {
        IntervalUnit.Day   => date.AddDays(Count),
        IntervalUnit.Month => date.AddMonths(Count),
        IntervalUnit.Year  => date.AddYears(Count),
        // Construction rejects unknown units; the compiler still wants this arm.
        _ => throw new ArgumentOutOfRangeException(nameof(Unit), Unit, "Unknown interval unit.")
    };
}

