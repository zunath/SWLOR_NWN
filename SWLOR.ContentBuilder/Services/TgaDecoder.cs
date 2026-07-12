using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Decodes Truevision TGA images (the format tileset minimap textures ship in) into a WPF
    /// BitmapSource. Supports the variants actually seen in NWN data: uncompressed and RLE,
    /// color-mapped and truecolor/grayscale, 8/15/16/24/32 bpp, either scanline origin.
    /// No third-party imaging libraries are used.
    /// </summary>
    internal static class TgaDecoder
    {
        private const int HeaderSize = 18;

        /// <summary>Returns null if the bytes don't parse as a supported TGA variant.</summary>
        public static BitmapSource Decode(byte[] data)
        {
            if (data == null || data.Length < HeaderSize) return null;

            var idLength = data[0];
            var colorMapType = data[1];
            var imageType = data[2];

            var colorMapFirstEntry = ReadUInt16(data, 3);
            var colorMapLength = ReadUInt16(data, 5);
            var colorMapEntrySize = data[7];

            var width = ReadUInt16(data, 12);
            var height = ReadUInt16(data, 14);
            var pixelDepth = data[16];
            var imageDescriptor = data[17];

            if (width <= 0 || height <= 0) return null;

            var isRle = imageType is 9 or 10 or 11;
            var isColorMapped = imageType is 1 or 9;
            var isTrueColor = imageType is 2 or 10;
            var isGrayscale = imageType is 3 or 11;
            if (!isColorMapped && !isTrueColor && !isGrayscale) return null;

            var offset = HeaderSize + idLength;

            // Color map (palette), only present for color-mapped image types.
            byte[][] palette = null;
            var colorMapBytes = ((colorMapEntrySize + 7) / 8);
            if (colorMapType == 1 && colorMapLength > 0)
            {
                palette = new byte[colorMapFirstEntry + colorMapLength][];
                for (var i = 0; i < colorMapLength; i++)
                {
                    var paletteColor = ReadPixelBytes(data, offset, colorMapEntrySize);
                    palette[colorMapFirstEntry + i] = paletteColor;
                    offset += colorMapBytes;
                }
            }

            var indexBytes = ((pixelDepth + 7) / 8);
            var pixelCount = width * height;
            var bgra = new byte[pixelCount * 4];

            if (!isRle)
            {
                for (var p = 0; p < pixelCount; p++)
                {
                    var pixel = ReadPixelValue(data, offset, indexBytes, isColorMapped, isGrayscale, palette, colorMapEntrySize);
                    Array.Copy(pixel, 0, bgra, p * 4, 4);
                    offset += indexBytes;
                }
            }
            else
            {
                var p = 0;
                while (p < pixelCount)
                {
                    var packetHeader = data[offset];
                    offset += 1;
                    var count = (packetHeader & 0x7F) + 1;
                    var isRunLength = (packetHeader & 0x80) != 0;

                    if (isRunLength)
                    {
                        var pixel = ReadPixelValue(data, offset, indexBytes, isColorMapped, isGrayscale, palette, colorMapEntrySize);
                        offset += indexBytes;
                        for (var i = 0; i < count && p < pixelCount; i++, p++)
                            Array.Copy(pixel, 0, bgra, p * 4, 4);
                    }
                    else
                    {
                        for (var i = 0; i < count && p < pixelCount; i++, p++)
                        {
                            var pixel = ReadPixelValue(data, offset, indexBytes, isColorMapped, isGrayscale, palette, colorMapEntrySize);
                            offset += indexBytes;
                            Array.Copy(pixel, 0, bgra, p * 4, 4);
                        }
                    }
                }
            }

            // Descriptor bit 5 (0x20): 1 = image origin is top-left (rows already stored top-down);
            // 0 = origin is bottom-left (rows stored bottom-up, the common TGA default), so those
            // need flipping to produce the top-down row order BitmapSource expects.
            var topLeftOrigin = (imageDescriptor & 0x20) != 0;
            var stride = width * 4;

            byte[] topDown;
            if (topLeftOrigin)
            {
                topDown = bgra;
            }
            else
            {
                topDown = new byte[bgra.Length];
                for (var row = 0; row < height; row++)
                {
                    var srcRow = height - 1 - row;
                    Array.Copy(bgra, srcRow * stride, topDown, row * stride, stride);
                }
            }

            var bitmap = BitmapSource.Create(
                width, height, 96, 96, PixelFormats.Bgra32, null, topDown, stride);
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>Reads one pixel's raw bytes (index or direct color) and resolves it to BGRA.</summary>
        private static byte[] ReadPixelValue(
            byte[] data, int offset, int indexBytes, bool isColorMapped, bool isGrayscale,
            byte[][] palette, int colorMapEntrySize)
        {
            if (isColorMapped)
            {
                int index = indexBytes switch
                {
                    1 => data[offset],
                    2 => ReadUInt16(data, offset),
                    _ => data[offset]
                };

                if (palette != null && index >= 0 && index < palette.Length && palette[index] != null)
                    return palette[index];

                return new byte[] { 0, 0, 0, 255 };
            }

            if (isGrayscale)
            {
                var g = data[offset];
                return new byte[] { g, g, g, 255 };
            }

            return ReadPixelBytes(data, offset, indexBytes * 8);
        }

        /// <summary>Decodes a direct-color pixel (or palette entry) of the given bit depth to BGRA.</summary>
        private static byte[] ReadPixelBytes(byte[] data, int offset, int bitsPerPixel)
        {
            switch (bitsPerPixel)
            {
                case 8:
                {
                    var g = data[offset];
                    return new byte[] { g, g, g, 255 };
                }
                case 15:
                case 16:
                {
                    var value = ReadUInt16(data, offset);
                    var b = (value & 0x1F) * 255 / 31;
                    var g = ((value >> 5) & 0x1F) * 255 / 31;
                    var r = ((value >> 10) & 0x1F) * 255 / 31;
                    // Minimaps are opaque regardless of the 1555 alpha bit; always render solid.
                    return new byte[] { (byte)b, (byte)g, (byte)r, 255 };
                }
                case 24:
                {
                    var b = data[offset];
                    var g = data[offset + 1];
                    var r = data[offset + 2];
                    return new byte[] { b, g, r, 255 };
                }
                case 32:
                {
                    var b = data[offset];
                    var g = data[offset + 1];
                    var r = data[offset + 2];
                    var a = data[offset + 3];
                    return new byte[] { b, g, r, a };
                }
                default:
                    return new byte[] { 0, 0, 0, 255 };
            }
        }

        private static int ReadUInt16(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }
    }
}
