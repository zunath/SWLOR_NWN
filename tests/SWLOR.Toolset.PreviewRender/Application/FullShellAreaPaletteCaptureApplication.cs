using Avalonia.Controls.ApplicationLifetimes;
using SWLOR.Toolset;

namespace SWLOR.Toolset.PreviewRender.Application;

internal sealed class FullShellAreaPaletteCaptureApplication : App
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            throw new InvalidOperationException("A real desktop lifetime is required for full-shell capture.");
        }

        try
        {
            FullShellAreaPaletteCapture.Start(desktop, this);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Full-shell SWLOR Palette capture setup failed: " + exception);
            desktop.Shutdown(1);
        }
    }
}
