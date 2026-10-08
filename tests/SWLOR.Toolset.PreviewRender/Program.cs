using Avalonia;
using SWLOR.Toolset.PreviewRender.Application;

namespace SWLOR.Toolset.PreviewRender;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var mode = Environment.GetEnvironmentVariable("SWLOR_PREVIEW_RENDER_MODE");
        var appBuilder = mode switch
        {
            "area-editor" => AppBuilder.Configure<FullAreaEditorCaptureApplication>(),
            "full-shell-area-palette" => AppBuilder.Configure<FullShellAreaPaletteCaptureApplication>()
                .With(new Win32PlatformOptions { OverlayPopups = true })
                .WithInterFont()
                .LogToTrace(),
            _ => AppBuilder.Configure<NativeViewportApplication>()
        };
        return appBuilder.UsePlatformDetect().StartWithClassicDesktopLifetime(args);
    }
}
