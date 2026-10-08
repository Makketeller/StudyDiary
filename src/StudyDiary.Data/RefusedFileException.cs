// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.Text.Json;

namespace StudyDiary.Data;

/// <summary>
/// A <see cref="JsonException"/> that also names the profile file it came
/// from, so the refusal's details can say which file to open (DESIGN §7).
/// Still a JsonException, so everything that catches the damage signal
/// keeps working (ARCHITECTURE).
/// </summary>
internal sealed class RefusedFileException(string filePath, JsonException cause)
    : JsonException(cause.Message, cause.Path, cause.LineNumber, cause.BytePositionInLine, cause)
{
    // System.IO spelled out: inside a JsonException, Path is the JSON path.
    /// <summary>The refused file's name, such as <c>payload.json</c>.</summary>
    public string FileName { get; } = System.IO.Path.GetFileName(filePath);
}
