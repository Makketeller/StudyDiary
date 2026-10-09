// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.App.ViewModels;
using StudyDiary.Domain.Entries;

namespace StudyDiary.App.Tests.ViewModels;

public class DiaryWindowViewModelShould
{
    [Fact]
    public async Task ListEntriesNewestFirst()
    {
        var oldest = AnEntry("Oldest", new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
        var middle = AnEntry("Middle", new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero));
        var newest = AnEntry("Newest", new DateTimeOffset(2026, 10, 3, 9, 0, 0, TimeSpan.Zero));

        var diary = await DiaryViewModel.LoadAsync(new InMemoryEntryStore([middle, newest, oldest]));

        Assert.Equal(
            new[] { "Newest", "Middle", "Oldest" },
            diary.Entries.Select(entry => entry.Title));
    }

    private static Entry AnEntry(string title, DateTimeOffset createdAt) =>
        Entry.Create(title, "Body", DateOnly.FromDateTime(createdAt.DateTime), createdAt);
}
