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
            case OpenOutcome.Opened:
                viewModel.ShowDiary();
                break;

            case OpenOutcome.NoProfile:
                await JsonEntryStore.CreateAsync(profileFolder, DefaultProfileName, clock);
                viewModel.ShowDiary();
                break;

            case OpenOutcome.Newer or OpenOutcome.Damaged:
                viewModel.ShowPlaceholder(outcome.ToString());
                break;

            default:
                throw new UnreachableException($"Open returned an unknown outcome: {outcome}");
        }
    }
}
