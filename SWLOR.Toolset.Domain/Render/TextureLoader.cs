// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using Pfim;
using SWLOR.NWN.Formats.Plt;
using SWLOR.NWN.Formats.Tga;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>The source representation from which a preview texture was decoded.</summary>
    public enum TextureSourceFormat
    {
        Tga,
        Dds,
        Plt
    }

    /// <summary>A decoded texture in canonical top-left, row-major RGBA byte order.</summary>
    public sealed class TextureImage
    {
        public const byte DefaultAlphaCutoff = 96;

        public required int Width { get; init; }
        public required int Height { get; init; }
        public required byte[] Pixels { get; init; }
        public required TextureSourceFormat SourceFormat { get; init; }

        /// <summary>
        /// Texels below this value are discarded by software preview rendering. Ordinary textures
        /// retain the historic default; material-derived tint textures carry their shader cutoff.
        /// </summary>
        public byte AlphaCutoff { get; init; } = DefaultAlphaCutoff;

        /// <summary>
        /// The compact BioWare DDS header's authored alpha mean, when present. Standard DDS, TGA,
        /// and PLT sources leave this null.
        /// </summary>
        public float? AlphaMean { get; init; }
    }

    /// <summary>
    /// Resolves and decodes the texture representations consumed by toolset previews.
    /// </summary>
    /// <remarks>
    /// TGA decoding is delegated to the standalone formats library. Standard DDS is decoded by
    /// Pfim; compact BioWare DDS is decoded directly from its 20-byte little-endian header and
    /// DXT payload. PLT remains palette policy here rather than in the low-level PLT reader.
    /// </remarks>
    public static class TextureLoader
    {
        private const int CompactDdsHeaderSize = 20;
        private const int MaximumDimension = 16_384;
        private const int MaximumPixels = 64_000_000;
        private const int MaximumCompressedBytes = 512 * 1024 * 1024;

        private static readonly string[] PaletteNames =
        {
            "pal_skin01",
            "pal_hair01",
            "pal_armor01",
            "pal_armor02",
            "pal_cloth01",
            "pal_cloth01",
            "pal_leath01",
            "pal_leath01",
            "pal_tattoo01",
            "pal_tattoo01"
        };

        /// <summary>
        /// Resolves an extensionless texture name. When PLT color choices are supplied, PLT wins;
        /// otherwise ordinary TGA/DDS artwork is preferred before the PLT fallback.
        /// </summary>
        public static TextureImage? Load(
            ResourceIndex resourceIndex,
            string resRef,
            IReadOnlyDictionary<int, int>? layerColorIndices = null)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            // An authored TGA always wins, dyes or not. A PLT is only a picture once its layers are
            // coloured, so it looks like the obvious source for a dyeable part - but where a part
            // ships both, the TGA is the appearance the artist baked and the PLT alongside it is a
            // base-game leftover under the same name. Preferring the PLT repainted SWLOR's custom
            // parts in unrelated palette colours. Genuinely dyeable parts carry a PLT and no TGA,
            // which is how Aurora decides the same question.
            return LoadTga(resourceIndex, resRef) ??
                   LoadDds(resourceIndex, resRef) ??
                   LoadPlt(resourceIndex, resRef, layerColorIndices);
        }

        /// <summary>Loads a TGA resource, returning null for missing or malformed data.</summary>
        public static TextureImage? LoadTga(ResourceIndex resourceIndex, string resRef)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);
            if (!TryGetBytes(resourceIndex, resRef, "tga", out var bytes))
                return null;

            try
            {
                var image = TgaReader.Read(bytes);
                return new TextureImage
                {
                    Width = image.Width,
                    Height = image.Height,
                    Pixels = image.Pixels,
                    SourceFormat = TextureSourceFormat.Tga
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Loads either a standard DDS stream or Aurora's compact 20-byte DDS representation.
        /// </summary>
        public static TextureImage? LoadDds(ResourceIndex resourceIndex, string resRef)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);
            if (!TryGetBytes(resourceIndex, resRef, "dds", out var bytes))
                return null;

            try
            {
                return IsStandardDds(bytes)
                    ? DecodeStandardDds(bytes)
                    : DecodeCompactDds(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Loads and recolors an Aurora PLT. Missing palette resources use deterministic grayscale,
        /// keeping a preview useful in a hak-only resource index.
        /// </summary>
        public static TextureImage? LoadPlt(
            ResourceIndex resourceIndex,
            string resRef,
            IReadOnlyDictionary<int, int>? layerColorIndices = null)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);
            if (!TryGetBytes(resourceIndex, resRef, "plt", out var bytes))
                return null;

            try
            {
                var plt = PltReader.Read(bytes);
                var palettes = new TextureImage?[PltLayers.Count];
                var paletteLoaded = new bool[PltLayers.Count];
                var output = new byte[checked(plt.Width * plt.Height * 4)];

                for (var sourceIndex = 0; sourceIndex < plt.Pixels.Count; sourceIndex++)
                {
                    var sourceX = sourceIndex % plt.Width;
                    var sourceY = sourceIndex / plt.Width;
                    var targetY = plt.Height - 1 - sourceY;
                    var targetOffset = (targetY * plt.Width + sourceX) * 4;
                    var pixel = plt.Pixels[sourceIndex];

                    var layer = pixel.Layer;
                    if (!paletteLoaded[layer])
                    {
                        palettes[layer] = LoadTga(resourceIndex, PaletteNames[layer]);
                        paletteLoaded[layer] = true;
                    }

                    var palette = palettes[layer];
                    if (palette == null || palette.Width <= 0 || palette.Height <= 0 ||
                        palette.Pixels.Length < palette.Width * palette.Height * 4)
                    {
                        output[targetOffset] = pixel.Intensity;
                        output[targetOffset + 1] = pixel.Intensity;
                        output[targetOffset + 2] = pixel.Intensity;
                        output[targetOffset + 3] = pixel.Intensity == 0 ? (byte)0 : (byte)255;
                        continue;
                    }

                    var row = layerColorIndices != null &&
                              layerColorIndices.TryGetValue(layer, out var selected)
                        ? Math.Clamp(selected, 0, palette.Height - 1)
                        : 0;
                    var column = palette.Width == 1
                        ? 0
                        : pixel.Intensity * (palette.Width - 1) / 255;
                    var paletteOffset = (row * palette.Width + column) * 4;

                    output[targetOffset] = palette.Pixels[paletteOffset];
                    output[targetOffset + 1] = palette.Pixels[paletteOffset + 1];
                    output[targetOffset + 2] = palette.Pixels[paletteOffset + 2];
                    output[targetOffset + 3] = palette.Pixels[paletteOffset + 3];
                }

                return new TextureImage
                {
                    Width = plt.Width,
                    Height = plt.Height,
                    Pixels = output,
                    SourceFormat = TextureSourceFormat.Plt
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool TryGetBytes(
            ResourceIndex resourceIndex,
            string resRef,
            string extension,
            out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            var fileName = Path.GetFileName(resRef.Trim());
            var resourceExtension = "." + extension.TrimStart('.');
            var normalizedResRef = fileName.EndsWith(
                resourceExtension,
                StringComparison.OrdinalIgnoreCase)
                ? fileName[..^resourceExtension.Length]
                : fileName;
            var identity = new ResourceIdentity(
                normalizedResRef,
                ResourceIdentity.TypeFromExtension(extension));
            if (!resourceIndex.TryLookup(identity, out var handle))
                return false;

            // Only "not indexed" means "no such artwork". A read that throws (file vanished,
            // sharing violation, BIF extraction failure) must escape so callers report a failure
            // and retry later, instead of persisting a no-artwork result for a real texture.
            bytes = handle.GetBytes();
            return bytes.Length > 0;
        }

        private static bool IsStandardDds(byte[] bytes) =>
            bytes.Length >= 4 &&
            bytes[0] == (byte)'D' &&
            bytes[1] == (byte)'D' &&
            bytes[2] == (byte)'S' &&
            bytes[3] == (byte)' ';

        private static TextureImage DecodeStandardDds(byte[] bytes)
        {
            ValidateStandardDdsHeader(bytes);

            using var stream = new MemoryStream(bytes, writable: false);
            using var image = Pfimage.FromStream(stream);
            ValidateDimensions(image.Width, image.Height);

            var output = new byte[checked(image.Width * image.Height * 4)];
            var sourcePixelBytes = image.Format switch
            {
                ImageFormat.Rgba32 => 4,
                ImageFormat.Rgb24 => 3,
                ImageFormat.Rgb8 => 1,
                _ => throw new InvalidDataException($"Unsupported decoded DDS pixel format {image.Format}.")
            };

            var stride = Math.Abs(image.Stride);
            if (stride < checked(image.Width * sourcePixelBytes) ||
                image.Data.Length < checked(stride * image.Height))
            {
                throw new InvalidDataException("Decoded DDS rows do not fit the returned pixel buffer.");
            }

            for (var y = 0; y < image.Height; y++)
            {
                // Pfim exposes positive-stride DDS rows in file order. NWN artwork was authored
                // for the engine's bottom-up UV convention, so the toolset's consumer-facing
                // orientation reverses those rows. Negative-stride output is already reversed.
                var sourceY = image.Stride > 0 ? image.Height - 1 - y : y;
                var sourceRow = sourceY * stride;
                var targetRow = y * image.Width * 4;
                for (var x = 0; x < image.Width; x++)
                {
                    var source = sourceRow + x * sourcePixelBytes;
                    var target = targetRow + x * 4;
                    if (sourcePixelBytes == 1)
                    {
                        output[target] = image.Data[source];
                        output[target + 1] = image.Data[source];
                        output[target + 2] = image.Data[source];
                        output[target + 3] = 255;
                    }
                    else
                    {
                        // Pfim exposes DDS color bytes blue-first.
                        output[target] = image.Data[source + 2];
                        output[target + 1] = image.Data[source + 1];
                        output[target + 2] = image.Data[source];
                        output[target + 3] = sourcePixelBytes == 4 ? image.Data[source + 3] : (byte)255;
                    }
                }
            }

            return new TextureImage
            {
                Width = image.Width,
                Height = image.Height,
                Pixels = output,
                SourceFormat = TextureSourceFormat.Dds
            };
        }

        private static void ValidateStandardDdsHeader(byte[] bytes)
        {
            const int standardHeaderSize = 128;
            if (bytes.Length < standardHeaderSize)
                throw new InvalidDataException("Standard DDS header is truncated.");
            if (!IsStandardDds(bytes))
                throw new InvalidDataException("Standard DDS signature is invalid.");
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)) != 124)
                throw new InvalidDataException("Standard DDS header size must be 124 bytes.");
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(76, 4)) != 32)
                throw new InvalidDataException("Standard DDS pixel-format header size must be 32 bytes.");

            var rawHeight = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12, 4));
            var rawWidth = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16, 4));
            if (rawWidth > int.MaxValue || rawHeight > int.MaxValue)
                throw new InvalidDataException($"Standard DDS dimensions {rawWidth}x{rawHeight} are invalid.");

            // This check deliberately precedes Pfim.FromStream. A hostile header must not be allowed
            // to make the third-party decoder size or allocate a surface outside project limits.
            ValidateDimensions((int)rawWidth, (int)rawHeight);
            _ = checked((int)rawWidth * (int)rawHeight * 4);
        }

        private static TextureImage DecodeCompactDds(byte[] bytes)
        {
            if (bytes.Length < CompactDdsHeaderSize)
                throw new InvalidDataException("Compact DDS header is truncated.");

            var width = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4));
            var height = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4));
            var channels = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8, 4));
            var linearSize = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12, 4));
            var alphaMean = BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(16, 4)));

            ValidateDimensions(width, height);
            if (channels is not (3 or 4))
                throw new InvalidDataException($"Compact DDS channel count {channels} is unsupported.");
            if (!float.IsFinite(alphaMean))
                throw new InvalidDataException("Compact DDS alpha mean is not finite.");
            if (linearSize <= 0 || linearSize > MaximumCompressedBytes ||
                linearSize > bytes.Length - CompactDdsHeaderSize)
            {
                throw new InvalidDataException("Compact DDS linear size is outside the payload.");
            }

            var blockBytes = channels == 3 ? 8 : 16;
            var blockColumns = (width + 3) / 4;
            var blockRows = (height + 3) / 4;
            var required = checked(blockColumns * blockRows * blockBytes);
            if (linearSize < required)
                throw new InvalidDataException("Compact DDS payload is shorter than its top mip surface.");

            var output = new byte[checked(width * height * 4)];
            var payload = bytes.AsSpan(CompactDdsHeaderSize, linearSize);
            for (var blockY = 0; blockY < blockRows; blockY++)
            {
                for (var blockX = 0; blockX < blockColumns; blockX++)
                {
                    var blockOffset = (blockY * blockColumns + blockX) * blockBytes;
                    if (channels == 3)
                        DecodeDxt1Block(payload.Slice(blockOffset, 8), output, width, height, blockX, blockY);
                    else
                        DecodeDxt5Block(payload.Slice(blockOffset, 16), output, width, height, blockX, blockY);
                }
            }

            return new TextureImage
            {
                Width = width,
                Height = height,
                Pixels = output,
                SourceFormat = TextureSourceFormat.Dds,
                AlphaMean = alphaMean
            };
        }

        private static void DecodeDxt1Block(
            ReadOnlySpan<byte> block,
            byte[] output,
            int width,
            int height,
            int blockX,
            int blockY)
        {
            var color0 = BinaryPrimitives.ReadUInt16LittleEndian(block.Slice(0, 2));
            var color1 = BinaryPrimitives.ReadUInt16LittleEndian(block.Slice(2, 2));
            Span<Rgba> colors = stackalloc Rgba[4];
            colors[0] = Expand565(color0);
            colors[1] = Expand565(color1);
            if (color0 > color1)
            {
                colors[2] = Interpolate(colors[0], colors[1], 2, 1, 3);
                colors[3] = Interpolate(colors[0], colors[1], 1, 2, 3);
            }
            else
            {
                colors[2] = Interpolate(colors[0], colors[1], 1, 1, 2);
                colors[3] = new Rgba(0, 0, 0, 0);
            }

            var indices = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(4, 4));
            WriteColorBlock(
                output,
                width,
                height,
                blockX,
                blockY,
                colors,
                indices,
                alphaValues: default,
                alphaIndices: 0);
        }

        private static void DecodeDxt5Block(
            ReadOnlySpan<byte> block,
            byte[] output,
            int width,
            int height,
            int blockX,
            int blockY)
        {
            Span<byte> alphas = stackalloc byte[8];
            alphas[0] = block[0];
            alphas[1] = block[1];
            if (alphas[0] > alphas[1])
            {
                for (var index = 1; index <= 6; index++)
                    alphas[index + 1] = (byte)(((7 - index) * alphas[0] + index * alphas[1]) / 7);
            }
            else
            {
                for (var index = 1; index <= 4; index++)
                    alphas[index + 1] = (byte)(((5 - index) * alphas[0] + index * alphas[1]) / 5);
                alphas[6] = 0;
                alphas[7] = 255;
            }

            ulong alphaIndices = 0;
            for (var index = 0; index < 6; index++)
                alphaIndices |= (ulong)block[2 + index] << (index * 8);

            var color0 = BinaryPrimitives.ReadUInt16LittleEndian(block.Slice(8, 2));
            var color1 = BinaryPrimitives.ReadUInt16LittleEndian(block.Slice(10, 2));
            Span<Rgba> colors = stackalloc Rgba[4];
            colors[0] = Expand565(color0);
            colors[1] = Expand565(color1);
            colors[2] = Interpolate(colors[0], colors[1], 2, 1, 3);
            colors[3] = Interpolate(colors[0], colors[1], 1, 2, 3);

            var colorIndices = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(12, 4));
            WriteColorBlock(
                output,
                width,
                height,
                blockX,
                blockY,
                colors,
                colorIndices,
                alphas,
                alphaIndices);
        }

        private static void WriteColorBlock(
            byte[] output,
            int width,
            int height,
            int blockX,
            int blockY,
            ReadOnlySpan<Rgba> colors,
            uint colorIndices,
            ReadOnlySpan<byte> alphaValues,
            ulong alphaIndices)
        {
            for (var pixel = 0; pixel < 16; pixel++)
            {
                var x = blockX * 4 + pixel % 4;
                var y = blockY * 4 + pixel / 4;
                if (x >= width || y >= height)
                    continue;

                var color = colors[(int)((colorIndices >> (pixel * 2)) & 0x3)];
                var alpha = alphaValues.IsEmpty
                    ? color.A
                    : alphaValues[(int)((alphaIndices >> (pixel * 3)) & 0x7)];
                // Decoded blocks arrive in file order (top-down). DecodeStandardDds reverses rows
                // to match the toolset's bottom-up consumer convention, so this path must do the
                // same per-pixel-row flip rather than a per-block-row one, since height % 4 may
                // leave a partial bottom block.
                var targetY = height - 1 - y;
                var target = (targetY * width + x) * 4;
                output[target] = color.R;
                output[target + 1] = color.G;
                output[target + 2] = color.B;
                output[target + 3] = alpha;
            }
        }

        private static Rgba Expand565(ushort value)
        {
            var red5 = (value >> 11) & 0x1F;
            var green6 = (value >> 5) & 0x3F;
            var blue5 = value & 0x1F;
            return new Rgba(
                (byte)((red5 << 3) | (red5 >> 2)),
                (byte)((green6 << 2) | (green6 >> 4)),
                (byte)((blue5 << 3) | (blue5 >> 2)),
                255);
        }

        private static Rgba Interpolate(Rgba left, Rgba right, int leftWeight, int rightWeight, int divisor) =>
            new(
                (byte)((left.R * leftWeight + right.R * rightWeight) / divisor),
                (byte)((left.G * leftWeight + right.G * rightWeight) / divisor),
                (byte)((left.B * leftWeight + right.B * rightWeight) / divisor),
                255);

        private static void ValidateDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0 || width > MaximumDimension || height > MaximumDimension)
                throw new InvalidDataException($"Texture dimensions {width}x{height} are invalid.");

            var pixels = checked((long)width * height);
            if (pixels > MaximumPixels)
                throw new InvalidDataException($"Texture pixel count {pixels:N0} exceeds {MaximumPixels:N0}.");
        }

        private readonly record struct Rgba(byte R, byte G, byte B, byte A);
    }
}
