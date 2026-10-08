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
/// <c>profile.json</c> seen only for its <c>schemaVersion</c>, read before
/// anything else in the file is judged (ARCHITECTURE). A newer file carries
/// keys this app has never seen, so this one type skips unknown keys; every
/// other rule of the shared options still applies.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
internal sealed class VersionProbeDto
{
    [JsonRequired]
    public int SchemaVersion { get; set; }
}
