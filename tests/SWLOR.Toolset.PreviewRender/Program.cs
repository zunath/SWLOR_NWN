using Avalonia;
using SWLOR.Toolset.PreviewRender.Application;

namespace SWLOR.Toolset.PreviewRender;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => AppBuilder.Configure<NativeViewportApplication>()
        .UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
