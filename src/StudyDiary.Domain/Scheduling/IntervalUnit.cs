// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Domain.Scheduling;

/// <summary>The calendar unit an interval is measured in.</summary>
public enum IntervalUnit
{
    Day = 0,
    Month = 1,
    Year = 2
}
