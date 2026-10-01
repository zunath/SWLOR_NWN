// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using Nwn.Preview.Dds;
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
    /// TGA decoding is delegated to the standalone formats library. Standard and compact DDS
    /// decoding is delegated to the bounded shared reader; NWN's standard DDS row convention is
    /// selected by this adapter. PLT remains palette policy here rather than in the low-level reader.
    /// </remarks>
    public static class TextureLoader
    {
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
            IReadOnlyDictionary<int, int>? layerColorIndices = null,
            DdsStoredRowOrder standardDdsRowOrder = DdsStoredRowOrder.BottomUp)
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
                   LoadDds(resourceIndex, resRef, standardDdsRowOrder) ??
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
        public static TextureImage? LoadDds(
            ResourceIndex resourceIndex,
            string resRef,
            DdsStoredRowOrder standardDdsRowOrder = DdsStoredRowOrder.BottomUp)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);
            if (!TryGetBytes(resourceIndex, resRef, "dds", out var bytes))
                return null;

            try
            {
                if (bytes.Length > MaximumCompressedBytes)
                    throw new InvalidDataException("DDS resource exceeds the configured compressed-input limit.");

                var isStandard = IsStandardDds(bytes);
                var rowOrder = isStandard ? standardDdsRowOrder : DdsStoredRowOrder.FormatDefault;
                var decoded = DdsDecoder.Decode(bytes, new DdsDecodeOptions { StoredRowOrder = rowOrder });
                return new TextureImage
                {
                    Width = decoded.Width,
                    Height = decoded.Height,
                    Pixels = decoded.CopyRgbaBytes(),
                    SourceFormat = TextureSourceFormat.Dds,
                    AlphaMean = isStandard ? null : ReadCompactAlphaMean(bytes)
                };
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

        private static float ReadCompactAlphaMean(byte[] bytes) =>
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(16, 4)));
    }
}
