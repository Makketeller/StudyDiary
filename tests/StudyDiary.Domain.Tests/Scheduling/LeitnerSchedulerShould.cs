// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Domain.Tests.Scheduling;

public class LeitnerSchedulerShould
{
    private static readonly DateOnly ReviewedOn = new(2026, 9, 2);
    private static readonly DateOnly SomeEarlierDay = ReviewedOn.AddDays(-30);
    private readonly LeitnerScheduler _scheduler = new();

    // DESIGN §3 — box transitions

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(4, 5)]
    public void PromoteOneBoxOnPass(int before, int after)
    {
        var state = new ReviewState(before, SomeEarlierDay);

        var advanced = _scheduler.Advance(
            state, ReviewOutcome.Pass, ReviewedOn, LeitnerLadder.Default);

        Assert.Equal(after, advanced.Box);
    }

    [Fact]
    public void HoldAtTheCapOnPass()
    {
        var cap = LeitnerLadder.Default.MaxBox;
        var state = new ReviewState(cap, SomeEarlierDay);

        var advanced = _scheduler.Advance(
            state, ReviewOutcome.Pass, ReviewedOn, LeitnerLadder.Default);
        
        Assert.Equal(cap, advanced.Box);
    }

    [Fact]
    public void TakeTheCapFromTheLadderRatherThanALiteralFive()
    {
        var twoBoxes = new LeitnerLadder([
            new ReviewInterval(1, IntervalUnit.Day),
            new ReviewInterval(7, IntervalUnit.Day)
        ]);
        var state = new ReviewState(2, SomeEarlierDay);
    
        var advanced = _scheduler.Advance(
            state, ReviewOutcome.Pass, ReviewedOn, twoBoxes);
    
        Assert.Equal(2, advanced.Box);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void ReturnToBoxOneOnFail(int before)
    {
        var state = new ReviewState(before, SomeEarlierDay);

        var advanced = _scheduler.Advance(
            state, ReviewOutcome.Fail, ReviewedOn, LeitnerLadder.Default);
    
        Assert.Equal(1, advanced.Box);
    }

    [Theory]
    [InlineData(ReviewOutcome.Pass)]
    [InlineData(ReviewOutcome.Fail)]
    public void AnchorEnteredOnToTheActualReviewDate(ReviewOutcome outcome)
    {
        var state = new ReviewState(2, SomeEarlierDay);

        var advanced = _scheduler.Advance(
            state, outcome, ReviewedOn, LeitnerLadder.Default);

        Assert.Equal(ReviewedOn, advanced.EnteredOn);
    }

    [Fact]
    public void RejectAnUnknownOutcome()
    {
        var state = new ReviewState(1, SomeEarlierDay);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _scheduler.Advance(
                state, (ReviewOutcome)99, ReviewedOn, LeitnerLadder.Default));
    }

    // DESIGN §4 — readiness

    [Fact]
    public void NotBeReadyTheDayBeforeTheIntervalElapses()
    {
        var state = new ReviewState(2, ReviewedOn); // box 2 waits 7 days
        
        Assert.False(_scheduler.IsReady(
            state, ReviewedOn.AddDays(6), LeitnerLadder.Default));
    }

    [Fact]
    public void BeReadyOnTheExactDayTheIntervalElapses()
    {
        var state = new ReviewState(2, ReviewedOn); 
        
        Assert.True(_scheduler.IsReady(
            state, ReviewedOn.AddDays(7), LeitnerLadder.Default));
    }

    [Fact]
    public void StayReadyLongAfterTheIntervalElapses()
    {
        var state = new ReviewState(2, ReviewedOn); 
        
        Assert.True(_scheduler.IsReady(
            state, ReviewedOn.AddDays(400), LeitnerLadder.Default));
    }

    [Fact]
    public void TakeTheFailIntervalFromBoxOneOfTheLadderInUse()
    {
        var slowFirstBox = new LeitnerLadder([
            new ReviewInterval(3, IntervalUnit.Day),
            new ReviewInterval(7, IntervalUnit.Day)
        ]);

        var failed = _scheduler.Advance(
            new ReviewState(2, SomeEarlierDay),
            ReviewOutcome.Fail,
            ReviewedOn,
            slowFirstBox);

        Assert.False(_scheduler.IsReady(failed, ReviewedOn.AddDays(2), slowFirstBox));
        Assert.True(_scheduler.IsReady(failed, ReviewedOn.AddDays(3), slowFirstBox));
    }
    
}
