// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.Text.Json.Serialization;

namespace StudyDiary.Data;

/// <summary>
/// <c>profile.json</c> as it sits on disk: the header the picker reads
/// without ever opening the payload (DESIGN §7). Settable
/// properties, built empty and filled in. Plaintext forever - the profile
/// name is the one accepted leak of user text outside the payload.
/// </summary>
internal sealed class ProfileDto
{
    [JsonRequired]
    public int SchemaVersion { get; set; }

    [JsonRequired]
    public Guid Id { get; set; }

    [JsonRequired]
    public string Name { get; set; } = "";

    [JsonRequired]
    public string Encryption { get; set; } = "none";
}