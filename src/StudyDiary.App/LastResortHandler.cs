using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace StudyDiary.App;

/// <summary>
/// Where every bug ends up (ARCHITECTURE): one plain message that the last
/// change was not saved and the diary file is unchanged, then the app
/// closes. Installed only once the window exists, so a bug before that
/// crashes outright.
/// </summary>
internal sealed class LastResortHandler
{
    private readonly Window _window;
    private bool _hasFailed;

    private LastResortHandler(Window window)
    {
        _window = window;
    }

    public static void Install(Window window)
    {
        var handler = new LastResortHandler(window);
        Dispatcher.UIThread.UnhandledException += handler.OnUnhandledException;
    }

    // As little as possible inside the event (ARCHITECTURE). A second
    // failure is left unhandled, so a bug in ShowFailure crashes rather
    // than loops.
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_hasFailed)
        {
            return;
        }

        _hasFailed = true;
        e.Handled = true;
        var exception = e.Exception; // Avalonia reuses the event's arguments
        Dispatcher.UIThread.Post(() => ShowFailure(exception));
    }

    private void ShowFailure(Exception exception)
    {
        Console.Error.WriteLine(exception);

        var close = new Button { Content = "Close" };
        close.Click += (_, _) => _window.Close();

        _window.Content = new ScrollViewer
        {
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "The app ran into a problem and has to close",
                        FontSize = 20,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    Paragraph("Your diary is safe. Everything saved before this is still in your diary file."),
                    Paragraph("The last thing you did may not have been saved. When you open the app again, check whether it is there."),
                    Paragraph("The app is closing because it can no longer be sure that what it shows matches your diary file."),
                    new Expander
                    {
                        Header = "Details for a bug report",
                        Content = new SelectableTextBlock
                        {
                            Text = exception.ToString(),
                            TextWrapping = TextWrapping.Wrap,
                        },
                    },
                    close,
                },
            },
        };
    }

    private static TextBlock Paragraph(string text) =>
        new() { Text = text, TextWrapping = TextWrapping.Wrap };
}
