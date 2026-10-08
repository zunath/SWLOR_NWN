using FluentAssertions;
using NUnit.Framework;
using Nwn.Formats.Mtr;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class MaterialResolverAdapterTests
    {
        [Test]
        public void ParseUsesSharedBindingsAndPreservesTypedAndUninterpretedParameters()
        {
            const string source =
                "renderhint NormalAndSpecMapped\n" +
                "texture0 diffuse\n" +
                "texture1 NULL\n" +
                "texture7 mask\n" +
                "texture10 palette\n" +
                "customshaderVSH vertex_shader\n" +
                "customshaderPSH fs_plt_tinter\n" +
                "parameter float scalar 0.25\n" +
                "parameter float vector 0.1 0.2 0.3 1.0\n" +
                "parameter bool futureFlag 1\n" +
                "transparency 1\n";

            var material = MaterialResolver.Parse(source);

            material.RenderHint.Should().Be("NormalAndSpecMapped");
            MaterialResolver.GetTexture(material, 0).Should().Be("diffuse");
            material.Textures.Should().ContainKey(1).WhoseValue.Should().BeNull();
            MaterialResolver.GetTexture(material, 1).Should().BeNull();
            material.Textures.Should().NotContainKey(2);
            MaterialResolver.GetTexture(material, 2).Should().BeNull();
            MaterialResolver.GetTexture(material, 7).Should().Be("mask");
            MaterialResolver.GetTexture(material, 10).Should().Be("palette");
            material.RawShaderBindings.Should().ContainKey("customshaderVSH").WhoseValue.Should().Be("vertex_shader");
            material.RawShaderBindings.Should().ContainKey("customshaderPSH").WhoseValue.Should().Be("fs_plt_tinter");
            material.Parameters["scalar"].TypeName.Should().Be("float");
            material.Parameters["scalar"].Values.Should().ContainSingle().Which.Should().BeApproximately(0.25, 0.00001);
            material.Parameters["vector"].RawValues.Should().Equal("0.1", "0.2", "0.3", "1.0");
            material.Parameters["vector"].Values.Should().Equal(
                (double)0.1f,
                (double)0.2f,
                (double)0.3f,
                1d);
            material.Parameters["futureFlag"].Kind.Should().Be(MtrParameterKind.Uninterpreted);
            material.Parameters["futureFlag"].TypeName.Should().Be("bool");
            material.Parameters["futureFlag"].RawValues.Should().Equal("1");
            material.UnrecognizedDirectives.Should().ContainSingle(item => item.Text == "transparency 1");
            MaterialResolver.GetAlphaSource(material).Should().BeNull();
        }

        [Test]
        public void TintAlphaPolicyReadsSharedRawParameterValuesAndRespectsAbsentSlots()
        {
            var material = MaterialResolver.Parse(
                "texture1 normal\n" +
                "texture9 mask\n" +
                "parameter float useTexture1Alpha 0\n" +
                "parameter float useTexture9Alpha 1.0\n");

            MaterialResolver.GetAlphaSource(material).Should().Be(
                new MtrAlphaSource("mask", UsesRedChannel: true));

            var absentMask = MaterialResolver.Parse(
                "parameter float useTexture9Alpha 1.0\n");
            MaterialResolver.GetAlphaSource(absentMask).Should().BeNull();
        }

        [Test]
        public void ExplicitMaterialResolutionUsesSlotsWhileBitmapResolutionBypassesSameNameMtr()
        {
            var directory = CreateResourceDirectory();
            try
            {
                File.WriteAllText(
                    Path.Combine(directory, "surface.mtr"),
                    "texture0 diffuse_from_material\n" +
                    "texture1 normal_from_material\n" +
                    "texture2 specular_from_material\n" +
                    "texture3 roughness_from_material\n" +
                    "texture4 null\n");
                File.WriteAllText(
                    Path.Combine(directory, "bitmap_name.mtr"),
                    "texture0 should_not_be_selected\n");
                var resources = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: [new ResourceIndex.HakLayer("fixture", directory)]);

                var materialMaps = MaterialResolver.ResolveMaterialMaps(
                    resources,
                    "surface",
                    resolveMaterial: true);
                var bitmapMaps = MaterialResolver.ResolveMaterialMaps(
                    resources,
                    "bitmap_name",
                    resolveMaterial: false);

                materialMaps.Diffuse.Should().Be("diffuse_from_material");
                materialMaps.Normal.Should().Be("normal_from_material");
                materialMaps.Specular.Should().Be("specular_from_material");
                materialMaps.Roughness.Should().Be("roughness_from_material");
                bitmapMaps.Diffuse.Should().Be("bitmap_name");
                bitmapMaps.Normal.Should().BeNull();
                bitmapMaps.Specular.Should().BeNull();
                bitmapMaps.Roughness.Should().BeNull();
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void DuplicateRecognizedBindingFailsWithItsSourceLineInsteadOfChoosingOne()
        {
            Action parse = () => MaterialResolver.Parse("texture0 first\ntexture0 second\n");

            parse.Should().Throw<FormatException>().WithMessage("*line 2*Duplicate texture binding 0*");
        }

        [Test]
        public void AllConfiguredHakMaterialsParseAndKeepUnsupportedLinesVisible()
        {
            var haksRoot = Support.ToolsetCorpusPaths.HaksRoot;
            haksRoot.Should().NotBeNullOrWhiteSpace(
                "the MTR corpus gate must use an explicit, complete HAK fixture root");
            Directory.Exists(haksRoot).Should().BeTrue($"configured HAK root exists: {haksRoot}");

            var files = Directory
                .EnumerateFiles(haksRoot!, "*.mtr", SearchOption.AllDirectories)
                .ToArray();
            files.Should().NotBeEmpty("the configured HAK root must contain native material resources");

            var parsedCount = 0;
            var unsupportedCount = 0;
            var failures = new List<string>();
            foreach (var file in files)
            {
                try
                {
                    var material = MaterialResolver.Parse(File.ReadAllBytes(file));
                    parsedCount++;
                    unsupportedCount += material.UnrecognizedDirectives.Count;
                }
                catch (Exception exception)
                {
                    failures.Add($"{Path.GetFileName(file)}: {exception.Message}");
                }
            }

            parsedCount.Should().Be(files.Length);
            unsupportedCount.Should().BeGreaterThan(
                0,
                "the adapter should retain corpus directives outside the shared reader's typed model");
            failures.Should().BeEmpty();
        }

        private static string CreateResourceDirectory()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                $"swlor-material-adapter-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
