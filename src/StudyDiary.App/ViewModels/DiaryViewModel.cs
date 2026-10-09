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

    public bool CanAddEntry => !Entry.IsBlank(NewTitle, NewBody);

    private DiaryViewModel(IEnumerable<Entry> newestFirst)
    {
        Entries = new ObservableCollection<Entry>(newestFirst);
    }

    public ObservableCollection<Entry> Entries { get; }

    public static async Task<DiaryViewModel> LoadAsync(IEntryStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var entries = await store.GetAllAsync();
        return new DiaryViewModel(entries.OrderByDescending(entry => entry.CreatedAt));
    }
}
