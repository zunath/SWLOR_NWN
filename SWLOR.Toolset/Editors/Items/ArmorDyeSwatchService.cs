using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// Decodes NWN's PLT palette textures and samples the color a given palette index (0-175)
    /// renders as. Item armor dyes and segmented-creature colors share this service so their
    /// pickers cannot drift from the renderer or from one another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per the Aurora PLT layer mapping, skin, hair, and tattoo layers read
    /// <c>pal_skin01</c>, <c>pal_hair01</c>, and <c>pal_tattoo01</c>. Cloth1/Cloth2 share
    /// <c>pal_cloth01</c> and
    /// Leather1/Leather2 share <c>pal_leath01</c>. The metal channels are the exception:
    /// Metal1 reads <c>pal_armor01</c> while Metal2 reads <c>pal_armor02</c>.
    /// </para>
    /// <para>
    /// Sampling mirrors <see cref="TextureLoader.LoadPlt"/>'s own palette math exactly - row is the
    /// color index, column is the grayscale intensity scaled across the palette's width - at a fixed
    /// mid-tone intensity, so the swatch reads as the row's hue rather than its shadow or highlight
    /// extreme, and can never disagree with what the PLT decoder renders.
    /// </para>
    /// <para>
    /// Decoded palettes are cached for the life of the service - they never change while a session
    /// runs. A palette texture this build can't resolve or decode (this dev/test corpus ships no
    /// base-game palette TGAs at all, only SWLOR's own hak content) degrades to null, so the caller
    /// renders a neutral chip instead of throwing.
    /// </para>
    /// </remarks>
    public sealed class ArmorDyeSwatchService
    {
        /// <summary>Which Aurora PLT palette texture an appearance channel reads.</summary>
        public enum DyeMaterial
        {
            Cloth,
            Leather,
            Metal1,
            Metal2,
            Skin,
            Hair,
            Tattoo
        }

        /// <summary>Grayscale intensity sampled to stand in for a palette row's hue (mid-tone).</summary>
        private const int SwatchIntensity = 128;

        private readonly ResourceIndex? _resourceIndex;

        private readonly Dictionary<string, TextureImage?> _paletteCache =
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
            if (palette == null || palette.Width <= 0 || palette.Height <= 0 ||
                palette.Pixels.Length < palette.Width * palette.Height * 4)
            {
                return null;
            }

            var row = Math.Clamp(index, 0, palette.Height - 1);
            var column = palette.Width == 1
                ? 0
                : SwatchIntensity * (palette.Width - 1) / 255;
            var offset = (row * palette.Width + column) * 4;

            return (palette.Pixels[offset], palette.Pixels[offset + 1], palette.Pixels[offset + 2]);
        }

        /// <summary>
        /// Every color the material's palette offers, in index order - what the Colors panel's
        /// picker shows. Empty when the palette texture cannot be resolved or decoded, which is
        /// the caller's cue to fall back to plain index entry rather than an unusable grid of
        /// identical chips.
        /// </summary>
        /// <remarks>
        /// The row count IS the color count: a dye palette is one row per selectable color, each
        /// row a grayscale-to-color ramp (see <see cref="GetColor"/>). Reading it from the texture
        /// rather than assuming NWN's 176 keeps a re-authored or hak-overridden palette honest.
        /// </remarks>
        public IReadOnlyList<(byte R, byte G, byte B)> GetPaletteColors(DyeMaterial material)
        {
            var palette = GetPalette(PaletteResRef(material));
            if (palette == null || palette.Width <= 0 || palette.Height <= 0 ||
                palette.Pixels.Length < palette.Width * palette.Height * 4)
            {
                return Array.Empty<(byte, byte, byte)>();
            }

            var colors = new List<(byte R, byte G, byte B)>(palette.Height);
            for (var index = 0; index < palette.Height; index++)
            {
                var color = GetColor(material, index);
                colors.Add(color ?? default);
            }

            return colors;
        }

        /// <summary>
        /// Finds the palette row whose rendered mid-tone most closely matches
        /// <paramref name="target"/>, or null when the palette is unavailable.
        /// </summary>
        public int? FindClosestColorIndex(
            DyeMaterial material,
            (byte R, byte G, byte B) target)
        {
            var colors = GetPaletteColors(material);
            if (colors.Count == 0)
                return null;

            var bestIndex = 0;
            var bestDistance = long.MaxValue;
            for (var index = 0; index < colors.Count; index++)
            {
                var red = colors[index].R - target.R;
                var green = colors[index].G - target.G;
                var blue = colors[index].B - target.B;
                var distance = (long)red * red + (long)green * green + (long)blue * blue;
                if (distance >= bestDistance)
                    continue;

                bestIndex = index;
                bestDistance = distance;
            }

            return bestIndex;
        }

        private static string PaletteResRef(DyeMaterial material) => material switch
        {
            DyeMaterial.Cloth => "pal_cloth01",
            DyeMaterial.Leather => "pal_leath01",
            DyeMaterial.Metal1 => "pal_armor01",
            DyeMaterial.Metal2 => "pal_armor02",
            DyeMaterial.Skin => "pal_skin01",
            DyeMaterial.Hair => "pal_hair01",
            DyeMaterial.Tattoo => "pal_tattoo01",
            _ => throw new ArgumentOutOfRangeException(nameof(material))
        };

        private TextureImage? GetPalette(string resRef)
        {
            if (_paletteCache.TryGetValue(resRef, out var cached))
                return cached;

            var palette = _resourceIndex == null ? null : TextureLoader.LoadTga(_resourceIndex, resRef);
            _paletteCache[resRef] = palette;
            return palette;
        }
    }
}
