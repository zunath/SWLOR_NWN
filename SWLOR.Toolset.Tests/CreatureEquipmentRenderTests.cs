using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>End-to-end coverage for visible equipment on placed creature geometry.</summary>
    [TestFixture]
    [Category("LicensedCorpus")]
    [NonParallelizable]
    public sealed class CreatureEquipmentRenderTests
    {
        private BlueprintPreviewRenderer _renderer = null!;
        private ResourceIndex _resources = null!;
        private ModuleWorkspace _workspace = null!;
        private GitDocument _hangarGit = null!;
        private GitDocument _anchorGit = null!;

        private static string RepoRoot
        {
            get
            {
                for (var current = new DirectoryInfo(AppContext.BaseDirectory);
                     current != null;
                     current = current.Parent)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks", "sw_2da")))
                    {
                        return current.FullName;
                    }
                }

                throw new DirectoryNotFoundException("Could not locate the repository rendering corpus.");
            }
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var installRoot = NwnInstallLocator.Locate();
            if (installRoot == null)
            {
                Assert.Ignore("the licensed rendering corpus requires NWN:EE");
                return;
            }

            var baseLayer = KeyBifCatalog.Load(Path.Combine(installRoot, "data"));
            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                baseLayer);
            index.EnsureInitialized();
            _resources = index;

            var twoDa = new TwoDaService(index);
            var tlk = TlkService.Load(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json"));
            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path, index), log);
            context.Open(CorpusLocator.ModuleDirectory);
            await context.Catalog!.BuildTask;
            _renderer = new BlueprintPreviewRenderer(
                context,
                index,
                appearances: new AppearanceService(twoDa, tlk),
                baseItems: new BaseItemIconService(twoDa),
                twoDa: twoDa,
                tlk: tlk);

            _workspace = context.Workspace!;
            (_, _hangarGit, _) = _workspace.LoadArea("czs220_hangar");
            (_, _anchorGit, _) = _workspace.LoadArea("anchor_entreenor");
        }

        [Test]
        public void PlacedChestArmorReplacesNakedTorsoAndKeepsArmorDyes()
        {
            var dressed = _renderer.BuildModel(ResourceType.Utc, _hangarGit.Creatures.First());

            dressed.Should().NotBeNull();
            dressed!.LayerColorIndices[SWLOR.NWN.Formats.Plt.PltLayers.Cloth1].Should().Be(107);
            dressed.Meshes.Should().Contain(
                mesh => mesh.TextureName.Equals("pmh0_chest189", StringComparison.OrdinalIgnoreCase),
                "the embedded Czerka uniform's torso geometry must replace the naked chest");
            dressed.Meshes.Should().NotContain(
                mesh => mesh.TextureName.Equals("pmh0_chest001", StringComparison.OrdinalIgnoreCase),
                "the equipped torso must not coexist with the naked torso");

            var dockhandRoot = _hangarGit.Creatures.First(creature =>
                creature.GetListOrEmpty("Equip_ItemList").Any(item =>
                    System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == "2" &&
                    item.GetIntOrNull("ArmorPart_Torso") == 102));
            var dockhand = _renderer.BuildModel(ResourceType.Utc, dockhandRoot);
            dockhand.Should().NotBeNull();
            dockhand!.Meshes.Should().Contain(
                mesh => mesh.TextureName.Equals("pmh0_chest102", StringComparison.OrdinalIgnoreCase));
            dockhand.Meshes.Should().NotContain(
                mesh => mesh.TextureName.Equals("pmh0_chest001", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void EmbeddedArmorCarriesCustomTintOverridesIntoRenderedMeshes()
        {
            var creature = InstanceFieldMap.Duplicate(_hangarGit.Creatures.First());
            var armor = creature.GetListOrEmpty("Equip_ItemList").Single(item =>
                System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == "2");
            const string tintKey = "TM_embedded_test_4";
            new VarTable(armor).SetInt(tintKey, 654321);

            var model = _renderer.BuildModel(ResourceType.Utc, creature);

            model.Should().NotBeNull();
            var armorMeshes = model!.Meshes.Where(mesh => mesh.UsesItemTintOverrides).ToList();
            armorMeshes.Should().NotBeEmpty();
            armorMeshes.Should().OnlyContain(mesh =>
                mesh.TintMapOverrides.ContainsKey(tintKey) &&
                mesh.TintMapOverrides[tintKey] == 654321,
                "the equipped item owns TM_* values at runtime and the preview must use that same source");
        }

        [Test]
        public void EmbeddedRightHandWeaponAddsCreatureGeometry()
        {
            var armedRoot = _anchorGit.Creatures.First(creature => creature.GetListOrEmpty("Equip_ItemList")
                .Any(item => System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == "16"));
            var armed = _renderer.BuildModel(ResourceType.Utc, armedRoot);
            var unarmed = _renderer.BuildModel(ResourceType.Utc, WithoutEquippedSlot(armedRoot, 16));

            armed.Should().NotBeNull();
            unarmed.Should().NotBeNull();
            armed!.Meshes.Sum(mesh => mesh.VertexCount).Should().BeGreaterThan(
                unarmed!.Meshes.Sum(mesh => mesh.VertexCount),
                "the embedded rifle parts must add geometry to the right-hand skeleton bone");
        }

        [Test]
        public void EquippedCloakUsesWearerGeometryTextureAndOwnDyes()
        {
            var gravius = _renderer.BuildModel(
                ResourceType.Utc,
                _workspace.LoadBlueprint(ResourceType.Utc, "darthgravius").Fields);
            gravius.Should().NotBeNull();
            gravius!.LayerColorIndices[SWLOR.NWN.Formats.Plt.PltLayers.Cloth1].Should().Be(97);
            var cloakMeshes = gravius.Meshes.Where(mesh =>
                mesh.TextureName.Contains("cloak", StringComparison.OrdinalIgnoreCase)).ToList();
            cloakMeshes.Should().NotBeEmpty(
                "the female elf must wear the cloak model authored for her own skeleton");
            cloakMeshes.Should().OnlyContain(mesh => mesh.LayerColorIndices.ContainsKey(
                SWLOR.NWN.Formats.Plt.PltLayers.Cloth1));
            cloakMeshes.Should().OnlyContain(mesh =>
                mesh.TextureName.Equals("pfe0_cloak_020", StringComparison.OrdinalIgnoreCase),
                "cloak appearance 20 maps to geometry 20 and texture 20 through cloakmodel.2da");
            cloakMeshes.Select(mesh =>
                    mesh.LayerColorIndices[SWLOR.NWN.Formats.Plt.PltLayers.Cloth1])
                .Should().OnlyContain(cloth => cloth == 45,
                    "the cloak's dyes must not be replaced by the equipped chest robe's palette");
        }

        [Test]
        public void EquippedHelmetKeepsItsOwnDyes()
        {
            var commando = _renderer.BuildModel(
                ResourceType.Utc,
                _workspace.LoadBlueprint(ResourceType.Utc, "sith_commando").Fields);
            commando.Should().NotBeNull();
            var helmetMeshes = commando!.Meshes.Where(mesh =>
                mesh.LayerColorIndices.ContainsKey(SWLOR.NWN.Formats.Plt.PltLayers.Cloth1) &&
                mesh.LayerColorIndices[SWLOR.NWN.Formats.Plt.PltLayers.Cloth1] == 63 &&
                mesh.LayerColorIndices.ContainsKey(SWLOR.NWN.Formats.Plt.PltLayers.Metal2) &&
                mesh.LayerColorIndices[SWLOR.NWN.Formats.Plt.PltLayers.Metal2] == 0).ToList();
            helmetMeshes.Should().NotBeEmpty(
                "the helmet's Cloth1 63 / Metal2 0 UTI palette must not inherit the armor's 23 / 17 dyes");
        }

        [Test]
        public void FixedModelCreatureComposesHeldWeapon()
        {
            // Fixed-model creatures still need their attachments composed. This is also the model
            // path shared by software thumbnails, which previously rendered only the bare base MDL.
            var fixedCreature = _workspace.LoadBlueprint(ResourceType.Utc, "bf_butcher").Fields;
            var armedFixed = _renderer.BuildModel(ResourceType.Utc, fixedCreature);
            var unarmedFixed = _renderer.BuildModel(
                ResourceType.Utc, WithoutEquippedSlot(fixedCreature, 16));

            armedFixed.Should().NotBeNull();
            unarmedFixed.Should().NotBeNull();
            armedFixed!.Meshes.Sum(mesh => mesh.VertexCount).Should().BeGreaterThan(
                unarmedFixed!.Meshes.Sum(mesh => mesh.VertexCount),
                "a simple creature's held item must be composed with its base model");
        }

        [Test]
        public void AgricultureGuildMasterTintSourcesRemainVisible()
        {
            var model = _renderer.BuildModel(
                ResourceType.Utc,
                _workspace.LoadBlueprint(ResourceType.Utc, "agr_guildmaster").Fields);

            model.Should().NotBeNull();
            var textures = new PreviewTextureCache(_resources);
            foreach (var surface in new[]
                     {
                         "pmh0_legl087", "pmh0_legr087", "pmh0_shinl085", "pmh0_shinr085"
                     })
            {
                var meshes = model!.Meshes
                    .Where(mesh => mesh.TextureName.Equals(surface, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                meshes.Should().NotBeEmpty($"the equipped armor uses body-part surface {surface}");
                meshes.Should().OnlyContain(
                    mesh => mesh.MaterialName.Equals(surface, StringComparison.OrdinalIgnoreCase),
                    $"NULL-bitmap body part {surface} must receive its generated tint material");
                textures.Get(surface, model.LayerColorIndices, resolveMaterial: true)
                    .Should().NotBeNull($"converted tint surface {surface} must remain renderable");
            }

            var head = model!.Meshes.Single(mesh =>
                mesh.MaterialName.Equals("pmh0_head038", StringComparison.OrdinalIgnoreCase));
            head.TextureName.Should().Be("pmh0_head220",
                "the creature's authored head model and its isolated tint material are distinct resrefs");
            head.MaterialName.Should().Be("pmh0_head038",
                "the binary model's explicit generated tint material must reach the preview renderer");

            var texture = textures.Get(
                head.MaterialName,
                model.LayerColorIndices,
                resolveMaterial: true);

            texture.Should().NotBeNull();
            var visible = texture!.Pixels
                .Chunk(4)
                .Where(pixel => pixel[3] > 0)
                .ToList();
            visible.Should().NotBeEmpty();
            visible.Average(pixel => pixel[0] + pixel[1] + pixel[2]).Should().BeGreaterThan(45,
                "skin palette row 44 must resolve from the bottom of the NWN palette atlas instead of the black decoded top row");

            var inheritedMaterials = model.Meshes
                .Where(mesh => string.IsNullOrWhiteSpace(mesh.MaterialName))
                .Select(mesh => mesh.TextureName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(textureName => MaterialResolver.TryParseMaterial(_resources, textureName) is { } material &&
                    material.CustomShaders.Values.Any(shader =>
                        shader.Equals("fs_plt_tinter", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            inheritedMaterials.Should().Contain("pmh0_chest027",
                "segmented composition inherits this generated MTR through its bitmap resref");
            foreach (var inheritedMaterial in inheritedMaterials)
            {
                textures.Get(
                        inheritedMaterial,
                        model.LayerColorIndices,
                        resolveMaterial: true)
                    .Should().NotBeNull(
                        $"same-resref MTR {inheritedMaterial} must replace its removed PLT");
            }
        }

        [Test]
        public void InstalledHakStackRendersAgricultureGuildMasterTintSources()
        {
            var installRoot = NwnInstallLocator.Locate();
            var profile = NwnIniProfile.Load();
            var ifoPath = Path.Combine(RepoRoot, "Module", "ifo", "module.ifo.json");
            if (installRoot == null || profile.HakDirectory == null || !File.Exists(ifoPath))
                Assert.Ignore("the installed NWN HAK stack is unavailable");

            var resolution = profile.ResolveHakLayers(IfoDocument.Load(ifoPath).HakNames);
            if (resolution.MissingHakNames.Count > 0)
                Assert.Ignore("the installed NWN profile is missing one or more module HAKs");

            var resources = ResourceIndex.CreateDeferred(
                resolution.Layers,
                () => KeyBifCatalog.Load(Path.Combine(installRoot, "data")));
            resources.EnsureInitialized();
            var twoDa = new TwoDaService(resources);
            var tlk = TlkService.Load(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json"));
            var context = new WorkspaceContext(
                path => new ModuleWorkspace(path, resources),
                new OutputLogService());
            context.Open(CorpusLocator.ModuleDirectory);
            var renderer = new BlueprintPreviewRenderer(
                context,
                resources,
                appearances: new AppearanceService(twoDa, tlk),
                baseItems: new BaseItemIconService(twoDa),
                twoDa: twoDa,
                tlk: tlk);

            var model = renderer.BuildModel(ResourceType.Utc, "agr_guildmaster");
            model.Should().NotBeNull();
            var textures = new PreviewTextureCache(resources);
            var tintMaterials = model!.Meshes
                .Select(mesh => string.IsNullOrWhiteSpace(mesh.MaterialName)
                    ? mesh.TextureName
                    : mesh.MaterialName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => (Name: name, Material: MaterialResolver.TryParseMaterial(resources, name)))
                .Where(entry => entry.Material?.CustomShaders.Values.Any(shader =>
                    shader.Equals("fs_plt_tinter", StringComparison.OrdinalIgnoreCase)) == true)
                .ToList();
            tintMaterials.Select(entry => entry.Name).Should().Contain("pmh0_handl003",
                "the left hand was the known legacy-DDS collision that corrupted creature colors");
            foreach (var (name, material) in tintMaterials)
            {
                var tintTexture = material!.GetTexture(7);
                tintTexture.Should().MatchRegex("^tm_[0-9a-f]{13}$",
                    $"{name} must bind a collision-proof internal tint resource");
                var tintImage = TextureLoader.Load(resources, tintTexture);
                tintImage.Should().NotBeNull($"installed HAK resources must resolve {name}'s tint mask");
                var expectedWidth = int.Parse(
                    material.Parameters["tintMapWidth"].Split('.')[0],
                    System.Globalization.CultureInfo.InvariantCulture);
                var expectedHeight = int.Parse(
                    material.Parameters["tintMapHeight"].Split('.')[0],
                    System.Globalization.CultureInfo.InvariantCulture);
                tintImage!.Width.Should().Be(expectedWidth,
                    $"{name} must not resolve a same-name legacy DDS with different dimensions");
                tintImage.Height.Should().Be(expectedHeight);
            }
            foreach (var surface in new[]
                     {
                         "pmh0_legl087", "pmh0_legr087", "pmh0_shinl085", "pmh0_shinr085"
                     })
            {
                model.Meshes.Should().Contain(mesh =>
                    mesh.TextureName.Equals(surface, StringComparison.OrdinalIgnoreCase) &&
                    mesh.MaterialName.Equals(surface, StringComparison.OrdinalIgnoreCase));
                textures.Get(surface, model.LayerColorIndices, resolveMaterial: true)
                    .Should().NotBeNull($"installed HAK resources must decode {surface}");
            }

            var image = renderer.Render(ResourceType.Utc, "agr_guildmaster");
            image.Should().NotBeNull();
        }

        [Test]
        public void TintPaletteAtlasMatchesEveryLegacyPltPalette()
        {
            const int paletteWidth = 256;
            const int paletteColorCount = 176;
            const int atlasHeight = 2048;
            var atlas = TextureLoader.LoadTga(_resources, "plt_palette");

            atlas.Should().NotBeNull();
            atlas!.Width.Should().Be(paletteWidth);
            atlas.Height.Should().Be(atlasHeight);

            var palettes = new (string Resource, int AtlasBaseRow)[]
            {
                ("pal_skin01", 0),
                ("pal_hair01", 176),
                ("pal_armor01", 352),
                ("pal_armor02", 528),
                ("pal_cloth01", 704),
                ("pal_leath01", 880),
                ("pal_tattoo01", 1056),
            };

            foreach (var (resource, atlasBaseRow) in palettes)
            {
                var legacy = TextureLoader.LoadTga(_resources, resource);
                legacy.Should().NotBeNull($"{resource} is the palette used by legacy PLT rendering");
                legacy!.Width.Should().Be(paletteWidth);
                legacy.Height.Should().Be(paletteColorCount);

                for (var color = 0; color < paletteColorCount; color++)
                {
                    var legacyOffset = color * paletteWidth * 4;
                    var atlasOffset = (atlasHeight - 1 - atlasBaseRow - color) * paletteWidth * 4;
                    var legacyRow = legacy.Pixels.AsSpan(legacyOffset, paletteWidth * 4);
                    var atlasRow = atlas.Pixels.AsSpan(atlasOffset, paletteWidth * 4);
                    for (var channelOffset = 0; channelOffset < legacyRow.Length; channelOffset++)
                    {
                        if (legacyRow[channelOffset] == atlasRow[channelOffset])
                            continue;

                        Assert.Fail(
                            $"Atlas row {atlasBaseRow + color} does not reproduce {resource} color {color} " +
                            $"at byte {channelOffset}: expected {legacyRow[channelOffset]}, " +
                            $"found {atlasRow[channelOffset]}.");
                    }
                }
            }
        }

        private static JsonGffStruct WithoutEquippedSlot(JsonGffStruct source, int slot)
        {
            var clone = InstanceFieldMap.Duplicate(source);
            var equipment = clone.GetOrNull("Equip_ItemList")!;
            var index = equipment.Elements!.FindIndex(item =>
                System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == slot.ToString());
            index.Should().BeGreaterThanOrEqualTo(0);
            equipment.Elements.RemoveAt(index);
            return clone;
        }
    }
}
