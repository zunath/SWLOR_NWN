// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Tga;

/// <summary>
/// A decoded image in canonical top-left, row-major RGBA byte order.
/// </summary>
public sealed class TgaImage
{
    internal TgaImage(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }
}
