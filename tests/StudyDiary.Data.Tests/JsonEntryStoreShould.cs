// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using System.Text.Json;

namespace StudyDiary.Data.Tests;

public class JsonEntryStoreShould : IDisposable
{
    private static readonly DateTimeOffset Instant =
        new(2026, 10, 6, 0, 30, 0, TimeSpan.FromHours(9));

    private readonly string _folder =
        Directory.CreateTempSubdirectory("studydiary-test-").FullName;

    private readonly FixedTimeProvider _clock = new(Instant);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

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
    public async Task RefuseAProfileFolderThatIsNotAFullPath() =>
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
}
