// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Plt;

/// <summary>
/// A parsed PLT V1 layered texture.
/// </summary>
public sealed class PltFile
{
    internal PltFile(int width, int height, IReadOnlyList<PltPixel> pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<PltPixel> Pixels { get; }
}
