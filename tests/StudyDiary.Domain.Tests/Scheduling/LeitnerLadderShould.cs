// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Domain.Tests.Scheduling;

public class LeitnerLadderShould
{
    [Fact]
    public void EqualAnotherLadderWithTheSameIntervals()
    {
        var a = new LeitnerLadder([
            new ReviewInterval(1, IntervalUnit.Day),
            new ReviewInterval(7, IntervalUnit.Day)
        ]);

        var b = new LeitnerLadder([
            new ReviewInterval(1, IntervalUnit.Day),
            new ReviewInterval(7, IntervalUnit.Day)
        ]);

        Assert.Equal(a, b);
    }

    [Fact]
    public void NotEqualALadderWithReoreredIntervals()
    {
        var a = new LeitnerLadder([
            new ReviewInterval(1, IntervalUnit.Day),
            new ReviewInterval(7, IntervalUnit.Day)
        ]);

        var b = new LeitnerLadder([
            new ReviewInterval(7, IntervalUnit.Day),
            new ReviewInterval(1, IntervalUnit.Day)
        ]);

        Assert.NotEqual(a, b);        
    }

    [Fact]
    public void CollapseToOneEntryInAHashSet()
    {
        var a = new LeitnerLadder([
            new ReviewInterval(1, IntervalUnit.Day),
            new ReviewInterval(7, IntervalUnit.Day)
        ]);

        var b = new LeitnerLadder([
            new ReviewInterval(1, IntervalUnit.Day),
            new ReviewInterval(7, IntervalUnit.Day)
        ]);

        var ladders = new HashSet<LeitnerLadder> { a, b };
        Assert.Single(ladders);  
    }
}
