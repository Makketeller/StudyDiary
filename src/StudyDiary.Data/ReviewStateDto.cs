// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;
using System.Text.Json.Serialization;

namespace StudyDiary.Data;

/// <summary>
/// <see cref="ReviewState"/> as it sits on disk: settable
/// properties, built empty and filled in. Nests inside <c>EntryDto</c>,
/// because box-and-entered-day is one thing in DESIGN §3 and stays one
/// thing in the file (ARCHITECTURE).
/// </summary>
internal sealed class ReviewStateDto
{
    [JsonRequired]
    public int Box { get; set; }

    [JsonRequired]
    public DateOnly EnteredOn { get; set; }
}
