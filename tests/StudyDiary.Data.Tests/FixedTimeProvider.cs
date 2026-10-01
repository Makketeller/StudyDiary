// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Data.Tests;

/// <summary>
/// A clock that never moves, in a time zone that never changes, so no
/// test depends on when or where it runs (ARCHITECTURE). The zone is a
/// fixed offset taken from the instant it is given, with no daylight
/// saving.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;
    private readonly TimeZoneInfo _zone;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
        _zone = TimeZoneInfo.CreateCustomTimeZone(
            "Fixed", now.Offset, "Fixed", "Fixed");
    }

    public override DateTimeOffset GetUtcNow() => _now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => _zone;
}
