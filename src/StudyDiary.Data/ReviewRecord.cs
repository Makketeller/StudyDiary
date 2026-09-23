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
    private readonly int _boxBefore = Validated(BoxBefore, nameof(BoxBefore));
    private readonly int _boxAfter = Validated(BoxAfter, nameof(BoxAfter));

    public int BoxBefore
    {
        get => _boxBefore;
        init => _boxBefore = Validated(value, nameof(BoxBefore));
    }

    public int BoxAfter
    {
        get => _boxAfter;
        init => _boxAfter = Validated(value, nameof(BoxAfter));
    }
    private static int Validated(int box, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(box, 1, paramName);
        return box;
    }
}
