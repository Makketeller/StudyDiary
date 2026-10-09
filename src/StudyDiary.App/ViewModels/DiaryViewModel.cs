// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.Collections.ObjectModel;
using StudyDiary.Data;
using StudyDiary.Domain.Entries;

namespace StudyDiary.App.ViewModels;

/// <summary>
/// The open diary: its entries, newest first. Made only by
/// <see cref="LoadAsync"/>, so no half-loaded diary can exist.
/// </summary>
public sealed class DiaryViewModel : ViewModelBase
{
    private string _newTitle = "";
    private string _newBody = "";
    private readonly IEntryStore _store;
    private readonly TimeProvider _clock;
    public string NewTitle
    {
        get => _newTitle;
        set
        {
            if (SetField(ref _newTitle, value))
                OnPropertyChanged(nameof(CanAddEntry));
        }
    }

    public string NewBody
    {
        get => _newBody;
        set
        {
            if (SetField(ref _newBody, value))
                OnPropertyChanged(nameof(CanAddEntry));
        }
    }

    public ObservableCollection<Entry> Entries { get; }
    public bool CanAddEntry => !Entry.IsBlank(NewTitle, NewBody);

    // Bound to the Add button: it only awaits, so a failure reaches the
    // last-resort handler (ARCHITECTURE).
    public async void AddEntry() => await AddEntryAsync();

    public async Task AddEntryAsync()
    {
        var now = _clock.GetLocalNow();
        var entry = Entry.Create(NewTitle, NewBody, DateOnly.FromDateTime(now.DateTime), now);

        // Cleared before the save, so the button disables at once and a
        // double click cannot add the same entry twice.
        NewTitle = "";
        NewBody = "";

        await _store.AddAsync(entry);
        Entries.Insert(0, entry);
    }

    private DiaryViewModel(IEntryStore store, TimeProvider clock, IEnumerable<Entry> newestFirst)
    {
        _store = store;
        _clock = clock;
        Entries = new ObservableCollection<Entry>(newestFirst);
    }

    public static async Task<DiaryViewModel> LoadAsync(IEntryStore store, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(clock);

        var entries = await store.GetAllAsync();
        return new DiaryViewModel(
            store, clock, entries.OrderByDescending(entry => entry.CreatedAt));
    }
}
