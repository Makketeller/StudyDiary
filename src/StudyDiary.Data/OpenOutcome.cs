// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Data;

/// <summary>
/// What happened when a profile was opened: exactly one of the cases below,
/// each holding only what its own case needs (ARCHITECTURE).
/// </summary>
public abstract record OpenOutcome
{
    private OpenOutcome() { }

    /// <summary>The profile loaded and passed every check.</summary>
    public sealed record Opened(IEntryStore Store) : OpenOutcome;

    /// <summary>Nothing of ours in the folder: a first run.</summary>
    public sealed record NoProfile : OpenOutcome;
}
