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
    /// <summary>Advance a whole-day date by this interval.</summary>
    public DateOnly AddTo(DateOnly date) => Unit switch
    {
        IntervalUnit.Day   => date.AddDays(Count),
        IntervalUnit.Month => date.AddMonths(Count),
        IntervalUnit.Year  => date.AddYears(Count),
        _ => throw new ArgumentOutOfRangeException(nameof(Unit), Unit, "Unknown interval unit.")
    };
}

