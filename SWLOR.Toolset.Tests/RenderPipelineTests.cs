using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP4.2 Domain-level render pipeline: <see cref="MdlMeshBuilder"/>,
    /// <see cref="TextureLoader"/>, <see cref="TxiInfo"/>, and <see cref="MaterialResolver"/>.
    /// Mirrors the repo-hak-only fixture pattern established by <see cref="ResourceIndexTests"/>
    /// and <see cref="SetParserTests"/> so everything here runs deterministically without
    /// depending on a local NWN:EE install.
    /// </summary>
    public class RenderPipelineTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var hakBuilderConfig = Path.Combine(current.FullName, "Build", "hakbuilder.json");
                    var haksDirectory = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (File.Exists(hakBuilderConfig) && Directory.Exists(haksDirectory))
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static string HakBuilderConfigPath => Path.Combine(RepoRoot, "Build", "hakbuilder.json");

        private static string HaksDirectory => Path.Combine(RepoRoot, "SWLOR_Haks");

        private static ResourceIndex BuildHakOnlyIndex() =>
            ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);

        // Tileset directories chosen for variety (dungeon/castle/crypt/mine/city interiors),
        // each contributing several distinct tile models toward the >= 20 sample requirement.
        private static readonly string[] SampleTilesetFiles =
        {
            "sw_t_dungeon/tde01.set",
            "sw_t_castle1/tic01.set",
            "sw_t_crypt/tdc01.set",
            "sw_t_mine/tdm01.set",
            "sw_t_cityext/tcn01.set"
        };

        /// <summary>
        /// Collects a deterministic sample of at least 20 distinct tile model resrefs by parsing
        /// several .set tileset files (via the WP2.2 <see cref="SetFileParser"/>) and taking each
        /// tileset's distinct, non-blank <c>Model</c> values in file order.
        /// </summary>
        private static IReadOnlyList<string> GetSampleModelResRefs()
        {
            var resRefs = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var relativeSetPath in SampleTilesetFiles)
            {
                var setPath = Path.Combine(HaksDirectory, relativeSetPath.Replace('/', Path.DirectorySeparatorChar));
                var tileset = SetFileParser.ParseFile(setPath);

                var takenFromThisTileset = 0;
                foreach (var tile in tileset.Tiles)
                {
                    if (string.IsNullOrWhiteSpace(tile.Model))
                        continue;
                    if (!seen.Add(tile.Model))
                        continue;

                    resRefs.Add(tile.Model);
                    takenFromThisTileset++;
                    if (takenFromThisTileset >= 6)
                        break;
                }
            }

            return resRefs;
        }

        [Test]
        public void MdlMeshBuilder_ForSampleOfCorpusTileModels_BuildsValidGeometryForEveryModel()
        {
            var modelResRefs = GetSampleModelResRefs();
            modelResRefs.Count.Should().BeGreaterThanOrEqualTo(20, "the sample must cover at least 20 distinct MDL models");

            // Tile geometry referenced by a .set file's "Model=" key is frequently inherited
            // unchanged from the base-game tileset (e.g. tde01_a13_01.mdl lives in nwn_base's
            // BIF, not in sw_t_dungeon's loose-file hak) - SWLOR's hak only overrides/adds a
            // subset of tiles. A hak-only index therefore can't resolve most sampled models, so
            // this test needs the base game layer too; per WP2.3's established pattern, skip
            // gracefully if no local NWN:EE install is found instead of failing the build.
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE installation was found (Steam/GOG); skipping base-game-backed tile model sample test.");
                return;
            }

            var dataDirectory = Path.Combine(installPath, "data");
            var keyPath = Path.Combine(dataDirectory, "nwn_base.key");
            if (!File.Exists(keyPath))
            {
                Assert.Ignore($"NWN install found at '{installPath}' but no nwn_base.key under its data directory; skipping.");
                return;
            }

            var baseLayer = KeyBifCatalog.Load(dataDirectory);
            var index = ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory, baseLayer);
            var reader = new MdlReader();

            var modelsWithNoVisibleMesh = new List<string>();
            var totalMeshesBuilt = 0;

            foreach (var resRef in modelResRefs)
            {
                var identity = new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("mdl"));
                index.TryLookup(identity, out var handle).Should().BeTrue($"'{resRef}.mdl' should resolve through the hak layers or the base game");

                var bytes = handle.GetBytes();
                bytes.Should().NotBeEmpty();

                MdlModel model = null!;
                RenderModel renderModel = null!;

                Action parseAndBuild = () =>
                {
                    model = reader.Parse(bytes);
                    renderModel = MdlMeshBuilder.Build(model);
                };
                parseAndBuild.Should().NotThrow($"parsing and mesh-building '{resRef}.mdl' must not throw");

                if (renderModel.Meshes.Count == 0)
                {
                    modelsWithNoVisibleMesh.Add(resRef);
                    continue;
                }

                foreach (var mesh in renderModel.Meshes)
                {
                    totalMeshesBuilt++;

                    mesh.VertexCount.Should().BeGreaterThan(0, $"'{resRef}' mesh '{mesh.NodeName}' should have vertices");
                    mesh.Indices.Length.Should().BeGreaterThan(0, $"'{resRef}' mesh '{mesh.NodeName}' should have face indices");
                    (mesh.Indices.Length % 3).Should().Be(0, "indices are emitted as whole triangles");

                    foreach (var vertexIndex in mesh.Indices)
                    {
                        vertexIndex.Should().BeInRange(0, mesh.VertexCount - 1,
                            $"'{resRef}' mesh '{mesh.NodeName}' has a face index out of vertex bounds");
                    }

                    if (mesh.Normals.Length > 0)
                        mesh.Normals.Length.Should().Be(mesh.VertexCount * 3, "normals, when present, are parallel to positions");

                    if (mesh.TexCoords.Length > 0)
                        mesh.TexCoords.Length.Should().Be(mesh.VertexCount * 2, "UVs, when present, are parallel to positions");
                }
            }

            TestContext.Out.WriteLine(
                $"Sampled {modelResRefs.Count} models, built {totalMeshesBuilt} visible meshes total. " +
                $"{modelsWithNoVisibleMesh.Count} model(s) had no visible trimesh: {string.Join(", ", modelsWithNoVisibleMesh)}");

            totalMeshesBuilt.Should().BeGreaterThan(0, "at least one model in the sample must produce visible geometry");
        }

        [Test]
        public void TextureLoader_LoadTga_ForKnownCorpusTexture_DecodesToReportedDimensions()
        {
            var index = BuildHakOnlyIndex();

            // A plain (non-BioWare-DDS) TGA texture used by the dungeon tileset.
            var image = TextureLoader.LoadTga(index, "zde01_wall9k1");

            image.Should().NotBeNull();
            image!.Width.Should().BeGreaterThan(0);
            image.Height.Should().BeGreaterThan(0);
            image.Pixels.Length.Should().Be(image.Width * image.Height * 4);
            image.SourceFormat.Should().Be(TextureSourceFormat.Tga);
        }

        [Test]
        public void TextureLoader_LoadDds_ForKnownBiowareFormatCorpusTexture_DecodesToReportedDimensions()
        {
            var index = BuildHakOnlyIndex();

            // ad_btllady.dds ships in sw_cr_creature and is in BioWare's proprietary DDS format
            // (no "DDS " magic; header is width=512, height=512, channels=4 -> DXT5).
            var image = TextureLoader.LoadDds(index, "ad_btllady");

            image.Should().NotBeNull();
            image!.Width.Should().Be(512);
            image.Height.Should().Be(512);
            image.Pixels.Length.Should().Be(image.Width * image.Height * 4);
            image.SourceFormat.Should().Be(TextureSourceFormat.Dds);
        }

        [Test]
        public void TextureLoader_LoadPlt_ForKnownCorpusTexture_DecodesToReportedDimensions()
        {
            var index = BuildHakOnlyIndex();

            // Palette TGAs (pal_skin01 etc.) ship only in the base game BIF, not in SWLOR_Haks,
            // so this hak-only index will fall back to grayscale for every layer - PltReader.Render
            // still succeeds and yields non-empty RGBA of the reported dimensions, which is all
            // this test asserts (color-accurate palette resolution is exercised separately, see
            // the WORKLOG note on this dev machine's local NWN:EE install for future coverage).
            var image = TextureLoader.LoadPlt(index, "cpex_direwolf");

            image.Should().NotBeNull();
            image!.Width.Should().BeGreaterThan(0);
            image.Height.Should().BeGreaterThan(0);
            image.Pixels.Length.Should().Be(image.Width * image.Height * 4);
            image.SourceFormat.Should().Be(TextureSourceFormat.Plt);
        }

        [Test]
        public void TextureLoader_Load_WhenResourceMissing_ReturnsNull()
        {
            var index = BuildHakOnlyIndex();

            TextureLoader.Load(index, "definitely_not_a_real_texture_12345").Should().BeNull();
        }

        [Test]
        public void TxiInfo_Parse_EnvMapAndAlphaMean_MatchesRealCorpusFile()
        {
            var path = Path.Combine(HaksDirectory, "sw_cr_creature", "hp_brain_jar.txi");
            File.Exists(path).Should().BeTrue();

            var info = TxiInfo.Parse(File.ReadAllText(path));

            info.EnvMapTexture.Should().Be("TTR01__ref01");
            info.AlphaMean.Should().NotBeNull();
            info.AlphaMean!.Value.Should().BeApproximately(0.364f, 0.0001f);
            info.HasTransparencyHint.Should().BeTrue();
        }

        [Test]
        public void TxiInfo_Parse_PunchthroughBlending_MatchesRealCorpusFile()
        {
            var path = Path.Combine(HaksDirectory, "sw_plc", "ct01_treefol08.txi");
            File.Exists(path).Should().BeTrue();

            var info = TxiInfo.Parse(File.ReadAllText(path));

            info.Blending.Should().Be(TxiBlendMode.PunchThrough);
            info.HasTransparencyHint.Should().BeTrue();
        }

        [Test]
        public void TxiInfo_Parse_Cube_MatchesRealCorpusFile()
        {
            var path = Path.Combine(HaksDirectory, "sw_t_mine", "ztall_sky.txi");
            File.Exists(path).Should().BeTrue();

            var info = TxiInfo.Parse(File.ReadAllText(path));

            info.Cube.Should().BeTrue();
        }

        [Test]
        public void TxiInfo_Parse_UnknownKeysAreIgnoredWithoutThrowing()
        {
            const string content = "somefuturekey somevalue\nblending additive\nchannelscale 0\n0\n0\n0\n0\n";

            Action parse = () =>
            {
                var info = TxiInfo.Parse(content);
                info.Blending.Should().Be(TxiBlendMode.Additive);
            };

            parse.Should().NotThrow();
        }

        [Test]
        public void MaterialResolver_Parse_InlineSample_ExtractsRenderHintAndTexture0()
        {
            const string sample =
                "// Renderhint\n" +
                "renderhint NormalAndSpecMapped\n" +
                "\n" +
                "// Textures\n" +
                "texture0 hutt_hbody\n";

            var material = MaterialResolver.Parse(sample);

            material.RenderHint.Should().Be("NormalAndSpecMapped");
            material.GetTexture(0).Should().Be("hutt_hbody");
        }

        [Test]
        public void MaterialResolver_Parse_MultiTextureAndCustomShaderSample_ExtractsAllSlots()
        {
            const string sample =
                "renderhint Legacy\n" +
                "texture0 base_diffuse\n" +
                "texture1 base_normal\n" +
                "customshaderVSH my_vertex_shader\n" +
                "customshaderPSH my_pixel_shader\n" +
                "someunknownparam 1.0\n";

            var material = MaterialResolver.Parse(sample);

            material.RenderHint.Should().Be("Legacy");
            material.GetTexture(0).Should().Be("base_diffuse");
            material.GetTexture(1).Should().Be("base_normal");
            material.CustomShaders.Should().ContainKey("customshaderVSH").WhoseValue.Should().Be("my_vertex_shader");
            material.CustomShaders.Should().ContainKey("customshaderPSH").WhoseValue.Should().Be("my_pixel_shader");
        }

        [Test]
        public void MaterialResolver_Parse_RealCorpusMtrFile_MatchesFileContents()
        {
            // Located by name rather than by hak folder: the haks reorganise periodically
            // (this file has already moved from sw_cr_creature to sw_cr_vehicle), and what the
            // test cares about is that a real shipped .mtr parses, not where it currently lives.
            var path = Directory
                .EnumerateFiles(HaksDirectory, "c_huttbomb1.mtr", SearchOption.AllDirectories)
                .FirstOrDefault();
            path.Should().NotBeNull("c_huttbomb1.mtr must ship in one of the haks");

            var material = MaterialResolver.Parse(File.ReadAllText(path!));

            material.RenderHint.Should().Be("NormalAndSpecMapped");
            material.GetTexture(0).Should().Be("hutt_hbody");
        }

        [Test]
        public void MaterialResolver_ResolveDiffuseTextureName_WhenMtrExistsInIndex_ReturnsTexture0()
        {
            var index = BuildHakOnlyIndex();

            // c_huttbomb1.mtr ships in the haks (confirmed above) and declares texture0
            // "hutt_hbody" - resolving the mesh/material name "c_huttbomb1" should follow the
            // material to its diffuse texture rather than passing the bare name through.
            var resolved = MaterialResolver.ResolveDiffuseTextureName(index, "c_huttbomb1");

            resolved.Should().Be("hutt_hbody");
        }

        [Test]
        public void MaterialResolver_ResolveDiffuseTextureName_WhenNoMtrExists_PassesNameThrough()
        {
            var index = BuildHakOnlyIndex();

            var resolved = MaterialResolver.ResolveDiffuseTextureName(index, "zde01_wall9k1");

            resolved.Should().Be("zde01_wall9k1");
        }

        [Test]
        public void MaterialResolver_ResolveMaterialMaps_MtrDeclaresNormalAndSpecular_ReturnsAllThree()
        {
            var index = BuildHakOnlyIndex();

            // fishing_rod.mtr ships in the haks declaring texture0 fishing_rod,
            // texture1 fishing_rod_n, texture2 fishing_rod_s, texture3 fishing_rod_r
            // (plus literal "null" placeholders in texture4/5, which must be ignored).
            var maps = MaterialResolver.ResolveMaterialMaps(index, "fishing_rod");

            maps.Diffuse.Should().Be("fishing_rod");
            maps.Normal.Should().Be("fishing_rod_n");
            maps.Specular.Should().Be("fishing_rod_s");
            maps.Roughness.Should().Be("fishing_rod_r");
        }

        [Test]
        public void MaterialResolver_ResolveMaterialMaps_MtrNullSlot_MeansNoMapAndNoSuffixGuessing()
        {
            var index = BuildHakOnlyIndex();

            // c_n_thranta.mtr declares "texture1 null" alongside real texture2/texture3 slots -
            // the literal null placeholder must come back as no normal map, not a texture
            // named "null".
            var maps = MaterialResolver.ResolveMaterialMaps(index, "c_n_thranta");

            maps.Diffuse.Should().Be("c_n_thrant");
            maps.Normal.Should().BeNull();
            maps.Specular.Should().Be("Aegis_specular");
            maps.Roughness.Should().Be("Aegis_specular");
        }

        [Test]
        public void MaterialResolver_ResolveMaterialMaps_NoMtr_FindsSuffixCompanions()
        {
            var index = BuildHakOnlyIndex();

            // heavy_repeater has no .mtr; heavy_repeater_s.dds ships beside heavy_repeater.dds
            // (and no _n/_r companions exist), exercising NWN:EE's automatic suffix convention.
            var maps = MaterialResolver.ResolveMaterialMaps(index, "heavy_repeater");

            maps.Diffuse.Should().Be("heavy_repeater");
            maps.Normal.Should().BeNull();
            maps.Specular.Should().Be("heavy_repeater_s");
            maps.Roughness.Should().BeNull();
        }

        [Test]
        public void MaterialResolver_ResolveMaterialMaps_NoMtrAndNoCompanions_ReturnsDiffuseOnly()
        {
            var index = BuildHakOnlyIndex();

            var maps = MaterialResolver.ResolveMaterialMaps(index, "zde01_wall9k1");

            maps.Diffuse.Should().Be("zde01_wall9k1");
            maps.Normal.Should().BeNull();
            maps.Specular.Should().BeNull();
            maps.Roughness.Should().BeNull();
        }
    }
}
