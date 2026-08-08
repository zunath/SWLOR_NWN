using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class CreatureAppearancePreviewTests
    {
        [Test]
        public void NeutralSkinPaletteIsOnlyAppliedToAffectedBaseDynamicRaces()
        {
            foreach (var appearanceId in new[] { 0, 1, 5 })
                BlueprintPreviewRenderer.UsesNeutralSkinPalette(appearanceId).Should().BeTrue();

            foreach (var appearanceId in new[] { 2, 3, 4, 6 })
                BlueprintPreviewRenderer.UsesNeutralSkinPalette(appearanceId).Should().BeFalse();
        }

        [Test]
        public void EveryDynamicAppearanceRendersARepresentativeCreatureForItsRace()
        {
            var repositoryRoot = CorpusLocator.RepositoryRoot;
            var haksRoot = Path.Combine(repositoryRoot, "SWLOR_Haks");
            if (!Directory.Exists(Path.Combine(haksRoot, "sw_2da")))
            {
                haksRoot = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT") ?? haksRoot;
            }

            if (!Directory.Exists(Path.Combine(haksRoot, "sw_2da")))
            {
                Assert.Ignore(
                    "The SWLOR_Haks submodule is not initialized and SWLOR_TEST_HAKS_ROOT was not supplied.");
            }

            var twoDa = new TwoDaService(Path.Combine(haksRoot, "sw_2da"));
            var tlk = TlkService.Load(Path.Combine(haksRoot, "sw_tlk", "sw_tlk.tlk.json"));
            var appearances = new AppearanceService(twoDa, tlk);
            var installRoot = NwnInstallLocator.Locate();
            if (installRoot == null)
            {
                Assert.Ignore("No local NWN:EE installation was found for the base segmented body models.");
            }

            var baseLayer = KeyBifCatalog.Load(Path.Combine(installRoot, "data"));
            var resources = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(repositoryRoot, "Build", "hakbuilder.json"),
                haksRoot,
                baseLayer);
            resources.EnsureInitialized();

            var context = new WorkspaceContext(
                path => new ModuleWorkspace(path, resources),
                new OutputLogService());
            var renderer = new BlueprintPreviewRenderer(
                context,
                resources,
                appearances: appearances,
                twoDa: twoDa,
                tlk: tlk);

            var dynamicRows = appearances.GetAll()
                .Where(row =>
                    row.Label.StartsWith("(Dynamic)", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase))
                .ToList();
            dynamicRows.Should().HaveCountGreaterThan(7,
                "the regression must cover SWLOR's custom dynamic species as well as the seven base rows");

            foreach (var row in dynamicRows)
            {
                AssertVisiblePreview(
                    renderer.RenderCreatureAppearance(row.Id),
                    $"{row.DisplayName} should use the generic segmented body for race code {row.Race}");
            }

            foreach (var row in appearances.GetAll().Take(48))
            {
                AssertVisiblePreview(
                    renderer.RenderCreatureAppearance(row.Id),
                    $"the initial Appearance gallery page must show a model preview for {row.DisplayName}");
            }
        }

        private static void AssertVisiblePreview(IconImage? preview, string because)
        {
            preview.Should().NotBeNull(because);
            preview!.Bgra
                .Where((_, index) => index % 4 == 3)
                .Should().Contain(alpha => alpha != 0, because);
        }
    }
}
