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
using System.Diagnostics;
using System.Windows.Markup;

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
                viewModel.ShowDiary(await DiaryViewModel.LoadAsync(opened.Store));
                break;

            case OpenOutcome.NoProfile:
                var store = await JsonEntryStore.CreateAsync(profileFolder, DefaultProfileName, clock);
                viewModel.ShowDiary(await DiaryViewModel.LoadAsync(store));
                break;

            case OpenOutcome.Newer or OpenOutcome.Damaged:
                viewModel.ShowPlaceholder(outcome.ToString());
                break;

            default:
                throw new UnreachableException($"Open returned an unknown outcome: {outcome}");
        }
    }
}
