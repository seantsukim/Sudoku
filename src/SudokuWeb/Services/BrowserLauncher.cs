using System.Diagnostics;

namespace SudokuWeb.Services;

/// <summary>
/// Opens the Sudoku page in the default web browser once the server is ready,
/// so running <c>dotnet run</c> from the command line brings up the game directly.
/// </summary>
public static class BrowserLauncher
{
    /// <summary>
    /// Waits for the app to start listening, then opens its address in the browser.
    /// Only runs when the "OpenBrowser" setting is true (it is in appsettings.Development.json).
    /// Turn it off for one run with: dotnet run --project src/SudokuWeb -- --OpenBrowser false
    /// </summary>
    public static void OpenWhenStarted(WebApplication app)
    {
        // Skip when the setting is off, or when hosted by IIS / IIS Express
        // (Visual Studio already opens the browser for that profile).
        if (!app.Configuration.GetValue<bool>("OpenBrowser")
            || Environment.GetEnvironmentVariable("APP_POOL_ID") is not null)
        {
            return;
        }

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            // Prefer the http:// address to avoid certificate warnings on first run.
            string? url = app.Urls.FirstOrDefault(u => u.StartsWith("http://"))
                          ?? app.Urls.FirstOrDefault();
            if (url is null)
            {
                return;
            }

            // A server bound to "every address" (0.0.0.0, [::], * or +) is reached at localhost.
            url = url.Replace("0.0.0.0", "localhost")
                     .Replace("[::]", "localhost")
                     .Replace("://*", "://localhost")
                     .Replace("://+", "://localhost");

            try
            {
                // UseShellExecute hands the URL to the operating system, which opens the
                // default browser (Windows shell, "open" on macOS, "xdg-open" on Linux).
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                app.Logger.LogInformation("Opened {Url} in your browser.", url);
            }
            catch (Exception ex)
            {
                // No browser available (e.g. a server without a desktop): just tell the user where to go.
                app.Logger.LogWarning("Could not open a browser automatically ({Reason}). Open {Url} yourself.", ex.Message, url);
            }
        });
    }
}
