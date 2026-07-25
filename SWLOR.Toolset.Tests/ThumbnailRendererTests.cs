using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The software thumbnail rasterizer. The behaviours worth pinning are the ones a palette depends
    /// on: something with geometry draws pixels, something without returns null so the caller can fall
    /// back, and the framing is uniform so a long corridor is not stretched to fill a square tile.
    /// </summary>
    [TestFixture]
    public class ThumbnailRendererTests
    {
        private const int Size = 32;

        private static RenderModel Box(float sizeX = 1f, float sizeY = 1f, float sizeZ = 1f)
        {
            // Two triangles per face is more than needed; one quad's worth of geometry is enough to
            // prove projection, sorting and fill without hand-writing a cube.
            var positions = new[]
            {
                0f, 0f, 0f,
                sizeX, 0f, 0f,
                sizeX, sizeY, sizeZ,
                0f, sizeY, sizeZ
            };

            return new RenderModel
            {
                Name = "test",
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "quad",
                        TextureName = string.Empty,
                        Positions = positions,
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = new[] { 0, 1, 2, 0, 2, 3 },
                        Transform = Matrix4x4.Identity
                    }
                }
            };
        }

        private static int OpaquePixels(byte[] pixels)
        {
            var count = 0;
            for (var i = 3; i < pixels.Length; i += ThumbnailRenderer.BytesPerPixel)
            {
                if (pixels[i] != 0)
                    count++;
            }

            return count;
        }

        [Test]
        public void Renders_A_Buffer_Of_The_Requested_Size()
        {
            var pixels = ThumbnailRenderer.Render(Box(), Size);

            pixels.Should().NotBeNull();
            pixels!.Length.Should().Be(Size * Size * ThumbnailRenderer.BytesPerPixel);
        }

        [Test]
        public void Draws_Something_For_A_Model_With_Geometry()
        {
            var pixels = ThumbnailRenderer.Render(Box(), Size)!;

            OpaquePixels(pixels).Should().BeGreaterThan(Size,
                because: "a quad facing the camera should cover a meaningful part of the tile");
        }

        [Test]
        public void Background_Is_Transparent_So_Tiles_Keep_Their_Own_Surface()
        {
            var pixels = ThumbnailRenderer.Render(Box(0.2f, 0.2f, 0.2f), Size)!;

            // The corners can never be covered by a centred, margined model.
            pixels[3].Should().Be(0);
        }

        [Test]
        public void A_Model_With_No_Triangles_Renders_Nothing()
        {
            var empty = new RenderModel
            {
                Name = "empty",
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "none",
                        TextureName = string.Empty,
                        Positions = Array.Empty<float>(),
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = Array.Empty<int>(),
                        Transform = Matrix4x4.Identity
                    }
                }
            };

            ThumbnailRenderer.Render(empty, Size).Should().BeNull(
                because: "the caller falls back to its placeholder rather than showing an empty box");
        }

        [Test]
        public void A_Null_Model_Renders_Nothing()
        {
            ThumbnailRenderer.Render(null, Size).Should().BeNull();
        }

        [Test]
        public void Degenerate_Geometry_Does_Not_Throw()
        {
            var flat = Box(0f, 0f, 0f);

            var act = () => ThumbnailRenderer.Render(flat, Size);

            act.Should().NotThrow();
        }

        [Test]
        public void Out_Of_Range_Indices_Are_Skipped_Rather_Than_Fatal()
        {
            var model = new RenderModel
            {
                Name = "broken",
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "broken",
                        TextureName = string.Empty,
                        Positions = new[] { 0f, 0f, 0f, 1f, 0f, 0f, 1f, 1f, 0f },
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = new[] { 0, 1, 2, 0, 1, 99 },
                        Transform = Matrix4x4.Identity
                    }
                }
            };

            var act = () => ThumbnailRenderer.Render(model, Size);

            act.Should().NotThrow();
            ThumbnailRenderer.Render(model, Size).Should().NotBeNull();
        }

        [Test]
        public void Framing_Is_Uniform_So_A_Long_Model_Is_Not_Stretched()
        {
            var wide = ThumbnailRenderer.Render(Box(8f, 1f, 1f), Size)!;
            var square = ThumbnailRenderer.Render(Box(1f, 1f, 1f), Size)!;

            OpaquePixels(wide).Should().BeLessThan(OpaquePixels(square),
                because: "a long thin model should letterbox inside the tile, not fill it");
        }
    }
}
