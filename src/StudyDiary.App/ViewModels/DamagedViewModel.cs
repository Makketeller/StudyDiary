// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using System.Globalization;
using StudyDiary.Data;

namespace StudyDiary.App.ViewModels;

/// <summary>
/// The screen for a diary that would not open (DESIGN §7): what is safe,
/// where the damaged files were kept, the newest copy that opens, and the
/// details for fixing the file by hand.
/// </summary>
public sealed class DamagedViewModel : ViewModelBase
{
    private readonly CopyOffer? _offer;
    private bool _canOpenCopy;


    public DamagedViewModel(DamageDetail detail, string? keptAt, CopyOffer? offer, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(detail);
        ArgumentNullException.ThrowIfNull(clock);

        _offer = offer;
        _canOpenCopy = offer is not null;

        Message = offer is null
            ? "Your diary couldn't be opened, so nothing has been changed. StudyDiary keeps its "
              + "own copies, but none of them could be opened either."
            : "Your diary couldn't be opened, so nothing has been changed. StudyDiary keeps its "
              + $"own copies, and the most recent one is from {When(offer.TakenAt, clock)}. "
              + "Open that copy instead?";

        KeptAtMessage = keptAt is null
            ? null
            : $"The damaged files have been kept at {keptAt}. "
              + "You can look at them yourself if you want to.";

        Details = DetailsFor(detail);
    }

    public string Message { get; }

    /// <summary>Null when neither file was there to keep (DESIGN §7).</summary>
    public string? KeptAtMessage { get; }

    public string Details { get; }

    public bool HasCopy => _offer is not null;

    public bool CanOpenCopy
    {
        get => _canOpenCopy;
        private set => SetField(ref _canOpenCopy, value);
    }


    // Bound to the Open that copy button: it only awaits (ARCHITECTURE).
    public async void OpenCopy() => await OpenCopyAsync();

    public async Task OpenCopyAsync()
    {
        if (_offer is null || !CanOpenCopy)
            throw new InvalidOperationException("No copy is waiting to be opened.");

        // Disabled before the restore, like every save: a double click
        // cannot restore twice.
        CanOpenCopy = false;
        await _offer.Open();
    }

    private static string When(DateTime takenAt, TimeProvider clock)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var day = DateOnly.FromDateTime(takenAt);
        var time = takenAt.ToString("HH:mm", CultureInfo.InvariantCulture);

        if (day == today)
            return $"today at {time}";
        if (day == today.AddDays(-1))
            return $"yesterday at {time}";

        return $"{day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} at {time}";
    }

    // The line as editors count it, and the reader's own message as it is,
    // which counts from 0 and is said so beside it (DESIGN §7).
    private static string DetailsFor(DamageDetail detail)
    {
        var lines = new List<string> { $"File: {detail.FileName}" };
        if (detail.Line is not null)
            lines.Add($"Line: {detail.Line}");
        if (detail.JsonPath is not null)
            lines.Add($"Where in the file: {detail.JsonPath}");
        lines.Add($"Reader's message (it counts lines from 0): {detail.Message}");

        return string.Join('\n', lines);
    }
}
