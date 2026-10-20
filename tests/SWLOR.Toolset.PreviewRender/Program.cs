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
        // The viewport needs OpenGL. Avalonia refuses software GL (Mesa llvmpipe) on X11 by default
        // and falls back to a renderer that cannot host it, so allow it for GPU-less Linux hosts.
        // Windows ignores X11 options.
        return appBuilder.UsePlatformDetect()
            .With(new X11PlatformOptions { GlxRendererBlacklist = [] })
            .StartWithClassicDesktopLifetime(args);
    }
}
