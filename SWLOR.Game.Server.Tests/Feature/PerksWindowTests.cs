using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PerksWindowTests
{
    [Test]
    public void SortOptions_AreEmbeddedInThePerksWindowLayout()
    {
        var root = FindRepositoryRoot();
        var definitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "PerksDefinition.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));

        definitionSource.Should().Contain(".AddOption(\"Alphabetical (A-Z)\", 0)");
        definitionSource.Should().Contain(".AddOption(\"Alphabetical (Z-A)\", 1)");
        definitionSource.Should().Contain(".AddOption(\"Skill Level (Asc)\", 2)");
        definitionSource.Should().Contain(".AddOption(\"Skill Level (Desc)\", 3)");
        definitionSource.Should().NotContain("model => model.SortOptions");
        viewModelSource.Should().NotContain("public GuiBindingList<GuiComboEntry> SortOptions");
        viewModelSource.Should().NotContain("SortAlphabeticalAscending");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
