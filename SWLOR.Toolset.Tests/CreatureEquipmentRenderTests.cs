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
