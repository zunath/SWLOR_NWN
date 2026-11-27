using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The appearance editor's colour swatches (20 armor targets by 6 dye channels) are bound by name. The fixture holds what the
/// editor declared and published before its 480 per-channel properties were replaced by ArmorColorRegionCatalog: each
/// swatch, its four binding names and its first region, in publishing order, then every declared property name.
/// </summary>
public class ArmorColorRegionCatalogTests
{
    private static string[] Fixture => File.ReadAllLines(Path.Combine(
        FindRepositoryRoot().FullName, "SWLOR.Game.Server.Tests", "Feature", "Fixtures", "ArmorColorRegionMapping.txt"));

    [Test]
    public void TheCatalogListsEverySwatchAsTheEditorPublishedItBefore()
    {
        var expected = Fixture.Skip(1).TakeWhile(line => !line.StartsWith('#')).ToArray();
        var actual = ArmorColorRegionCatalog.Entries.Select(swatch =>
        {
            var region = ArmorColorRegionCatalog.NeutralRegion();
            return $"{swatch.Target} {swatch.Channel} {swatch.RegionName} {swatch.SelectedName} {swatch.TintName} {swatch.CustomName} " +
                   $"{region.X},{region.Y},{region.Width},{region.Height}";
        }).ToArray();

        actual.Should().Equal(expected);
        expected.Should().HaveCount(120);
    }

    [Test]
    public void TheCatalogNamesAreTheNamesTheEditorDeclaredBefore()
    {
        var declared = Fixture.SkipWhile(line => !line.StartsWith("# declared properties")).Skip(1).ToArray();
        var names = ArmorColorRegionCatalog.Entries
            .SelectMany(swatch => new[] { swatch.RegionName, swatch.SelectedName, swatch.TintName, swatch.CustomName })
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        names.Should().Equal(declared);
        declared.Should().HaveCount(480);
    }

    [Test]
    public void TheEditorDeclaresNoneOfThePerChannelPropertiesAnyMore()
    {
        var declared = typeof(AppearanceEditorViewModel).GetProperties().Select(property => property.Name).ToHashSet();

        ArmorColorRegionCatalog.Entries
            .SelectMany(swatch => new[] { swatch.RegionName, swatch.SelectedName, swatch.TintName, swatch.CustomName })
            .Where(declared.Contains)
            .Should().BeEmpty();
    }

    [Test]
    public void SelectionNamesFollowRegionNames()
    {
        ArmorColorRegionCatalog.GetSelectionName("ChestCloth1Region").Should().Be("ChestCloth1Selected");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
