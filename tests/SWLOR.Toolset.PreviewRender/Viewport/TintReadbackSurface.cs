using Avalonia.OpenGL;
using Nwn.Toolset.Avalonia.Viewport;
using Silk.NET.OpenGL;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.PreviewRender.Viewport;

internal sealed class TintReadbackSurface : ModelViewportSurface
{
    private readonly string _output;
    private int _frames;
    private bool _complete;

    public TintReadbackSurface(NativeModelPreviewData preview, string output)
    {
        _output = Path.GetFullPath(output);
        SetScene(preview.Scene);
        SetTextures(preview.Textures);
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
        var colored = 0;
        for (var index = 0; index < bytes.Length; index += 4)
        {
            if (Math.Abs(bytes[index] - 31) <= 2 && Math.Abs(bytes[index + 1] - 36) <= 2 && Math.Abs(bytes[index + 2] - 46) <= 2) continue;
            if (Math.Abs(bytes[index] - bytes[index + 1]) > 8 || Math.Abs(bytes[index + 1] - bytes[index + 2]) > 8) colored++;
        }
        if (colored < 1000 || GeometryUploadCount != 1 || TextureUploadCount != 1)
        { Finish(false, $"Unexpected frame: {colored} colored pixels, {GeometryUploadCount} geometry and {TextureUploadCount} texture uploads."); return; }
        if (_frames++ == 0) { RequestNextFrameRendering(); return; }
        FramebufferImage.Save(_output, width, height, bytes);
        Finish(true, $"SWLOR pfa0_chest001: {width}x{height}, {colored} colored pixels; one geometry/texture upload reused; {ContextDescription}; {_output}.");
    }

    private void Finish(bool passed, string message)
    { _complete = true; Completed?.Invoke(passed, message); }
}
