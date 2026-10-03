using Avalonia;
using SWLOR.Toolset.PreviewRender.Application;

namespace SWLOR.Toolset.PreviewRender;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var appBuilder = string.Equals(Environment.GetEnvironmentVariable("SWLOR_PREVIEW_RENDER_MODE"), "area-editor", StringComparison.Ordinal)
            ? AppBuilder.Configure<FullAreaEditorCaptureApplication>()
            : AppBuilder.Configure<NativeViewportApplication>();
        return appBuilder.UsePlatformDetect().StartWithClassicDesktopLifetime(args);
    }
}
