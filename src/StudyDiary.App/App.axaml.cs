// StudyDiary — local-first study diary with spaced repetition.
// Copyright (C) 2026 Markus Wallin
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See LICENSE for details.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using StudyDiary.App.ViewModels;
using StudyDiary.App.Views;
using StudyDiary.Data;
using StudyDiary.Domain.Scheduling;
using System.Diagnostics;

namespace StudyDiary.App;

public partial class App : Application
{
    // Written into profile.json on the first run, and shown only once
    // there is a picker (DESIGN §6).
    private const string DefaultProfileName = "Default";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            var window = new MainWindow { DataContext = viewModel };
            LastResortHandler.Install(window);
            desktop.MainWindow = window;
            Open(viewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    // The startup entry point: it only awaits, so anything OpenAsync
    // thros reaches the last-resort handler (ARCHITECTURE).
    private static async void Open(MainWindowViewModel viewModel) =>
        await OpenAsync(viewModel);

    // The composition root: the one place that names the real clock, the
    // real data folder and the JSON store (ARCHITECTURE).
    private static async Task OpenAsync(MainWindowViewModel viewModel)
    {
        var clock = TimeProvider.System;
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);
        var profileFolder = DataLayout.DefaultProfileFolder(localAppData);

        var outcome = await JsonEntryStore.OpenAsync(profileFolder, clock);

        switch (outcome)
        {
            case OpenOutcome.Opened opened:
                viewModel.ShowDiary(await LoadDiaryAsync(opened.Store));
                break;

            case OpenOutcome.NoProfile:
                var created = await JsonEntryStore.CreateAsync(profileFolder, DefaultProfileName, clock);
                viewModel.ShowDiary(await LoadDiaryAsync(created));
                break;

            case OpenOutcome.Newer newer:
                viewModel.ShowNewer(new NewerViewModel(
                    newer.FileSchemaVersion, newer.SupportedSchemaVersion));
                break;

            case OpenOutcome.Damaged damaged:
                var offer = damaged.NewestPassingCopy is { } copy
                    ? new CopyOffer(copy.TakenAt, () => OpenCopyAsync(copy))
                    : null;
                viewModel.ShowDamaged(new DamagedViewModel(
                    damaged.Detail, damaged.KeptAt, offer, clock));
                break;

            default:
                throw new UnreachableException($"Open returned an unknown outcome: {outcome}");
        }

        // The rest of the diary's world: the real scheduler, the shipped
        // ladder, and the shared random source for serving.
        Task<DiaryViewModel> LoadDiaryAsync(IEntryStore store) =>
            DiaryViewModel.LoadAsync(
                store, clock, new LeitnerScheduler(), LeitnerLadder.Default, Random.Shared);

        // Restore stays here with the other JSON-store calls; the damaged
        // screen holds only the offer (ARCHITECTURE).
        async Task OpenCopyAsync(RecoveryCopy copy)
        {
            var restored = await JsonEntryStore.RestoreAsync(profileFolder, copy, clock);
            viewModel.ShowDiary(await LoadDiaryAsync(restored));
        }
    }
}
