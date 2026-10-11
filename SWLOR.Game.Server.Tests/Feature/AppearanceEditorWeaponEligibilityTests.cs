using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class AppearanceEditorWeaponEligibilityTests
{
    [TestCase(BaseItem.Lightsaber)]
    [TestCase(BaseItem.Saberstaff)]
    [TestCase(BaseItem.Longsword)]
    public void CursedItemsAreBlockedRegardlessOfPlotFlag(BaseItem type)
    {
        AllowsFlags(type, false, true).Should().BeFalse();
        AllowsFlags(type, true, true).Should().BeFalse();
        AllowsFlags(type, false, false).Should().BeTrue();
    }

    [TestCase(BaseItem.Lightsaber, true)]
    [TestCase(BaseItem.Saberstaff, true)]
    [TestCase(BaseItem.Longsword, false)]
    public void PlotExceptionIsLimitedToSaberBaseTypes(BaseItem type, bool allowed)
    {
        AllowsFlags(type, true, false).Should().Be(allowed);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TrainingPartsRequireEveryModelAndColorToBeAllowed(bool staff)
    {
        WeaponAppearanceBaseDefinition appearance = staff
            ? new SaberstaffAppearanceDefinition() : new LightsaberAppearanceDefinition();
        var tops = staff ? new[] { 102, 107, 111 } : new[] { 114, 108, 102 };
        var bottoms = staff ? new[] { 107, 207, 307 } : new[] { 115, 215, 315 };
        appearance.TopParts.Should().BeEquivalentTo(tops);
        appearance.MiddleParts.Should().Equal(101);
        appearance.BottomParts.Should().BeEquivalentTo(bottoms);

        foreach (var top in tops)
        foreach (var bottom in bottoms)
        {
            AllowsParts(appearance, top, 101, bottom).Should().BeTrue();
            // Allowlists encode model + color * 100; reject unsupported models and colors independently.
            AllowsParts(appearance, top + 100, 101, bottom).Should().BeFalse();
            AllowsParts(appearance, 199, 101, bottom).Should().BeFalse();
            AllowsParts(appearance, top, 102, bottom).Should().BeFalse();
            AllowsParts(appearance, top, 201, bottom).Should().BeFalse();
            AllowsParts(appearance, top, 101, 199).Should().BeFalse();
            AllowsParts(appearance, top, 101, bottom + 300).Should().BeFalse();
        }
    }

    private static bool AllowsFlags(BaseItem type, bool plot, bool cursed) =>
        (bool)typeof(AppearanceEditorViewModel).GetMethod("AllowsItemFlags", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { type, plot, cursed })!;

    private static bool AllowsParts(WeaponAppearanceBaseDefinition appearance, int top, int middle, int bottom) =>
        (bool)typeof(AppearanceEditorViewModel).GetMethod("AllowsWeaponParts", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { appearance, top, middle, bottom })!;
}
