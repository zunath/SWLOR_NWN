using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The Manage City City tab's street life switches (Player City Revamp, "Ambient townsfolk").</summary>
public class ManageCityStreetLifeTests
{
    [Test]
    public void ManageCity_WithTheStreetLifeSwitches_BuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ManageCityDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.ManageCity);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheCityTab_HasABindingForEachSwitch()
    {
        var model = typeof(ManageCityViewModel);

        foreach (var name in new[]
                 {
                     nameof(ManageCityViewModel.TownsfolkSwitchText), nameof(ManageCityViewModel.CrittersSwitchText),
                     nameof(ManageCityViewModel.TownsfolkSwitchTooltip), nameof(ManageCityViewModel.CrittersSwitchTooltip),
                     nameof(ManageCityViewModel.CanSwitchStreetLife), nameof(ManageCityViewModel.ShowCrittersSwitch),
                 })
        {
            model.GetProperty(name).Should().NotBeNull($"{name} is bound by the City tab");
        }

        model.GetMethod(nameof(ManageCityViewModel.ToggleTownsfolk)).Should().NotBeNull();
        model.GetMethod(nameof(ManageCityViewModel.ToggleCritters)).Should().NotBeNull();
    }

    [Test]
    public void TheSwitchesSection_IsInItsOwnPartialSoTheCityTabEditStaysSmall()
    {
        var folder = Path.Combine(RepositoryRoot, "SWLOR.Game.Server", "Feature", "GuiDefinition");
        var definition = File.ReadAllText(Path.Combine(folder, "ManageCityDefinition.cs"));
        var section = File.ReadAllText(Path.Combine(folder, "ManageCityDefinition.StreetLife.cs"));

        definition.Should().Contain("AddStreetLifeSection(col);");
        section.Should().Contain("model.ToggleTownsfolk()").And.Contain("model.ToggleCritters()");
        section.Should().Contain(".BindIsEnabled(model => model.CanSwitchStreetLife)");
        section.Should().Contain("row.BindIsVisible(model => model.ShowCrittersSwitch)", "a station has no critters");
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
