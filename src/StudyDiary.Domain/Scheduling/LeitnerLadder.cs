// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.Collections.Immutable;

namespace StudyDiary.Domain.Scheduling;

/// <summary>
/// The Leitner ladder as *data* (DESIGN.md §3): an initial delay plus one
/// interval per box. Reshaping a box is an edit to this list — never an
/// `if (box == 2)` branch in the scheduler.
/// </summary>
public sealed record LeitnerLadder(
    ReviewInterval InitialDelay,
    ImmutableArray<ReviewInterval> BoxIntervals)
{
    /// <summary>The shipped default ladder: 1d / 7d / 1m / 6m / 1y, box 5 caps.</summary>
    public static LeitnerLadder Default { get; } = new(
        InitialDelay: new ReviewInterval(1, IntervalUnit.Day),
        BoxIntervals:
        [
            new ReviewInterval(1, IntervalUnit.Day),    // Box 1
            new ReviewInterval(7, IntervalUnit.Day),    // Box 2
            new ReviewInterval(1, IntervalUnit.Month),  // Box 3
            new ReviewInterval(6, IntervalUnit.Month),  // Box 4
            new ReviewInterval(1, IntervalUnit.Year)    // Box 5 (cap)
        ]);

    /// <summary>Highest box number (the cap). Boxes are 1-based.</summary>
    public int MaxBox => BoxIntervals.Length;

    /// <summary>
    /// The waiting interval for a given box. Boxes are 1-based while the array is
    /// 0-indexed; this method is the only place that translation happens.
    /// </summary>
    public ReviewInterval IntervalForBox(int box)
    {
        if (box < 1 || box > MaxBox)
            throw new ArgumentOutOfRangeException(
                nameof(box), box, $"Box must be between 1 and {MaxBox}.");

        return BoxIntervals[box - 1];
    }
}