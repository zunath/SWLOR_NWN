using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SWLOR.Toolset.PreviewRender.Viewport;

internal static class FramebufferImage
{
    public static void Save(string path, int width, int height, byte[] bottomUp)
    {
        var topDown = new byte[bottomUp.Length];
        var stride = checked(width * 4);
        for (var row = 0; row < height; row++)
            Array.Copy(bottomUp, row * stride, topDown, (height - row - 1) * stride, stride);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var pinned = GCHandle.Alloc(topDown, GCHandleType.Pinned);
        try
        {
            using var bitmap = new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Opaque, pinned.AddrOfPinnedObject(), new PixelSize(width, height), new Vector(96, 96), stride);
            bitmap.Save(path);
        }
        finally { pinned.Free(); }
    }
}
