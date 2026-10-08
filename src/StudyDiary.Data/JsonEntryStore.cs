// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Entries;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace StudyDiary.Data;

/// <summary>
/// The JSON implementation of <see cref="IEntryStore"/>: one profile's
/// folder, held whole in memory as its two DTOs and written whole on every
/// change (ARCHITECTURE). Made only by <see cref="CreateAsync"/>,
/// <see cref="OpenAsync"/> or <see cref="RestoreAsync"/>, so a half-loaded
/// store cannot exist.
/// </summary>
public sealed class JsonEntryStore : IEntryStore
{
    /// <summary>
    /// The <c>SchemaVersion</c> this app writes and understands. Bumped
    /// with every minor and major release, never a patch (DESIGN §7).
    /// </summary>
    internal const int CurrentSchemaVersion = 1;

    // DESIGN §7's numbers: constants, not format, so changing them changes
    // no file.
    private const int NewestCopiesKept = 25;
    private const int DaysOfCopiesKept = 7;

    private readonly string _headerPath;
    private readonly string _payloadPath;
    private readonly TimeProvider _clock;
    private readonly ProfileDto _header;
    private readonly PayloadDto _payload;
    private readonly string _profileFolder;

    private JsonEntryStore(
        string profileFolder, TimeProvider clock, ProfileDto header, PayloadDto payload)
    {
        _headerPath = HeaderPath(profileFolder);
        _payloadPath = PayloadPath(profileFolder);
        _clock = clock;
        _header = header;
        _payload = payload;
        _profileFolder = profileFolder;
    }

    /// <summary>
    /// Makes a new, empty profile in <paramref name="profileFolder"/>,
    /// creating the folder if needed, and returns its store.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The folder is not a full path.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The folder already holds a profile file or a recovery copy. Nothing is changed.
    /// </exception>
    public static async Task<IEntryStore> CreateAsync(
        string profileFolder, string name, TimeProvider clock)
    {
        RequireFullPath(profileFolder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(clock);

        if (HoldsAnythingOfOurs(profileFolder))
            throw new InvalidOperationException(
                $"{profileFolder} already holds a profile; creating one would overwrite it.");

        Directory.CreateDirectory(profileFolder);

        var header = new ProfileDto
        {
            SchemaVersion = CurrentSchemaVersion,
            Id = Guid.NewGuid(),
            Name = name,
            Encryption = ProfileDto.NoEncryption,
        };

        var store = new JsonEntryStore(profileFolder, clock, header, new PayloadDto());
        await store.SaveAsync();
        return store;
    }

    /// <summary>
    /// Opens the profile in <paramref name="profileFolder"/>, reading and
    /// checking both files. Never creates or changes either file; on damage
    /// it keeps copies of them under <c>damaged/</c> (ARCHITECTURE).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The folder is not a full path.
    /// </exception>
    public static async Task<OpenOutcome> OpenAsync(string profileFolder, TimeProvider clock)
    {
        RequireFullPath(profileFolder);
        ArgumentNullException.ThrowIfNull(clock);

        if (!HoldsAnythingOfOurs(profileFolder))
            return new OpenOutcome.NoProfile();

        var headerPath = HeaderPath(profileFolder);

        try
        {
            var headerBytes = await ReadProfileFileAsync(headerPath);
            var version = ReadSchemaVersion(headerBytes, headerPath);

            // Decided before the strict read, which would call a newer file
            // damaged, or worse, read one it misunderstands (DESIGN §7).
            if (version > CurrentSchemaVersion)
                return new OpenOutcome.Newer(version, CurrentSchemaVersion);

            var (header, payload) =
                await CheckAsync(headerBytes, headerPath, PayloadPath(profileFolder));

            return new OpenOutcome.Opened(
                new JsonEntryStore(profileFolder, clock, header, payload));
        }
        catch (RefusedFileException refusal)
        {
            // Kept before anything is said, so the folder in the message is
            // real whatever the user answers; with neither file there,
            // nothing is kept (DESIGN §7).
            var keptAt = HoldsAProfileFile(profileFolder)
                ? CopyProfileFiles(profileFolder, DataLayout.DamagedFolderName, clock)
                : null;

            return new OpenOutcome.Damaged(
                refusal.ToDamageDetail(), keptAt, await NewestPassingCopyAsync(profileFolder));
        }
    }

    /// <summary>
    /// Puts <paramref name="copy"/> back as the profile in
    /// <paramref name="profileFolder"/> and returns its store. Called only
    /// after the user accepts the copy Open offered: Open looks, Restore
    /// touches (ARCHITECTURE). The copy is checked again and written through
    /// the ordinary save, so a restore is read back and copied like any
    /// other change.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The folder is not a full path.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The copy no longer passes the check. Nothing is changed.
    /// </exception>
    public static async Task<IEntryStore> RestoreAsync(
        string profileFolder, RecoveryCopy copy, TimeProvider clock)
    {
        RequireFullPath(profileFolder);
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(clock);

        (ProfileDto Header, PayloadDto Payload) files;
        try
        {
            files = await ReadAndCheckAsync(HeaderPath(copy.Folder), PayloadPath(copy.Folder));
        }
        catch (RefusedFileException e)
        {
            // Changed under the open dialog: not damage to report, since the
            // diary is unchanged and the next Open offers the next copy
            // (ARCHITECTURE).
            throw new InvalidOperationException(
                $"The recovery copy in {copy.Folder} no longer passes the check; "
                + "nothing was changed.", e);
        }

        var store = new JsonEntryStore(profileFolder, clock, files.Header, files.Payload);
        await store.SaveAsync();
        return store;
    }

    public Task<IReadOnlyList<Entry>> GetAllAsync() =>
        Task.FromResult(EntryMapping.ToEntries(_payload.Entries));

    public async Task AddAsync(Entry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_payload.Entries.Exists(dto => dto.Id == entry.Id))
            throw new InvalidOperationException($"Entry {entry.Id} is already held.");

        _payload.Entries.Add(EntryMapping.ToDto(entry, []));
        await SaveAsync();
    }

    public async Task UpdateAsync(Entry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var index = IndexOfHeldEntry(entry.Id);

        var history = _payload.Entries[index].ReviewHistory;
        _payload.Entries[index] = EntryMapping.ToDto(entry, history);
        await SaveAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        _payload.Entries.RemoveAt(IndexOfHeldEntry(id));
        await SaveAsync();
    }

    public async Task AppendReviewAsync(Guid entryId, ReviewRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var index = IndexOfHeldEntry(entryId);

        _payload.Entries[index].ReviewHistory.Add(EntryMapping.ToDto(record));
        await SaveAsync();
    }

    /// <summary>
    /// The whole check a profile's two files face, for a save's read-back
    /// and the recovery copies: a version this app reads, then each file
    /// strictly, then every Entry rebuilt through the Domain (ARCHITECTURE).
    /// Open makes the same checks but answers a newer version as an outcome.
    /// </summary>
    /// <exception cref="RefusedFileException">
    /// Either file is refused, including a header newer than this app reads;
    /// the exception names which.
    /// </exception>
    internal static async Task<(ProfileDto Header, PayloadDto Payload)> ReadAndCheckAsync(
        string headerPath, string payloadPath)
    {
        var headerBytes = await ReadProfileFileAsync(headerPath);
        var version = ReadSchemaVersion(headerBytes, headerPath);

        if (version > CurrentSchemaVersion)
            throw new RefusedFileException(headerPath, new JsonException(
                $"{headerPath} has schemaVersion {version}; this app reads up to "
                + $"{CurrentSchemaVersion}.",
                path: "$.schemaVersion", lineNumber: null, bytePositionInLine: null));

        return await CheckAsync(headerBytes, headerPath, payloadPath);
    }

    /// <summary>
    /// The <c>schemaVersion</c> of a header, read on its own before anything
    /// else in the file is judged, so a newer file is recognised rather than
    /// refused as damaged (ARCHITECTURE).
    /// </summary>
    /// <exception cref="RefusedFileException">
    /// The file is not valid JSON, or its version is missing, repeated, null,
    /// not an integer, or below 1.
    /// </exception>
    internal static int ReadSchemaVersion(byte[] headerBytes, string headerPath)
    {
        try
        {
            var probe = Parse(headerBytes, headerPath, StudyDiaryJson.Context.VersionProbeDto);

            return probe.SchemaVersion >= 1
                ? probe.SchemaVersion
                : throw new JsonException(
                    $"{headerPath} has schemaVersion {probe.SchemaVersion}; "
                    + "no release writes below 1.",
                    path: "$.schemaVersion", lineNumber: null, bytePositionInLine: null);
        }
        catch (JsonException e)
        {
            throw new RefusedFileException(headerPath, e);
        }
    }

    // A wrong id is a bug in the caller, refused before anything changes
    // (ARCHITECTURE).
    private int IndexOfHeldEntry(Guid id)
    {
        var index = _payload.Entries.FindIndex(dto => dto.Id == id);

        return index >= 0
            ? index
            : throw new KeyNotFoundException($"No entry {id} is held.");
    }

    private async Task SaveAsync()
    {
        var headerTemp = _headerPath + DataLayout.TempFileSuffix;
        var payloadTemp = _payloadPath + DataLayout.TempFileSuffix;

        // This app's version, never the one read from disk: the header must
        // describe the content written beside it (DESIGN §7).
        _header.SchemaVersion = CurrentSchemaVersion;

        try
        {
            await WriteToDiskAsync(headerTemp, _header, StudyDiaryJson.Context.ProfileDto);
            await WriteToDiskAsync(payloadTemp, _payload, StudyDiaryJson.Context.PayloadDto);
            await ReadAndCheckAsync(headerTemp, payloadTemp);
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException(
                "A save produced files this app cannot read back. Neither file was replaced.", e);
        }

        File.Move(headerTemp, _headerPath, overwrite: true);
        File.Move(payloadTemp, _payloadPath, overwrite: true);

        // Only after both renames, so the newest copy is always a save that
        // passed the read-back (ARCHITECTURE).
        CopyProfileFiles(_profileFolder, DataLayout.RecoveryFolderName, _clock);
        PruneRecoveryCopies(_profileFolder);
    }

    // Flushed to disk before it returns, and closed by the using: the rename
    // after it must never point at bytes still in memory, and Windows will
    // not rename a file that is open.
    private static async Task WriteToDiskAsync<T>(
        string path, T value, JsonTypeInfo<T> typeInfo)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);

        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        await file.WriteAsync(bytes);
        file.Flush(flushToDisk: true);
    }

    // The strict half of the check, over header bytes already read once and
    // already found to be a version this app reads.
    private static async Task<(ProfileDto Header, PayloadDto Payload)> CheckAsync(
        byte[] headerBytes, string headerPath, string payloadPath)
    {
        var header = CheckHeader(headerBytes, headerPath);
        var payload = CheckPayload(await ReadProfileFileAsync(payloadPath), payloadPath);

        return (header, payload);
    }

    // A missing file is damage like any other (ARCHITECTURE), so it becomes
    // the same refusal, named for the file, rather than a second signal.
    private static async Task<byte[]> ReadProfileFileAsync(string filePath)
    {
        try
        {
            return await File.ReadAllBytesAsync(filePath);
        }
        catch (FileNotFoundException e)
        {
            throw new RefusedFileException(filePath, new JsonException(
                $"{filePath} is missing.", path: null, lineNumber: null,
                bytePositionInLine: null, innerException: e));
        }
    }

    // Newest first through the full check, so a copy saved by a newer build
    // is passed over like a damaged one (ARCHITECTURE). Null when none passes.
    private static async Task<RecoveryCopy?> NewestPassingCopyAsync(string profileFolder)
    {
        foreach (var copy in Enumerable.Reverse(RecoveryCopies(profileFolder)))
        {
            try
            {
                await ReadAndCheckAsync(HeaderPath(copy.Folder), PayloadPath(copy.Folder));
                return copy;
            }
            catch (RefusedFileException)
            {
                // This copy is refused too; try the next older one.
            }
        }

        return null;
    }

    // Both files as they are now, byte for byte, into a new folder under the
    // profile's folderName folder, named for this moment (DESIGN §7).
    // Copies in one second get _2, _3, as below; a
    // missing file is left out. Returns the new folder.
    private static string CopyProfileFiles(
        string profileFolder, string folderName, TimeProvider clock)
    {
        var parent = Path.Combine(profileFolder, folderName);
        var stamp = clock.GetLocalNow().ToString(
            DataLayout.TimestampFormat, CultureInfo.InvariantCulture);

        // One past the highest counter this second already has, never a gap
        // left by pruning: a reused lower number would sort the newest copy
        // among the oldest (ARCHITECTURE).
        var highest = Directory.Exists(parent)
            ? Directory.GetDirectories(parent)
                .Select(path => Path.GetFileName(path))
                .Where(name => name.StartsWith(stamp, StringComparison.Ordinal))
                .Select(name => DataLayout.ReadTimestampedName(name)?.Counter ?? 0)
                .DefaultIfEmpty(0)
                .Max()
            : 0;

        var copyFolder = Path.Combine(parent, highest == 0 ? stamp : $"{stamp}_{highest + 1}");

        Directory.CreateDirectory(copyFolder);

        string[] files = [HeaderPath(profileFolder), PayloadPath(profileFolder)];
        foreach (var file in files)
        {
            if (File.Exists(file))
                File.Copy(file, Path.Combine(copyFolder, Path.GetFileName(file)));
        }

        return copyFolder;
    }

    // Keeps the newest copies and the first of each of the latest days that
    // have one, ordered by time and then counter (ARCHITECTURE). Only
    // folders whose names are ours are ever deleted.
    private static void PruneRecoveryCopies(string profileFolder)
    {
        var copies = RecoveryCopies(profileFolder);

        var newest = copies.TakeLast(NewestCopiesKept);
        var firstOfEachDay = copies
            .GroupBy(copy => copy.TakenAt.Date)
            .TakeLast(DaysOfCopiesKept)
            .Select(day => day.First());

        var kept = newest.Concat(firstOfEachDay).ToHashSet();

        foreach (var copy in copies.Where(copy => !kept.Contains(copy)))
            Directory.Delete(copy.Folder, recursive: true);
    }

    // The app's own copies in recovery/, oldest first by time and then
    // counter; folders whose names are not ours are not among them
    // (ARCHITECTURE).
    private static List<RecoveryCopy> RecoveryCopies(string profileFolder)
    {
        var recovery = Path.Combine(profileFolder, DataLayout.RecoveryFolderName);
        if (!Directory.Exists(recovery))
            return [];

        return Directory.GetDirectories(recovery)
            .Select(RecoveryCopy.FromFolder)
            .OfType<RecoveryCopy>()
            .OrderBy(copy => copy.TakenAt)
            .ThenBy(copy => copy.Counter)
            .ToList();
    }

    // Each file's part of the check catches whatever it refuses and names the
    // file on it, so a check added inside is named without anyone remembering
    // to (ARCHITECTURE).
    private static ProfileDto CheckHeader(byte[] headerBytes, string headerPath)
    {
        try
        {
            var header = Parse(headerBytes, headerPath, StudyDiaryJson.Context.ProfileDto);

            // A truly encrypted file comes from a newer version and was turned
            // away by the probe, so anything else here is a hand-edit (ARCHITECTURE).
            if (header.Encryption != ProfileDto.NoEncryption)
                throw new JsonException(
                    $"{headerPath} has encryption '{header.Encryption}'; this version reads only "
                    + $"'{ProfileDto.NoEncryption}'.",
                    path: "$.encryption", lineNumber: null, bytePositionInLine: null);

            return header;
        }
        catch (JsonException e)
        {
            throw new RefusedFileException(headerPath, e);
        }
    }

    private static PayloadDto CheckPayload(byte[] payloadBytes, string payloadPath)
    {
        try
        {
            var payload = Parse(payloadBytes, payloadPath, StudyDiaryJson.Context.PayloadDto);
            _ = EntryMapping.ToEntries(payload.Entries);

            return payload;
        }
        catch (JsonException e)
        {
            throw new RefusedFileException(payloadPath, e);
        }
    }

    // A file that is only `null` parses without complaint; it is damage.
    private static T Parse<T>(byte[] bytes, string path, JsonTypeInfo<T> typeInfo)
        where T : class =>
        JsonSerializer.Deserialize(WithoutByteOrderMark(bytes), typeInfo)
            ?? throw new JsonException($"{path} holds only null.");

    // A UTF-8 byte-order mark is skipped, not refused (ARCHITECTURE): some
    // editors add one, and the reader would otherwise call the file damaged.
    private static ReadOnlySpan<byte> WithoutByteOrderMark(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith(Encoding.UTF8.Preamble)
            ? bytes[Encoding.UTF8.Preamble.Length..]
            : bytes;

    private static void RequireFullPath(string profileFolder)
    {
        if (!Path.IsPathFullyQualified(profileFolder))
            throw new ArgumentException(
                "The profile folder must be a full path.", nameof(profileFolder));
    }

    private static bool HoldsAProfileFile(string profileFolder) =>
        File.Exists(HeaderPath(profileFolder)) || File.Exists(PayloadPath(profileFolder));

    // A recovery copy counts, since a diary lived here; damaged/ does not,
    // being an archive (ARCHITECTURE).
    private static bool HoldsAnythingOfOurs(string profileFolder) =>
        HoldsAProfileFile(profileFolder) || RecoveryCopies(profileFolder).Count > 0;

    private static string HeaderPath(string folder) =>
        Path.Combine(folder, DataLayout.HeaderFileName);

    private static string PayloadPath(string folder) =>
        Path.Combine(folder, DataLayout.PayloadFileName);
}
