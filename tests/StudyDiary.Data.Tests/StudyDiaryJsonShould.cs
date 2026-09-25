// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using StudyDiary.Domain.Scheduling;
using System.Text.Json;

namespace StudyDiary.Data.Tests;

public class StudyDiaryJsonShould
{
    // A complete, valid payload. Each refusal test breaks
    // one thing in it, and ReadAValidPayload proves they fail
    // for that reason, not a typo here.
    private const string ValidPayload = """
        {
          "entries": [
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "title": "Q",
              "body": "A",
              "createdOn": "2026-09-24",
              "createdAt": "2026-09-24T14:30:00+00:00",
              "reviewState": { "box": 2, "enteredOn": "2026-09-25" },
              "reviewHistory": [
                {
                  "reviewedOn": "2026-09-25",
                  "outcome": "Pass",
                  "boxBefore": 1,
                  "boxAfter": 2,
                  "isPractice": false
                }
              ]
            }
          ],
          "dayLogs": []
        }
        """;

    private static ProfileDto AProfileNamed(string name) => new()
    {
        SchemaVersion = 1,
        Id = new Guid("11111111-1111-1111-1111-111111111111"),
        Name = name,
        Encryption = "none",
    };

    private static string WriteProfile(ProfileDto profile) =>
        JsonSerializer.Serialize(profile, StudyDiaryJson.Context.ProfileDto);

    private static string WritePayload(PayloadDto payload) =>
        JsonSerializer.Serialize(payload, StudyDiaryJson.Context.PayloadDto);

    private static PayloadDto? ReadPayload(string json) =>
        JsonSerializer.Deserialize(json, StudyDiaryJson.Context.PayloadDto);

    [Fact]
    public void WriteKeysInCamelCase() =>
        Assert.Contains("\"schemaVersion\": 1", WriteProfile(AProfileNamed("Default")));

    // Checks "\n" alone, which is inside both Linux and Windows line breaks.
    [Fact]
    public void IndentTheFile() =>
        Assert.Contains("\n  \"name\": ", WriteProfile(AProfileNamed("Default")));

    [Theory]
    [InlineData("Bråk")]
    [InlineData("μ ö")]
    [InlineData("a & b")]
    [InlineData("x < y > Z")]
    [InlineData("it's")]
    public void WriteTextAsItself(string text) =>
        Assert.Contains($"\"name\": \"{text}\"", WriteProfile(AProfileNamed(text)));

    [Fact]
    public void ReadAValidPayload()
    {
        var payload = ReadPayload(ValidPayload);

        Assert.NotNull(payload);
        var entry = Assert.Single(payload.Entries);
        var record = Assert.Single(entry.ReviewHistory);
        Assert.Equal(ReviewOutcome.Pass, record.Outcome);
    }

    [Fact]
    public void WriteAnOutcomeAsItsName()
    {
        var payload = ReadPayload(ValidPayload)!;

        Assert.Contains("\"outcome\": \"Pass\"", WritePayload(payload));
    }

    [Fact]
    public void RefuseAnUnknownKey()
    {
        var json = ValidPayload.Replace(
            "\"title\": \"Q\",",
            "\"title\": \"Q\", \"notes\": \"added by hand\",");

        Assert.ThrowsAny<JsonException>(() => ReadPayload(json));
    }

    [Fact]
    public void RefuseAKeyGivenTwice()
    {
        var json = ValidPayload.Replace(
            "\"title\": \"Q\",",
            "\"title\": \"Q\", \"title\": \"R\",");

        Assert.ThrowsAny<JsonException>(() => ReadPayload(json));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("\"1\"")]
    [InlineData("\"Maybe\"")]
    public void RefuseAnOutcomeThatIsNotAMemberName(string outcome)
    {
        var json = ValidPayload.Replace(
            "\"outcome\": \"Pass\"",
            $"\"outcome\": {outcome}");

        Assert.ThrowsAny<JsonException>(() => ReadPayload(json));
    }

    [Fact]
    public void RefuseATypeNobodyRegistered() =>
        Assert.ThrowsAny<NotSupportedException>(
            () => StudyDiaryJson.Context.Options.GetTypeInfo(typeof(Uri)));
}
