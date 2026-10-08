// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Data;

/// <summary>
/// One of the app's own recovery copies: a folder under <c>recovery/</c>
/// holding both files, named for when it was taken (DESIGN §7). App can
/// read when it was taken and hand it back, but only Data can make one, so
/// nothing can point Restore at a folder of its own (ARCHITECTURE).
/// </summary>
public sealed record RecoveryCopy
{
    private RecoveryCopy(string folder, DateTime takenAt, int counter)
    {
        Folder = folder;
        TakenAt = takenAt;
        Counter = counter;
    }

    /// <summary>
    /// When it was taken, as the clock on the wall read: the name records
    /// no offset, so none is claimed.
    /// </summary>
    public DateTime TakenAt { get; }

    internal string Folder { get; }

    // Orders copies taken in the same second: 1 for the first, then 2, 3
    // for _2, _3.
    internal int Counter { get; }

    // Null unless the folder's name is one of ours (ARCHITECTURE).
    internal static RecoveryCopy? FromFolder(string folder)
    {
        var name = DataLayout.ReadTimestampedName(Path.GetFileName(folder));

        return name is null
            ? null
            : new RecoveryCopy(folder, name.Value.Time, name.Value.Counter);
    }
}
