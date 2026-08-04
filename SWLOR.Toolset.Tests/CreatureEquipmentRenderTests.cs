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

        [Test]
        public void PlacedArmorAndHeldWeaponBecomeCreatureGeometry()
        {
            var installRoot = NwnInstallLocator.Locate();
            installRoot.Should().NotBeNull("the licensed rendering corpus requires NWN:EE");

            var baseLayer = KeyBifCatalog.Load(Path.Combine(installRoot!, "data"));
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
            var renderer = new BlueprintPreviewRenderer(
                context,
                index,
                appearances: new AppearanceService(twoDa, tlk),
                baseItems: new BaseItemIconService(twoDa),
                twoDa: twoDa,
                tlk: tlk);

            var workspace = context.Workspace!;
            var (_, hangarGit, _) = workspace.LoadArea("czs220_hangar");
            var dressed = renderer.BuildModel(ResourceType.Utc, hangarGit.Creatures.First());

            dressed.Should().NotBeNull();
            dressed!.LayerColorIndices[SWLOR.NWN.Formats.Plt.PltLayers.Cloth1].Should().Be(107);
            dressed.Meshes.Should().Contain(
                mesh => mesh.TextureName.Equals("pmh0_chest189", StringComparison.OrdinalIgnoreCase),
                "the embedded Czerka uniform's torso geometry must replace the naked chest");
            dressed.Meshes.Should().NotContain(
                mesh => mesh.TextureName.Equals("pmh0_chest001", StringComparison.OrdinalIgnoreCase),
                "the equipped torso must not coexist with the naked torso");

            var dockhandRoot = hangarGit.Creatures.First(creature =>
                creature.GetListOrEmpty("Equip_ItemList").Any(item =>
                    System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == "2" &&
                    item.GetIntOrNull("ArmorPart_Torso") == 102));
            var dockhand = renderer.BuildModel(ResourceType.Utc, dockhandRoot);
            dockhand.Should().NotBeNull();
            dockhand!.Meshes.Should().Contain(
                mesh => mesh.TextureName.Equals("pmh0_chest102", StringComparison.OrdinalIgnoreCase));
            dockhand.Meshes.Should().NotContain(
                mesh => mesh.TextureName.Equals("pmh0_chest001", StringComparison.OrdinalIgnoreCase));

            var (_, anchorGit, _) = workspace.LoadArea("anchor_entreenor");
            var armedRoot = anchorGit.Creatures.First(creature => creature.GetListOrEmpty("Equip_ItemList")
                .Any(item => System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == "16"));
            var armed = renderer.BuildModel(ResourceType.Utc, armedRoot);

            var equipment = armedRoot.GetOrNull("Equip_ItemList")!;
            var rightHandIndex = equipment.Elements!.FindIndex(
                item => System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == "16");
            equipment.Elements.RemoveAt(rightHandIndex);
            var unarmed = renderer.BuildModel(ResourceType.Utc, armedRoot);

            armed.Should().NotBeNull();
            unarmed.Should().NotBeNull();
            armed!.Meshes.Sum(mesh => mesh.VertexCount).Should().BeGreaterThan(
                unarmed!.Meshes.Sum(mesh => mesh.VertexCount),
                "the embedded rifle parts must add geometry to the right-hand skeleton bone");
        }
    }
}
