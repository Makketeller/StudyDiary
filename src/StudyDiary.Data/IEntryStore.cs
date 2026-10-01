// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Domain.Entries;

namespace StudyDiary.Data;

/// <summary>
/// Where entries and their review history are kept. Says what App wants to
/// do, not how storage does it, so a second implementation can replace the
/// JSON one without App changing (ARCHITECTURE). Await every call: an
/// exception inside an un-awaited task is never seen.
/// </summary>
public interface IEntryStore
{
    /// <summary>
    /// Every entry, in no promised order. The entries are the caller's own:
    /// changing one changes nothing here until it is passed to
    /// <see cref="UpdateAsync"/>.
    /// </summary>
    Task<IReadOnlyList<Entry>> GetAllAsync();

    /// <summary>Keeps a new entry, with an empty review history.</summary>
    /// <exception cref="InvalidOperationException">
    /// An entry with this id is already held. Nothing is changed.
    /// </exception>
    Task AddAsync(Entry entry);

    /// <summary>
    /// Replaces the held entry that has this id. Its review history is kept.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// No entry with this id is held. Nothing is changed.
    /// </exception>
    Task UpdateAsync(Entry entry);

    /// <summary>Removes an entry, and its review history with it.</summary>
    /// <exception cref="KeyNotFoundException">
    /// No entry with this id is held. Nothing is changed.
    /// </exception>
    Task DeleteAsync(Guid id);

    /// <summary>Adds one review to the end of an entry's history.</summary>
    /// <exception cref="KeyNotFoundException">
    /// No entry with this id is held. Nothing is changed.
    /// </exception>
    Task AppendReviewAsync(Guid entryId, ReviewRecord record);
}

