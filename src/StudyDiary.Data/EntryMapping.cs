// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Entries;
using StudyDiary.Domain.Scheduling;
using System.Text.Json;

namespace StudyDiary.Data;

/// <summary>
/// Copies entries and their reviews between the Domain shape and the file
/// shape, by hand, both directions (ARCHITECTURE). Loading goes through the
/// Domain constructors, and whatever they refuse comes out as a
/// <see cref="JsonException"/> naming the entry (DESIGN §7).
/// </summary>
internal static class EntryMapping
{
    /// <summary>
    /// The entry's file shape. <paramref name="history"/> is required because
    /// <see cref="Entry"/> has none: a new entry passes an empty list, an
    /// update passes the history of the DTO it replaces.
    /// </summary>
    public static EntryDto ToDto(Entry entry, List<ReviewRecordDto> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return new()
        {
            Id = entry.Id,
            Title = entry.Title,
            Body = entry.Body,
            CreatedOn = entry.CreatedOn,
            CreatedAt = entry.CreatedAt,
            ReviewState = new()
            {
                Box = entry.ReviewState.Box,
                EnteredOn = entry.ReviewState.EnteredOn,
            },
            ReviewHistory = history,
        };
    }

    public static ReviewRecordDto ToDto(ReviewRecord record) => new()
    {
        ReviewedOn = record.ReviewedOn,
        Outcome = record.Outcome,
        BoxBefore = record.BoxBefore,
        BoxAfter = record.BoxAfter,
        IsPractice = record.IsPractice,
    };

    public static ReviewRecord ToReviewRecord(ReviewRecordDto dto) =>
        new(dto.ReviewedOn, dto.Outcome, dto.BoxBefore, dto.BoxAfter, dto.IsPractice);

    /// <summary>
    /// Every entry in the file, or a <see cref="JsonException"/> if any of
    /// them is impossible: refused by the Domain, null, or sharing an id with
    /// an earlier one. Nothing is skipped (DESIGN §7).
    /// </summary>
    public static IReadOnlyList<Entry> ToEntries(IReadOnlyList<EntryDto> dtos)
    {
        var entries = new List<Entry>(dtos.Count);
        var ids = new HashSet<Guid>();

        for (var i = 0; i < dtos.Count; i++)
        {
            var path = $"$.entries[{i}]";
            var dto = dtos[i] ?? throw Damaged(path, "The entry is null.");
            var entry = ToEntry(dto, path);

            if (!ids.Add(entry.Id))
                throw Damaged(path, $"Entry {entry.Id} appears more than once.");

            entries.Add(entry);
        }

        return entries;
    }

    // Each review is rebuilt only so its constructor checks it; nothing
    // reads history back yet. Catches ArgumentException and nothing wider:
    // anything else is a bug here, not damage in the file (ARCHITECTURE).
    private static Entry ToEntry(EntryDto dto, string path)
    {
        try
        {
            for (var j = 0; j < dto.ReviewHistory.Count; j++)
            {
                var record = dto.ReviewHistory[j]
                    ?? throw Damaged(path, $"Entry {dto.Id} is invalid: review {j} is null.");
                _ = ToReviewRecord(record);
            }

            return new Entry(
                dto.Id,
                dto.Title,
                dto.Body,
                dto.CreatedOn,
                dto.CreatedAt,
                new ReviewState(dto.ReviewState.Box, dto.ReviewState.EnteredOn));
        }
        catch (ArgumentException e)
        {
            throw Damaged(path, $"Entry {dto.Id} is invalid: {e.Message}", e);
        }
    }

    private static JsonException Damaged(string path, string message, Exception? inner = null) =>
        new(message, path, lineNumber: null, bytePositionInLine: null, inner);
}
