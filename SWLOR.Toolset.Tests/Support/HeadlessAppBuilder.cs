using Avalonia;
using Avalonia.Headless;

namespace SWLOR.Toolset.Tests.Support;

/// <summary>Uses the application's styles and templates with real Skia rendering.</summary>
public static class HeadlessAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .UseSkia()
        .WithInterFont();
}
