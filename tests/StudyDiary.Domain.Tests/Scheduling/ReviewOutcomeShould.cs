// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Domain.Tests.Scheduling;

public class ReviewOutcomeShould
{
    [Theory]
    [InlineData(ReviewOutcome.Fail, 0)]
    [InlineData(ReviewOutcome.Pass, 1)]
    public void KeepItsPinnedIntegerValue(ReviewOutcome outcome, int pinned) => 
        Assert.Equal(pinned, (int)outcome);
}