// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Domain.Scheduling;

/// <summary>
/// An entry's scheduling state: which box it sits in, and the day it entered
/// that box. The next review date is *derived* from these plus the ladder
/// (DESIGN §3) and is deliberately not stored - See ARCHITECTURE.
/// </summary>
public sealed record ReviewState(int Box, DateOnly EnteredOn)
{
    private readonly int _box = Validated(Box);

    public int Box
    {
        get => _box;
        init => _box = Validated(value);
    }


    private static int Validated(int box)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(box, 1);
        return box;
    }
}