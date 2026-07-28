using Radoub.Formats.Plt;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// Decodes NWN's three dye-palette textures and samples the color a given dye index (0-175)
    /// renders as, for the Appearance tab's armor Colors panel swatches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per the Aurora item format, each material shares ONE palette texture between its two dye
    /// slots: Metal1/Metal2 both read <c>pal_armor01</c>, Cloth1/Cloth2 read <c>pal_cloth01</c>,
    /// Leather1/Leather2 read <c>pal_leath01</c> (the "1"/"2" split is which PLT layer uses the
    /// looked-up index, not a separate file - confirmed against <c>PltLayers.GetPaletteResRef</c>).
    /// </para>
    /// <para>
    /// A palette texture is 256 columns (grayscale) by up to 176 rows (dye index), per
    /// <c>PltReader.Render</c>: X is the grayscale value, Y is the color index. This samples a fixed
    /// mid-tone column so the returned color reads as the row's hue rather than its shadow or
    /// highlight extreme.
    /// </para>
    /// <para>
    /// Decoded palettes are cached for the life of the service - they never change while a session
    /// runs. A palette texture this build can't resolve or decode (this dev/test corpus ships no
    /// base-game palette TGAs at all, only SWLOR's own hak content - see
    /// <c>RenderPipelineTests.TextureLoader_LoadPlt_ForKnownCorpusTexture_DecodesToReportedDimensions</c>)
    /// degrades to null, so the caller renders a neutral chip instead of throwing.
    /// </para>
    /// </remarks>
    public sealed class ArmorDyeSwatchService
    {
        /// <summary>Which of the three shared palette textures a dye channel reads.</summary>
        public enum DyeMaterial
        {
            Cloth,
            Leather,
            Metal
        }

        /// <summary>Grayscale column sampled to stand in for a palette row's hue (mid-tone).</summary>
        private const int SwatchGrayscale = 128;

        private readonly ResourceIndex? _resourceIndex;

        private readonly Dictionary<string, PaletteData?> _paletteCache =
            new(StringComparer.OrdinalIgnoreCase);

        public ArmorDyeSwatchService(ResourceIndex? resourceIndex)
        {
            _resourceIndex = resourceIndex;
        }

        /// <summary>
        /// The RGB color dye <paramref name="index"/> (0-175) renders as, or null when the palette
        /// texture cannot be resolved or decoded.
        /// </summary>
        public (byte R, byte G, byte B)? GetColor(DyeMaterial material, int index)
        {
            var palette = GetPalette(PaletteResRef(material));
            if (palette == null)
                return null;

            var (r, g, b, _) = palette.GetColor(SwatchGrayscale, index);
            return (r, g, b);
        }

        private static string PaletteResRef(DyeMaterial material) => material switch
        {
            DyeMaterial.Cloth => "pal_cloth01",
            DyeMaterial.Leather => "pal_leath01",
            DyeMaterial.Metal => "pal_armor01",
            _ => throw new ArgumentOutOfRangeException(nameof(material))
        };

        private PaletteData? GetPalette(string resRef)
        {
            if (_paletteCache.TryGetValue(resRef, out var cached))
                return cached;

            var decoded = _resourceIndex == null ? null : TextureLoader.LoadTga(_resourceIndex, resRef);
            var palette = decoded == null ? null : new PaletteData(decoded.Width, decoded.Height, decoded.Pixels);
            _paletteCache[resRef] = palette;
            return palette;
        }
    }
}
