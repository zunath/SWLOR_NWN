using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class SettingsWindowTests
{
    [Test]
    public void Tabs_UseSeparatePartialViewsAndPreserveWindowGeometry()
    {
        var (definitionSource, viewModelSource) = LoadSettingsSources();

        definitionSource.Should().Contain(".DefinePartialView(SettingsViewModel.GeneralPartial");
        definitionSource.Should().Contain(".DefinePartialView(SettingsViewModel.IdentityPartial");
        definitionSource.Should().Contain(".DefinePartialView(SettingsViewModel.ChatPartial");
        definitionSource.Should().NotContain(".BindIsVisible(model => model.IsGeneralSelected)");
        definitionSource.Should().NotContain(".BindIsVisible(model => model.IsIdentitySelected)");
        definitionSource.Should().NotContain(".BindIsVisible(model => model.IsChatSelected)");

        var changeSettingsView = ExtractMethod(
            viewModelSource,
            "private void ChangeSettingsView",
            "private string GetSelectedPartial");
        var geometryCapture = changeSettingsView.IndexOf("UpdatePropertyFromClient(nameof(Geometry));", StringComparison.Ordinal);
        var partialSwap = changeSettingsView.IndexOf("ChangePartialView(SettingsView, partialName);", StringComparison.Ordinal);

        geometryCapture.Should().BeGreaterThanOrEqualTo(0);
        partialSwap.Should().BeGreaterThan(geometryCapture);
        viewModelSource.Should().Contain("ChangeSettingsView(GeneralPartial);");
        viewModelSource.Should().Contain("ChangeSettingsView(IdentityPartial);");
        viewModelSource.Should().Contain("ChangeSettingsView(ChatPartial);");
    }

    [Test]
    public void TabChanges_DoNotReloadUnsavedSettings()
    {
        var (_, viewModelSource) = LoadSettingsSources();
        var initializeMethod = ExtractMethod(viewModelSource, "protected override void Initialize", "private void LoadGeneralView");
        var tabMethods = ExtractMethod(viewModelSource, "public Action OnClickGeneral", "public Action OnClickSelectChat");

        initializeMethod.Should().Contain("LoadGeneralView();");
        initializeMethod.Should().Contain("LoadIdentityView();");
        initializeMethod.Should().Contain("LoadChatView();");
        tabMethods.Should().NotContain("LoadGeneralView();");
        tabMethods.Should().NotContain("LoadIdentityView();");
        tabMethods.Should().NotContain("LoadChatView();");
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
        saveMethod.Should().Contain("Log.Write(LogGroup.Server, $\"Settings saved for player {playerId}.\");");
        saveMethod.Should().NotContain("Gui.TogglePlayerWindow");
        definitionSource.Should().Contain(".SetIsClosable(true)");
    }

    private static string ExtractMethod(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);

        start.Should().BeGreaterThanOrEqualTo(0);
        end.Should().BeGreaterThan(start);
        return source[start..end];
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
