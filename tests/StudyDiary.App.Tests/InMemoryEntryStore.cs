// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using StudyDiary.Domain.Entries;

namespace StudyDiary.App.Tests;

/// <summary>
/// A store that keeps its entries in a list, so App's tests reach no
/// disk (ARCHITECTURE). Grows a method at a time as tests need them.
/// </summary>
internal sealed class InMemoryEntryStore(IEnumerable<Entry> entries) : IEntryStore
{
    private readonly List<Entry> _entries = [.. entries];

    public Task<IReadOnlyList<Entry>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Entry>>([.. _entries]);

    public Task AddAsync(Entry entry)
    {
        if (_entries.Exists(held => held.Id == entry.Id))
            throw new InvalidOperationException($"Entry {entry.Id} is already held.");

        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Entry entry) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();

    public Task AppendReviewAsync(Guid entryId, ReviewRecord record) =>
        throw new NotImplementedException();
}
