using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The Toolset's custom RGB preview must shade like the game's tint shaders
    /// (SWLOR_Haks/tools/TintRgbShading.py and TestPaletteSwatchLookup.py).
    /// </summary>
    [TestFixture]
    public class TintRgbShadingTests
    {
        private static readonly TintMapLayerType[] Layers = Enum.GetValues<TintMapLayerType>();

        private static TextureImage _palette = null!;

        [OneTimeSetUp]
        public void LoadPalette()
        {
            var haks = Support.ToolsetCorpusPaths.HaksRoot
                       ?? throw new DirectoryNotFoundException("The SWLOR_Haks corpus was not found.");
            var index = new ResourceIndex(null, new[] { new ResourceIndex.HakLayer("sw_item", Path.Combine(haks, "sw_item")) });
            _palette = TextureLoader.Load(index, "plt_palette")
                       ?? throw new FileNotFoundException("sw_item/plt_palette.tga did not load.");
            _palette.Width.Should().Be(256);
        }

        [Test]
        public void EverySwatchSentAsRgbMatchesItsOwnPaletteRow()
        {
            foreach (var layer in Layers)
            {
                var baseRow = TintMapMaterialRegistry.GetLayer(layer).PaletteBaseRow;
                var midtones = Enumerable.Range(0, TintMapMaterialRegistry.PaletteColorCount)
                    .Select(color => Midtone(baseRow + color))
                    .ToList();
                for (var color = 0; color < midtones.Count; color++)
                {
                    var midtone = midtones[color];
                    if (Luminance(midtone) < 2.0)
                        continue; // black rows and skin row 31: see BlackClothRendersMatteBlackWithFirstRowCoverage

                    // Equal midtones resolve to the lowest ID, like the game's swatch lookup.
                    var chosen = TintMapPaletteColors.GetClosestColorId(layer, midtone);
                    chosen.Should().Be(midtones.IndexOf(midtone), $"{layer} swatch {color} sent as RGB {midtone}");

                    var shades = Shade(layer, midtone);
                    var row = Row(baseRow + chosen);
                    for (var offset = 0; offset < row.Length; offset++)
                        Math.Abs(shades[offset] - row[offset]).Should().BeLessThanOrEqualTo(
                            1,
                            $"{layer} swatch {color} sent as RGB {midtone}, shade {offset / 4} channel {offset % 4}");
                }
            }
        }

        [TestCase(0, 0, 0)]
        [TestCase(2, 2, 2)]
        [TestCase(5, 4, 6)]
        public void BlackClothRendersMatteBlackWithFirstRowCoverage(byte red, byte green, byte blue)
        {
            // Bug report 2026-11-20: black and near-black cloth took the half-reflective black
            // row 57 and rendered a flat grey sheen. The black rows differ only in reflection.
            foreach (var layer in Layers)
            {
                var first = Row(TintMapMaterialRegistry.GetLayer(layer).PaletteBaseRow);
                var shades = Shade(layer, new TintMapColor(red, green, blue));
                for (var shade = 0; shade < 256; shade++)
                {
                    var offset = shade * 4;
                    shades[offset + 3].Should().Be(first[offset + 3], $"{layer} coverage at shade {shade}");
                    ((int)Math.Max(shades[offset], Math.Max(shades[offset + 1], shades[offset + 2])))
                        .Should().BeLessThanOrEqualTo((int)(Math.Max(red, Math.Max(green, blue)) * 1.6 + 0.5),
                            $"{layer} stays black at shade {shade}");
                }
            }
        }

        [TestCase(2, 2, 2)]
        [TestCase(20, 20, 24)]
        [TestCase(44, 44, 48)]
        public void ReportedDarkNeutralClothStaysDarkAndNeutral(byte red, byte green, byte blue)
        {
            // Bug report 2026-11-20: graphite 20,20,24 took olive row 167's hue and went green
            // in shadow and blue-white in highlights.
            var rgb = new TintMapColor(red, green, blue);
            var spread = Math.Max(red, Math.Max(green, blue)) - Math.Min(red, Math.Min(green, blue));
            foreach (var layer in new[] { TintMapLayerType.Cloth1, TintMapLayerType.Leather1, TintMapLayerType.Tattoo1 })
            {
                var shades = Shade(layer, rgb);
                for (var shade = 0; shade < 255; shade++)
                {
                    var offset = shade * 4;
                    var channels = new[] { shades[offset], shades[offset + 1], shades[offset + 2] };
                    (channels.Max() - channels.Min()).Should().BeLessThanOrEqualTo(
                        (int)(spread * 1.5 + 3),
                        $"{layer} RGB {rgb} stays neutral at shade {shade}");
                    if (shade <= 128)
                        ((int)channels.Max()).Should().BeLessThanOrEqualTo(
                            Math.Max(red, Math.Max(green, blue)) + 1,
                            $"{layer} RGB {rgb} stays dark at shade {shade}");
                }
            }
        }

        // Expected shades 40, 128 and 224 from SWLOR_Haks/tools/TintRgbShading.py custom_rgb_shades.
        [TestCase(TintMapLayerType.Skin, 35, 109, 150, new byte[] { 14, 40, 54, 255, 35, 109, 150, 255, 56, 135, 178, 255 })]
        [TestCase(TintMapLayerType.Hair, 90, 60, 30, new byte[] { 18, 9, 5, 249, 90, 60, 30, 228, 146, 116, 73, 214 })]
        [TestCase(TintMapLayerType.Cloth1, 20, 20, 24, new byte[] { 10, 9, 12, 255, 20, 20, 24, 255, 53, 53, 57, 255 })]
        [TestCase(TintMapLayerType.Cloth1, 200, 30, 30, new byte[] { 50, 2, 17, 255, 200, 30, 30, 255, 255, 199, 191, 255 })]
        public void MatchesTheShaderReference(TintMapLayerType layer, byte red, byte green, byte blue, byte[] expected)
        {
            var shades = Shade(layer, new TintMapColor(red, green, blue));
            new[] { 40, 128, 224 }
                .SelectMany(shade => shades.Skip(shade * 4).Take(4))
                .Should().Equal(expected);
        }

        private static byte[] Shade(TintMapLayerType layer, TintMapColor color)
        {
            var baseRow = TintMapMaterialRegistry.GetLayer(layer).PaletteBaseRow;
            var swatch = TintMapPaletteColors.GetClosestColorId(layer, color);
            return TintRgbShading.ShadeRow(
                color,
                _palette,
                TintMapTextureRenderer.PaletteImageRow(_palette, baseRow + swatch),
                TintMapTextureRenderer.PaletteImageRow(_palette, baseRow));
        }

        private static byte[] Row(int shaderRow) =>
            _palette.Pixels
                .AsSpan(TintMapTextureRenderer.PaletteImageRow(_palette, shaderRow) * _palette.Width * 4, _palette.Width * 4)
                .ToArray();

        private static TintMapColor Midtone(int shaderRow)
        {
            var row = Row(shaderRow);
            return new TintMapColor(row[128 * 4], row[128 * 4 + 1], row[128 * 4 + 2]);
        }

        private static double Luminance(TintMapColor color) =>
            color.Red * 0.2126 + color.Green * 0.7152 + color.Blue * 0.0722;
    }
}
