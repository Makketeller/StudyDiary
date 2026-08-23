// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Domain.Scheduling;

/// <summary>Classic Leitner: pass promotes one box, fail returns to box 1 (DESIGN §3);
public sealed class LeitnerScheduler : IReviewScheduler
{
    public ReviewState Advance(
        ReviewState current,
        ReviewOutcome outcome,
        DateOnly reviewedOn,
        LeitnerLadder ladder)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(ladder);

        var box = outcome switch
        {
            ReviewOutcome.Pass => Math.Min(current.Box + 1, ladder.MaxBox),
            ReviewOutcome.Fail => 1,
            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome), outcome, "Unknown review outcome.")
        };

        return new ReviewState(box, reviewedOn); 
    }

    public bool IsReady(ReviewState state, DateOnly today, LeitnerLadder ladder)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ladder);

        return NextReviewOn(state, ladder) <= today;
    }

    private static DateOnly NextReviewOn(ReviewState state, LeitnerLadder ladder) =>
        ladder.IntervalForBox(state.Box).AddTo(state.EnteredOn);
}