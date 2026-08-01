using Avalonia;

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
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
}
