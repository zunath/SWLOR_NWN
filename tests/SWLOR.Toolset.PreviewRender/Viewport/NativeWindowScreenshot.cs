using Avalonia.Controls;
using Avalonia.Platform;
using SWLOR.Toolset.PreviewRender.Viewport;
using System.Runtime.InteropServices;

namespace SWLOR.Toolset.PreviewRender.Viewport;

internal static class NativeWindowScreenshot
{
    private const uint DibRgbColors = 0;
    private const uint RenderFullContent = 2;

    public static (int Width, int Height, long BytesWritten, nint Hwnd) Capture(Window window, string path)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("A full native window capture requires the Windows desktop compositor.");
        if (!window.IsVisible)
            throw new InvalidOperationException("The AreaEditorView window must be visible before capture.");

        var platformHandle = window.TryGetPlatformHandle()
            ?? throw new InvalidOperationException("Avalonia did not expose the AreaEditorView window handle.");
        var handle = platformHandle.Handle;
        if (!IsWindowVisible(handle))
            throw new InvalidOperationException("The AreaEditorView native window is not visible.");

        var rectangle = new NativeRect();
        if (!GetWindowRect(handle, ref rectangle))
            throw new InvalidOperationException("The AreaEditorView window bounds could not be read.");
        var width = rectangle.Right - rectangle.Left;
        var height = rectangle.Bottom - rectangle.Top;
        if (width < 800 || height < 500 || (long)width * height > 4_000_000)
            throw new InvalidOperationException($"The full window capture size is outside its bounds: {width}x{height}.");
        var screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
            throw new InvalidOperationException("The desktop device context could not be acquired.");

        var memoryDc = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var originalBitmap = IntPtr.Zero;
        try
        {
            memoryDc = CreateCompatibleDC(screenDc);
            bitmap = CreateCompatibleBitmap(screenDc, width, height);
            if (memoryDc == IntPtr.Zero || bitmap == IntPtr.Zero)
                throw new InvalidOperationException("A full-window capture surface could not be allocated.");
            originalBitmap = SelectObject(memoryDc, bitmap);
            DwmFlush();
            if (!PrintWindow(handle, memoryDc, RenderFullContent))
                throw new InvalidOperationException("Windows could not render the offscreen AreaEditorView into the capture surface.");
            SelectObject(memoryDc, originalBitmap);

            var pixels = new byte[checked(width * height * 4)];
            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0,
                    ImageSize = (uint)pixels.Length,
                },
            };
            if (GetDIBits(screenDc, bitmap, 0, (uint)height, pixels, ref info, DibRgbColors) != height)
                throw new InvalidOperationException("The complete window pixels could not be read back.");

            var bottomUpRgba = new byte[pixels.Length];
            var rowBytes = checked(width * 4);
            for (var y = 0; y < height; y++)
            {
                var sourceRow = y * rowBytes;
                var targetRow = (height - y - 1) * rowBytes;
                for (var x = 0; x < width; x++)
                {
                    var source = sourceRow + (x * 4);
                    var target = targetRow + (x * 4);
                    bottomUpRgba[target] = pixels[source + 2];
                    bottomUpRgba[target + 1] = pixels[source + 1];
                    bottomUpRgba[target + 2] = pixels[source];
                    bottomUpRgba[target + 3] = 255;
                }
            }

            FramebufferImage.Save(path, width, height, bottomUpRgba);
            return (width, height, new FileInfo(path).Length, handle);
        }
        finally
        {
            if (originalBitmap != IntPtr.Zero && memoryDc != IntPtr.Zero)
                SelectObject(memoryDc, originalBitmap);
            if (bitmap != IntPtr.Zero)
                DeleteObject(bitmap);
            if (memoryDc != IntPtr.Zero)
                DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint ImageSize;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Color;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, ref NativeRect rectangle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmFlush();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr deviceContext, int width, int height);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicsObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int GetDIBits(
        IntPtr deviceContext, IntPtr bitmap, uint startScan, uint scanLines,
        [Out] byte[] bits, ref BitmapInfo bitmapInfo, uint usage);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr graphicsObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr deviceContext);
}
