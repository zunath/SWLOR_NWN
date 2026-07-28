using FluentAssertions;
using NUnit.Framework;
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

        private static Domain.Gff.JsonGffStruct CorpusItem(string resRef) =>
            new ModuleWorkspace(CorpusLocator.ModuleDirectory).LoadBlueprint(ResourceType.Uti, resRef).Document.Root;
    }
}
