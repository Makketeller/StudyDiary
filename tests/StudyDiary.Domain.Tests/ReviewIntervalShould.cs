// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Domain.Tests.Scheduling;

public class ReviewIntervalShould
{   
    [Fact]
    public void PreserveCountGivenToConstructor()
    {
        var interval = new ReviewInterval(7, IntervalUnit.Day);

        Assert.Equal(7, interval.Count);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-7)]
    public void RejectNegativeCount(int count) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReviewInterval(count, IntervalUnit.Day));

    [Fact]
    public void RejectNegativeCountViaWith()
    {
        var valid = new ReviewInterval(7,IntervalUnit.Day);
    
        Assert.Throws<ArgumentOutOfRangeException>(
            () => valid with { Count = -1});
    }

    [Fact]
    public void PermitZeroCount()
    {
        var interval = new ReviewInterval(0, IntervalUnit.Day);
        var date = new DateOnly(2026, 8, 13);

        Assert.Equal(date, interval.AddTo(date));
    }
}