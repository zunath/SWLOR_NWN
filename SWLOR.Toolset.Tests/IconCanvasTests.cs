using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render.Icons;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The small drawing surface the type symbols are built from: fills land inside the shape, stay out of
    /// it, and soften at the boundary rather than stepping.
    /// </summary>
    public class IconCanvasTests
    {
        private const uint OpaqueWhite = 0xFFFFFFFF;

        private static byte AlphaAt(IconImage image, int x, int y) =>
            image.Bgra[y * image.Stride + x * IconImage.BytesPerPixel + 3];

        [Test]
        public void A_Filled_Square_Covers_Its_Interior()
        {
            var canvas = new IconCanvas(32, 32);
            canvas.FillPolygon(new[] { new Vector2(8, 8), new Vector2(24, 8), new Vector2(24, 24), new Vector2(8, 24) }, OpaqueWhite);

            AlphaAt(canvas.ToImage(), 16, 16).Should().Be(255);
        }

        [Test]
        public void A_Filled_Square_Leaves_The_Rest_Of_The_Canvas_Transparent()
        {
            var canvas = new IconCanvas(32, 32);
            canvas.FillPolygon(new[] { new Vector2(8, 8), new Vector2(24, 8), new Vector2(24, 24), new Vector2(8, 24) }, OpaqueWhite);

            var image = canvas.ToImage();
            AlphaAt(image, 0, 0).Should().Be(0);
            AlphaAt(image, 31, 31).Should().Be(0);
        }

        [Test]
        public void A_Diagonal_Edge_Is_Anti_Aliased_Rather_Than_Stepped()
        {
            var canvas = new IconCanvas(32, 32);
            canvas.FillPolygon(new[] { new Vector2(2, 2), new Vector2(30, 2), new Vector2(2, 30) }, OpaqueWhite);

            var image = canvas.ToImage();
            var partial = 0;
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var alpha = AlphaAt(image, x, y);
                    if (alpha is > 0 and < 255)
                        partial++;
                }
            }

            partial.Should().BeGreaterThan(4, because: "the hypotenuse should produce partially covered pixels");
        }

        [Test]
        public void A_Degenerate_Polygon_Draws_Nothing_And_Does_Not_Throw()
        {
            var canvas = new IconCanvas(8, 8);
            var act = () => canvas.FillPolygon(new[] { new Vector2(1, 1), new Vector2(2, 2) }, OpaqueWhite);

            act.Should().NotThrow();
            canvas.ToImage().Bgra.Should().AllSatisfy(value => value.Should().Be(0));
        }

        [Test]
        public void Shapes_Outside_The_Canvas_Are_Clipped_Rather_Than_Fatal()
        {
            var canvas = new IconCanvas(8, 8);
            var act = () =>
            {
                canvas.FillCircle(new Vector2(-40, -40), 5, OpaqueWhite);
                canvas.FillPolygon(new[] { new Vector2(100, 100), new Vector2(140, 100), new Vector2(140, 140) }, OpaqueWhite);
                canvas.StrokeLine(new Vector2(-20, 4), new Vector2(60, 4), 2, OpaqueWhite);
            };

            act.Should().NotThrow();
        }

        [Test]
        public void A_Stroked_Line_Marks_The_Pixels_It_Passes_Through()
        {
            var canvas = new IconCanvas(32, 32);
            canvas.StrokeLine(new Vector2(4, 16), new Vector2(28, 16), thickness: 4, OpaqueWhite);

            var image = canvas.ToImage();
            AlphaAt(image, 16, 16).Should().Be(255);
            AlphaAt(image, 16, 2).Should().Be(0);
        }

        [Test]
        public void An_Ellipse_Is_Wider_Than_It_Is_Tall_When_Told_To_Be()
        {
            var canvas = new IconCanvas(64, 64);
            canvas.FillEllipse(new Vector2(32, 32), radiusX: 28, radiusY: 6, OpaqueWhite);

            var image = canvas.ToImage();
            AlphaAt(image, 58, 32).Should().Be(255);
            AlphaAt(image, 32, 58).Should().Be(0);
        }

        [Test]
        public void Translucent_Paint_Accumulates_Towards_Opaque()
        {
            var canvas = new IconCanvas(8, 8);
            var square = new[] { new Vector2(0, 0), new Vector2(8, 0), new Vector2(8, 8), new Vector2(0, 8) };

            canvas.FillPolygon(square, 0x80FFFFFF);
            var afterOne = AlphaAt(canvas.ToImage(), 4, 4);

            canvas.FillPolygon(square, 0x80FFFFFF);
            var afterTwo = AlphaAt(canvas.ToImage(), 4, 4);

            afterTwo.Should().BeGreaterThan(afterOne);
        }

        [Test]
        public void A_Non_Positive_Canvas_Is_Rejected()
        {
            var act = () => new IconCanvas(0, 8);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
