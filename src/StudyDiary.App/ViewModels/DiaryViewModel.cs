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
    private Entry? _selectedEntry;
    private Entry? _entryToDelete;
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

    public Entry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (!SetField(ref _selectedEntry, value))
                return;

            OnPropertyChanged(nameof(CanDeleteEntry));
            EntryToDelete = null;
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


    public bool CanDeleteEntry => SelectedEntry is not null;

    /// <summary>
    /// The entry the delete question is about, or null when none is asked:
    /// the answer deletes exactly this one (ARCHITECTURE).
    /// </summary>
    public Entry? EntryToDelete
    {
        get => _entryToDelete;
        private set
        {
            if (SetField(ref _entryToDelete, value))
                OnPropertyChanged(nameof(IsConfirmingDelete));
        }
    }

    public bool IsConfirmingDelete => EntryToDelete is not null;

    public void AskToDeleteEntry() => EntryToDelete = SelectedEntry;

    public void CancelDelete() => EntryToDelete = null;

    // Bound to the question's Delete button: it only awaits (ARCHITECTURE).
    public async void DeleteEntry() => await DeleteEntryAsync();

    public async Task DeleteEntryAsync()
    {
        var entry = EntryToDelete
            ?? throw new InvalidOperationException("No entry is waiting to be deleted.");

        // Cleared before the save, like adding: a double click finds no
        // question left to answer.
        EntryToDelete = null;
        SelectedEntry = null;

        await _store.DeleteAsync(entry.Id);
        Entries.Remove(entry);
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
