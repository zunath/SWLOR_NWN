using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The Manage City window's tabs: the existing City page and the new Edicts page.</summary>
public class ManageCityEdictsTabTests
{
    [Test]
    public void ManageCity_BuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ManageCityDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.ManageCity);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void ManageCity_DefinesAPartialForEachTabAndTheStandardLayoutHostsThem()
    {
        var definition = File.ReadAllText(Path.Combine(RepositoryRoot, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ManageCityDefinition.cs"));

        definition.Should().Contain(".DefinePartialView(ManageCityViewModel.CityTabPartial, AddCityTab)");
        definition.Should().Contain(".DefinePartialView(ManageCityViewModel.EdictsTabPartial, AddEdictsTab)");
        definition.Should().Contain("window.AddStandardLayout(");
        definition.Should().Contain("layout.SetContentPartialElement(ManageCityViewModel.TabContentElement);");
    }

    [Test]
    public void ManageCity_EachTabHasItsOwnPartialName()
    {
        ManageCityViewModel.TabContentElement.Should().NotBeNullOrWhiteSpace();
        ManageCityViewModel.CityTabPartial.Should().NotBe(ManageCityViewModel.EdictsTabPartial);
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
