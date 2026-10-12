// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using StudyDiary.Data;
using StudyDiary.Domain.Entries;
using StudyDiary.Domain.Scheduling;

namespace StudyDiary.App.ViewModels;


/// <summary>
/// A review session over the diary's ready entries: prompt, reveal, pass
/// or fail, and never a count (DESIGN §4, §5). Made by the diary, which
/// learns through <c>finished</c> that the user is done for now.
/// </summary>
public sealed class ReviewViewModel : ViewModelBase
{
    // DESIGN §4: the most a session serves, and the share of it that goes
    // to the entries ready longest (half, rounded up). Neither is shown.
    private const int SessionCap = 10;
    private const int LongestReadyShare = (SessionCap + 1) / 2;

    private readonly IReadOnlyCollection<Entry> _entries;
    private readonly IEntryStore _store;
    private readonly IReviewScheduler _scheduler;
    private readonly LeitnerLadder _ladder;
    private readonly TimeProvider _clock;
    private readonly Random _random;
    private readonly Action _finished;
    private readonly Queue<Entry> _session = new();

    private Entry? _currentEntry;
    private bool _isRevealed;
    private bool _canKeepGoing;

    internal ReviewViewModel(
        IReadOnlyCollection<Entry> entries,
        IEntryStore store,
        IReviewScheduler scheduler,
        LeitnerLadder ladder,
        TimeProvider clock,
        Random random,
        Action finished)
    {
        _entries = entries;
        _store = store;
        _scheduler = scheduler;
        _ladder = ladder;
        _clock = clock;
        _random = random;
        _finished = finished;

        StartSession();
    }


    public Entry? CurrentEntry
    {
        get => _currentEntry;
        private set
        {
            if (!SetField(ref _currentEntry, value))
                return;

            OnPropertyChanged(nameof(IsSessionOver));
            OnPropertyChanged(nameof(Prompt));
            OnPropertyChanged(nameof(Answer));
        }
    }

    public bool IsSessionOver => CurrentEntry is null;

    /// <summary>
    /// The title, or for an entry without one the day it was written: it is
    /// then a Note, recalled as a gist (DESIGN §2).
    /// </summary>
    public string Prompt
    {
        get
        {
            if (CurrentEntry is null)
                return "";

            return string.IsNullOrWhiteSpace(CurrentEntry.Title)
                ? $"What did you write on {CurrentEntry.CreatedOn:yyyy-MM-dd}?"
                : CurrentEntry.Title;
        }
    }

    public string Answer => CurrentEntry?.Body ?? "";

    public bool IsRevealed
    {
        get => _isRevealed;
        private set => SetField(ref _isRevealed, value);
    }


    /// <summary>True once a session is over while something is still ready.</summary>
    public bool CanKeepGoing
    {
        get => _canKeepGoing;
        private set => SetField(ref _canKeepGoing, value);
    }

    public void Reveal() => IsRevealed = true;

    // Bound to the Pass and Fail buttons: each only awaits (ARCHITECTURE).
    public async void Pass() => await AnswerAsync(ReviewOutcome.Pass);

    public async void Fail() => await AnswerAsync(ReviewOutcome.Fail);

    public async Task AnswerAsync(ReviewOutcome outcome)
    {
        var entry = CurrentEntry
            ?? throw new InvalidOperationException("No entry is being reviewed.");

        var today = Today();
        var after = _scheduler.Advance(entry.ReviewState, outcome, today, _ladder);
        var record = new ReviewRecord(
            today, outcome, entry.ReviewState.Box, after.Box, IsPractice: false);
        entry.ApplyReview(after);

        // Moved on before the saves, like adding and deleting: a second
        // click lands on the next entry, never on this one again.
        ShowNext();

        // History first, so a crash between the two costs a review rather
        // than a record (ARCHITECTURE).
        await _store.AppendReviewAsync(entry.Id, record);
        await _store.UpdateAsync(entry);
    }

    public void KeepGoing() => StartSession();

    // Done for now: records nothing, and nothing notices (DESIGN §4).
    public void Finish() => _finished();

    private void StartSession()
    {
        _session.Clear();
        foreach (var entry in ServeSession())
            _session.Enqueue(entry);

        ShowNext();
    }

    private void ShowNext()
    {
        IsRevealed = false;
        CurrentEntry = _session.TryDequeue(out var next) ? next : null;
        CanKeepGoing = CurrentEntry is null && ReadyEntries().Any();
    }

    // DESIGN §4: the longest-ready half first, the rest drawn at random,
    // then all of it shuffled so entries written together are not served
    // together.
    private Entry[] ServeSession()
    {
        var ready = ReadyEntries().ToArray();
        _random.Shuffle(ready); // a random draw for the rest, random ties for the half

        var longestReady = ready
            .OrderBy(entry => _scheduler.NextReviewOn(entry.ReviewState, _ladder))
            .Take(LongestReadyShare)
            .ToArray();
        var session = longestReady
            .Concat(ready.Except(longestReady).Take(SessionCap - longestReady.Length))
            .ToArray();

        _random.Shuffle(session);
        return session;
    }

    private IEnumerable<Entry> ReadyEntries()
    {
        var today = Today();
        return _entries.Where(entry =>  _scheduler.IsReady(entry.ReviewState, today, _ladder));
    }


    private DateOnly Today() => DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
}
