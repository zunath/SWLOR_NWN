using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class AppearanceEditorEquipmentColorTests
{
    [TestCase(0, 0, AppearanceArmorColor.Cloth1)]
    [TestCase(0, 1, AppearanceArmorColor.Cloth2)]
    [TestCase(1, 0, AppearanceArmorColor.Leather1)]
    [TestCase(1, 1, AppearanceArmorColor.Leather2)]
    [TestCase(2, 0, AppearanceArmorColor.Metal1)]
    [TestCase(2, 1, AppearanceArmorColor.Metal2)]
    [TestCase(-1, 0, null)]
    [TestCase(3, 0, null)]
    [TestCase(0, 2, null)]
    public void EquipmentListEventsSelectTheCorrectChannel(int row, int column, AppearanceArmorColor? expected)
    {
        var method = typeof(AppearanceEditorViewModel).GetMethod("EquipmentSwatchChannel", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object[] { row, column }).Should().Be(expected);
    }

    [Test]
    public void EquipmentSwatchDeduplicationKeepsActualColorAndRegionEdits()
    {
        var method = typeof(AppearanceEditorViewModel).GetMethod("SameEquipmentSwatchValue", BindingFlags.NonPublic | BindingFlags.Static)!;
        bool Same(object first, object second) => (bool)method.Invoke(null, new[] { first, second })!;
        Same(new GuiRectangle(0, 16, 16, 16), new GuiRectangle(0, 16, 16, 16)).Should().BeTrue();
        Same(new GuiRectangle(0, 16, 16, 16), new GuiRectangle(16, 16, 16, 16)).Should().BeFalse();
        Same(new GuiColor(12, 34, 56), new GuiColor(12, 34, 56)).Should().BeTrue();
        Same(new GuiColor(12, 34, 56), new GuiColor(13, 34, 56)).Should().BeFalse();
        Same(false, false).Should().BeTrue();
        Same(false, true).Should().BeFalse();
    }

}
