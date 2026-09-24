// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Data;

/// <summary>
/// One review that happened: the day, the outcome, the boxes either side,
/// and whether it was free practice (DESIGN §7). Public because App
/// constructs it; <c>isPractice</c> is why it cannot live in Domain
/// (ARCHITECTURE). Carries no entry id - it nests inside its entry on
/// disk, so ownership is structural.
/// </summary>
public sealed record ReviewRecord(
    DateOnly ReviewedOn,
    ReviewOutcome Outcome,
    int BoxBefore,
    int BoxAfter,
    bool IsPractice)
{
    private readonly ReviewOutcome _outcome = ValidatedOutcome(Outcome);
    private readonly int _boxBefore = ValidatedBox(BoxBefore, nameof(BoxBefore));
    private readonly int _boxAfter = ValidatedBox(BoxAfter, nameof(BoxAfter));

    public ReviewOutcome Outcome
    {
        get => _outcome;
        init => _outcome = ValidatedOutcome(value);
    }
    
    public int BoxBefore
    {
        get => _boxBefore;
        init => _boxBefore = ValidatedBox(value, nameof(BoxBefore));
    }

    public int BoxAfter
    {
        get => _boxAfter;
        init => _boxAfter = ValidatedBox(value, nameof(BoxAfter));
    }

    private static ReviewOutcome ValidatedOutcome(ReviewOutcome outcome)
    {
        if (!Enum.IsDefined(outcome))
            throw new ArgumentOutOfRangeException(
                nameof(Outcome), outcome, "Unknown review outcome.");
        
        return outcome;      
    }

    private static int ValidatedBox(int box, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(box, 1, paramName);
        return box;
    }
}
