// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Domain.Tests.Scheduling;

public class ReviewStateShould
{
    private static readonly DateOnly AnyDay = new(2026, 9, 2);

    [Fact]
    public void PreserveBoxGivenToConstructor()
    {
        var state = new ReviewState(3, AnyDay);
    
        Assert.Equal(3, state.Box);
    }

    [Fact]
    public void PreserveEnteredOnGivenToConstructor()
    {
        var state = new ReviewState(1, AnyDay);

        Assert.Equal(AnyDay, state.EnteredOn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectBoxBelowOne(int box) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ReviewState(box, AnyDay));

    [Fact]
    public void RejectBoxBelowOneViaWith()
    {
        var valid = new ReviewState(1, AnyDay);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => valid with { Box = 0});
    }
}