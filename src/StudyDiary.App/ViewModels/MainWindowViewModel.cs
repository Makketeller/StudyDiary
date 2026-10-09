// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.
namespace StudyDiary.App.ViewModels;

/// <summary>The main window: which one screen it shows (ARCHITECTURE).</summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _current = new OpeningViewModel();

    public ViewModelBase Current
    {
        get => _current;
        private set => SetField(ref _current, value);
    }

    public void ShowDiary(DiaryViewModel diary)
    {
        ArgumentNullException.ThrowIfNull(diary);
        Current = diary;
    }

    public void ShowPlaceholder(string text) => Current = new PlaceholderViewModel(text);
}
