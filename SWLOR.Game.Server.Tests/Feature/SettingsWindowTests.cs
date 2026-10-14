using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Reflection;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

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
        definitionSource.Should().Contain(".DefinePartialView(SettingsViewModel.TargetingPartial");
        definitionSource.Should().Contain("window.AddStandardLayout(layout =>");
        definitionSource.Should().Contain("layout.SetContentPartialElement(SettingsViewModel.SettingsView);");
        definitionSource.Should().Contain(".BindSelectedValue(model => model.TabToggleValue)");
        definitionSource.Should().NotContain(".BindIsVisible(model => model.IsGeneralSelected)");
        definitionSource.Should().NotContain(".BindIsVisible(model => model.IsIdentitySelected)");
        definitionSource.Should().NotContain(".BindIsVisible(model => model.IsChatSelected)");

        var selectTab = ExtractMethod(
            viewModelSource,
            "private void SelectTab",
            "private void RefreshPartialViewBindings");
        var geometryCapture = selectTab.IndexOf("UpdatePropertyFromClient(nameof(Geometry));", StringComparison.Ordinal);
        var partialSwap = selectTab.IndexOf("Tabs.Select(this, SettingsView, tabId, onAfterApply: RefreshPartialViewBindings);", StringComparison.Ordinal);

        geometryCapture.Should().BeGreaterThanOrEqualTo(0);
        partialSwap.Should().BeGreaterThan(geometryCapture);
        viewModelSource.Should().Contain("new GuiTabGroup<SettingsViewModel, GuiPayloadBase>()");
        viewModelSource.Should().Contain(".AddTab(0, GeneralPartial)");
        viewModelSource.Should().Contain(".AddTab(1, IdentityPartial)");
        viewModelSource.Should().Contain(".AddTab(2, ChatPartial)");
        viewModelSource.Should().Contain(".AddTab(3, TargetingPartial)");
        viewModelSource.Should().Contain("_tabToggles.HandleClientChange(value, SelectTab);");
        selectTab.Should().Contain("_tabToggles.SyncTo(tabId, value => TabToggleValue = value);");
    }

    [Test]
    public void TabChanges_DoNotReloadUnsavedSettings()
    {
        var (_, viewModelSource) = LoadSettingsSources();
        var initializeMethod = ExtractMethod(viewModelSource, "protected override void Initialize", "private void LoadTargetingView");
        var selectTab = ExtractMethod(viewModelSource, "private void SelectTab", "private void RefreshPartialViewBindings");

        initializeMethod.Should().Contain("LoadGeneralView();");
        initializeMethod.Should().Contain("LoadIdentityView();");
        initializeMethod.Should().Contain("LoadChatView();");
        initializeMethod.Should().Contain("LoadTargetingView();");
        selectTab.Should().NotContain("LoadGeneralView();");
        selectTab.Should().NotContain("LoadIdentityView();");
        selectTab.Should().NotContain("LoadChatView();");
        selectTab.Should().NotContain("LoadTargetingView();");
    }

    [Test]
    public void PartialChanges_RepublishExistingBindingsAfterInsertion()
    {
        var (_, viewModelSource) = LoadSettingsSources();
        var selectTab = ExtractMethod(
            viewModelSource,
            "private void SelectTab",
            "private void RefreshPartialViewBindings");
        var refresh = ExtractMethod(viewModelSource, "private void RefreshPartialViewBindings", "protected override void OnModalClosedRestore");
        var restoreMainView = ExtractMethod(
            viewModelSource,
            "protected override void OnModalClosedRestore",
            "private void LoadColor");

        selectTab.Should().Contain("Tabs.Select(this, SettingsView, tabId, onAfterApply: RefreshPartialViewBindings);");
        refresh.Should().Contain("OnPropertyChanged(nameof(ShowOwnDescriptor));");
        refresh.Should().Contain("OnPropertyChanged(nameof(ShowDescriptorsForNamedPlayers));");
        refresh.Should().Contain("OnPropertyChanged(nameof(ScrambleAccountName));");
        refresh.Should().Contain("ChatColorNames?.ResetBindings();");
        refresh.Should().Contain("ChatColors?.ResetBindings();");
        refresh.Should().Contain("ChatColorToggles?.ResetBindings();");
        refresh.Should().NotContain("LoadChatView();");
        refresh.Should().NotContain("LoadTargetingView();");
        restoreMainView.Should().Contain("SelectTab(SelectedTabId);");
        restoreMainView.Should().NotContain("ChangePartialView(SettingsView");
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

    [Test]
    public void Targeting_OffersOnlyTwoGlobalPreferencesWithShortTooltips()
    {
        var (definitionSource, viewModelSource) = LoadSettingsSources();
        var targetingPartial = ExtractSection(definitionSource,
            ".DefinePartialView(SettingsViewModel.TargetingPartial", "window.AddStandardLayout");
        definitionSource.Should().Contain(".AddOption(\"Targeting\")");
        targetingPartial.Should().Contain(".BindIsChecked(model => model.SelfQuickCastEnabled)");
        targetingPartial.Should().Contain(".BindIsChecked(model => model.EnemyQuickCastEnabled)");
        targetingPartial.Should().Contain("Automatically targets you for all eligible support abilities. Off by default.");
        targetingPartial.Should().Contain("Uses the enemy you are attacking for all eligible hostile casts. Off by default.");
        targetingPartial.Should().NotContain(".AddText(");
        targetingPartial.Should().NotContain(".AddTextEdit(");
        targetingPartial.Should().NotContain(".AddComboBox(");
        targetingPartial.Should().NotContain(".AddButton(");
        targetingPartial.Split("row.AddCheckBox()", StringSplitOptions.None).Should().HaveCount(3);

        var initialize = ExtractMethod(viewModelSource, "protected override void Initialize", "private void LoadTargetingView");
        foreach (var property in new[] { "SelfQuickCastEnabled", "EnemyQuickCastEnabled" })
            initialize.IndexOf($"WatchOnClient(model => model.{property});", StringComparison.Ordinal)
                .Should().BeGreaterThan(initialize.IndexOf("LoadTargetingView();", StringComparison.Ordinal));
    }

    [Test]
    public void Targeting_NewAndLegacySettingsDefaultOff()
    {
        var defaults = new PlayerSettings();
        defaults.SelfQuickCastEnabled.Should().BeFalse();
        defaults.EnemyQuickCastEnabled.Should().BeFalse();

        var legacy = JsonConvert.DeserializeObject<PlayerSettings>("{\"DisplayAchievementNotification\":true}");
        legacy.Should().NotBeNull();
        legacy!.SelfQuickCastEnabled.Should().BeFalse();
        legacy.EnemyQuickCastEnabled.Should().BeFalse();
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void Targeting_PreferencesPersistIndependentlyAcrossSerializationCycles(bool self, bool enemy)
    {
        var settings = new PlayerSettings { SelfQuickCastEnabled = self, EnemyQuickCastEnabled = enemy };
        for (var cycle = 0; cycle < 3; cycle++)
        {
            settings = JsonConvert.DeserializeObject<PlayerSettings>(JsonConvert.SerializeObject(settings))!;
            settings.Should().NotBeNull();
            settings.SelfQuickCastEnabled.Should().Be(self);
            settings.EnemyQuickCastEnabled.Should().Be(enemy);
        }
    }

    [Test]
    public void Targeting_LoadsSavedPreferences_AndSaveAppliesThemAfterPersistence()
    {
        var (_, source) = LoadSettingsSources();
        var load = ExtractMethod(source, "private void LoadTargetingView", "private void LoadGeneralView");
        load.Should().Contain("SelfQuickCastEnabled = dbPlayer.Settings.SelfQuickCastEnabled;");
        load.Should().Contain("EnemyQuickCastEnabled = dbPlayer.Settings.EnemyQuickCastEnabled;");
        load.Should().NotContain("QuickCast.");

        var save = ExtractMethod(source, "public Action OnSave()", "public Action OnCancel()");
        var persist = save.IndexOf("DB.Set(dbPlayer);", StringComparison.Ordinal);
        foreach (var property in new[] { "SelfQuickCastEnabled", "EnemyQuickCastEnabled" })
        {
            var assignment = save.IndexOf($"dbPlayer.Settings.{property} = {property};", StringComparison.Ordinal);
            assignment.Should().BeGreaterThanOrEqualTo(0);
            persist.Should().BeGreaterThan(assignment);
        }
        save.IndexOf("QuickCast.ApplyPreferences(Player);", StringComparison.Ordinal).Should().BeGreaterThan(persist);

        var cancel = ExtractMethod(source, "public Action OnCancel()", "public Action OnClickChangeDescription()");
        cancel.Should().Contain("Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);");
        cancel.Should().NotContain("QuickCast.");
        cancel.Should().NotContain("DB.Set");
    }

    [Test]
    public void TargetingPartial_RepublishesCheckboxesWithoutReloadingDrafts()
    {
        var model = new SettingsViewModel { SelfQuickCastEnabled = true, EnemyQuickCastEnabled = false };
        var published = new List<string>();
        model.PropertyChanged += (_, args) => published.Add(args.PropertyName!);

        typeof(SettingsViewModel).GetMethod("RefreshPartialViewBindings", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(model, null);

        published.Should().Contain(new[]
        {
            nameof(SettingsViewModel.SelfQuickCastEnabled), nameof(SettingsViewModel.EnemyQuickCastEnabled)
        });
        model.SelfQuickCastEnabled.Should().BeTrue();
        model.EnemyQuickCastEnabled.Should().BeFalse();
    }

    [Test]
    public void CommsRangeWarnings_AreEnabledByDefaultAndPersistedFromGeneralSettings()
    {
        var root = FindRepositoryRoot();
        var playerSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Entity",
            "Player.cs"));
        var (definitionSource, viewModelSource) = LoadSettingsSources();
        var generalPartial = ExtractSection(
            definitionSource,
            ".DefinePartialView(SettingsViewModel.GeneralPartial",
            ".DefinePartialView(SettingsViewModel.IdentityPartial");
        var chatPartial = ExtractSection(
            definitionSource,
            ".DefinePartialView(SettingsViewModel.ChatPartial",
            ".DefinePartialView(SettingsViewModel.TargetingPartial");

        playerSource.Should().Contain("public bool? DisplayCommsOutOfRangeWarnings { get; set; }");
        playerSource.Should().Contain("DisplayCommsOutOfRangeWarnings = true;");
        generalPartial.Should().Contain(".SetText(\"Comms Range Warnings\")");
        generalPartial.Should().Contain(".BindIsChecked(model => model.DisplayCommsOutOfRangeWarnings)");
        chatPartial.Should().NotContain("DisplayCommsOutOfRangeWarnings");
        viewModelSource.Should().Contain("WatchOnClient(model => model.DisplayCommsOutOfRangeWarnings);");
        var loadGeneralView = ExtractMethod(viewModelSource, "private void LoadGeneralView", "private void LoadIdentityView");
        var loadChatView = ExtractMethod(viewModelSource, "private void LoadChatView", "private void SelectTab");
        loadGeneralView.Should().Contain(
            "DisplayCommsOutOfRangeWarnings = dbPlayer.Settings.DisplayCommsOutOfRangeWarnings ?? true;");
        loadChatView.Should().NotContain("DisplayCommsOutOfRangeWarnings");
        viewModelSource.Should().Contain(
            "dbPlayer.Settings.DisplayCommsOutOfRangeWarnings = DisplayCommsOutOfRangeWarnings;");
    }

    [Test]
    public void CooldownFloatingText_DefaultsOffForNewAndLegacySettings_AndPersistsWhenEnabled()
    {
        var defaults = new PlayerSettings();
        defaults.DisplayCooldownFloatingText.Should().BeFalse();

        var legacy = JsonConvert.DeserializeObject<PlayerSettings>("{\"DisplayAchievementNotification\":true}");
        legacy.Should().NotBeNull();
        legacy!.DisplayCooldownFloatingText.Should().BeFalse();

        var enabled = JsonConvert.DeserializeObject<PlayerSettings>(
            JsonConvert.SerializeObject(new PlayerSettings { DisplayCooldownFloatingText = true }));
        enabled.Should().NotBeNull();
        enabled!.DisplayCooldownFloatingText.Should().BeTrue();

        var root = FindRepositoryRoot();
        var playerSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Entity", "Player.cs"));
        var (definitionSource, viewModelSource) = LoadSettingsSources();
        var generalPartial = ExtractSection(
            definitionSource,
            ".DefinePartialView(SettingsViewModel.GeneralPartial",
            ".DefinePartialView(SettingsViewModel.IdentityPartial");
        var initializeMethod = ExtractMethod(viewModelSource, "protected override void Initialize", "private void LoadTargetingView");
        var loadGeneralView = ExtractMethod(viewModelSource, "private void LoadGeneralView", "private void LoadIdentityView");
        var saveMethod = ExtractMethod(viewModelSource, "public Action OnSave()", "public Action OnCancel()");

        playerSource.Should().Contain("public bool DisplayCooldownFloatingText { get; set; }");
        generalPartial.Should().Contain(".SetText(\"Cooldown Floating Text\")");
        generalPartial.Should().Contain("at most once every two seconds. Off by default.");
        generalPartial.Should().Contain(".BindIsChecked(model => model.DisplayCooldownFloatingText)");
        initializeMethod.Should().Contain("LoadGeneralView();");
        var initialLoadIndex = initializeMethod.IndexOf("LoadGeneralView();", StringComparison.Ordinal);
        var watchIndex = initializeMethod.IndexOf(
            "WatchOnClient(model => model.DisplayCooldownFloatingText);",
            StringComparison.Ordinal);
        initialLoadIndex.Should().BeGreaterThanOrEqualTo(0);
        watchIndex.Should().BeGreaterThan(initialLoadIndex);
        loadGeneralView.Should().Contain("DisplayCooldownFloatingText = dbPlayer.Settings.DisplayCooldownFloatingText;");
        saveMethod.Should().Contain("dbPlayer.Settings.DisplayCooldownFloatingText = DisplayCooldownFloatingText;");
        viewModelSource.Should().Contain("OnPropertyChanged(nameof(DisplayCooldownFloatingText));");
    }

    private static string ExtractSection(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);

        start.Should().BeGreaterThanOrEqualTo(0);
        end.Should().BeGreaterThan(start);
        return source[start..end];
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
