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
/// <see cref="ReviewRecord"/> as it sits on disk: settable
/// properties, built empty and filled in. The reader refuses an outcome
/// that is not a member name, as it refuses any unknown or repeated key
/// (ARCHITECTURE). A box number below one still parses, and fails in the
/// mapping, where the file and entry can be named.
/// </summary>
internal sealed class ReviewRecordDto
{
    [JsonRequired]
    public DateOnly ReviewedOn { get; set; }

    [JsonRequired]
    public ReviewOutcome Outcome { get; set; }

    [JsonRequired]
    public int BoxBefore { get; set; }

    [JsonRequired]
    public int BoxAfter { get; set; }

    [JsonRequired]
    public bool IsPractice { get; set; }
}
