// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.App.ViewModels;

/// <summary>
/// What the main window shows: the diary opening, then the diary, or for
/// now a placeholder for a file that would not open.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private bool _isOpening = true;
    private string _status = "Opening your diary...";

    public bool IsOpening
    {
        get => _isOpening;
        private set => SetField(ref _isOpening, value);
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public void ShowDiary()
    {
        IsOpening = false;
        Status = "Your diary is open.";
    }

    // Stands in for the newer and damaged panels until they exist.
    public void ShowPlaceholder(string text)
    {
        IsOpening = false;
        Status = text;
    }
}
