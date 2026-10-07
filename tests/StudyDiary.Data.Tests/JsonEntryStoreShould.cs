// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using StudyDiary.Domain.Entries;
using StudyDiary.Domain.Scheduling;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace StudyDiary.Data.Tests;

public class JsonEntryStoreShould : IDisposable
{
    private static readonly ReviewRecord Review =
        new(new DateOnly(2026, 10, 20), ReviewOutcome.Pass, 2, 3, IsPractice: true);

    private static readonly DateTimeOffset Instant =
        new(2026, 10, 6, 0, 30, 0, TimeSpan.FromHours(9));

    private readonly string _folder =
        Directory.CreateTempSubdirectory("studydiary-test-").FullName;

    private readonly FixedTimeProvider _clock = new(Instant);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string HeaderPath => Path.Combine(_folder, "profile.json");
    private string PayloadPath => Path.Combine(_folder, "payload.json");

    // Every value differs from its type's default, so a field the store
    // forgets comes back different, not equal by luck.
    private static Entry AnEntry() => new(
        Guid.NewGuid(), "Bloch's theorem", "ψ(r + R) = e^{ik·R} ψ(r)",
        new DateOnly(2026, 10, 6), Instant,
        new ReviewState(3, new DateOnly(2026, 10, 20)));

    private Task<IEntryStore> CreateStoreAsync() =>
        JsonEntryStore.CreateAsync(_folder, "Physics", _clock);

    // Opens the folder afresh, as the app does at its next start.
    private async Task<IEntryStore> ReopenAsync()
    {
        var outcome = await JsonEntryStore.OpenAsync(_folder, _clock);
        return Assert.IsType<OpenOutcome.Opened>(outcome).Store;
    }

    // A refused call must throw before writing anything: both files keep
    // the date they were given.
    private async Task AssertRefusedWithoutWriting<TException>(Func<Task> call)
        where TException : Exception
    {
        var longAgo = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(HeaderPath, longAgo);
        File.SetLastWriteTimeUtc(PayloadPath, longAgo);

        await Assert.ThrowsAsync<TException>(call);

        Assert.Equal(longAgo, File.GetLastWriteTimeUtc(HeaderPath));
        Assert.Equal(longAgo, File.GetLastWriteTimeUtc(PayloadPath));
    }
    private void SaveHeaderAsALaterVersion(bool withANewKey)
    {
        EditHeader("schemaVersion", JsonEntryStore.CurrentSchemaVersion + 1);

        if (withANewKey)
            EditHeader("colour", "blue");
    }

    // Changes one key of the saved header, as a hand-edit would.
    private void EditHeader(string key, JsonNode? value)
    {
        var header = JsonNode.Parse(File.ReadAllText(HeaderPath))!;
        header[key] = value;
        File.WriteAllText(HeaderPath, header.ToJsonString());
    }

    [Fact]
    public async Task WriteTheHeaderOfANewProfile()
    {
        await JsonEntryStore.CreateAsync(_folder, "Physics", _clock);

        var header = JsonSerializer.Deserialize(
            File.ReadAllBytes(Path.Combine(_folder, "profile.json")),
            StudyDiaryJson.Context.ProfileDto);

        Assert.NotNull(header);
        Assert.Equal(JsonEntryStore.CurrentSchemaVersion, header.SchemaVersion);
        Assert.NotEqual(Guid.Empty, header.Id);
        Assert.Equal("Physics", header.Name);
        Assert.Equal("none", header.Encryption);
    }

    [Fact]
    public async Task WriteAnEmptyPayloadForANewProfile()
    {
        await JsonEntryStore.CreateAsync(_folder, "Physics", _clock);

        var payload = JsonSerializer.Deserialize(
            File.ReadAllBytes(Path.Combine(_folder, "payload.json")),
            StudyDiaryJson.Context.PayloadDto);

        Assert.NotNull(payload);
        Assert.Empty(payload.Entries);
        Assert.Empty(payload.DayLogs);
    }

    [Fact]
    public async Task LeaveOnlyTheTwoProfileFilesAfterCreating()
    {
        await JsonEntryStore.CreateAsync(_folder, "Physics", _clock);

        var names = Directory.GetFiles(_folder)
            .Select(path => Path.GetFileName(path))
            .Order()
            .ToArray();

        Assert.Equal(new[] { "payload.json", "profile.json" }, names);
    }

    [Theory]
    [InlineData("profile.json")]
    [InlineData("payload.json")]
    public async Task RefuseToCreateOverAProfileFile(string fileName)
    {
        var path = Path.Combine(_folder, fileName);
        File.WriteAllText(path, "keep me");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => JsonEntryStore.CreateAsync(_folder, "Physics", _clock));

        Assert.Equal("keep me", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task RefuseToCreateInAFolderThatIsNotAFullPath() =>
        await Assert.ThrowsAsync<ArgumentException>(
            () => JsonEntryStore.CreateAsync(
                Path.Combine("relative", "folder"), "Physics", _clock));

    [Theory]
    [InlineData("profile.json")]
    [InlineData("payload.json")]
    public async Task AcceptAFileThatStartsWithAByteOrderMark(string fileName)
    {
        await JsonEntryStore.CreateAsync(_folder, "Physics", _clock);
        var path = Path.Combine(_folder, fileName);
        var original = File.ReadAllBytes(path);
        File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, .. original]);

        var (header, payload) = await JsonEntryStore.ReadAndCheckAsync(
            Path.Combine(_folder, "profile.json"),
            Path.Combine(_folder, "payload.json"));

        Assert.Equal("Physics", header.Name);
        Assert.Empty(payload.Entries);
    }

    [Fact]
    public void ReadTheSchemaVersionOfAFileWithKeysItDoesNotKnow()
    {
        var bytes = Encoding.UTF8.GetBytes(
            """{ "schemaVersion": 7, "name": "Physics", "colour": "blue" }""");

        Assert.Equal(7, JsonEntryStore.ReadSchemaVersion(bytes, HeaderPath));
    }

    [Fact]
    public void ReadTheSchemaVersionOfAFileThatStartsWithAByteOrderMark()
    {
        byte[] bytes =
            [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("""{ "schemaVersion": 1 }""")];

        Assert.Equal(1, JsonEntryStore.ReadSchemaVersion(bytes, HeaderPath));
    }

    [Theory]
    [InlineData("""{ "name": "Physics" }""")]
    [InlineData("""{ "schemaVersion": 1, "schemaVersion": 1 }""")]
    [InlineData("""{ "schemaVersion": null }""")]
    [InlineData("""{ "schemaVersion": "1" }""")]
    [InlineData("""{ "schemaVersion": 1.5 }""")]
    [InlineData("""{ "schemaVersion": 0 }""")]
    [InlineData("""{ "schemaVersion": -1 }""")]
    [InlineData("null")]
    public void RefuseAHeaderWithoutAUsableSchemaVersion(string json) =>
        Assert.ThrowsAny<JsonException>(
            () => JsonEntryStore.ReadSchemaVersion(Encoding.UTF8.GetBytes(json), HeaderPath));

    // Each is broken after the version, so the probe has its number before
    // it reaches the fault: a broken file is damage, never Newer.
    [Theory]
    [InlineData("""{ "schemaVersion": 1, "name": "Physics" """)]
    [InlineData("""{ "schemaVersion": 1, "name": }""")]
    [InlineData("""{ "schemaVersion": 1 } }""")]
    public void RefuseAHeaderThatIsNotValidJson(string json) =>
        Assert.ThrowsAny<JsonException>(
            () => JsonEntryStore.ReadSchemaVersion(Encoding.UTF8.GetBytes(json), HeaderPath));

    [Fact]
    public async Task ReportNoProfileForAnEmptyFolder()
    {
        var outcome = await JsonEntryStore.OpenAsync(_folder, _clock);

        Assert.IsType<OpenOutcome.NoProfile>(outcome);
    }

    [Fact]
    public async Task ReportNoProfileForAFolderThatDoesNotExist()
    {
        var missing = Path.Combine(_folder, "missing");

        var outcome = await JsonEntryStore.OpenAsync(missing, _clock);

        Assert.IsType<OpenOutcome.NoProfile>(outcome);
    }

    [Fact]
    public async Task NotCreateAMissingFolderWhenOpening()
    {
        var missing = Path.Combine(_folder, "missing");

        await JsonEntryStore.OpenAsync(missing, _clock);

        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public async Task OpenAProfileItCreated()
    {
        await JsonEntryStore.CreateAsync(_folder, "Physics", _clock);

        var outcome = await JsonEntryStore.OpenAsync(_folder, _clock);

        Assert.IsType<OpenOutcome.Opened>(outcome);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReportNewerForAProfileSavedByALaterVersion(bool withANewKey)
    {
        await CreateStoreAsync();
        SaveHeaderAsALaterVersion(withANewKey);

        var outcome = await JsonEntryStore.OpenAsync(_folder, _clock);

        Assert.Equal(
            new OpenOutcome.Newer(
                JsonEntryStore.CurrentSchemaVersion + 1, JsonEntryStore.CurrentSchemaVersion),
            outcome);
    }

    [Fact]
    public async Task FailTheCheckForAProfileSavedByALaterVersion()
    {
        await CreateStoreAsync();
        SaveHeaderAsALaterVersion(withANewKey: false);

        var refusal = await Assert.ThrowsAsync<JsonException>(
            () => JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath));

        Assert.Equal("$.schemaVersion", refusal.Path);
    }

    [Theory]
    [InlineData("aes-256-gcm")]
    [InlineData("None")]
    [InlineData("")]
    public async Task FailTheCheckForAnEncryptionOtherThanNone(string encryption)
    {
        await CreateStoreAsync();
        EditHeader("encryption", encryption);

        var refusal = await Assert.ThrowsAsync<JsonException>(
            () => JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath));

        Assert.Equal("$.encryption", refusal.Path);
    }

    [Fact]
    public async Task WriteNothingWhenOpening()
    {
        await JsonEntryStore.CreateAsync(_folder, "Physics", _clock);
        var longAgo = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        string[] paths =
            [Path.Combine(_folder, "profile.json"), Path.Combine(_folder, "payload.json")];

        foreach (var path in paths)
            File.SetLastWriteTimeUtc(path, longAgo);

        await JsonEntryStore.OpenAsync(_folder, _clock);

        foreach (var path in paths)
            Assert.Equal(longAgo, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task RefuseToOpenAFolderThatIsNotAFullPath() =>
        await Assert.ThrowsAsync<ArgumentException>(
            () => JsonEntryStore.OpenAsync(Path.Combine("relative", "folder"), _clock));

    [Fact]
    public async Task RoundTripAnEntryUnchanged()
    {
        var store = await CreateStoreAsync();
        var entry = AnEntry();
        await store.AddAsync(entry);

        var reopened = await ReopenAsync();
        var loaded = Assert.Single(await reopened.GetAllAsync());

        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal(entry.Title, loaded.Title);
        Assert.Equal(entry.Body, loaded.Body);
        Assert.Equal(entry.CreatedOn, loaded.CreatedOn);
        Assert.Equal(entry.CreatedAt, loaded.CreatedAt);
        Assert.Equal(entry.CreatedAt.Offset, loaded.CreatedAt.Offset);
        Assert.Equal(entry.ReviewState, loaded.ReviewState);
    }

    [Fact]
    public async Task ReturnTheUpdatedStateAfterReopening()
    {
        var store = await CreateStoreAsync();
        var entry = AnEntry();
        await store.AddAsync(entry);
        var promoted = new ReviewState(4, new DateOnly(2026, 11, 3));

        entry.ApplyReview(promoted);
        await store.UpdateAsync(entry);

        var reopened = await ReopenAsync();
        Assert.Equal(promoted, Assert.Single(await reopened.GetAllAsync()).ReviewState);
    }

    [Fact]
    public async Task KeepTheReviewHistoryThroughAnUpdate()
    {
        var store = await CreateStoreAsync();
        var entry = AnEntry();
        await store.AddAsync(entry);

        await store.AppendReviewAsync(entry.Id, Review);
        await store.UpdateAsync(entry);

        var (_, payload) = await JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath);
        var saved = Assert.Single(Assert.Single(payload.Entries).ReviewHistory);
        Assert.Equal(Review, EntryMapping.ToReviewRecord(saved));
    }

    [Fact]
    public async Task RemoveADeletedEntryFromTheFile()
    {
        var store = await CreateStoreAsync();
        var kept = AnEntry();
        var deleted = AnEntry();
        await store.AddAsync(kept);
        await store.AddAsync(deleted);

        await store.DeleteAsync(deleted.Id);

        var reopened = await ReopenAsync();
        Assert.Equal(kept.Id, Assert.Single(await reopened.GetAllAsync()).Id);
    }

    [Fact]
    public async Task IgnoreAChangedEntryUntilItIsUpdated()
    {
        var store = await CreateStoreAsync();
        var entry = AnEntry();
        await store.AddAsync(entry);

        var handedOut = Assert.Single(await store.GetAllAsync());
        handedOut.ApplyReview(new ReviewState(4, new DateOnly(2026, 11, 3)));

        Assert.Equal(entry.ReviewState, Assert.Single(await store.GetAllAsync()).ReviewState);
    }

    [Fact]
    public async Task RefuseToAddAnEntryItAlreadyHolds()
    {
        var store = await CreateStoreAsync();
        var entry = AnEntry();
        await store.AddAsync(entry);

        await AssertRefusedWithoutWriting<InvalidOperationException>(
            () => store.AddAsync(entry));
    }

    [Fact]
    public async Task RefuseToUpdateAnEntryItDoesNotHold()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(AnEntry());

        await AssertRefusedWithoutWriting<KeyNotFoundException>(
            () => store.UpdateAsync(AnEntry()));
    }

    [Fact]
    public async Task RefuseToDeleteAnEntryItDoesNotHold()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(AnEntry());

        await AssertRefusedWithoutWriting<KeyNotFoundException>(
            () => store.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RefuseToAppendAReviewToAnEntryItDoesNotHold()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(AnEntry());

        await AssertRefusedWithoutWriting<KeyNotFoundException>(
            () => store.AppendReviewAsync(Guid.NewGuid(), Review));
    }
}
