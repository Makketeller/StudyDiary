// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Data.Tests;

public class ReviewRecordShould
{
    private static readonly DateOnly ReviewedOn = new(2026, 9, 15);

    [Fact]
    public void PreserveEveryValueGivenToConstructor()
    {
        var record = new ReviewRecord(
            ReviewedOn, ReviewOutcome.Pass, 2, 3, false);

        Assert.Equal(ReviewedOn, record.ReviewedOn);
        Assert.Equal(ReviewOutcome.Pass, record.Outcome);
        Assert.Equal(2, record.BoxBefore);
        Assert.Equal(3, record.BoxAfter);
        Assert.False(record.IsPractice);   
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void RejectBoxBelowOne(int before, int after) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReviewRecord(
                ReviewedOn, ReviewOutcome.Pass, before, after, false));
    
    [Fact]
    public void RejectBoxBelowOneViaWith()
    {
        var valid = new ReviewRecord(
            ReviewedOn, ReviewOutcome.Pass, 1, 2, false);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => valid with { BoxBefore = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(
            () => valid with { BoxAfter = 0});
    }

    [Fact]
    public void EqualAnotherRecordWithTheSameValues()
    {
        var a = new ReviewRecord(
            ReviewedOn, ReviewOutcome.Pass, 1, 2, false);
        var b = new ReviewRecord(
            ReviewedOn, ReviewOutcome.Pass, 1, 2, false);

        Assert.Equal(a, b);
    }

    [Fact]
    public void PermitAnUnchangedBoxForAPracticeRep()
    {
        var record = new ReviewRecord(
            ReviewedOn, ReviewOutcome.Pass, 3, 3, true);

        Assert.Equal(record.BoxBefore, record.BoxAfter);
        Assert.True(record.IsPractice);
    }
}