// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;

namespace StudyDiary.Domain.Entries;

/// <summary>
/// One thing you want to remember: a title, a body, and where it sits on the
/// ladder. An entity, not a value - two entries with identical text are
/// different entries, and editing one leaves it the same entry (ARCHITECTURE).
/// </summary>
public sealed class Entry
{
    public Guid Id { get; }
    public string Title { get; }
    public string Body { get; }
    public DateOnly CreatedOn { get; }
    public DateTimeOffset CreatedAt { get; }
    public ReviewState ReviewState { get; private set; }

    /// <summary>
    /// Rebuilds an entry that already exists, e.g. when loading from storage.
    /// To write a new one, use <see cref="Create"/>.
    /// </summary>
    public Entry(
        Guid id,
        string title,
        string body,
        DateOnly createdOn,
        DateTimeOffset createdAt,
        ReviewState reviewState)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("An entry needs an id.", nameof(id));

        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(reviewState);

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("An entry needs a title or a body.");

        Id = id;
        Title = title;
        Body = body;
        CreatedOn = createdOn;
        CreatedAt = createdAt;
        ReviewState = reviewState;
    }

    /// <summary>
    /// A newly written entry: fresh id, box 1, entered on its creation day
    /// (DESIGN §3). Both dates arrive from the App layer's TimeProvider.
    /// </summary>
    public static Entry Create(
        string title,
        string body,
        DateOnly createdOn,
        DateTimeOffset createdAt) =>
        new(Guid.NewGuid(),
            title,
            body,
            createdOn,
            createdAt,
            new ReviewState(1, createdOn));

    /// <summary>Replaces scheduling state with the scheduler's result.</summary>
    public void ApplyReview(ReviewState newState)
    {
        ArgumentNullException.ThrowIfNull(newState);
        ReviewState = newState;
    }
}