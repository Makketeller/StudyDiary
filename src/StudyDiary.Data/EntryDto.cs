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
/// One entry as it sits on disk: settable properties, built empty and
/// filled in. Nests its <see cref="ReviewStateDto"/> and carries its own
/// review history, which has no counterpart on <c>Entry</c> (ARCHITECTURE).
/// </summary>
internal sealed class EntryDto
{
    [JsonRequired]
    public Guid Id { get; set; }
    
    [JsonRequired]
    public string Title { get; set; } = "";
    
    [JsonRequired]
    public string Body { get; set; } = "";
    
    [JsonRequired]
    public DateOnly CreatedOn { get; set; }
    
    [JsonRequired]
    public DateTimeOffset CreatedAt { get; set; }
    
    [JsonRequired]
    public ReviewStateDto ReviewState { get; set; } = new();
    
    [JsonRequired]
    public List<ReviewRecordDto> ReviewHistory { get; set; } = [];
}
