// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

namespace StudyDiary.App.ViewModels;

/// <summary>
/// A recovery copy the damaged screen may offer: when it was taken, as the
/// wall clock read, and the function that opens it (ARCHITECTURE).
/// </summary>
public sealed record CopyOffer(DateTime TakenAt, Func<Task> Open);

