using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum.Item;
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
        public void A_Transition_Model_With_No_Triangles_Renders_The_Doorway_Fallback()
        {
            var transition = new RenderModel
            {
                Name = "transition",
                Meshes = Array.Empty<RenderMesh>(),
                IsDoorTransitionGeometry = true
            };

            var pixels = ThumbnailRenderer.Render(transition, Size);

            pixels.Should().NotBeNull();
            OpaquePixels(pixels!).Should().BeGreaterThan(0);
        }

        [Test]
        public void Transition_Metadata_Without_A_Model_Renders_The_Doorway_Fallback()
        {
            var pixels = ThumbnailRenderer.Render(
                model: null,
                size: Size,
                renderDoorTransitionFallback: true);

            pixels.Should().NotBeNull();
            OpaquePixels(pixels!).Should().BeGreaterThan(0);
        }

        [Test]
        public void A_Transition_Model_With_Unprojectable_Triangles_Renders_The_Doorway_Fallback()
        {
            var degenerate = Box(0f, 0f, 0f);
            var transition = new RenderModel
            {
                Name = degenerate.Name,
                Meshes = degenerate.Meshes,
                IsDoorTransitionGeometry = true
            };

            var pixels = ThumbnailRenderer.Render(transition, Size);

            pixels.Should().NotBeNull();
            OpaquePixels(pixels!).Should().BeGreaterThan(0);
        }

        [Test]
        public void A_Transition_Model_With_Collinear_Triangles_Renders_The_Doorway_Fallback()
        {
            var collinear = Box(1f, 0f, 0f);
            var transition = new RenderModel
            {
                Name = collinear.Name,
                Meshes = collinear.Meshes,
                IsDoorTransitionGeometry = true
            };

            var pixels = ThumbnailRenderer.Render(transition, Size);

            pixels.Should().NotBeNull();
            OpaquePixels(pixels!).Should().BeGreaterThan(0,
                "a nonzero projected span is not enough when every authored triangle has zero area");
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

        // ---- Textured rendering ----

        /// <summary>A textured version of <see cref="Box"/>: same quad, with a full 0..1 UV set.</summary>
        private static RenderModel TexturedQuad(string textureName, Vector3? diffuse = null) =>
            new()
            {
                Name = "textured",
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "quad",
                        TextureName = textureName,
                        DiffuseColor = diffuse ?? Vector3.One,
                        Positions = new[] { 0f, 0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f, 0f, 1f, 1f },
                        Normals = Array.Empty<float>(),
                        TexCoords = new[] { 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f },
                        Indices = new[] { 0, 1, 2, 0, 2, 3 },
                        Transform = Matrix4x4.Identity
                    }
                }
            };

        private static TextureImage SolidTexture(
            byte r,
            byte g,
            byte b,
            byte a = 255,
            int size = 4,
            byte alphaCutoff = TextureImage.DefaultAlphaCutoff)
        {
            var pixels = new byte[size * size * 4];
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = r;
                pixels[i + 1] = g;
                pixels[i + 2] = b;
                pixels[i + 3] = a;
            }

            return new TextureImage
            {
                Width = size,
                Height = size,
                Pixels = pixels,
                SourceFormat = TextureSourceFormat.Tga,
                AlphaCutoff = alphaCutoff
            };
        }

        private static (byte B, byte G, byte R) Brightest(byte[] pixels)
        {
            (byte B, byte G, byte R) best = (0, 0, 0);
            var bestSum = -1;
            for (var i = 0; i < pixels.Length; i += ThumbnailRenderer.BytesPerPixel)
            {
                if (pixels[i + 3] == 0)
                    continue;

                var sum = pixels[i] + pixels[i + 1] + pixels[i + 2];
                if (sum > bestSum)
                {
                    bestSum = sum;
                    best = (pixels[i], pixels[i + 1], pixels[i + 2]);
                }
            }

            return best;
        }

        [Test]
        public void A_Textured_Mesh_Takes_Its_Colour_From_The_Texture()
        {
            var pixels = ThumbnailRenderer.Render(
                TexturedQuad("wall"), Size, palette: null,
                resolveTexture: _ => SolidTexture(r: 200, g: 30, b: 30))!;

            var brightest = Brightest(pixels);
            brightest.R.Should().BeGreaterThan(brightest.B,
                because: "the quad should read as the texture's red, not the palette's blue");
        }

        [Test]
        public void A_Mesh_Takes_Its_Colour_From_Its_Diffuse_When_The_Texture_Is_White()
        {
            // Every waypoint marker in the haks is drawn on tcn01_white and coloured entirely by the
            // node's diffuse, so a rasterizer that samples the texture alone renders the cyan flag,
            // the orange flag and the treasure chest as the same white shape.
            var cyan = TexturedQuad("tcn01_white", new Vector3(0f, 0.5f, 0.5f));
            var orange = TexturedQuad("tcn01_white", new Vector3(0.7f, 0.35f, 0f));

            var cyanPixels = ThumbnailRenderer.Render(
                cyan, Size, palette: null, resolveTexture: _ => SolidTexture(255, 255, 255))!;
            var orangePixels = ThumbnailRenderer.Render(
                orange, Size, palette: null, resolveTexture: _ => SolidTexture(255, 255, 255))!;

            var cyanBrightest = Brightest(cyanPixels);
            cyanBrightest.B.Should().BeGreaterThan(cyanBrightest.R);
            cyanBrightest.G.Should().BeGreaterThan(cyanBrightest.R);

            var orangeBrightest = Brightest(orangePixels);
            orangeBrightest.R.Should().BeGreaterThan(orangeBrightest.B);
        }

        [Test]
        public void An_Unstated_Diffuse_Leaves_A_Texture_Exactly_As_It_Is()
        {
            // Nearly every mesh states white, and the untextured helper planes in this module's
            // content state nothing at all. Neither may tint anything.
            var pixels = ThumbnailRenderer.Render(
                TexturedQuad("wall"), Size, palette: null,
                resolveTexture: _ => SolidTexture(r: 200, g: 200, b: 200))!;

            var brightest = Brightest(pixels);
            brightest.R.Should().Be(brightest.G);
            brightest.G.Should().Be(brightest.B);
        }

        [Test]
        public void A_Mesh_Whose_Texture_Does_Not_Resolve_Still_Renders_Flat()
        {
            var pixels = ThumbnailRenderer.Render(
                TexturedQuad("missing"), Size, palette: null, resolveTexture: _ => null)!;

            OpaquePixels(pixels).Should().BeGreaterThan(Size,
                because: "an unresolvable texture must not blank the model out");
        }

        [Test]
        public void A_Mesh_With_No_Texture_Name_Is_Not_Looked_Up_At_All()
        {
            var lookups = 0;

            ThumbnailRenderer.Render(Box(), Size, palette: null, resolveTexture: _ =>
            {
                lookups++;
                return null;
            });

            lookups.Should().Be(0);
        }

        [Test]
        public void A_Texture_Is_Only_Resolved_Once_Per_Render()
        {
            var lookups = 0;
            var model = new RenderModel
            {
                Name = "twoMeshes",
                Meshes = new[] { TexturedQuad("shared").Meshes[0], TexturedQuad("shared").Meshes[0] }
            };

            ThumbnailRenderer.Render(model, Size, palette: null, resolveTexture: _ =>
            {
                lookups++;
                return SolidTexture(10, 10, 10);
            });

            lookups.Should().Be(1);
        }

        [Test]
        public void TheSameTextureWithDifferentEquipmentPalettesIsResolvedPerMesh()
        {
            var first = TexturedQuad("shared_plt").Meshes[0];
            first.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
            var second = TexturedQuad("shared_plt").Meshes[0];
            second.LayerColorIndices = new Dictionary<int, int> { [2] = 97 };
            var seen = new List<int>();
            var model = new RenderModel
            {
                Name = "independent-dyes",
                Meshes = new[] { first, second }
            };

            ThumbnailRenderer.Render(
                model,
                Size,
                resolveLayeredTexture: (_, colors) =>
                {
                    seen.Add(colors![2]);
                    return SolidTexture(10, 10, 10);
                });

            seen.Should().BeEquivalentTo(new[] { 45, 97 },
                "a cloak and chest can share a PLT name while selecting different palette rows");
        }

        [Test]
        public void APlainTextureResolverIgnoresUnusedMeshPalettesInItsCacheKey()
        {
            var first = TexturedQuad("shared").Meshes[0];
            first.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
            var second = TexturedQuad("shared").Meshes[0];
            second.LayerColorIndices = new Dictionary<int, int> { [2] = 97 };
            var lookups = 0;

            ThumbnailRenderer.Render(
                new RenderModel { Name = "plain-texture", Meshes = [first, second] },
                Size,
                resolveTexture: _ =>
                {
                    lookups++;
                    return SolidTexture(10, 10, 10);
                });

            lookups.Should().Be(1,
                "the plain resolver cannot use layer colors and should share one decoded texture");
        }

        [Test]
        public void APerMeshResolverCachesDistinctDyeTintOwnershipAndArmorPartStateSeparately()
        {
            var first = TexturedQuad("shared_mtr").Meshes[0];
            first.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
            first.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 123 };
            first.ArmorPart = AppearanceArmor.LeftHand;
            var otherPart = TexturedQuad("shared_mtr").Meshes[0];
            otherPart.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
            otherPart.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 123 };
            otherPart.ArmorPart = AppearanceArmor.RightHand;
            var itemOwned = TexturedQuad("shared_mtr").Meshes[0];
            itemOwned.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
            itemOwned.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 123 };
            itemOwned.UsesItemTintOverrides = true;
            var second = TexturedQuad("shared_mtr").Meshes[0];
            second.LayerColorIndices = new Dictionary<int, int> { [2] = 97 };
            second.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 456 };
            var seen = new List<(int Palette, int Tint)>();

            ThumbnailRenderer.Render(
                new RenderModel
                {
                    Name = "independent-custom-tints",
                    Meshes = [first, otherPart, itemOwned, second]
                },
                Size,
                resolveMeshTexture: mesh =>
                {
                    seen.Add((mesh.LayerColorIndices[2], mesh.TintMapOverrides["TM_shared_mtr_2"]));
                    return SolidTexture(10, 10, 10);
                });

            seen.Should().BeEquivalentTo(new[] { (45, 123), (45, 123), (45, 123), (97, 456) },
                "a per-mesh resolver uses armor part, ownership, palette rows, and custom RGB overrides");
        }

        [Test]
        public void Fully_Transparent_Texels_Are_Cut_Out_Rather_Than_Drawn()
        {
            var opaque = ThumbnailRenderer.Render(
                TexturedQuad("leaf"), Size, palette: null, resolveTexture: _ => SolidTexture(9, 9, 9))!;
            var cutOut = ThumbnailRenderer.Render(
                TexturedQuad("leaf"), Size, palette: null, resolveTexture: _ => SolidTexture(9, 9, 9, a: 0))!;

            OpaquePixels(cutOut).Should().Be(0);
            OpaquePixels(opaque).Should().BeGreaterThan(0);
        }

        [Test]
        public void TextureSpecificAlphaCutoffControlsSoftwarePreviewCutout()
        {
            var runtimeTintCutoff = ThumbnailRenderer.Render(
                TexturedQuad("leaf"), Size, palette: null,
                resolveTexture: _ => SolidTexture(9, 9, 9, a: 80, alphaCutoff: 77))!;
            var legacyCutoff = ThumbnailRenderer.Render(
                TexturedQuad("leaf"), Size, palette: null,
                resolveTexture: _ => SolidTexture(9, 9, 9, a: 80))!;

            OpaquePixels(runtimeTintCutoff).Should().BeGreaterThan(0,
                "texture9 alpha 80 survives the shader's byte cutoff of 77");
            OpaquePixels(legacyCutoff).Should().Be(0,
                "ordinary preview textures retain the historical byte cutoff of 96");
        }

        [Test]
        public void A_Texture_Shorter_Than_Its_Declared_Size_Is_Ignored_Rather_Than_Fatal()
        {
            var truncated = new TextureImage
            {
                Width = 64,
                Height = 64,
                Pixels = new byte[16],
                SourceFormat = TextureSourceFormat.Tga
            };

            var act = () => ThumbnailRenderer.Render(
                TexturedQuad("bad"), Size, palette: null, resolveTexture: _ => truncated);

            act.Should().NotThrow();
            OpaquePixels(act()!).Should().BeGreaterThan(0, because: "it falls back to the flat tone");
        }

        [Test]
        public void A_Throwing_Texture_Resolver_Does_Not_Take_The_Render_Down()
        {
            var act = () => ThumbnailRenderer.Render(
                TexturedQuad("boom"), Size, palette: null,
                resolveTexture: _ => throw new InvalidOperationException("resource layer exploded"));

            act.Should().NotThrow();
            OpaquePixels(act()!).Should().BeGreaterThan(0);
        }

        [Test]
        public void A_Mesh_With_No_Uvs_Falls_Back_To_Flat_Shading()
        {
            var noUvs = new RenderModel
            {
                Name = "noUvs",
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "quad",
                        TextureName = "wall",
                        Positions = new[] { 0f, 0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f, 0f, 1f, 1f },
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = new[] { 0, 1, 2, 0, 2, 3 },
                        Transform = Matrix4x4.Identity
                    }
                }
            };

            var pixels = ThumbnailRenderer.Render(
                noUvs, Size, palette: null, resolveTexture: _ => SolidTexture(200, 30, 30))!;

            var brightest = Brightest(pixels);
            brightest.B.Should().BeGreaterThan(brightest.R,
                because: "with no UVs to sample, the palette's blue is the only honest answer");
        }
    }
}
