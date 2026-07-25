using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Guards the vertical texture convention the GL viewport and the software thumbnail renderer
    /// have to agree on: decoded rows arrive top-down, NWN UVs run bottom-up.
    /// </summary>
    public class TextureOrientationTests
    {
        /// <summary>A 1-pixel-wide, 4-row RGBA image whose rows are red, green, blue, white.</summary>
        private static byte[] FourRowStripe() =>
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255,
        ];

        [Test]
        public void FlipRows_ReversesRowOrder()
        {
            var flipped = TextureOrientation.FlipRows(1, 4, FourRowStripe());

            flipped.Should().Equal(
                255, 255, 255, 255,
                0, 0, 255, 255,
                0, 255, 0, 255,
                255, 0, 0, 255);
        }

        [Test]
        public void FlipRows_LeavesEachRowsPixelOrderIntact()
        {
            // Two rows of two pixels: [A B] over [C D] must become [C D] over [A B], not [D C].
            byte[] image =
            [
                1, 1, 1, 255, 2, 2, 2, 255,
                3, 3, 3, 255, 4, 4, 4, 255,
            ];

            var flipped = TextureOrientation.FlipRows(2, 2, image);

            flipped.Should().Equal(
                3, 3, 3, 255, 4, 4, 4, 255,
                1, 1, 1, 255, 2, 2, 2, 255);
        }

        [Test]
        public void FlipRows_AppliedTwice_ReturnsTheOriginal()
        {
            var original = FourRowStripe();

            TextureOrientation.FlipRows(1, 4, TextureOrientation.FlipRows(1, 4, original))
                .Should().Equal(original);
        }

        /// <summary>
        /// A malformed image is passed through rather than throwing: a texture that decoded badly
        /// should draw wrongly, not take down the frame that was rendering it.
        /// </summary>
        [TestCase(0, 4)]
        [TestCase(1, 0)]
        [TestCase(4, 4)] // claims 4x4 but the buffer only holds 4 pixels
        public void FlipRows_DimensionsThatDoNotDescribeTheBuffer_ReturnsInputUnchanged(int width, int height)
        {
            var original = FourRowStripe();

            TextureOrientation.FlipRows(width, height, original).Should().BeSameAs(original);
        }
    }
}
