// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.Data;

/// <summary>
/// What a damaged profile's collapsed details show, for someone fixing the
/// file by hand (DESIGN §7): the refused file, the line where reading
/// stopped, where in the file, and the reader's own message. The line counts
/// from 1, as editors do, and is null when the fault has none, such as an
/// entry the Domain refused. The message is untouched, so its own text still
/// counts lines from 0.
/// </summary>
public sealed record DamageDetail(string FileName, long? Line, string? JsonPath, string Message);
