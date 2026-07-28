// SPDX-License-Identifier: MIT

using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Tga;

/// <summary>
/// Decodes uncompressed and RLE true-color, grayscale, and color-mapped TGA images.
/// </summary>
public static class TgaReader
{
    private const int HeaderSize = 18;
    private const int MaximumDimension = 16_384;
    private const int MaximumPixels = 64_000_000;

    public static TgaImage Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Read(File.ReadAllBytes(path));
    }

    public static TgaImage Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var reader = new GuardedBinaryReader(bytes);
        reader.ValidateRange(0, HeaderSize, "TGA header");

        var idLength = reader.ReadByte(0);
        var colorMapType = reader.ReadByte(1);
        var imageType = reader.ReadByte(2);
        var colorMapFirst = reader.ReadUInt16(3);
        var colorMapLength = reader.ReadUInt16(5);
        var colorMapDepth = reader.ReadByte(7);
        var width = reader.ReadUInt16(12);
        var height = reader.ReadUInt16(14);
        var pixelDepth = reader.ReadByte(16);
        var descriptor = reader.ReadByte(17);
        var attributeBits = descriptor & 0x0F;

        if ((descriptor & 0xC0) != 0)
            throw new NwnFormatException("TGA interleaved image data is not supported.");
        if (width == 0 || height == 0 || width > MaximumDimension || height > MaximumDimension)
            throw new NwnFormatException($"TGA dimensions {width}x{height} are invalid.");
        var pixelCount = checked((int)width * height);
        if (pixelCount > MaximumPixels)
            throw new NwnFormatException($"TGA pixel count {pixelCount} exceeds {MaximumPixels}.");

        var rle = imageType is 9 or 10 or 11;
        var baseType = rle ? imageType - 8 : imageType;
        if (baseType is not (1 or 2 or 3))
            throw new NwnFormatException($"Unsupported TGA image type {imageType}.");
        if (colorMapType is > 1 || (baseType == 1) != (colorMapType == 1))
            throw new NwnFormatException("TGA color-map declaration does not match its image type.");
        if (baseType == 2 && pixelDepth is not (24 or 32))
            throw new NwnFormatException($"Unsupported TGA true-color depth {pixelDepth}.");
        if (baseType == 3 && pixelDepth is not (8 or 16))
            throw new NwnFormatException($"Unsupported TGA grayscale depth {pixelDepth}.");
        if (baseType == 1 && pixelDepth is not (8 or 16))
            throw new NwnFormatException($"Unsupported TGA color-map index depth {pixelDepth}.");

        var cursor = HeaderSize + idLength;
        reader.ValidateRange(HeaderSize, idLength, "TGA image ID");

        Rgba32[]? palette = null;
        if (colorMapType == 1)
        {
            if (colorMapLength == 0 || colorMapDepth is not (24 or 32))
                throw new NwnFormatException("TGA color map must contain 24-bit or 32-bit entries.");
            var paletteEntryBytes = colorMapDepth / 8;
            reader.ValidateRange(cursor, (long)colorMapLength * paletteEntryBytes, "TGA color map");
            palette = new Rgba32[colorMapLength];
            for (var index = 0; index < colorMapLength; index++)
            {
                palette[index] = DecodeBgra(
                    reader,
                    cursor,
                    paletteEntryBytes,
                    attributeBits > 0);
                cursor += paletteEntryBytes;
            }
        }

        var sourceBytesPerPixel = pixelDepth / 8;
        var output = new byte[checked(pixelCount * 4)];
        var originTop = (descriptor & 0x20) != 0;
        var originRight = (descriptor & 0x10) != 0;
        var written = 0;
        while (written < pixelCount)
        {
            var packetCount = 1;
            var repeated = false;
            if (rle)
            {
                var packet = reader.ReadByte(cursor++);
                packetCount = (packet & 0x7F) + 1;
                repeated = (packet & 0x80) != 0;
                if (packetCount > pixelCount - written)
                    throw new NwnFormatException("TGA RLE packet overruns the declared image dimensions.");
            }

            if (repeated)
            {
                var pixel = DecodeSourcePixel(
                    reader,
                    ref cursor,
                    baseType,
                    sourceBytesPerPixel,
                    palette,
                    colorMapFirst,
                    attributeBits);
                for (var count = 0; count < packetCount; count++)
                    WritePixel(output, width, height, written++, originTop, originRight, pixel);
            }
            else
            {
                for (var count = 0; count < packetCount; count++)
                {
                    var pixel = DecodeSourcePixel(
                        reader,
                        ref cursor,
                        baseType,
                        sourceBytesPerPixel,
                        palette,
                        colorMapFirst,
                        attributeBits);
                    WritePixel(output, width, height, written++, originTop, originRight, pixel);
                }
            }
        }

        return new TgaImage(width, height, output);
    }

    private static Rgba32 DecodeSourcePixel(
        GuardedBinaryReader reader,
        ref int cursor,
        int baseType,
        int sourceBytesPerPixel,
        Rgba32[]? palette,
        ushort colorMapFirst,
        int attributeBits)
    {
        if (baseType == 2)
        {
            // NWN-corpus 32-bit TGAs frequently declare 0 attribute bits in the image
            // descriptor while still carrying meaningful alpha in the 4th byte, and the
            // NWN engine honors that byte regardless of the declared attribute bits. So
            // for 32-bpp true-color pixels (image types 2 and 10), always surface the 4th
            // byte as alpha rather than gating on attributeBits. This applies to both the
            // uncompressed and RLE decode paths, which both funnel through this method.
            var result = DecodeBgra(
                reader,
                cursor,
                sourceBytesPerPixel,
                hasAlpha: true);
            cursor += sourceBytesPerPixel;
            return result;
        }

        if (baseType == 3)
        {
            reader.ValidateRange(cursor, sourceBytesPerPixel, "TGA grayscale pixel");
            var intensity = reader.ReadByte(cursor);
            var alpha = sourceBytesPerPixel == 2 && attributeBits > 0
                ? reader.ReadByte(cursor + 1)
                : (byte)255;
            cursor += sourceBytesPerPixel;
            return new Rgba32(intensity, intensity, intensity, alpha);
        }

        var rawIndex = sourceBytesPerPixel == 1 ? reader.ReadByte(cursor) : reader.ReadUInt16(cursor);
        cursor += sourceBytesPerPixel;
        var paletteIndex = rawIndex - colorMapFirst;
        if (palette == null || paletteIndex < 0 || paletteIndex >= palette.Length)
            throw new NwnFormatException($"TGA palette index {rawIndex} is outside the declared color map.");
        return palette[paletteIndex];
    }

    private static Rgba32 DecodeBgra(
        GuardedBinaryReader reader,
        long offset,
        int byteCount,
        bool hasAlpha)
    {
        reader.ValidateRange(offset, byteCount, "TGA color pixel");
        return new Rgba32(
            reader.ReadByte(offset + 2),
            reader.ReadByte(offset + 1),
            reader.ReadByte(offset),
            byteCount == 4 && hasAlpha ? reader.ReadByte(offset + 3) : (byte)255);
    }

    private static void WritePixel(
        byte[] output,
        int width,
        int height,
        int sourceIndex,
        bool originTop,
        bool originRight,
        Rgba32 pixel)
    {
        var sourceX = sourceIndex % width;
        var sourceY = sourceIndex / width;
        var targetX = originRight ? width - 1 - sourceX : sourceX;
        var targetY = originTop ? sourceY : height - 1 - sourceY;
        var target = (targetY * width + targetX) * 4;
        output[target] = pixel.R;
        output[target + 1] = pixel.G;
        output[target + 2] = pixel.B;
        output[target + 3] = pixel.A;
    }

    private readonly record struct Rgba32(byte R, byte G, byte B, byte A);
}
