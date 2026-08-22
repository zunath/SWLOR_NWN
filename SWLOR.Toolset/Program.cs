using Avalonia;
using Serilog;

namespace SWLOR.Toolset
{
    internal static class Program
    {
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                // Native popup windows can still deliver a queued pointer message after Avalonia
                // disposes their PopupRoot, which logs "PlatformImpl is null" on every dropdown or
                // gallery close. Keeping toolset popups in the main window avoids that teardown race.
                .With(new Win32PlatformOptions { OverlayPopups = true })
                .WithInterFont()
                .LogToTrace();

        [STAThread]
        public static void Main(string[] args)
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SWLOR Toolset",
                "Logs",
                "toolset-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.WithProperty("Application", "SWLOR.Toolset")
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true)
                .CreateLogger();

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SWLOR Toolset terminated unexpectedly.");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
