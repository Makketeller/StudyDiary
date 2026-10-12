// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.


namespace StudyDiary.App.ViewModels;

/// <summary>
/// The screen for a diary saved by a newer version: refused, never offered
/// a copy (DESIGN §7). The details carry both format numbers.
/// </summary>
public sealed class NewerViewModel(int fileSchemaVersion, int supportedSchemaVersion) : ViewModelBase
{
    public string Details { get; } =
        $"The file's format is version {fileSchemaVersion}. "
        + $"This version of StudyDiary reads up to version {supportedSchemaVersion}.";
}
