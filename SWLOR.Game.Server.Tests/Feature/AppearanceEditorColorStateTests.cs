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
        property.IndexOf("LoadItemParts()", StringComparison.Ordinal).Should()
            .BeLessThan(property.IndexOf("LoadItemTypeEditor()", StringComparison.Ordinal),
                "the current selections must be ready before the replacement widgets are shown");
        var select = FindMethod(root, "SelectEditorTab").ToString();
        select.Should().Contain("EditorToggles.SyncTo");
        select.Should().Contain("SettingsToggles.SyncTo");
        select.Should().Contain("_colorTarget = ColorTarget.Global");
    }

    [Test]
    public void TabRestorationPublishesBindingsAfterTheNestedPaletteAndBeforeEditingResumes()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        FindMethod(root, "LoadItemTypeEditor").ToString().Should()
            .Contain("EditorTabs.Select(this, MainPartialElement, partialTabId, OnEditorPartialApplied)");
        var restored = FindMethod(root, "OnEditorPartialApplied").ToString();
        restored.IndexOf("SuspendArmorClientWatches()", StringComparison.Ordinal).Should()
            .BeLessThan(restored.IndexOf("RestoreArmorPalette()", StringComparison.Ordinal));
        restored.IndexOf("RestoreArmorPalette()", StringComparison.Ordinal).Should()
            .BeLessThan(restored.IndexOf("RepublishBindings()", StringComparison.Ordinal));
        restored.IndexOf("RepublishBindings()", StringComparison.Ordinal).Should()
            .BeLessThan(restored.IndexOf("ResumeArmorClientWatches()", StringComparison.Ordinal));
        restored.IndexOf("SynchronizeTintControlBindings(", StringComparison.Ordinal).Should()
            .BeLessThan(restored.IndexOf("RestoreArmorPalette()", StringComparison.Ordinal),
                "replacement widgets must not edit colors while their current bindings are restored");
        restored.Should().NotContain("SetGroupLayout(");
        restored.Should().NotContain("LoadItemParts()");
        restored.Should().NotContain("ApplyCurrentColors");
    }

    [Test]
    public void ResizingDoesNotRebuildTheEditorOrReplayItsBindings()
    {
        var source = ReadViewModel();
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var clientUpdated = FindMethod(root, "OnClientPropertyUpdated").ToString();
        clientUpdated.Should().NotContain("Geometry");
        source.Should().NotContain("QueueEditorResize");
        source.Should().NotContain("BuildEditorPanel(");
        source.Should().NotContain("SetGroupLayout(");
        source.Should().NotContain("_editorResizeGeneration");
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

    private static string ReadViewModel()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var path = Path.Combine(directory.FullName, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel",
                "AppearanceEditorViewModel.cs");
            if (File.Exists(path))
                return File.ReadAllText(path);
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find the appearance editor source.");
    }
}
