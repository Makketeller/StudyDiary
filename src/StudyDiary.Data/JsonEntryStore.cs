// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Entries;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace StudyDiary.Data;

/// <summary>
/// The JSON implementation of <see cref="IEntryStore"/>: one profile's
/// folder, held whole in memory as its two DTOs and written whole on every
/// change (ARCHITECTURE). Made only by <see cref="CreateAsync"/> or
/// <see cref="OpenAsync"/>, so a half-loaded store cannot exist.
/// </summary>
public sealed class JsonEntryStore : IEntryStore
{
    /// <summary>
    /// The <c>SchemaVersion</c> this app writes and understands. Bumped
    /// with every minor and major release, never a patch (DESIGN §7).
    /// </summary>
    internal const int CurrentSchemaVersion = 1;

    private readonly string _headerPath;
    private readonly string _payloadPath;
    private readonly TimeProvider _clock;
    private readonly ProfileDto _header;
    private readonly PayloadDto _payload;

    private JsonEntryStore(
        string profileFolder, TimeProvider clock, ProfileDto header, PayloadDto payload)
    {
        _headerPath = HeaderPath(profileFolder);
        _payloadPath = PayloadPath(profileFolder);
        _clock = clock;
        _header = header;
        _payload = payload;
    }

    /// <summary>
    /// Makes a new, empty profile in <paramref name="profileFolder"/>,
    /// creating the folder if needed, and returns its store.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The folder is not a full path.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The folder already holds a profile file. Nothing is changed.
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
            Encryption = "none",
        };

        var store = new JsonEntryStore(profileFolder, clock, header, new PayloadDto());
        await store.SaveAsync();
        return store;
    }

    /// <summary>
    /// Opens the profile in <paramref name="profileFolder"/>, reading and
    /// checking both files. Never creates or changes anything there
    /// (ARCHITECTURE).
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

        var (header, payload) = await ReadAndCheckAsync(
            HeaderPath(profileFolder), PayloadPath(profileFolder));

        return new OpenOutcome.Opened(
            new JsonEntryStore(profileFolder, clock, header, payload));
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
    /// The check a profile's two files face, at Open and in every save's
    /// read-back: each read strictly, and every Entry rebuilt through the
    /// Domain (ARCHITECTURE).
    /// </summary>
    /// <exception cref="JsonException">Either file is refused.</exception>
    internal static async Task<(ProfileDto Header, PayloadDto Payload)> ReadAndCheckAsync(
        string headerPath, string payloadPath)
    {
        var header = await ReadAsync(headerPath, StudyDiaryJson.Context.ProfileDto);
        var payload = await ReadAsync(payloadPath, StudyDiaryJson.Context.PayloadDto);
        _ = EntryMapping.ToEntries(payload.Entries);

        return (header, payload);
    }

    /// <summary>
    /// The <c>schemaVersion</c> of a header, read on its own before anything
    /// else in the file is judged, so a newer file is recognised rather than
    /// refused as damaged (ARCHITECTURE).
    /// </summary>
    /// <exception cref="JsonException">
    /// The version is missing, repeated, null, not an integer, or below 1.
    /// </exception>
    internal static int ReadSchemaVersion(byte[] headerBytes, string headerPath)
    {
        var probe = Parse(headerBytes, headerPath, StudyDiaryJson.Context.VersionProbeDto);

        return probe.SchemaVersion >= 1
            ? probe.SchemaVersion
            : throw new JsonException(
                $"{headerPath} has schemaVersion {probe.SchemaVersion}; no release writes below 1.",
                path: "$.schemaVersion", lineNumber: null, bytePositionInLine: null);
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

    private static async Task<T> ReadAsync<T>(string path, JsonTypeInfo<T> typeInfo)
        where T : class =>
        Parse(await File.ReadAllBytesAsync(path), path, typeInfo);

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

    private static bool HoldsAnythingOfOurs(string profileFolder) =>
        File.Exists(HeaderPath(profileFolder)) || File.Exists(PayloadPath(profileFolder));

    private static string HeaderPath(string folder) =>
        Path.Combine(folder, DataLayout.HeaderFileName);

    private static string PayloadPath(string folder) =>
        Path.Combine(folder, DataLayout.PayloadFileName);
}
