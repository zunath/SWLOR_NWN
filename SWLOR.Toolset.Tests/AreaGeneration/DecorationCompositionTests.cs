using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Definitions;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public class DecorationCompositionTests
{
    /// <summary>Exercises seeded compositions at multiple sizes and densities for deterministic, usable and non-overlapping dressing.</summary>
    [TestCase(StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Organic)]
    [TestCase(StandardTilesetProfiles.Facility, StandardLayoutProfiles.Halls)]
    [TestCase(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Packed)]
    [TestCase(StandardTilesetProfiles.Sewers, StandardLayoutProfiles.Warren)]
    [TestCase(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed)]
    [TestCase(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Complex)]
    [TestCase(BaseGameTilesetProfiles.FutCityPlaza, StandardLayoutProfiles.Packed)]
    public void Compositions_KeepDeterministicUsefulDressingWithClearance(string profileKey, string layoutKey)
    {
        var catalog = new DefinitionCatalog();
        var profile = catalog.TilesetProfiles[profileKey];
        var path = Directory.EnumerateFiles(Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks"),
            profile.TilesetResref + ".set", SearchOption.AllDirectories).First();
        var tileset = TilesetSetParser.FromDefinition(profile.TilesetResref, SetFileParser.ParseFile(path));
        var composition = new DungeonComposition
        {
            Content = catalog.Themes.Single(theme => theme.ThemeKey == MineCaveDungeonDefinition.ThemeKey),
            Tileset = profile, Layout = catalog.LayoutProfiles[layoutKey]
        };
        foreach (var size in new[] { 16, 24 })
        foreach (var seed in new[] { 4242, 77231 })
        {
            var result = GenerationEngine.Generate(composition, tileset, size, size, seed, null);
            result.Success.Should().BeTrue($"{profileKey}/{layoutKey}/{size}/{seed}: {result.FailureReason}");
            result.PlannedDecorations.Should().NotBeEmpty($"{profileKey}/{layoutKey}/{size}/{seed}");
            var draft = new AreaGenerationDraft(new()
            {
                ThemeKey = composition.Content.ThemeKey, Seed = seed, Width = size, Height = size
            }, composition, tileset, result);
            AreaGenerationAuthoringService.ValidatePlaceableBlueprints(draft, new ModuleWorkspace(CorpusLocator.ModuleDirectory));
            GeneratedAreaDocumentPopulator.ValidateEncounterPlacement(draft, new ModuleWorkspace(CorpusLocator.ModuleDirectory));
            var report = result.DecorationPlacementReport;
            report.ProposedCount.Should().Be(report.PlacedCount + report.UnsupportedCount + report.RouteConflictCount + report.OverlapCount);

            var repeated = GenerationEngine.Generate(composition, tileset, size, size, seed, null);
            repeated.PlannedDecorations.Select(prop => (prop.Resref, prop.Position, prop.Facing, prop.FootprintRadius))
                .Should().Equal(result.PlannedDecorations.Select(prop => (prop.Resref, prop.Position, prop.Facing, prop.FootprintRadius)));
            var blocking = result.PlannedDecorations.Where(prop => prop.BlocksMovement && prop.SupportDecoration == null &&
                prop.Context != DecorationContext.BuildingFrontage).ToList();
            for (var i = 0; i < blocking.Count; i++)
            for (var j = i + 1; j < blocking.Count; j++)
            {
                var first = blocking[i];
                var second = blocking[j];
                var distance = MathF.Sqrt(MathF.Pow(first.Position.X - second.Position.X, 2) + MathF.Pow(first.Position.Y - second.Position.Y, 2));
                distance.Should().BeGreaterThanOrEqualTo(first.FootprintRadius * first.VisualScale + second.FootprintRadius * second.VisualScale + 0.399f);
            }
            foreach (var prop in result.PlannedDecorations.Where(prop => prop.SupportDecoration != null))
                result.PlannedDecorations.Should().Contain(prop.SupportDecoration);
            var buildings = result.PlannedDecorations.Where(prop => prop.FootprintBounds.HasValue).ToList();
            for (var i = 0; i < buildings.Count; i++)
            for (var j = i + 1; j < buildings.Count; j++)
                buildings[i].FootprintBounds!.Value.Overlaps(buildings[j].FootprintBounds!.Value).Should().BeFalse(
                    $"building {buildings[i].Resref} must not intersect {buildings[j].Resref}");

            var compact = DungeonDecorationPlanner.Plan(result.Resolved, profile, composition.Content, 100);
            var compactReport = DecorationPlacementSafety.Apply(compact, result.Resolved, profile, composition.Content, DecorationPlacementStyle.Compact, tileset);
            compact.Should().NotBeEmpty();
            var dense = DungeonDecorationPlanner.Plan(result.Resolved, profile, composition.Content, 200);
            DecorationPlacementSafety.Apply(dense, result.Resolved, profile, composition.Content, DecorationPlacementStyle.Compact, tileset);
            dense.Should().NotBeEmpty("maximum density must still produce a valid plan");
            TestContext.Out.WriteLine($"{profileKey}/{layoutKey} {size} seed {seed}: {report.PlacedCount}/{report.ProposedCount} spacious, {compactReport.PlacedCount} compact; " +
                $"omitted {report.UnsupportedCount} support, {report.RouteConflictCount} routes, {report.OverlapCount} overlap; 200% density keeps {dense.Count}.");
            var artifactDirectory = Environment.GetEnvironmentVariable("SWLOR_AREA_REVIEW_OUTPUT");
            if (!string.IsNullOrEmpty(artifactDirectory) && size == 16 && seed == 4242)
            {
                Directory.CreateDirectory(artifactDirectory);
                var preview = new AreaGenerationPreviewRenderer(null).Render(draft, AreaPreviewMode.Schematic, true, pixelsPerTile: 48, showRoutes: true);
                File.WriteAllBytes(Path.Combine(artifactDirectory, $"{profileKey}_{layoutKey}_{preview.Width}x{preview.Height}.rgba"), preview.Pixels);
            }
        }
    }
}
