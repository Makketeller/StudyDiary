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

    private readonly ManualTimeProvider _clock = new(Instant);

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

    // Rewrites the header as a later version would save it: a higher
    // schemaVersion and, if asked, a key this app has never seen.
    private void SaveHeaderAsALaterVersion(bool withANewKey)
    {
        EditHeader("schemaVersion", JsonEntryStore.CurrentSchemaVersion + 1);

        if (withANewKey)
            EditHeader("colour", "blue");
    }

    // Runs one hand-edit on a saved file and writes it back.
    private static void EditFile(string path, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(File.ReadAllText(path))!;
        edit(root);
        File.WriteAllText(path, root.ToJsonString());
    }

    // Changes one key of the saved header, as a hand-edit would.
    private void EditHeader(string key, JsonNode? value) =>
        EditFile(HeaderPath, header => header[key] = value);

    // A syntax error in payload.json, as a bad hand-edit leaves it.
    private void BreakThePayload() =>
        File.WriteAllText(PayloadPath, """{ "entries": [, ] }""");

    // Opens the folder afresh and requires the damaged outcome.
    private async Task<OpenOutcome.Damaged> OpenDamagedAsync() =>
        Assert.IsType<OpenOutcome.Damaged>(await JsonEntryStore.OpenAsync(_folder, _clock));

    // A copy folder must hold exactly what the live files hold now.
    private void AssertHoldsTheLiveFiles(string copyFolder)
    {
        Assert.Equal(
            File.ReadAllBytes(HeaderPath),
            File.ReadAllBytes(Path.Combine(copyFolder, "profile.json")));
        Assert.Equal(
            File.ReadAllBytes(PayloadPath),
            File.ReadAllBytes(Path.Combine(copyFolder, "payload.json")));
    }

    // One more save, the given time after the last one.
    private async Task SaveAfterAsync(IEntryStore store, TimeSpan wait)
    {
        _clock.Advance(wait);
        await store.AddAsync(AnEntry());
    }

    // The names of the recovery copies left, as text in order.
    private string[] RecoveryCopyNames() =>
        Directory.GetDirectories(Path.Combine(_folder, "recovery"))
            .Select(path => Path.GetFileName(path))
            .Order()
            .ToArray();

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
    public async Task TakeTheFirstRecoveryCopyWhenCreating()
    {
        await CreateStoreAsync();

        AssertHoldsTheLiveFiles(Path.Combine(_folder, "recovery", "2026-10-06_00-30-00"));
    }

    [Fact]
    public async Task CopyWhatEachSaveWrote()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(AnEntry());

        AssertHoldsTheLiveFiles(Path.Combine(_folder, "recovery", "2026-10-06_00-30-00_2"));
    }

    // Thirty saves a minute apart after Create: the first of the day and the
    // newest twenty-five stay, so only 00:31 to 00:35 go.
    [Fact]
    public async Task KeepTheNewestTwentyFiveCopiesAndTheFirstOfTheDay()
    {
        var store = await CreateStoreAsync();
        for (var save = 0; save < 30; save++)
            await SaveAfterAsync(store, TimeSpan.FromMinutes(1));

        var names = RecoveryCopyNames();

        Assert.Equal(26, names.Length);
        Assert.Equal("2026-10-06_00-30-00", names[0]);
        Assert.Equal("2026-10-06_00-36-00", names[1]);
    }

    // A copy on each of eight more days, then thirty saves on the last: the
    // newest twenty-five all fall on that day, so earlier days survive only
    // as the first copy of each of the last seven.
    [Fact]
    public async Task KeepTheFirstCopyOfEachOfTheLastSevenDays()
    {
        var store = await CreateStoreAsync();
        for (var day = 0; day < 8; day++)
            await SaveAfterAsync(store, TimeSpan.FromDays(1));
        for (var save = 0; save < 30; save++)
            await SaveAfterAsync(store, TimeSpan.FromMinutes(1));

        var names = RecoveryCopyNames();

        string[] firstOfEachDay =
        [
            "2026-10-08_00-30-00", "2026-10-09_00-30-00", "2026-10-10_00-30-00",
            "2026-10-11_00-30-00", "2026-10-12_00-30-00", "2026-10-13_00-30-00",
            "2026-10-14_00-30-00",
        ];
        Assert.Equal(firstOfEachDay, names[..7]);
        Assert.Equal(32, names.Length);
    }

    // Thirty saves in one second: as plain text _10 sorts before _2, so
    // only ordering by the counter deletes the right five.
    [Fact]
    public async Task OrderCopiesTakenInOneSecondByTheirCounter()
    {
        var store = await CreateStoreAsync();
        for (var save = 0; save < 30; save++)
            await SaveAfterAsync(store, TimeSpan.Zero);

        var names = RecoveryCopyNames();

        Assert.Equal(26, names.Length);
        Assert.DoesNotContain("2026-10-06_00-30-00_6", names);
        Assert.Contains("2026-10-06_00-30-00_7", names);
    }

    [Fact]
    public async Task LeaveAFolderInRecoveryThatIsNotOneOfItsCopies()
    {
        var store = await CreateStoreAsync();
        var stranger = Path.Combine(_folder, "recovery", "my notes");
        Directory.CreateDirectory(stranger);

        for (var save = 0; save < 30; save++)
            await SaveAfterAsync(store, TimeSpan.FromMinutes(1));

        Assert.True(Directory.Exists(stranger));
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

        var refusal = await Assert.ThrowsAsync<RefusedFileException>(
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

        var refusal = await Assert.ThrowsAsync<RefusedFileException>(
            () => JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath));

        Assert.Equal("$.encryption", refusal.Path);
    }

    // One break per part of the header's check: the probe, the newer
    // version, the strict read, and the encryption check.
    [Theory]
    [InlineData("schemaVersion", "0")]
    [InlineData("schemaVersion", "99")]
    [InlineData("colour", "\"blue\"")]
    [InlineData("encryption", "\"aes-256-gcm\"")]
    public async Task NameProfileJsonWhenTheHeaderIsRefused(string key, string valueJson)
    {
        await CreateStoreAsync();
        EditHeader(key, JsonNode.Parse(valueJson));

        var refusal = await Assert.ThrowsAsync<RefusedFileException>(
            () => JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath));

        Assert.Equal("profile.json", refusal.FileName);
    }

    [Theory]
    [InlineData("dayLogs", "null")]
    [InlineData("colour", "\"blue\"")]
    public async Task NamePayloadJsonWhenThePayloadIsRefused(string key, string valueJson)
    {
        await CreateStoreAsync();
        EditFile(PayloadPath, payload => payload[key] = JsonNode.Parse(valueJson));

        var refusal = await Assert.ThrowsAsync<RefusedFileException>(
            () => JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath));

        Assert.Equal("payload.json", refusal.FileName);
    }

    // Pins .NET's own count: the unit test of the +1 would still pass if
    // .NET started counting lines from 1.
    [Fact]
    public async Task GiveTheLineOfABrokenPayloadAsAnEditorCountsIt()
    {
        await CreateStoreAsync();
        File.WriteAllText(PayloadPath, """
            {
              "entries": [],
              "dayLogs": [,]
            }
            """);

        var refusal = await Assert.ThrowsAsync<RefusedFileException>(
            () => JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath));

        Assert.Equal<long?>(3, refusal.ToDamageDetail().Line);
    }

    [Fact]
    public async Task ReportDamagedNamingTheFileThatBroke()
    {
        await CreateStoreAsync();
        BreakThePayload();

        var damaged = await OpenDamagedAsync();

        Assert.Equal("payload.json", damaged.Detail.FileName);
    }

    [Theory]
    [InlineData("profile.json")]
    [InlineData("payload.json")]
    public async Task ReportDamagedNamingAMissingFile(string fileName)
    {
        await CreateStoreAsync();
        File.Delete(Path.Combine(_folder, fileName));

        var damaged = await OpenDamagedAsync();

        Assert.Equal(fileName, damaged.Detail.FileName);
    }


    [Fact]
    public async Task KeepBothFilesAsFoundWhenTheProfileIsDamaged()
    {
        await CreateStoreAsync();
        BreakThePayload();
        var header = File.ReadAllBytes(HeaderPath);
        var payload = File.ReadAllBytes(PayloadPath);

        var damaged = await OpenDamagedAsync();

        Assert.Equal(header, File.ReadAllBytes(Path.Combine(damaged.KeptAt, "profile.json")));
        Assert.Equal(payload, File.ReadAllBytes(Path.Combine(damaged.KeptAt, "payload.json")));
    }

    [Fact]
    public async Task KeepTheDamagedFilesInAFolderNamedForWhen()
    {
        await CreateStoreAsync();
        BreakThePayload();

        var damaged = await OpenDamagedAsync();

        Assert.Equal(Path.Combine(_folder, "damaged", "2026-10-06_00-30-00"), damaged.KeptAt);
    }

    [Fact]
    public async Task KeepASecondOpeningOfTheSameDamageSeparately()
    {
        await CreateStoreAsync();
        BreakThePayload();

        var first = await OpenDamagedAsync();
        var second = await OpenDamagedAsync();

        Assert.Equal(first.KeptAt + "_2", second.KeptAt);
    }

    [Fact]
    public async Task LeaveADamagedProfileUnchangedWhenOpening()
    {
        await CreateStoreAsync();
        BreakThePayload();
        var header = File.ReadAllBytes(HeaderPath);
        var payload = File.ReadAllBytes(PayloadPath);

        await JsonEntryStore.OpenAsync(_folder, _clock);

        Assert.Equal(header, File.ReadAllBytes(HeaderPath));
        Assert.Equal(payload, File.ReadAllBytes(PayloadPath));
    }

    [Fact]
    public async Task NamePayloadJsonWhenAnEntryIsRefused()
    {
        var store = await CreateStoreAsync();
        await store.AddAsync(AnEntry());
        EditFile(PayloadPath, payload => payload["entries"]![0]!["reviewState"]!["box"] = 0);

        var refusal = await Assert.ThrowsAsync<RefusedFileException>(
            () => JsonEntryStore.ReadAndCheckAsync(HeaderPath, PayloadPath));

        Assert.Equal("payload.json", refusal.FileName);
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
