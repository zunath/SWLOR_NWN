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

        var female = ArmorPartAppearance.FilterOptions("pfh0", AppearanceArmor.LeftThigh, options, _ => 0, models.Contains);
        var male = ArmorPartAppearance.FilterOptions("pmh0", AppearanceArmor.LeftThigh, options, _ => 0, models.Contains);

        Assert.That(female.Select(option => option.Value), Is.EqualTo(new[] { 0, 23, 24 }));
        Assert.That(male.Select(option => option.Value), Is.EqualTo(new[] { 0, 23, 24, 30 }));
    }

    [Test]
    public void CreaturesWithoutModularBodiesKeepEveryOption()
    {
        var options = new[] { 0, 30 }.Select(value => new GuiComboEntry(value.ToString(), value)).ToArray();
        var kept = ArmorPartAppearance.FilterOptions(string.Empty, AppearanceArmor.LeftThigh, options, _ => 0, _ => false);
        Assert.That(kept.Select(option => option.Value), Is.EqualTo(new[] { 0, 30 }));
    }

    [Test]
    public void SavedPartsTheBodyCannotRenderAreFound()
    {
        var models = new HashSet<string> { "pfh0_legr024", "pfh0_shinl001", "pfh0_chest004" };
        var saved = new Dictionary<AppearanceArmor, int>
        {
            [AppearanceArmor.LeftThigh] = 30,
            [AppearanceArmor.RightThigh] = 24,
            [AppearanceArmor.LeftShin] = 23,
            [AppearanceArmor.Torso] = 4,
            [AppearanceArmor.Neck] = 258,
        };

        var parts = ArmorPartAppearance.FindUnrenderableParts("pfh0",
            part => saved.TryGetValue(part, out var style) ? style : 0, _ => 0, models.Contains);

        // The neck has its own repair; unset slots (zero) always render the body part.
        Assert.That(parts, Is.EquivalentTo(new[] { AppearanceArmor.LeftThigh, AppearanceArmor.LeftShin }));
        Assert.That(ArmorPartAppearance.FindUnrenderableParts(string.Empty, _ => 30, _ => 0, _ => false), Is.Empty);
    }

    [Test]
    public void RepairsUseAStyleEverySlotListOffers()
    {
        var shins = new GeneralArmorAppearanceDefinition().Shin;
        var thighs = new GeneralArmorAppearanceDefinition().Thigh;

        Assert.That(ArmorPartAppearance.GetRepairStyle(AppearanceArmor.LeftThigh), Is.EqualTo(0));
        Assert.That(thighs, Does.Contain(ArmorPartAppearance.GetRepairStyle(AppearanceArmor.LeftThigh)));
        Assert.That(shins, Does.Contain(ArmorPartAppearance.GetRepairStyle(AppearanceArmor.RightShin)));
    }
}
