using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Render.Icons;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Compositing decoded icon textures into a tile image. The behaviours that matter are the ones a
    /// composite weapon icon depends on - layers stack in order, channels end up BGRA, and a layer that
    /// did not decode is simply absent rather than fatal.
    /// </summary>
    public class IconComposerTests
    {
        private static TextureImage Solid(int width, int height, byte r, byte g, byte b, byte a)
        {
            var pixels = new byte[width * height * 4];
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
                pixels[i + 3] = a;
            }

            return new TextureImage
            {
                Width = width,
                Height = height,
                Pixels = pixels,
                SourceFormat = TextureSourceFormat.Tga
            };
        }

        private static (byte B, byte G, byte R, byte A) PixelAt(IconImage image, int x, int y)
        {
            var offset = y * image.Stride + x * IconImage.BytesPerPixel;
            return (image.Bgra[offset], image.Bgra[offset + 1], image.Bgra[offset + 2], image.Bgra[offset + 3]);
        }

        [Test]
        public void No_Layers_Composes_Nothing()
        {
            IconComposer.Compose(Array.Empty<TextureImage>()).Should().BeNull();
        }

        [Test]
        public void Rgba_Source_Becomes_Bgra_Output()
        {
            var image = IconComposer.Compose(new[] { Solid(2, 2, r: 10, g: 20, b: 30, a: 255) })!;

            PixelAt(image, 0, 0).Should().Be(((byte)30, (byte)20, (byte)10, (byte)255));
        }

        [Test]
        public void The_Canvas_Is_As_Large_As_The_Largest_Layer()
        {
            var image = IconComposer.Compose(new[]
            {
                Solid(8, 4, 1, 1, 1, 255),
                Solid(2, 16, 2, 2, 2, 255)
            })!;

            image.Width.Should().Be(8);
            image.Height.Should().Be(16);
        }

        [Test]
        public void Later_Layers_Paint_Over_Earlier_Ones()
        {
            var image = IconComposer.Compose(new[]
            {
                Solid(2, 2, r: 255, g: 0, b: 0, a: 255),
                Solid(2, 2, r: 0, g: 255, b: 0, a: 255)
            })!;

            PixelAt(image, 1, 1).Should().Be(((byte)0, (byte)255, (byte)0, (byte)255));
        }

        [Test]
        public void A_Fully_Transparent_Layer_Leaves_What_Is_Underneath_Alone()
        {
            var image = IconComposer.Compose(new[]
            {
                Solid(2, 2, r: 255, g: 0, b: 0, a: 255),
                Solid(2, 2, r: 0, g: 0, b: 255, a: 0)
            })!;

            PixelAt(image, 0, 1).Should().Be(((byte)0, (byte)0, (byte)255, (byte)255));
        }

        [Test]
        public void A_Half_Transparent_Layer_Blends_Towards_Its_Own_Colour()
        {
            var image = IconComposer.Compose(new[]
            {
                Solid(2, 2, r: 0, g: 0, b: 0, a: 255),
                Solid(2, 2, r: 255, g: 255, b: 255, a: 128)
            })!;

            var pixel = PixelAt(image, 0, 0);
            pixel.A.Should().Be(255);
            pixel.R.Should().BeInRange(120, 136);
        }

        [Test]
        public void Nothing_Is_Drawn_Where_Every_Layer_Is_Transparent()
        {
            var image = IconComposer.Compose(new[] { Solid(2, 2, 9, 9, 9, a: 0) })!;

            PixelAt(image, 0, 0).A.Should().Be(0);
        }

        [Test]
        public void Degenerate_And_Truncated_Layers_Are_Skipped_Rather_Than_Fatal()
        {
            var truncated = new TextureImage
            {
                Width = 4,
                Height = 4,
                Pixels = new byte[8], // Far short of 4*4*4.
                SourceFormat = TextureSourceFormat.Tga
            };

            IconComposer.Compose(new[] { truncated }).Should().BeNull();
            IconComposer.Compose(new[] { truncated, Solid(2, 2, 1, 2, 3, 255) })!.Width.Should().Be(2);
        }
    }
}
