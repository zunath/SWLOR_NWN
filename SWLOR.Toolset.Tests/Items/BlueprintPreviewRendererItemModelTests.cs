using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Plt;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// End-to-end coverage that <see cref="BlueprintPreviewRenderer.BuildModel(ResourceType, Domain.Gff.JsonGffStruct, bool)"/>
    /// actually produces geometry for a real corpus item now that <see cref="BlueprintModelResolver"/>
    /// has a <see cref="ResourceType.Uti"/> case - before this it always returned null via the
    /// resolver's default arm for every base item, composite or not.
    /// </summary>
    [NonParallelizable]
    public class BlueprintPreviewRendererItemModelTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static BlueprintPreviewRenderer BuildRenderer()
        {
            var twoDa = new TwoDaService(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_2da"));
            var tlk = TlkService.Load(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json"));
            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"));
            index.EnsureInitialized();

            // BuildModel(type, root) never touches the workspace for a Uti - only creature armor
            // resolution does - so the context is never opened.
            var context = new WorkspaceContext(path => new ModuleWorkspace(path, index), new OutputLogService());

            return new BlueprintPreviewRenderer(
                context, index, baseItems: new BaseItemIconService(twoDa), twoDa: twoDa, tlk: tlk);
        }

        [Test]
        public void CompositeLightsaber_BuildModel_ProducesMergedGeometryFromItsThreeParts()
        {
            var root = CorpusItem("bobsaber");
            var renderer = BuildRenderer();

            var model = renderer.BuildModel(ResourceType.Uti, root);

            model.Should().NotBeNull("bobsaber's composite parts (wswglsbr_b_032/_m_011/_t_014) exist in sw_weapon");
            model!.Meshes.Should().NotBeEmpty();
        }

        [Test]
        public void ArmorCarriesItsDyeChoicesOnTheModelSoTheViewportCanColourThem()
        {
            // A PLT is not a picture until its layers are coloured. The 2D icon passed the dye
            // indices straight to the texture cache, but the 3D viewport only ever sees the model -
            // so with nothing on the model, every dyed surface drew at the palette's default row and
            // changing a dye channel did nothing at all in the viewport.
            var root = CorpusItem("adren_harness");
            var renderer = BuildRenderer();

            var store = new ItemValueStore(root);
            store.SetInteger(BehaviorFieldStorage.Field, "Cloth1Color", Domain.Gff.GffFieldType.Byte, 3);
            store.SetInteger(BehaviorFieldStorage.Field, "Metal1Color", Domain.Gff.GffFieldType.Byte, 7);
            var model = renderer.BuildModel(ResourceType.Uti, root);

            model.Should().NotBeNull();
            model!.LayerColorIndices.Should().NotBeEmpty("the mannequin's dyed layers need their palette rows");
            model.LayerColorIndices[PltLayers.Cloth1].Should().Be(3);
            model.LayerColorIndices[PltLayers.Metal1].Should().Be(7);

            store.SetInteger(BehaviorFieldStorage.Field, "Cloth1Color", Domain.Gff.GffFieldType.Byte, 11);
            renderer.BuildModel(ResourceType.Uti, root)!
                .LayerColorIndices[PltLayers.Cloth1].Should().Be(11, "a dye edit reaches the model");
        }


        [Test]
        public void AWornPartsSkinLayerIsNotLeftOnThePalettesWhiteRow()
        {
            // Every palette runs light to dark, so row 0 is white - pal_skin01's is #FFFEFE. An item
            // names no skin colour because it has no wearer, and a glove is roughly a third skin
            // pixels by area (pfh0_handl002: Skin 34%, Tattoo1 7%), so leaving those layers at 0
            // rendered white patches on the hands that read as a missing texture.
            var root = CorpusItem("adren_harness");
            var renderer = BuildRenderer();

            var model = renderer.BuildModel(ResourceType.Uti, root);

            model.Should().NotBeNull();
            model!.LayerColorIndices[PltLayers.Skin].Should().NotBe(0, "row 0 is white");
            model.LayerColorIndices[PltLayers.Tattoo1].Should().NotBe(0);
            model.LayerColorIndices[PltLayers.Tattoo2].Should().NotBe(0);
        }

        private static Domain.Gff.JsonGffStruct CorpusItem(string resRef) =>
            new ModuleWorkspace(CorpusLocator.ModuleDirectory).LoadBlueprint(ResourceType.Uti, resRef).Document.Root;
    }
}
