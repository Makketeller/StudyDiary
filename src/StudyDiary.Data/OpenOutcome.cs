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

    /// <summary>
    /// Saved by a newer version of the app: refused without being read
    /// further, and never offered a recovery copy (DESIGN §7). Both numbers
    /// travel so the message can show them.
    /// </summary>
    public sealed record Newer(int FileSchemaVersion, int SupportedSchemaVersion) : OpenOutcome;

    /// <summary>
    /// The profile would not load (DESIGN §7). Both files were kept aside
    /// before this was returned, so <see cref="KeptAt"/> is a real folder,
    /// or null when neither file was there to keep.
    /// <see cref="NewestPassingCopy"/> is the newest recovery copy that
    /// passed the same check, or null when none did.
    /// </summary>
    public sealed record Damaged(
        DamageDetail Detail, string? KeptAt, RecoveryCopy? NewestPassingCopy) : OpenOutcome;
}
