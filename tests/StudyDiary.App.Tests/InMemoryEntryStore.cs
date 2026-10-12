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
/// A store that keeps its entries in a list, so App's tests reach no disk
/// (ARCHITECTURE). Logs each saving call, so a test can check the order.
/// </summary>
internal sealed class InMemoryEntryStore(IEnumerable<Entry> entries) : IEntryStore
{
    private readonly List<Entry> _entries = [.. entries];
    private readonly Dictionary<Guid, List<ReviewRecord>> _history = new();

    /// <summary>The saving calls made so far, in order.</summary>
    public List<string> Calls { get; } = [];

    public IReadOnlyList<ReviewRecord> HistoryOf(Guid entryId) =>
        _history.TryGetValue(entryId, out var records) ? records : [];

    public Task<IReadOnlyList<Entry>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Entry>>([.. _entries]);

    public Task AddAsync(Entry entry)
    {
        if (_entries.Exists(held => held.Id == entry.Id))
            throw new InvalidOperationException($"Entry {entry.Id} is already held.");

        _entries.Add(entry);
        Calls.Add(nameof(AddAsync));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Entry entry)
    {
        var index = _entries.FindIndex(held => held.Id == entry.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Entry {entry.Id} is not held.");

        _entries[index] = entry;
        Calls.Add(nameof(UpdateAsync));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        if (_entries.RemoveAll(held => held.Id == id) == 0)
            throw new KeyNotFoundException($"Entry {id} is not held.");

        _history.Remove(id);
        Calls.Add(nameof(DeleteAsync));
        return Task.CompletedTask;
    }

    public Task AppendReviewAsync(Guid entryId, ReviewRecord record)
    {
        if (!_entries.Exists(held => held.Id == entryId))
            throw new KeyNotFoundException($"Entry {entryId} is not held.");

        if (!_history.TryGetValue(entryId, out var records))
        {
            records = new List<ReviewRecord>();
            _history[entryId] = records;
        }

        records.Add(record);
        Calls.Add(nameof(AppendReviewAsync));
        return Task.CompletedTask;
    }
}
