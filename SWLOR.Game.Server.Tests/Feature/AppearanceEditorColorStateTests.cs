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
    public void GlobalPickerReadsTheGlobalPaletteAndSupportsCurrentlyUnusedChannels()
    {
        var root = CSharpSyntaxTree.ParseText(ReadViewModel()).GetRoot();
        FindMethod(root, "TryGetEditableTintSelections").ToString()
            .Should().Contain("selections.Count == 0 && !isGlobalArmorColor");
        var load = FindMethod(root, "LoadTintMapEditor").ToString();
        load.Should().Contain("GetItemAppearance(GetItem(), ItemAppearanceType.ArmorColor, (int)_selectedColorChannel)");
        load.IndexOf("var globalColor", StringComparison.Ordinal).Should()
            .BeLessThan(load.IndexOf("var effectiveColors", StringComparison.Ordinal),
                "explicit part colors cannot replace the global picker preview");
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
