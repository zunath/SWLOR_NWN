// SPDX-License-Identifier: MIT

using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Plt;

/// <summary>
/// Reads Aurora PLT V1 layered textures without applying palette policy.
/// </summary>
public static class PltReader
{
    private const int HeaderSize = 24;
    private const int MaximumDimension = 16_384;
    private const int MaximumPixels = 64_000_000;

    public static PltFile Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Read(File.ReadAllBytes(path));
    }

    public static PltFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var reader = new GuardedBinaryReader(bytes);
        reader.ValidateRange(0, HeaderSize, "PLT header");
        if (reader.ReadAscii(0, 4, "PLT signature") != "PLT ")
            throw new NwnFormatException("Invalid PLT signature.");
        if (reader.ReadAscii(4, 4, "PLT version") != "V1  ")
            throw new NwnFormatException("Unsupported PLT version; expected V1.");

        var width = reader.ReadUInt32(16);
        var height = reader.ReadUInt32(20);
        if (width == 0 || height == 0 || width > MaximumDimension || height > MaximumDimension)
            throw new NwnFormatException($"PLT dimensions {width}x{height} are invalid.");

        long pixelCount;
        try
        {
            pixelCount = checked((long)width * height);
        }
        catch (OverflowException ex)
        {
            throw new NwnFormatException("PLT pixel count overflows.", ex);
        }
        if (pixelCount > MaximumPixels)
            throw new NwnFormatException($"PLT pixel count {pixelCount} exceeds {MaximumPixels}.");

        var payloadLength = checked(pixelCount * 2);
        reader.ValidateRange(HeaderSize, payloadLength, "PLT pixels");
        if (reader.Length != HeaderSize + payloadLength)
            throw new NwnFormatException("PLT length does not exactly match its declared dimensions.");

        var pixels = new PltPixel[checked((int)pixelCount)];
        for (var index = 0; index < pixels.Length; index++)
        {
            var intensity = reader.ReadByte(HeaderSize + index * 2L);
            var layer = reader.ReadByte(HeaderSize + index * 2L + 1);
            if (layer >= PltLayers.Count)
                throw new NwnFormatException($"PLT pixel {index} uses invalid layer {layer}.");
            pixels[index] = new PltPixel(intensity, layer);
        }

        return new PltFile(checked((int)width), checked((int)height), pixels);
    }
}
