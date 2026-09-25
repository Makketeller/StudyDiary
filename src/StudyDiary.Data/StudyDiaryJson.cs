// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Scheduling;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyDiary.Data;

/// <summary>
/// The one set of rules every read and write of a profile's files goes
/// through (ARCHITECTURE): camelCase keys, readable text, enums by name,
/// and a strict reader that refuses an unknown or repeated key.
/// </summary>
internal static class StudyDiaryJson
{
    /// <summary>
    /// The generated context bound to these rules. Read and write through
    /// its <c>ProfileDto</c> and <c>PayloadDto</c> properties.
    /// </summary>
    public static StudyDiaryJsonContext Context { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        Converters =
        {
            new JsonStringEnumConverter<ReviewOutcome>(allowIntegerValues: false)
        },
    };
}
