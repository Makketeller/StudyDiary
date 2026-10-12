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
using StudyDiary.Domain.Entries;
using StudyDiary.Domain.Scheduling;

namespace StudyDiary.App.Tests.ViewModels;

public class ReviewViewModelShould
{
    [Fact]
    public async Task ServeOnlyReadyEntries()
    {
        var review = await AReview(new InMemoryEntryStore(
            [AnEntry("Ready", daysAgo: 1), AnEntry("Written today", daysAgo: 0)]));

        var served = await AnswerTheWholeSession(review, ReviewOutcome.Pass);

        Assert.Equal(new[] { "Ready" }, served.Select(entry => entry.Title));
    }

    [Fact]
    public async Task ServeNoMoreThanTenInOneSession()
    {
        var review = await AReview(new InMemoryEntryStore(ReadyEntries(15)));

        var served = await AnswerTheWholeSession(review, ReviewOutcome.Pass);

        Assert.Equal(10, served.Count);
    }

    [Fact]
    public async Task ServeTheLongestReadyHalfOfALargePool()
    {
        // Entry #i was written i + 1 days ago, so #10 to #14 have waited longest.
        var review = await AReview(new InMemoryEntryStore(ReadyEntries(15)));

        var served = await AnswerTheWholeSession(review, ReviewOutcome.Pass);

        var titles = served.Select(entry => entry.Title).ToList();
        foreach (var title in new[] { "#10", "#11", "#12", "#13", "#14" })
            Assert.Contains(title, titles);
    }

    [Fact]
    public async Task NotOfferToKeepGoingOnceNothingIsReady()
    {
        var review = await AReview(new InMemoryEntryStore(ReadyEntries(3)));

        await AnswerTheWholeSession(review, ReviewOutcome.Fail);

        Assert.True(review.IsSessionOver);
        Assert.False(review.CanKeepGoing);
    }

    [Fact]
    public async Task RecordAPassAndMoveTheEntryUpABox()
    {
        var store = new InMemoryEntryStore([AnEntry("Q", daysAgo: 1)]);
        var review = await AReview(store);
        var entry = review.CurrentEntry!;

        await review.AnswerAsync(ReviewOutcome.Pass);

        Assert.Equal(
            new ReviewRecord(Today, ReviewOutcome.Pass, 1, 2, IsPractice: false),
            Assert.Single(store.HistoryOf(entry.Id)));
        Assert.Equal(2, entry.ReviewState.Box);
    }

    [Fact]
    public async Task SaveTheHistoryBeforeTheEntry()
    {
        var store = new InMemoryEntryStore([AnEntry("Q", daysAgo: 1)]);
        var review = await AReview(store);

        await review.AnswerAsync(ReviewOutcome.Pass);

        Assert.Equal(
            new[] { nameof(IEntryStore.AppendReviewAsync), nameof(IEntryStore.UpdateAsync) },
            store.Calls);
    }


    [Fact]
    public async Task RecordNothingWhenDoneForNow()
    {
        var store = new InMemoryEntryStore([AnEntry("Q", daysAgo: 1)]);
        var diary = await ADiary(store);
        diary.StartReview();

        diary.Review!.Finish();

        Assert.Null(diary.Review);
        Assert.Empty(store.Calls);
    }

    [Fact]
    public async Task PromptAnUntitledEntryWithTheDayItWasWritten()
    {
        var review = await AReview(new InMemoryEntryStore(
            [AnEntry("", daysAgo: 1, body: "A")]));

        Assert.Equal("What did you write on 2026-10-08?", review.Prompt);
    }

    // 00:30 at +02:00, as in DiaryViewModelShould: the local day is the 9th.
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 0, 30, 0, TimeSpan.FromHours(2));
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static Task<DiaryViewModel> ADiary(InMemoryEntryStore store) =>
        DiaryViewModel.LoadAsync(
            store, new ManualTimeProvider(Now), new LeitnerScheduler(), LeitnerLadder.Default,
            new Random(1));

    private static async Task<ReviewViewModel> AReview(InMemoryEntryStore store)
    {
        var diary = await ADiary(store);
        diary.StartReview();
        return diary.Review!;
    }

    private static async Task<List<Entry>> AnswerTheWholeSession(
        ReviewViewModel review, ReviewOutcome outcome)
    {
        var served = new List<Entry>();
        while (review.CurrentEntry is not null)
        {
            served.Add(review.CurrentEntry);
            await review.AnswerAsync(outcome);
        }

        return served;
    }

    private static Entry[] ReadyEntries(int count) =>
        Enumerable.Range(0, count)
            .Select(i => AnEntry($"#{i}", daysAgo: i + 1))
            .ToArray();

    private static Entry AnEntry(string title, int daysAgo, string body = "Body")
    {
        var createdAt = Now.AddDays(-daysAgo);
        return Entry.Create(title, body, DateOnly.FromDateTime(createdAt.DateTime), createdAt);
    }
}
