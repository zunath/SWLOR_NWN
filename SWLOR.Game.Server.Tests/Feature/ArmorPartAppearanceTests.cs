using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class ArmorPartAppearanceTests
{
    [Test]
    public void ResolveUsesTheSlotsModelNameAndPhenotypeFallback()
    {
        var models = new HashSet<string> { "pfh0_legl024", "pfh0_legr024", "pmh0_legl030", "pfh2_chest004" };
        Assert.That(ArmorPartAppearance.ResolveModel("pfh0", AppearanceArmor.LeftThigh, 24, _ => 0, models.Contains),
            Is.EqualTo("pfh0_legl024"));
        Assert.That(ArmorPartAppearance.ResolveModel("pfh2", AppearanceArmor.RightThigh, 24, _ => 0, models.Contains),
            Is.EqualTo("pfh0_legr024"));
        Assert.That(ArmorPartAppearance.ResolveModel("pfh2", AppearanceArmor.Torso, 4, _ => 0, models.Contains),
            Is.EqualTo("pfh2_chest004"));
        Assert.That(ArmorPartAppearance.ResolveModel("pfh0", AppearanceArmor.LeftThigh, 30, _ => 0, models.Contains),
            Is.Empty);
        Assert.That(ArmorPartAppearance.ResolveModel("pfh0", AppearanceArmor.Robe, 20, _ => 0, _ => true),
            Is.Empty);
    }

    [Test]
    public void FemaleBodiesSkipThighsThatOnlyMalesHave()
    {
        var models = new HashSet<string> { "pfh0_legl023", "pfh0_legl024", "pmh0_legl023", "pmh0_legl024", "pmh0_legl030" };
        var options = new[] { 0, 23, 24, 30 }.Select(value => new GuiComboEntry(value.ToString(), value)).ToArray();

        var female = ArmorPartAppearance.FilterOptions("pfh0", AppearanceArmor.LeftThigh, options, 24, _ => 0, models.Contains);
        var male = ArmorPartAppearance.FilterOptions("pmh0", AppearanceArmor.LeftThigh, options, 24, _ => 0, models.Contains);

        Assert.That(female.Select(option => option.Value), Is.EqualTo(new[] { 0, 23, 24 }));
        Assert.That(male.Select(option => option.Value), Is.EqualTo(new[] { 0, 23, 24, 30 }));
    }

    [Test]
    public void CreaturesWithoutModularBodiesKeepEveryOption()
    {
        var options = new[] { 0, 30 }.Select(value => new GuiComboEntry(value.ToString(), value)).ToArray();
        var kept = ArmorPartAppearance.FilterOptions(string.Empty, AppearanceArmor.LeftThigh, options, 0, _ => 0, _ => false);
        Assert.That(kept.Select(option => option.Value), Is.EqualTo(new[] { 0, 30 }));
    }

    [Test]
    public void AnEquippedUnrenderablePartStaysInPlaceSoTheArrowsCanLeaveIt()
    {
        var models = new HashSet<string> { "pfh0_legl024", "pfh0_legl050" };
        var options = new[] { 0, 24, 30, 31, 50 }.Select(value => new GuiComboEntry(value.ToString(), value)).ToArray();

        var female = ArmorPartAppearance.FilterOptions("pfh0", AppearanceArmor.LeftThigh, options, 30, _ => 0, models.Contains);

        Assert.That(female.Select(option => option.Value), Is.EqualTo(new[] { 0, 24, 30, 50 }));
    }
}
