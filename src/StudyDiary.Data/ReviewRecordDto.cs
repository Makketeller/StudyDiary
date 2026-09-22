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
/// <see cref="ReviewRecord"/> as it sits on disk: no validation, settable
/// properties, built empty and filled in. Lenient on purpose, so a bad
/// value in a hand-edited file fails in the mapping, where the file and
/// entry can be named, not inside the JSON reader.
/// </summary>
internal sealed class ReviewRecordDto
{
    public DateOnly ReviewedOn { get; set; }
    public ReviewOutcome Outcome { get; set; }
    public int BoxBefore { get; set; }
    public int BoxAfter { get; set; }
    public bool IsPractice { get; set; }
}