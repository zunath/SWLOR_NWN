// SPDX-License-Identifier: MIT

using Nwn.Preview.Dds;
using Nwn.Preview.Pixels;
using Nwn.Preview.Plt;
using Nwn.Preview.Tga;
using Nwn.Preview.Textures;
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
            var request = CreateRequest(resourceIndex, layerColorIndices, standardDdsRowOrder);
            try
            {
                var loaded = TextureResourceLoader.Load(resRef, (name, type) => ReadResource(resourceIndex, name, (ushort)type), request);
                return loaded is null ? null : ToTextureImage(loaded);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Loads a TGA resource, returning null for missing or malformed data.</summary>
        public static TextureImage? LoadTga(ResourceIndex resourceIndex, string resRef)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);
            if (!TryGetBytes(resourceIndex, resRef, "tga", out var bytes))
                return null;

            try
            {
                var image = TgaDecoder.Decode(bytes);
                return new TextureImage
                {
                    Width = image.Width,
                    Height = image.Height,
                    Pixels = image.CopyRgbaBytes(),
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

                var loaded = TextureResourceLoader.Load(resRef,
                    (name, type) => type == Nwn.Formats.Resources.ResourceType.Dds
                        ? bytes
                        : null,
                    new TextureLoadRequest { StandardDdsRowOrder = standardDdsRowOrder });
                return loaded is null ? null : ToTextureImage(loaded);
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
                var request = CreateRequest(resourceIndex, layerColorIndices, DdsStoredRowOrder.BottomUp);
                var loaded = TextureResourceLoader.LoadPlt(bytes, request);
                return ToTextureImage(loaded);
            }
            catch (Exception)
            {
                return null;
            }
        }

    private static TextureLoadRequest CreateRequest(
        ResourceIndex resourceIndex,
        IReadOnlyDictionary<int, int>? layerColorIndices,
        DdsStoredRowOrder standardDdsRowOrder)
    {
        var rows = new Dictionary<byte, int>();
        if (layerColorIndices is not null)
        {
            foreach (var (layer, row) in layerColorIndices)
            {
                if (layer >= byte.MinValue && layer < PaletteNames.Length)
                    rows[(byte)layer] = row;
            }
        }

        return new TextureLoadRequest
        {
            PaletteRows = rows,
            StandardDdsRowOrder = standardDdsRowOrder,
            ResolvePalette = layer => layer < PaletteNames.Length
                ? LoadTga(resourceIndex, PaletteNames[layer]) is { } palette
                    ? new RgbaImage(palette.Width, palette.Height, palette.Pixels)
                    : null
                : null
        };
    }

    private static TextureImage ToTextureImage(TextureLoadResult loaded) => new()
    {
        Width = loaded.Image.Width,
        Height = loaded.Image.Height,
        Pixels = loaded.Image.CopyRgbaBytes(),
        SourceFormat = loaded.SourceFormat switch
        {
            Nwn.Preview.Textures.TextureSourceFormat.Tga => TextureSourceFormat.Tga,
            Nwn.Preview.Textures.TextureSourceFormat.Dds => TextureSourceFormat.Dds,
            _ => TextureSourceFormat.Plt
        },
        AlphaMean = loaded.CompactAlphaMean
    };

    private static byte[]? ReadResource(ResourceIndex resourceIndex, string resRef, ushort type)
    {
        var identity = new ResourceIdentity(resRef, type);
        return resourceIndex.TryLookup(identity, out var handle)
            ? handle.GetBytes(MaximumCompressedBytes)
            : null;
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
            bytes = handle.GetBytes(MaximumCompressedBytes);
            return bytes.Length > 0;
        }
    }
}
