// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Data.Tests;

public class FixedTimeProviderShould
{
    // Half past midnight at +09:00: an offset no Swedish machine has, and
    // still 1 October in UTC, so code reading the wrong clock gets the
    // wrong day.
    private static readonly DateTimeOffset Instant =
        new(2026, 10, 2, 0, 30, 0, TimeSpan.FromHours(9));

    [Fact]
    public void ReportTheGivenInstantInItsOwnOffset()
    {
        var clock = new FixedTimeProvider(Instant);

        var now = clock.GetLocalNow();

        Assert.Equal(Instant, now);
        Assert.Equal(Instant.Offset, now.Offset);
    }
}
