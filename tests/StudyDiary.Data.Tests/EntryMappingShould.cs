// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using StudyDiary.Domain.Entries;
using StudyDiary.Domain.Scheduling;
using System.Text.Json;

namespace StudyDiary.Data.Tests;

public class EntryMappingShould
{
    // Every value differs from its type's default, so a field the
    // mapping forgets to copy comes back different, not equal by luck.
    private static readonly DateOnly CreatedOn = new(2026, 9, 1);
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 1, 14, 30, 0, TimeSpan.FromHours(2));
    private static readonly ReviewRecord Review =
        new(new DateOnly(2026, 9, 20), ReviewOutcome.Pass, 2, 3, IsPractice: true);

    private static Entry AnEntry() => new(
        Guid.NewGuid(), "Title", "Body", CreatedOn, CreatedAt,
        new ReviewState(3, new DateOnly(2026, 9, 20)));

    private static EntryDto ADto() => EntryMapping.ToDto(AnEntry(), []);

    [Fact]
    public void RoundTripAnEntryUnchanged()
    {
        var entry = AnEntry();

        var loaded = Assert.Single(
            EntryMapping.ToEntries([EntryMapping.ToDto(entry, [])]));

        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal(entry.Title, loaded.Title);
        Assert.Equal(entry.Body, loaded.Body);
        Assert.Equal(entry.CreatedOn, loaded.CreatedOn);
        Assert.Equal(entry.CreatedAt, loaded.CreatedAt);
        Assert.Equal(entry.ReviewState, loaded.ReviewState);
    }

    [Fact]
    public void RoundTripAReviewUnchanged() =>
        Assert.Equal(Review, EntryMapping.ToReviewRecord(EntryMapping.ToDto(Review)));

    [Fact]
    public void KeepTheHistoryItIsGiven()
    {
        List<ReviewRecordDto> history = [EntryMapping.ToDto(Review)];

        var dto = EntryMapping.ToDto(AnEntry(), history);

        Assert.Equal(history, dto.ReviewHistory);
    }

    [Fact]
    public void RefuseANullHistory() =>
        Assert.Throws<ArgumentNullException>(() => EntryMapping.ToDto(AnEntry(), null!));

    [Fact]
    public void RefuseAnEntryTheDomainRefuses()
    {
        var bad = ADto();
        bad.ReviewState.Box = 0;

        var e = Assert.Throws<JsonException>(() => EntryMapping.ToEntries([ADto(), bad]));

        Assert.Equal("$.entries[1]", e.Path);
        Assert.IsType<ArgumentOutOfRangeException>(e.InnerException);
    }

    [Fact]
    public void RefuseAnEntryWithAnImpossibleReview()
    {
        var bad = EntryMapping.ToDto(AnEntry(), [EntryMapping.ToDto(Review)]);
        bad.ReviewHistory[0].BoxBefore = 0;

        Assert.Throws<JsonException>(() => EntryMapping.ToEntries([bad]));
    }

    [Fact]
    public void RefuseANullEntry()
    {
        var e = Assert.Throws<JsonException>(() => EntryMapping.ToEntries([ADto(), null!]));

        Assert.Equal("$.entries[1]", e.Path);
    }

    [Fact]
    public void RefuseAnEntryWithANullReview() =>
        Assert.Throws<JsonException>(
            () => EntryMapping.ToEntries([EntryMapping.ToDto(AnEntry(), [null!])]));

    [Fact]
    public void RefuseTwoEntriesWithTheSameId()
    {
        var entry = AnEntry();

        var e = Assert.Throws<JsonException>(() => EntryMapping.ToEntries(
            [EntryMapping.ToDto(entry, []), EntryMapping.ToDto(entry, [])]));

        Assert.Equal("$.entries[1]", e.Path);
    }

    // The reader refuses a null reviewState, so only a bug in our
    // own code can build this DTO. It must crash, not become damage.
    [Fact]
    public void NotDisguiseABugAsDamage()
    {
        var broken = ADto();
        broken.ReviewState = null!;

        Assert.Throws<NullReferenceException>(() => EntryMapping.ToEntries([broken]));
    }
}
