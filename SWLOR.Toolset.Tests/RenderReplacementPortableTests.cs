// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.NWN.Formats.Plt;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class RenderReplacementPortableTests
    {
        private string _resourceDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _resourceDirectory = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "render-replacement-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_resourceDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_resourceDirectory))
                Directory.Delete(_resourceDirectory, recursive: true);
        }

        [Test]
        public void StandardDxt1DdsDecodesToTopLeftRgba()
        {
            File.WriteAllBytes(Path.Combine(_resourceDirectory, "red.dds"), StandardDxt1(RedDxt1Block()));

            var image = TextureLoader.LoadDds(Index(), "red");

            image.Should().NotBeNull();
            image!.Width.Should().Be(4);
            image.Height.Should().Be(4);
            image.SourceFormat.Should().Be(TextureSourceFormat.Dds);
            image.Pixels.Should().HaveCount(4 * 4 * 4);
            Pixel(image, 0, 0).Should().Be((255, 0, 0, 255));
        }

        [Test]
        public void CompactDdsRowsAreFlippedLikeStandardDds()
        {
            // Compact BioWare DDS blocks decode in file order (red block first, then green), but
            // DecodeCompactDds must reverse rows to match DecodeStandardDds's bottom-up consumer
            // convention: the file-first (red) block lands at the bottom, and the file-last
            // (green) block lands at the top, same as the standard-DDS orientation test below.
            var payload = RedDxt1Block().Concat(GreenDxt1Block()).ToArray();
            File.WriteAllBytes(
                Path.Combine(_resourceDirectory, "compactrows.dds"),
                CompactDds(4, 8, 3, 1f, payload));

            var image = TextureLoader.LoadDds(Index(), "compactrows");

            image.Should().NotBeNull();
            Pixel(image!, 0, 0).Should().Be((0, 255, 0, 255));
            Pixel(image, 0, 7).Should().Be((255, 0, 0, 255));
        }

        [Test]
        public void StandardDdsPositiveStrideIsReversedForTheNwnUvContract()
        {
            // Pfim exposes these positive-stride rows in file order. The toolset reverses them to
            // match the orientation NWN artists authored against. Distinct block rows make an
            // accidental no-flip implementation visible: consumer-facing row zero must be green.
            var payload = RedDxt1Block().Concat(GreenDxt1Block()).ToArray();
            File.WriteAllBytes(
                Path.Combine(_resourceDirectory, "standardrows.dds"),
                StandardDxt1(payload, height: 8));

            var image = TextureLoader.LoadDds(Index(), "standardrows");

            image.Should().NotBeNull();
            Pixel(image!, 0, 0).Should().Be((0, 255, 0, 255));
            Pixel(image, 0, 7).Should().Be((255, 0, 0, 255));
        }

        [Test]
        public void CompactDxt1ReadsAlphaMeanAndStartsPayloadAfterByteTwenty()
        {
            File.WriteAllBytes(
                Path.Combine(_resourceDirectory, "compact3.dds"),
                CompactDds(4, 4, 3, 0.625f, RedDxt1Block()));

            var image = TextureLoader.LoadDds(Index(), "compact3");

            image.Should().NotBeNull();
            image!.AlphaMean.Should().BeApproximately(0.625f, 0.0001f);
            Pixel(image, 3, 3).Should().Be((255, 0, 0, 255));
        }

        [Test]
        public void CompactDxt5PreservesBlockAlpha()
        {
            var block = new byte[16];
            block[0] = 128;
            block[1] = 0;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(8, 2), 0x07E0);
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(10, 2), 0);

            File.WriteAllBytes(
                Path.Combine(_resourceDirectory, "compact4.dds"),
                CompactDds(4, 4, 4, 0.5f, block));

            var image = TextureLoader.LoadDds(Index(), "compact4");

            image.Should().NotBeNull();
            Pixel(image!, 0, 0).Should().Be((0, 255, 0, 128));
        }

        [Test]
        public void TruncatedCompactDdsFailsAsANullTexture()
        {
            var bytes = CompactDds(4, 4, 3, 1f, RedDxt1Block());
            Array.Resize(ref bytes, bytes.Length - 1);
            File.WriteAllBytes(Path.Combine(_resourceDirectory, "broken.dds"), bytes);

            TextureLoader.LoadDds(Index(), "broken").Should().BeNull();
        }

        [TestCase(16_385, 1)]
        [TestCase(16_000, 16_000)]
        public void OversizedStandardDdsIsRejectedBeforePfimSurfaceAllocation(int width, int height)
        {
            File.WriteAllBytes(
                Path.Combine(_resourceDirectory, "oversized.dds"),
                StandardDxt1(RedDxt1Block(), width, height));
            var index = Index();
            index.EnsureInitialized();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var image = TextureLoader.LoadDds(index, "oversized");
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            image.Should().BeNull();
            allocated.Should().BeLessThan(
                1_000_000,
                "the project dimension and pixel caps run before Pfim can size a decoded surface");
        }

        [Test]
        public void PltRowsAreReturnedTopFirstWithoutInstalledPalettes()
        {
            var bytes = new byte[28];
            "PLT V1  "u8.CopyTo(bytes);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 2);
            bytes[24] = 10;
            bytes[25] = 0;
            bytes[26] = 200;
            bytes[27] = 0;
            File.WriteAllBytes(Path.Combine(_resourceDirectory, "layers.plt"), bytes);

            var image = TextureLoader.LoadPlt(Index(), "layers");

            image.Should().NotBeNull();
            image!.Width.Should().Be(1);
            image.Height.Should().Be(2);
            Pixel(image, 0, 0).Should().Be((200, 200, 200, 255));
            Pixel(image, 0, 1).Should().Be((10, 10, 10, 255));
        }

        [TestCase(PltLayers.Cloth1, "pal_cloth01")]
        [TestCase(PltLayers.Cloth2, "pal_cloth01")]
        [TestCase(PltLayers.Leather1, "pal_leath01")]
        [TestCase(PltLayers.Leather2, "pal_leath01")]
        [TestCase(PltLayers.Metal1, "pal_armor01")]
        [TestCase(PltLayers.Metal2, "pal_armor02")]
        public void EveryPltMaterialLayerUsesItsAuroraPalette(int layer, string expectedPalette)
        {
            // Only the expected palette is present. A wrong layer-to-palette mapping therefore
            // falls back to grayscale instead of resolving this authored swatch color. In
            // particular, Aurora gives Metal2 its own pal_armor02 rather than pal_armor01.
            File.WriteAllBytes(
                Path.Combine(_resourceDirectory, expectedPalette + ".tga"),
                SolidColorTga(10, 20, 30));
            File.WriteAllBytes(
                Path.Combine(_resourceDirectory, "swatch.plt"),
                SinglePixelPlt(layer));

            var image = TextureLoader.LoadPlt(Index(), "swatch");

            image.Should().NotBeNull();
            Pixel(image!, 0, 0).Should().Be((10, 20, 30, 255));
        }

        [Test]
        public void PltRenderHintsUseAurorasStandaloneEnvironmentMap()
        {
            var image = new TextureImage
            {
                Width = 1,
                Height = 1,
                Pixels = new byte[] { 10, 20, 30, 0 },
                SourceFormat = TextureSourceFormat.Plt
            };

            var hints = TextureRenderPolicy.Resolve(Index(), "armor_part", image);

            hints.EnvironmentMapTexture.Should().Be(TextureRenderPolicy.StandaloneEnvironmentMap);
            hints.AlphaCutoff.Should().Be(0f,
                "PLT alpha blends the diffuse over chrome1 rather than punching a hole");
        }

        [Test]
        public void ExplicitTxiEnvironmentMapOverridesTheStandaloneDefault()
        {
            File.WriteAllText(
                Path.Combine(_resourceDirectory, "reflective.txi"),
                "envmaptexture TTR01__ref01");
            var image = new TextureImage
            {
                Width = 1,
                Height = 1,
                Pixels = new byte[] { 10, 20, 30, 0 },
                SourceFormat = TextureSourceFormat.Plt
            };

            var hints = TextureRenderPolicy.Resolve(Index(), "reflective", image);

            hints.EnvironmentMapTexture.Should().Be("TTR01__ref01");
            hints.AlphaCutoff.Should().Be(0f);
        }

        [Test]
        public void OrdinaryAlphaTextureRemainsPunchThroughRatherThanReflective()
        {
            var pixels = Enumerable.Range(0, 100)
                .SelectMany(_ => new byte[] { 10, 20, 30, 0 })
                .ToArray();
            var image = new TextureImage
            {
                Width = 10,
                Height = 10,
                Pixels = pixels,
                SourceFormat = TextureSourceFormat.Tga
            };

            var hints = TextureRenderPolicy.Resolve(Index(), "grate", image);

            hints.EnvironmentMapTexture.Should().BeNull();
            hints.AlphaCutoff.Should().Be(TextureAlphaPolicy.PunchThroughCutoff);
        }

        [Test]
        public void LitShaderKeepsTheEnvironmentPassOutOfDiffuseLighting()
        {
            var shader = typeof(SWLOR.Toolset.Viewport.GlAreaControl)
                .GetField(
                    "FragmentShaderBody",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static)!
                .GetRawConstantValue()
                .Should()
                .BeOfType<string>()
                .Subject;

            shader.Should().Contain("uniform sampler2D environmentTexture");
            shader.Should().Contain("SampleEnvironmentMap");
            shader.Should().Contain(
                "vec3 result = (ambientColor + diff * lightColor) * texColor.rgb;",
                "only the diffuse pass is affected by the model lighting");
            shader.Should().Contain(
                "result = mix(SampleEnvironmentMap(norm), result, texColor.a);",
                "Aurora draws an unlit environment pass and source-alpha blends the lit diffuse on top");
            shader.Should().NotContain(
                "(ambientColor + diff * lightColor) * surfaceColor",
                "lighting the combined passes incorrectly darkens reflective PLT regions");
        }

        [Test]
        public void SingleModelPreviewUsesAurorasNeutralGreyBackground()
        {
            var scene = new AreaScene
            {
                Tileset = string.Empty,
                Width = 0,
                Height = 0,
                Tiles = Array.Empty<TilePlacement>(),
                Instances =
                [
                    new InstanceMarker
                    {
                        Kind = InstanceMarkerKind.Item,
                        Position = Vector3.Zero,
                        Orientation = Vector2.UnitX
                    }
                ],
                Diagnostics = new AreaSceneDiagnostics()
            };
            var method = typeof(SWLOR.Toolset.Viewport.GlAreaControl)
                .GetMethod(
                    "BackgroundForScene",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static)!;

            var background = method.Invoke(null, [scene])
                .Should()
                .BeOfType<Vector3>()
                .Subject;

            background.Should().Be(new Vector3(0.4f, 0.4f, 0.4f));
        }

        [Test]
        public void TgaLoaderRetainsTheReadersTopFirstRgbaConvention()
        {
            var bytes = new byte[24];
            bytes[2] = 2;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), 2);
            bytes[16] = 24;

            // Source origin is bottom-left: blue is the lower row, red is the upper row.
            bytes[18] = 255;
            bytes[19] = 0;
            bytes[20] = 0;
            bytes[21] = 0;
            bytes[22] = 0;
            bytes[23] = 255;
            File.WriteAllBytes(Path.Combine(_resourceDirectory, "rows.tga"), bytes);

            var image = TextureLoader.LoadTga(Index(), "rows");

            image.Should().NotBeNull();
            Pixel(image!, 0, 0).Should().Be((255, 0, 0, 255));
            Pixel(image, 0, 1).Should().Be((0, 0, 255, 255));
        }

        [Test]
        public void TextureLoaderPreservesDotsInExtensionlessResourceReferences()
        {
            var bytes = new byte[21];
            bytes[2] = 2;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), 1);
            bytes[16] = 24;
            bytes[20] = 255;
            File.WriteAllBytes(Path.Combine(_resourceDirectory, "c_barract.001.tga"), bytes);

            var image = TextureLoader.LoadTga(Index(), "c_barract.001");

            image.Should().NotBeNull();
            Pixel(image!, 0, 0).Should().Be((255, 0, 0, 255));
        }

        [Test]
        public void MeshBuilderPreservesTileFadeAndTriangleCount()
        {
            var mesh = Triangle("ceiling");
            mesh.TileFade = -2;
            mesh.Position = new Vector3(3, 4, 5);
            var root = new MdlNode { Name = "root" };
            root.Children.Add(mesh);
            mesh.Parent = root;

            var built = MdlMeshBuilder.Build(new MdlModel { Name = "tile", GeometryRoot = root });

            built.Meshes.Should().ContainSingle();
            built.Meshes[0].TriangleCount.Should().Be(1);
            built.Meshes[0].VertexCount.Should().Be(3);
            built.Meshes[0].TileFade.Should().Be(-2);
            Vector3.Transform(Vector3.Zero, built.Meshes[0].Transform)
                .Should().Be(new Vector3(3, 4, 5));
        }

        [Test]
        public void AnimationScaleDoesNotResizeAuthoredMeshGeometry()
        {
            var mesh = Triangle("scaled-animation");
            mesh.Position = new Vector3(3, 4, 5);
            var root = new MdlNode { Name = "root" };
            root.Children.Add(mesh);
            mesh.Parent = root;
            var model = new MdlModel
            {
                Name = "animated",
                GeometryRoot = root,
                Scale = 3f
            };

            var built = MdlMeshBuilder.Build(model);

            Vector3.Transform(Vector3.UnitX, built.Meshes.Single().Transform)
                .Should().Be(new Vector3(4, 4, 5));
        }

        [Test]
        public void FlattenerTerminatesOnAChildCycleAndUsesExactVertexRadius()
        {
            var mesh = Triangle("cyclic");
            mesh.Vertices =
            [
                new Vector3(-10, 10, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, -1, 0)
            ];
            var root = new MdlNode { Name = "root" };
            root.Children.Add(mesh);
            mesh.Parent = root;
            mesh.Children.Add(root);
            root.Parent = mesh;
            var model = new MdlModel { Name = "cycle", GeometryRoot = root };

            var flatten = () => MdlGeometryFlattener.FlattenNodeTransforms(model);

            flatten.Should().NotThrow();
            model.BoundsMinimum.Should().Be(new Vector3(-10, -1, 0));
            model.BoundsMaximum.Should().Be(new Vector3(1, 10, 0));
            model.Radius.Should().BeApproximately(MathF.Sqrt(200), 0.0001f);
        }

        [Test]
        public void ComposerAttachesAClonedPartToItsCanonicalBone()
        {
            var skeletonRoot = new MdlNode { Name = "root" };
            var thigh = new MdlNode { Name = "lthigh_g", Parent = skeletonRoot };
            skeletonRoot.Children.Add(thigh);
            var skeleton = new MdlModel { Name = "pfh0", GeometryRoot = skeletonRoot };

            var partRoot = new MdlNode { Name = "part-root" };
            var partMesh = Triangle("part-mesh");
            partMesh.Bitmap = "stale";
            partMesh.Parent = partRoot;
            partRoot.Children.Add(partMesh);
            var part = new MdlModel { Name = "pfh0_legl001", GeometryRoot = partRoot };

            var composer = new MdlPartComposer((resRef, _) =>
                resRef.Equals("pfh0", StringComparison.OrdinalIgnoreCase) ? skeleton :
                resRef.Equals("pfh0_legl001", StringComparison.OrdinalIgnoreCase) ? part :
                null);

            var composed = composer.Compose(
                "pfh0",
                new[] { ("legl", "pfh0_legl001") },
                adjustSeams: true);

            composed.Should().NotBeNull();
            var attached = composed!.GetMeshNodes().Single();
            attached.Bitmap.Should().Be("pfh0_legl001");
            attached.Parent!.Parent!.Name.Should().Be("lthigh_g");
            attached.Should().NotBeSameAs(partMesh);
            partMesh.Bitmap.Should().Be("stale", "cached source models must not be mutated");
        }

        [Test]
        public void FullBodyRobeAttachesAtTheSkeletonRoot()
        {
            var skeletonRoot = new MdlNode { Name = "root" };
            var torso = new MdlNode
            {
                Name = "torso_g",
                Position = new Vector3(10f, 0f, 0f),
                Parent = skeletonRoot
            };
            skeletonRoot.Children.Add(torso);
            var skeleton = new MdlModel { Name = "pfh0", GeometryRoot = skeletonRoot };

            var robeRoot = new MdlNode { Name = "robe-root" };
            var robeMesh = Triangle("robe-mesh");
            robeMesh.Vertices =
            [
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 1.5f),
                new Vector3(1f, 0f, 0f)
            ];
            robeMesh.Parent = robeRoot;
            robeRoot.Children.Add(robeMesh);
            var robe = new MdlModel { Name = "full_robe", GeometryRoot = robeRoot };

            var composer = new MdlPartComposer((resRef, _) =>
                resRef == "skeleton" ? skeleton :
                resRef == "full_robe" ? robe :
                null);

            var composed = composer.Compose(
                "skeleton",
                new[] { ("robe", "full_robe") },
                adjustSeams: false);

            var attached = composed!.GetMeshNodes().Single();
            attached.Parent!.Parent.Should().BeSameAs(composed.GeometryRoot);
            Vector3.Transform(Vector3.Zero, MdlMeshBuilder.ComposeNodeTransform(attached))
                .X.Should().BeApproximately(0f, 0.0001f,
                    "the flattened robe must not receive the torso transform a second time");
        }

        [Test]
        public void SeamAdjustmentIncreasesHeadAndNeckOverlap()
        {
            var skeletonRoot = new MdlNode { Name = "root" };
            var headBone = new MdlNode { Name = "head_g", Parent = skeletonRoot };
            var neckBone = new MdlNode { Name = "neck_g", Parent = skeletonRoot };
            skeletonRoot.Children.Add(headBone);
            skeletonRoot.Children.Add(neckBone);
            var skeleton = new MdlModel { Name = "pfh0", GeometryRoot = skeletonRoot };

            static MdlModel Part(string name, float minimumZ, float maximumZ)
            {
                var root = new MdlNode { Name = name + "-root" };
                var mesh = Triangle(name + "-mesh");
                mesh.Vertices =
                [
                    new Vector3(0f, 0f, minimumZ),
                    new Vector3(1f, 0f, maximumZ),
                    new Vector3(0f, 1f, minimumZ)
                ];
                mesh.Parent = root;
                root.Children.Add(mesh);
                return new MdlModel { Name = name, GeometryRoot = root };
            }

            var head = Part("head_part", 1.0f, 1.1f);
            var neck = Part("neck_part", 0.8f, 0.95f);
            var composer = new MdlPartComposer((resRef, _) =>
                resRef == "skeleton" ? skeleton :
                resRef == "head_part" ? head :
                resRef == "neck_part" ? neck :
                null);
            var parts = new[]
            {
                ("head", "head_part"),
                ("neck", "neck_part")
            };

            var unchanged = composer.Compose("skeleton", parts, adjustSeams: false)!;
            var adjusted = composer.Compose("skeleton", parts, adjustSeams: true)!;
            var unchangedRoot = unchanged.GetMeshNodes()
                .Single(mesh => mesh.Bitmap == "head_part").Parent!;
            var adjustedRoot = adjusted.GetMeshNodes()
                .Single(mesh => mesh.Bitmap == "head_part").Parent!;

            unchangedRoot.Position.Z.Should().Be(0f);
            adjustedRoot.Position.Z.Should().BeLessThan(
                unchangedRoot.Position.Z,
                "the head must move toward the neck when their authored bounds leave a gap");
        }

        [Test]
        public void ComposerRadiusUsesTheFarthestTransformedVertexRatherThanMixedBoundsCorners()
        {
            var skeletonRoot = new MdlNode { Name = "root" };
            var thigh = new MdlNode { Name = "lthigh_g", Parent = skeletonRoot };
            skeletonRoot.Children.Add(thigh);
            var skeleton = new MdlModel { Name = "pfh0", GeometryRoot = skeletonRoot };

            var partRoot = new MdlNode { Name = "part-root" };
            var partMesh = Triangle("part-mesh");
            partMesh.Vertices =
            [
                new Vector3(-10, 10, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, -1, 0)
            ];
            partMesh.Parent = partRoot;
            partRoot.Children.Add(partMesh);
            var part = new MdlModel { Name = "part", GeometryRoot = partRoot };
            var composer = new MdlPartComposer((resRef, _) =>
                resRef == "skeleton" ? skeleton :
                resRef == "part" ? part :
                null);

            var composed = composer.Compose("skeleton", new[] { ("legl", "part") });

            composed.Should().NotBeNull();
            composed!.BoundsMinimum.Should().Be(new Vector3(-10, -1, 0));
            composed.BoundsMaximum.Should().Be(new Vector3(1, 10, 0));
            composed.Radius.Should().BeApproximately(MathF.Sqrt(200), 0.0001f);
        }

        private ResourceIndex Index() =>
            new(null, new[] { new ResourceIndex.HakLayer("fixture", _resourceDirectory) });

        private static MdlTrimeshNode Triangle(string name) =>
            new()
            {
                Name = name,
                Render = true,
                Bitmap = "surface",
                Vertices =
                [
                    Vector3.Zero,
                    Vector3.UnitX,
                    Vector3.UnitY
                ],
                Normals =
                [
                    Vector3.UnitZ,
                    Vector3.UnitZ,
                    Vector3.UnitZ
                ],
                TextureCoordinates =
                [
                    Vector2.Zero,
                    Vector2.UnitX,
                    Vector2.UnitY
                ],
                Faces =
                [
                    new MdlFace
                    {
                        VertexIndex0 = 0,
                        VertexIndex1 = 1,
                        VertexIndex2 = 2
                    }
                ]
            };

        private static byte[] CompactDds(
            int width,
            int height,
            int channels,
            float alphaMean,
            byte[] payload)
        {
            var bytes = new byte[20 + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), width);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), height);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), channels);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, 4), payload.Length);
            BinaryPrimitives.WriteInt32LittleEndian(
                bytes.AsSpan(16, 4),
                BitConverter.SingleToInt32Bits(alphaMean));
            payload.CopyTo(bytes, 20);
            return bytes;
        }

        private static byte[] StandardDxt1(byte[] payload, int width = 4, int height = 4)
        {
            var bytes = new byte[128 + payload.Length];
            "DDS "u8.CopyTo(bytes);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), 124);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 0x00081007);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), (uint)height);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), (uint)width);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), (uint)payload.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(76, 4), 32);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80, 4), 4);
            "DXT1"u8.CopyTo(bytes.AsSpan(84, 4));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(108, 4), 0x1000);
            payload.CopyTo(bytes, 128);
            return bytes;
        }

        private static byte[] SolidColorTga(byte r, byte g, byte b)
        {
            // Minimal 1x1 uncompressed 24-bit true-color TGA. Pixel data is stored BGR.
            var bytes = new byte[18 + 3];
            bytes[2] = 2;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), 1);
            bytes[16] = 24;
            bytes[18] = b;
            bytes[19] = g;
            bytes[20] = r;
            return bytes;
        }

        private static byte[] SinglePixelPlt(int layer)
        {
            // 24-byte PLT V1 header followed by one (intensity, layer) pixel for a 1x1 image.
            var bytes = new byte[24 + 2];
            "PLT V1  "u8.CopyTo(bytes);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 1);
            bytes[24] = 0;
            bytes[25] = (byte)layer;
            return bytes;
        }

        private static byte[] RedDxt1Block()
        {
            var block = new byte[8];
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(0, 2), 0xF800);
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2, 2), 0);
            return block;
        }

        private static byte[] GreenDxt1Block()
        {
            var block = new byte[8];
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(0, 2), 0x07E0);
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2, 2), 0);
            return block;
        }

        private static (byte R, byte G, byte B, byte A) Pixel(TextureImage image, int x, int y)
        {
            var offset = (y * image.Width + x) * 4;
            return (
                image.Pixels[offset],
                image.Pixels[offset + 1],
                image.Pixels[offset + 2],
                image.Pixels[offset + 3]);
        }
    }
}
