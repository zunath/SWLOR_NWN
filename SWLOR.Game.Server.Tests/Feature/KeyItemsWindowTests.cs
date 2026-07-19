using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class KeyItemsWindowTests
{
    [Test]
    public void KeyItemsLayout_UsesDetailsPaneInsteadOfDescriptionTooltips()
    {
        var root = FindRepositoryRoot();
        var definition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "KeyItemsDefinition.cs"));
        var viewModel = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "KeyItemsViewModel.cs"));

        definition.Should().Contain(".BindOnClicked(model => model.OnSelectKeyItem())");
        definition.Should().Contain(".BindIsToggled(model => model.Selections)");
        definition.Should().Contain(".BindResref(model => model.SelectedIcon)");
        definition.Should().Contain(".BindText(model => model.SelectedName)");
        definition.Should().Contain(".BindText(model => model.SelectedType)");
        definition.Should().Contain(".BindText(model => model.SelectedDescription)");
        definition.Should().NotContain(".BindTooltip(model => model.Descriptions)");

        viewModel.Should().Contain("SelectKeyItem(selectedIndex);");
        viewModel.Should().Contain("Selections[_selectedIndex] = true;");
        viewModel.Should().Contain("SelectedIcon = KeyItemIcon.GetIconResref(type);");
        viewModel.Should().Contain("SelectedDescription = detail.Description;");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
