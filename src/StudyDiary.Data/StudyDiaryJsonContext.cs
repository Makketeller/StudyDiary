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
/// What System.Text.Json needs to know about the two files' types,
/// written by the compiler at build time instead of looked up while
/// the app runs (ARCHITECTURE). Only the two top-level types are
/// listed; everything nested inside them follows. Never use
/// <c>Default</c>: it carries none of the file's rules. Go through
/// <see cref="StudyDiaryJson"/>.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ProfileDto))]
[JsonSerializable(typeof(PayloadDto))]
internal sealed partial class StudyDiaryJsonContext : JsonSerializerContext
{
}
