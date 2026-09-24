// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyDiary.Data;

/// <summary>
/// <c>payload.json</c> as it sits on disk: everything except the header
/// (DESIGN §7), in exactly two top-level keys (ARCHITECTURE). DayLogs are
/// held as raw JSON until the DayLog type exists, so any that turn up
/// before then - hand-written, or in a payload.json copied on its own from
/// a newer install - are carried through a save untouched, never dropped.
/// </summary>
internal sealed class PayloadDto
{
    [JsonRequired]
    public List<EntryDto> Entries { get; set; } = [];

    [JsonRequired]
    public List<JsonElement> DayLogs { get; set; } = [];
}
