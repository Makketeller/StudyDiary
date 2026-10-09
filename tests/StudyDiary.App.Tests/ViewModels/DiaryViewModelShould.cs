// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.App.ViewModels;
using StudyDiary.Domain.Entries;
using StudyDiary.Data.Tests;

namespace StudyDiary.App.Tests.ViewModels;

public class DiaryWindowViewModelShould
{
    // 00:30 at +02:00: still the 8th in UTC, so a day taken from UTC is wrong.
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 0, 30, 0, TimeSpan.FromHours(2));


    [Fact]
    public async Task ListEntriesNewestFirst()
    {
        var oldest = AnEntry("Oldest", new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
        var middle = AnEntry("Middle", new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero));
        var newest = AnEntry("Newest", new DateTimeOffset(2026, 10, 3, 9, 0, 0, TimeSpan.Zero));

        var diary = await ADiary(new InMemoryEntryStore([middle, newest, oldest]));

        Assert.Equal(
            new[] { "Newest", "Middle", "Oldest" },
            diary.Entries.Select(entry => entry.Title));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("", "   ")]
    [InlineData("   ", "")]
    public async Task RefuseToAddAnEntryWithNeitherTitleNorBody(string title, string body)
    {
        var diary = await AnEmptyDiary();

        diary.NewTitle = title;
        diary.NewBody = body;

        Assert.False(diary.CanAddEntry);
    }

    [Theory]
    [InlineData("Q", "")]
    [InlineData("", "A")]
    [InlineData("Q", "A")]
    public async Task AllowAddingAnEntryWithAtLeastOneOfTitleOrBody(string title, string body)
    {
        var diary = await AnEmptyDiary();

        diary.NewTitle = title;
        diary.NewBody = body;

        Assert.True(diary.CanAddEntry);
    }

    [Theory]
    [InlineData("Q", "")]
    [InlineData("", "A")]
    public async Task AnnounceThatAddingBecamePossible(string title, string body)
    {
        var diary = await AnEmptyDiary();
        var announced = new List<string?>();
        diary.PropertyChanged += (_, e) => announced.Add(e.PropertyName);

        diary.NewTitle = title;
        diary.NewBody = body;

        Assert.Contains(nameof(DiaryViewModel.CanAddEntry), announced);
    }

    [Fact]
    public async Task PutANewEntryAtTheTop()
    {
        var diary = await ADiary(new InMemoryEntryStore([AnEntry("Older", Now.AddDays(-1))]));
        diary.NewTitle = "New";

        await diary.AddEntryAsync();

       Assert.Equal(new[] { "New", "Older" }, diary.Entries.Select(entry => entry.Title));
    }

    [Fact]
    public async Task KeepANewEntryInTheStore()
    {
        var store = new InMemoryEntryStore([]);
        var diary = await ADiary(store);
        diary.NewTitle = "Q";

        await diary.AddEntryAsync();

        var kept = Assert.Single(await store.GetAllAsync());
        Assert.Equal(diary.Entries[0].Id, kept.Id);
    }

    [Fact]
    public async Task ClearTheBoxesOnceAnEntryIsAdded()
    {
        var diary = await AnEmptyDiary();
        diary.NewTitle = "Q";
        diary.NewBody = "A";

        await diary.AddEntryAsync();

        Assert.Equal("", diary.NewTitle);
        Assert.Equal("", diary.NewBody);
    }


    [Fact]
    public async Task DateANewEntryByTheLocalDay()
    {
        var diary = await AnEmptyDiary();
        diary.NewTitle = "Q";

        await diary.AddEntryAsync();

        var entry = Assert.Single(diary.Entries);
        Assert.Equal(new DateOnly(2026, 10, 9), entry.CreatedOn);
        Assert.Equal(Now, entry.CreatedAt);
        Assert.Equal(TimeSpan.FromHours(2), entry.CreatedAt.Offset);
    }

    private static Entry AnEntry(string title, DateTimeOffset createdAt) =>
        Entry.Create(title, "Body", DateOnly.FromDateTime(createdAt.DateTime), createdAt);

    private static Task<DiaryViewModel> ADiary(InMemoryEntryStore store) =>
        DiaryViewModel.LoadAsync(store, new ManualTimeProvider(Now));

    private static Task<DiaryViewModel> AnEmptyDiary() => ADiary(new InMemoryEntryStore([]));
}
