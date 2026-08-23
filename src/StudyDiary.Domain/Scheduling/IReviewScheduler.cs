// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Domain.Scheduling;

/// <summary>
/// Turns a review into new sceduling state, and dedices readiness.
/// Never sees an <c>Entry</c>: content type and review shape cannot reach it,
/// which is what makes DESIGN §2's invariant structural (ARCHITECTURE).
/// Never reads the clock - dates arrive as parameters.
/// </summary>
public interface IReviewScheduler
{
    /// <summary>The state after reviewing on <paramref name="reviewedOn"/>.</summary>
    ReviewState Advance(
        ReviewState current,
        ReviewOutcome outcome,
        DateOnly reviewedOn,
        LeitnerLadder ladder);

    /// <summary> Whether this state is ready for review on the given day.</summary>
    bool IsReady(ReviewState state, DateOnly today, LeitnerLadder ladder);
}