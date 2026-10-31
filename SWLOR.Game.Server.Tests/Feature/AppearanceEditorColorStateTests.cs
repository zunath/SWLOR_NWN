using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class AppearanceEditorColorStateTests
{
    [TestCase(255, false, 0, 0, null, 255)]
    [TestCase(0, false, 0, 0, null, 255)]
    [TestCase(0, true, 0, 0, null, 0)]
    [TestCase(74, false, 0, 0, null, 74)]
    [TestCase(74, true, 0, 0, null, 74)]
    [TestCase(77, false, 256, 78, null, 255)]
    [TestCase(77, true, 1, 78, null, 0)]
    [TestCase(89, false, 256, 78, null, 89)]
    [TestCase(77, false, 256, 78, 91, 91)]
    public void ArmorSwatchesDistinguishInheritanceFromAuthoredAndCustomPartColors(
        int nativeColor, bool explicitPreset, int baseline, int lastApplied, int? materialColor, int expected)
    {
        var method = typeof(AppearanceEditorViewModel).GetMethod("ResolveArmorSwatchColorId",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object[] { nativeColor, explicitPreset, baseline, lastApplied, materialColor })
            .Should().Be(expected);
    }

    [TestCase(255, 247f, 55f, 1f)]
    [TestCase(-1, 247f, 55f, 1f)]
    [TestCase(0, 0f, 0f, 16f)]
    [TestCase(174, 224f, 160f, 16f)]
    [TestCase(175, 240f, 160f, 16f)]
    public void InheritedSwatchesUseTheNeutralMarkerInsteadOfARealPaletteColor(
        int colorId, float x, float y, float size)
    {
        var method = typeof(AppearanceEditorViewModel).GetMethod("BuildPaletteRegion",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var region = (GuiRectangle)method.Invoke(null, new object[] { colorId })!;
        region.X.Should().Be(x);
        region.Y.Should().Be(y);
        region.Width.Should().Be(size);
        region.Height.Should().Be(size);
    }

    [Test]
    public void EquipmentChangesReloadPartOptionsBeforeRefreshingColors()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        var refreshMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText == "Refresh").ToArray();
        foreach (var eventType in new[] { "EquipItemRefreshEvent", "UnequipItemRefreshEvent" })
            refreshMethods.Single(method => method.ParameterList.Parameters[0].Type!.ToString() == eventType)
                .ToString().Should().Contain("RefreshTintMapEditorAfterAppearanceChange(refreshItemParts: true)");
        refreshMethods.Single(method => method.ParameterList.Parameters[0].Type!.ToString() == "AppearanceChangedRefreshEvent")
            .ToString().Should().NotContain("refreshItemParts: true",
                "appearance edits must not reload combo options while the user is adjusting a part");

        var refresh = FindMethod(root, "RefreshTintMapEditorAfterAppearanceChange").ToString();
        refresh.IndexOf("ToggleItemEquippedFlags()", StringComparison.Ordinal).Should()
            .BeLessThan(refresh.IndexOf("LoadItemParts()", StringComparison.Ordinal));
        refresh.Should().Contain("if (refreshItemParts)");
        refresh.IndexOf("LoadItemParts()", StringComparison.Ordinal).Should()
            .BeLessThan(refresh.IndexOf("LoadTintMapEditor()", StringComparison.Ordinal));
    }

    [Test]
    public void ArmorHydrationDisablesEditingUntilCurrentOptionsHaveBeenPublished()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        var load = FindMethod(root, "LoadItemParts").ToString();
        load.IndexOf("SuspendArmorClientWatches()", StringComparison.Ordinal).Should()
            .BeLessThan(load.IndexOf("PopulateItemParts()", StringComparison.Ordinal));
        load.IndexOf("_skipAdjustArmorPart = true", StringComparison.Ordinal).Should()
            .BeLessThan(load.IndexOf("PopulateItemParts()", StringComparison.Ordinal));
        load.Should().Contain("finally");
        load.Should().Contain("_skipAdjustArmorPart = wasSkipping");

        var setters = root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(property => property.Identifier.ValueText.EndsWith("Selection", StringComparison.Ordinal) &&
                               property.ToString().Contains("AdjustArmorPart", StringComparison.Ordinal))
            .ToArray();
        setters.Should().HaveCount(19);
        foreach (var setter in setters)
            setter.ToString().Should().Contain("_armorClientBindingsWatched && !_skipAdjustArmorPart",
                "a client hydration event must not execute the part-changing setter");
    }

    [Test]
    public void ReturningToArmorSelectsGlobalWithoutLettingBindingEchoesResetTheTarget()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == nameof(AppearanceEditorViewModel.SelectedItemTypeIndex)).ToString();
        property.IndexOf("value == _loadedItemTypeIndex", StringComparison.Ordinal).Should()
            .BeLessThan(property.IndexOf("_colorTarget = ColorTarget.Global", StringComparison.Ordinal));
        property.Should().Contain("_selectedColorChannel = AppearanceArmorColor.Leather1");
        var hidePartRows = property.IndexOf("SetIfChanged(nameof(IsPartColorsVisible), false)", StringComparison.Ordinal);
        hidePartRows.Should().BeGreaterThan(property.IndexOf("if (value != 0)", StringComparison.Ordinal),
            "helmets, cloaks and weapons hide the armor part colour rows");
        property.IndexOf("if (value != 0)", StringComparison.Ordinal).Should().BeGreaterThan(0);
        property.IndexOf("LoadItemParts()", StringComparison.Ordinal).Should()
            .BeLessThan(property.IndexOf("LoadItemTypeEditor()", StringComparison.Ordinal),
                "the current selections must be ready before the replacement widgets are shown");
        var select = FindMethod(root, "SelectEditorTab").ToString();
        select.Should().Contain("EditorToggles.SyncTo");
        select.Should().Contain("SettingsToggles.SyncTo");
        select.Should().Contain("_colorTarget = ColorTarget.Global");
    }

    [Test]
    public void TabRestorationPublishesBindingsBeforeEditingResumes()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        var load = FindMethod(root, "LoadItemTypeEditor").ToString();
        load.Should().Contain("EditorTabs.Select(this, MainPartialElement, partialTabId, OnEditorPartialApplied)");
        var restoredMethod = FindMethod(root, "OnEditorPartialApplied");
        var restored = restoredMethod.ToString();
        var order = new[]
        {
            "SuspendArmorClientWatches()", "SynchronizeTintControlBindings(", "UpdatePaletteSheetForChannel()",
            "RepublishBindings()", "ResumeArmorClientWatches()"
        };
        for (var index = 1; index < order.Length; index++)
        {
            restored.IndexOf(order[index - 1], StringComparison.Ordinal).Should().BeGreaterThanOrEqualTo(0)
                .And.BeLessThan(restored.IndexOf(order[index], StringComparison.Ordinal),
                    $"{order[index - 1]} must run before {order[index]}");
        }

        // The palette sheet and the replay happen while the replacement widgets'
        // tint bindings are suppressed, so restoring them cannot edit colours.
        var synchronized = restoredMethod.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression.ToString() == "SynchronizeTintControlBindings")
            .ArgumentList.Arguments.Single().Expression;
        synchronized.Should().BeAssignableTo<LambdaExpressionSyntax>();
        var calls = synchronized.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => call.ToString()).ToArray();
        calls.Should().Contain(new[] { "UpdatePaletteSheetForChannel()", "RepublishBindings()" });

        restored.Should().NotContain("RestoreArmorPalette");
        ReadViewModel().Should().NotContain("RestoreArmorPalette", "the nested armor palette partials were removed");
        restored.Should().NotContain("SetGroupLayout(");
        restored.Should().NotContain("LoadItemParts()");
        restored.Should().NotContain("ApplyCurrentColors");
    }

    [Test]
    public void LeavingThePartTabsClosesThePartBrowser()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        var browser = CSharpSyntaxTree.ParseText(ReadViewModel("AppearanceEditorViewModel.PartBrowser.cs")).GetRoot();
        var hidden = FindMethod(root, "SelectEditorTab").DescendantNodes().OfType<IfStatementSyntax>()
            .Single(statement => statement.Condition.ToString() == "IsPartEditorHidden");
        hidden.Statement.ToString().Should().Contain("ClearBrowser()",
            "Wardrobe and Settings must not leave a browser that can still change parts");
        FindMethod(browser, "ClearBrowser").ToString().Should().Contain("ClosePartBrowser()");
        var emptySlot = FindMethod(root, "ToggleItemEquippedFlags").DescendantNodes().OfType<IfStatementSyntax>()
            .Single(statement => statement.Condition.ToString() == "!hasItemEquipped");
        emptySlot.Statement.ToString().Should().Contain("ClearBrowser()",
            "an empty Main Hand or Off Hand must not leave the last item's parts in the browser");
        FindMethod(browser, "ApplyBrowserPart").ToString().Should().Contain("if (IsPartEditorHidden)",
            "a late tile click from the browser does nothing on Wardrobe or Settings");
    }

    [Test]
    public void GeometryUpdatesNeverRebuildTheEditor()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        var browser = CSharpSyntaxTree.ParseText(ReadViewModel("AppearanceEditorViewModel.PartBrowser.cs")).GetRoot();

        FindMethod(root, "OnClientPropertyUpdated").ToString().Should().NotContain("nameof(Geometry)",
            "the panels follow a resize on their own, so a move or resize rebuilds nothing");
        browser.ToString().Should().NotContain("ChangePartialView(",
            "the part pictures moved to their own window; the editor swaps only its tab panels");
        foreach (var source in new[] { root, browser })
        {
            source.ToString().Should().NotContain("BuildEditorPanel(");
            source.ToString().Should().NotContain("SetGroupLayout(");
            source.ToString().Should().NotContain("QueueBrowserResize");
        }
    }

    [Test]
    public void PickerSupportsUnusedNativeChannelsAndShowsInheritedArmorDefaults()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        FindMethod(root, "TryGetEditableTintSelections").ToString()
            .Should().NotContain("selections.Count == 0");
        var load = FindMethod(root, "LoadTintMapEditor").ToString();
        load.Should().Contain("GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, (int)_selectedColorChannel)");
        load.Should().Contain("GetArmorSwatchColor(GetItem(), GetArmorModelType(_colorTarget), _selectedColorChannel)");
        load.Should().Contain("if (paletteId == 255)");
        load.Should().Contain("GetColor(_target, (ColorChannel)SelectedColorCategoryIndex)");
        load.IndexOf("var globalColor", StringComparison.Ordinal).Should()
            .BeLessThan(load.IndexOf("var effectiveColors", StringComparison.Ordinal),
                "armor preview must honor its native explicit or inherited color even without a matching material");
    }

    [Test]
    public void RgbDraftCommitRejectsStaleTargetsWithoutRepublishingTextBuffers()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        var input = FindMethod(root, "SetCustomTintComponent").ToString();
        input.Should().Contain("DelayCommand(0.4f");
        input.Should().Contain("generation == _tintEditGeneration");
        input.Should().Contain("token == WindowToken");
        input.Should().Contain("Gui.IsWindowOpen(Player, WindowType)");
        input.Should().NotContain("ApplyCustomTintColor(");
        FindMethod(root, "LoadTintMapEditor").ToString().Should().Contain("_tintEditGeneration++");
        var closed = FindMethod(root, "OnCloseWindow").ToString();
        closed.IndexOf("_tintEditGeneration++", StringComparison.Ordinal).Should()
            .BeLessThan(closed.IndexOf("GetIsDM(_target)", StringComparison.Ordinal),
                "closing an NPC editor must cancel drafts before the player-only restoration guard");
        var commit = FindMethod(root, "CommitCustomTintComponents").ToString();
        commit.Should().Contain("synchronizeComponents: false");
        commit.Should().NotContain("OnPropertyChanged");
        var clientUpdate = FindMethod(root, "OnClientPropertyUpdated").ToString();
        clientUpdate.Should().Contain("_tintComponentCorrection == propertyName");
        clientUpdate.Should().Contain("OnPropertyChanged(propertyName)");
        clientUpdate.Should().NotContain("OnPropertyChanged(nameof(CustomTint");
    }

    private static MethodDeclarationSyntax FindMethod(Microsoft.CodeAnalysis.SyntaxNode root, string name) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == name);

    private static string ReadViewModel(string fileName = "AppearanceEditorViewModel.cs")
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var path = Path.Combine(directory.FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel",
                fileName);
            if (File.Exists(path))
                return File.ReadAllText(path);
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find the appearance editor source.");
    }
}
