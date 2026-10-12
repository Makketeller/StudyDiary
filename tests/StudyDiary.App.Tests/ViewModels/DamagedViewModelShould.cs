// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.App.ViewModels;
using StudyDiary.Data;
using StudyDiary.Data.Tests;

namespace StudyDiary.App.Tests.ViewModels;

public class DamagedViewModelShould
{
    [Theory]
    [InlineData(9, "today at 14:30")]
    [InlineData(8, "yesterday at 14:30")]
    [InlineData(1, "2026-10-01 at 14:30")]
    public void SayWhenTheOfferedCopyWasTaken(int day, string when)
    {
        var damaged = new DamagedViewModel(
            ADetail(), KeptAt, AnOffer(new DateTime(2026, 10, day, 14, 30, 0)), AClock());

        Assert.Contains($"the most recent one is from {when}. Open that copy instead?", damaged.Message);
    }

    [Fact]
    public void SayNoCopyOpensWhenNoneIsOffered()
    {
        var damaged = new DamagedViewModel(ADetail(), KeptAt, offer: null, AClock());

        Assert.Contains("none of them could be opened either", damaged.Message);
        Assert.False(damaged.HasCopy);
        Assert.False(damaged.CanOpenCopy);
    }

    [Fact]
    public void LeaveOutWhereFilesWereKeptWhenNoneWere()
    {
        var damaged = new DamagedViewModel(ADetail(), keptAt: null, offer: null, AClock());

        Assert.Null(damaged.KeptAtMessage);
    }


    [Fact]
    public void SayTheReaderCountsLinesFromZero()
    {
        var damaged = new DamagedViewModel(
            new DamageDetail("payload.json", 12, "$.entries[3].title", "LineNumber: 11"),
            KeptAt, offer: null, AClock());

        Assert.Contains("Line: 12", damaged.Details);
        Assert.Contains("Reader's message (it counts lines from 0): LineNumber: 11", damaged.Details);
    }

    [Fact]
    public async Task OpenTheOfferedCopyOnlyOnce()
    {
        var opened = 0;
        var offer = new CopyOffer(
            new DateTime(2026, 10, 9, 14, 30, 0),
            () => { opened++; return Task.CompletedTask; });
        var damaged = new DamagedViewModel(ADetail(), KeptAt, offer, AClock());

        await damaged.OpenCopyAsync();

        Assert.Equal(1, opened);
        Assert.False(damaged.CanOpenCopy);
        await Assert.ThrowsAsync<InvalidOperationException>(damaged.OpenCopyAsync);
    }

    private const string KeptAt = "/home/me/.local/share/studydiary/profiles/default/damaged/x";

    // 14:00 on the 9th, local time at +02:00.
    private static ManualTimeProvider AClock() =>
        new(new DateTimeOffset(2026, 10, 9, 14, 0, 0, TimeSpan.FromHours(2)));

    private static DamageDetail ADetail() =>
        new("payload.json", 3, "$.entries", "Unexpected character.");

    private static CopyOffer AnOffer(DateTime takenAt) => new(takenAt, () => Task.CompletedTask);

}
