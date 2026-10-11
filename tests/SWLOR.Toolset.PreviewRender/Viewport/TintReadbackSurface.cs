using Avalonia.OpenGL;
using Nwn.Preview.Areas;
using Nwn.Toolset.Avalonia.Areas;
using Silk.NET.OpenGL;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.PreviewRender.Viewport;

internal sealed class TintReadbackSurface : AreaViewportControl
{
    private readonly string _output;
    private int _frames;
    private bool _complete;

    public TintReadbackSurface(AreaScene scene, SwlorAreaViewportMaterialProvider materialProvider, string output)
    {
        _output = Path.GetFullPath(output);
        Scene = scene;
        MaterialProvider = materialProvider;
        MeshMetadataProvider = materialProvider;
        RenderStatusChanged += (_, message) => Console.Error.WriteLine("viewport: " + message);
    }

    public event Action<bool, string>? Completed;

    protected override unsafe void OnOpenGlRender(GlInterface gl, int framebuffer)
    {
        base.OnOpenGlRender(gl, framebuffer);
        if (_complete) return;
        using var api = GL.GetApi(gl.GetProcAddress);
        var viewport = stackalloc int[4];
        api.GetInteger(GetPName.Viewport, viewport);
        var width = viewport[2];
        var height = viewport[3];
        if (width < 100 || height < 100 || (long)width * height > 4_000_000)
        { Finish(false, "The framebuffer is outside its qualification bounds."); return; }
        var bytes = new byte[checked(width * height * 4)];
        fixed (byte* pixels = bytes) api.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        FramebufferImage.Save(_output, width, height, bytes);
        var changed = 0;
        for (var index = 0; index < bytes.Length; index += 4)
        {
            if (Math.Abs(bytes[index] - 102) > 8 || Math.Abs(bytes[index + 1] - 102) > 8 || Math.Abs(bytes[index + 2] - 102) > 8)
                changed++;
        }
        if (changed < 1000)
        { Finish(false, $"The shared area renderer changed only {changed} background pixels at {width}x{height}."); return; }
        if (_frames++ == 0) { RequestNextFrameRendering(); return; }
        Finish(true, $"SWLOR pfa0_chest001 through AreaViewportControl: {width}x{height}, {changed} visible pixels; {_output}.");
    }

    private void Finish(bool passed, string message)
    {
        _complete = true;
        Completed?.Invoke(passed, message);
    }
}
