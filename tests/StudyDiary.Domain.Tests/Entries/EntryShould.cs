// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Entries;
using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Domain.Tests.Entries;

public class EntryShould
{
    private static readonly Guid SharedId = 
        new("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly CreatedOn = new(2026, 9, 7);
    private static readonly DateTimeOffset CreatedAt = 
        new(2026, 9, 7, 14, 30, 0, TimeSpan.Zero);
    private static readonly ReviewState State = new(1, CreatedOn);

    [Fact]
    public void PreserveEveryValueGivenToConstructor()
    {
        var state = new ReviewState(3, CreatedOn);

        var entry = new Entry(
            SharedId, "Title", "Body", CreatedOn, CreatedAt, state);

        Assert.Equal(SharedId, entry.Id);
        Assert.Equal("Title", entry.Title);
        Assert.Equal("Body", entry.Body);
        Assert.Equal(CreatedOn, entry.CreatedOn);
        Assert.Equal(CreatedAt, entry.CreatedAt);
        Assert.Equal(state, entry.ReviewState);
    }

    [Fact]
    public void PreserveTheValuesPassedToCreate()
    {
        var entry = Entry.Create("Q", "A", CreatedOn, CreatedAt);

        Assert.Equal("Q", entry.Title);
        Assert.Equal("A", entry.Body);
        Assert.Equal(CreatedOn, entry.CreatedOn);
        Assert.Equal(CreatedAt, entry.CreatedAt);
    }

    [Fact]
    public void StartInBoxOneOnItsCreationDay()
    {
        var entry = Entry.Create("Q", "A", CreatedOn, CreatedAt);

        Assert.Equal(1, entry.ReviewState.Box);
        Assert.Equal(CreatedOn, entry.ReviewState.EnteredOn);
    }

    [Fact]
    public void GetADistinctIdEachTimeItIsCreated()
    {
        var first = Entry.Create("Q", "A", CreatedOn, CreatedAt);
        var second = Entry.Create("Q", "A", CreatedOn, CreatedAt);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void RejectAnEmptyId() =>
        Assert.Throws<ArgumentException>(
            () => new Entry(
                Guid.Empty, "Q", "A", CreatedOn, CreatedAt, State));
    
    [Fact]
    public void RejectANullTitle() =>
        Assert.Throws<ArgumentNullException>(
            () => new Entry(
                SharedId, null!, "A", CreatedOn, CreatedAt, State));

    [Fact]
    public void RejectANullBody() =>
        Assert.Throws<ArgumentNullException>(
            () => new Entry(
                SharedId, "Q", null!, CreatedOn, CreatedAt, State));

    [Fact]
    public void RejectANullReviewState() =>
        Assert.Throws<ArgumentNullException>(
            () => new Entry(
                SharedId, "Q", "A", CreatedOn, CreatedAt, null!));

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("", "   ")]
    [InlineData("   ", "")]
    public void RejectAnEntryWithNeitherTitleNorBody(string title, string body) =>
        Assert.Throws<ArgumentException>(
            () => new Entry(
                SharedId, title, body, CreatedOn, CreatedAt, State));

    [Theory]
    [InlineData("Q", "")]
    [InlineData("", "A")]
    [InlineData("Q", "A")]
    public void AcceptAnEntryWithAtLeastOneOfTitleOrBody(string title, string body)
    {
        var entry = new Entry(
            SharedId, title, body, CreatedOn, CreatedAt, State);
        
        Assert.Equal(title, entry.Title);
        Assert.Equal(body, entry.Body);
    }
    
    [Fact]
    public void ReplaceOnlyItsStateOnApplyReview()
    {
        var entry = new Entry(
            SharedId, "Q", "A", CreatedOn, CreatedAt, State);
        var promoted = new ReviewState(2, CreatedOn.AddDays(7));

        entry.ApplyReview(promoted);

        Assert.Equal(promoted, entry.ReviewState);
        Assert.Equal(SharedId, entry.Id);
        Assert.Equal("Q", entry.Title);
        Assert.Equal("A", entry.Body);
        Assert.Equal(CreatedOn, entry.CreatedOn);
        Assert.Equal(CreatedAt, entry.CreatedAt);
    }

    [Fact]
    public void RejectANullStateOnApplyReview()
    {
        var entry = Entry.Create("Q", "A", CreatedOn, CreatedAt);

        Assert.Throws<ArgumentNullException>(() => entry.ApplyReview(null!));
    }
    [Fact]
    public void NotEqualAnotherEntryWithIdenticalContents()
    {
        var a = new Entry(SharedId, "Q", "A", CreatedOn, CreatedAt, State);
        var b = new Entry(SharedId, "Q", "A", CreatedOn, CreatedAt, State);

        Assert.NotEqual(a, b);
    }
}

