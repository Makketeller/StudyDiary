// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.Globalization;

namespace StudyDiary.Data;

/// <summary>
/// Every folder and file name under the machine's data folder, in one
/// place (ARCHITECTURE). All lowercase, and never renamed once released:
/// renaming one means moving every user's diary.
/// </summary>
public static class DataLayout
{
    internal const string AppFolderName = "studydiary";
    internal const string ProfilesFolderName = "profiles";
    internal const string DefaultProfileFolderName = "default";
    internal const string HeaderFileName = "profile.json";
    internal const string PayloadFileName = "payload.json";
    internal const string TempFileSuffix = ".tmp";
    internal const string DamagedFolderName = "damaged";
    internal const string RecoveryFolderName = "recovery";

    // Folders named for a moment: sorts in time order as plain text, and no
    // colons, which Windows forbids in a name (DESIGN §7).
    internal const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";

    /// <summary>
    /// The default profile's folder, under the folder App resolved from
    /// <see cref="Environment.SpecialFolder.LocalApplicationData"/>.
    /// </summary>
    public static string DefaultProfileFolder(string localAppData) =>
        Path.Combine(
            localAppData,
            AppFolderName,
            ProfilesFolderName,
            DefaultProfileFolderName);

    // A folder name of ours: a timestamp, alone or followed by _2, _3. Every
    // field of the timestamp is fixed-width, so it is exactly as long as its
    // format string. Null for any other name (ARCHITECTURE).
    internal static (DateTime Time, int Counter)? ReadTimestampedName(string name)
    {
        var stampLength = TimestampFormat.Length;

        if (name.Length < stampLength
            || !DateTime.TryParseExact(name[..stampLength], TimestampFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return null;

        var suffix = name[stampLength..];
        if (suffix.Length == 0)
            return (time, 1);

        return suffix[0] == '_'
            && int.TryParse(suffix[1..], NumberStyles.None, CultureInfo.InvariantCulture,
                out var counter)
            && counter >= 2
                ? (time, counter)
                : null;
    }
}
