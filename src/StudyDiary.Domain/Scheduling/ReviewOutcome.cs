// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Domain.Scheduling;

/// <summary>The result of a review: binary pass/fail (DESIGN §3). </summary>
public enum ReviewOutcome
{
    Fail = 0,
    Pass = 1
}