using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class SettingsWindowTests
{
    [Test]
    public void Tabs_UseVisibilityBindingsWithoutRedrawingTheWindow()
    {
        var (definitionSource, viewModelSource) = LoadSettingsSources();

        definitionSource.Should().Contain(".DefinePartialView(SettingsViewModel.ContentPartial");
        definitionSource.Should().Contain(".BindIsVisible(model => model.IsGeneralSelected)");
        definitionSource.Should().Contain(".BindIsVisible(model => model.IsIdentitySelected)");
        definitionSource.Should().Contain(".BindIsVisible(model => model.IsChatSelected)");

        viewModelSource.Should().Contain("ChangePartialView(SettingsView, ContentPartial);");
        viewModelSource
            .Split("ChangePartialView(SettingsView, ContentPartial);", StringSplitOptions.None)
            .Should().HaveCount(2, "the content layout should only be mounted when the window initializes");
        viewModelSource.Should().NotContain("ChangePartialView(SettingsView, GeneralPartial)");
        viewModelSource.Should().NotContain("ChangePartialView(SettingsView, IdentityPartial)");
        viewModelSource.Should().NotContain("ChangePartialView(SettingsView, ChatPartial)");
    }

    [Test]
    public void Save_PersistsWithoutClosingTheWindow()
    {
        var (definitionSource, viewModelSource) = LoadSettingsSources();
        var saveStart = viewModelSource.IndexOf("public Action OnSave()", StringComparison.Ordinal);
        var cancelStart = viewModelSource.IndexOf("public Action OnCancel()", StringComparison.Ordinal);

        saveStart.Should().BeGreaterThanOrEqualTo(0);
        cancelStart.Should().BeGreaterThan(saveStart);

        var saveMethod = viewModelSource[saveStart..cancelStart];
        saveMethod.Should().Contain("DB.Set(dbPlayer);");
        saveMethod.Should().NotContain("Gui.TogglePlayerWindow");
        definitionSource.Should().Contain(".SetIsClosable(true)");
    }

    private static (string DefinitionSource, string ViewModelSource) LoadSettingsSources()
    {
        var root = FindRepositoryRoot();
        var definitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "SettingsDefinition.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "SettingsViewModel.cs"));

        return (definitionSource, viewModelSource);
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
