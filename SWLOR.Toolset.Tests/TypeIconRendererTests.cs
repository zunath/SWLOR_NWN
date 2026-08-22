using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The fallback symbols. What matters is that every blueprint type has one, that they are actually
    /// distinguishable from each other (a grid of identical placeholders would be no better than the
    /// letter it replaced), and that they leave the tile's own surface showing around them.
    /// </summary>
    public class TypeIconRendererTests
    {
        private const int Size = 64;

        private static readonly ResourceType[] PaletteTypes =
        {
            ResourceType.Utc, ResourceType.Uti, ResourceType.Utp, ResourceType.Utd,
            ResourceType.Utm, ResourceType.Utt, ResourceType.Uts, ResourceType.Utw
        };

        private static int OpaquePixels(IconImage image)
        {
            var count = 0;
            for (var i = 3; i < image.Bgra.Length; i += IconImage.BytesPerPixel)
            {
                if (image.Bgra[i] > 0)
                    count++;
            }

            return count;
        }

        [Test]
        [TestCaseSource(nameof(PaletteTypes))]
        public void Every_Palette_Type_Draws_A_Symbol(ResourceType type)
        {
            var image = TypeIconRenderer.Render(type, Size);

            image.Width.Should().Be(Size);
            image.Height.Should().Be(Size);
            OpaquePixels(image).Should().BeGreaterThan(Size,
                because: $"the {type} symbol should cover a meaningful part of the tile");
        }

        [Test]
        [TestCaseSource(nameof(PaletteTypes))]
        public void Symbols_Do_Not_Fill_The_Whole_Tile(ResourceType type)
        {
            var image = TypeIconRenderer.Render(type, Size);

            OpaquePixels(image).Should().BeLessThan(Size * Size,
                because: "the tile's own surface should still show around the symbol");
        }

        [Test]
        public void Every_Type_Gets_A_Distinguishable_Symbol()
        {
            var rendered = PaletteTypes
                .Select(type => Convert.ToHexString(TypeIconRenderer.Render(type, Size).Bgra))
                .ToList();

            rendered.Distinct().Should().HaveCount(PaletteTypes.Length,
                because: "a builder tells what a tile is from its symbol");
        }

        [Test]
        public void An_Unhandled_Type_Still_Gets_A_Plate_Rather_Than_Nothing()
        {
            OpaquePixels(TypeIconRenderer.Render(ResourceType.Area, Size)).Should().BeGreaterThan(Size);
        }

        [Test]
        public void The_Requested_Size_Is_Honoured()
        {
            var image = TypeIconRenderer.Render(ResourceType.Utw, 200);

            image.Width.Should().Be(200);
            image.Bgra.Length.Should().Be(200 * 200 * IconImage.BytesPerPixel);
        }

        [Test]
        public void A_Non_Positive_Size_Is_Rejected()
        {
            var act = () => TypeIconRenderer.Render(ResourceType.Utp, 0);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Shading_Keeps_Alpha_And_Scales_Colour()
        {
            var dimmed = TypeIconPalette.Shade(0xFF808080, 0.5f);

            dimmed.Should().Be(0xFF404040);
        }

        [Test]
        public void Shading_Clamps_Rather_Than_Wrapping_At_Full_Brightness()
        {
            TypeIconPalette.Shade(0xFFC0C0C0, 4f).Should().Be(0xFFFFFFFF);
        }
    }
}
