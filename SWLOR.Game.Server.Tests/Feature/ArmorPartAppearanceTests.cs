using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
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
    public void RepairsUseAStyleTheWearersCatalogOffers()
    {
        var human = ArmorAppearanceCatalog.Definitions[AppearanceType.Human];
        var droid = ArmorAppearanceCatalog.Definitions[AppearanceType.Droid];
        var parts = new[]
        {
            AppearanceArmor.Torso, AppearanceArmor.Pelvis, AppearanceArmor.LeftBicep, AppearanceArmor.LeftForearm,
            AppearanceArmor.LeftHand, AppearanceArmor.LeftThigh, AppearanceArmor.LeftShin, AppearanceArmor.LeftFoot,
        };

        foreach (var definition in new[] { human, droid })
        {
            foreach (var part in parts)
            {
                var styles = ArmorAppearanceCatalog.GetStyles(definition, part);
                Assert.That(styles, Does.Contain(ArmorPartAppearance.GetRepairStyle(styles, _ => true)), $"{definition.GetType().Name} {part}");
            }
        }

        // Zero (the wearer's own body part) wins where offered; otherwise the first style the body renders.
        Assert.That(ArmorPartAppearance.GetRepairStyle(ArmorAppearanceCatalog.GetStyles(human, AppearanceArmor.LeftThigh), _ => true), Is.EqualTo(0));
        Assert.That(ArmorPartAppearance.GetRepairStyle(ArmorAppearanceCatalog.GetStyles(human, AppearanceArmor.LeftShin), _ => true), Is.EqualTo(1));
        Assert.That(ArmorPartAppearance.GetRepairStyle(new[] { 5, 7, 9 }, style => style == 7), Is.EqualTo(7));
    }

    [Test]
    public void OnlyPlayableAppearancesAreRepaired()
    {
        var models = new HashSet<string> { "pfh0_legl001", "pfh0_shinl001" };
        int Saved(AppearanceArmor part) => part == AppearanceArmor.LeftThigh ? 30 : part == AppearanceArmor.LeftShin ? 23 : 0;

        var human = ArmorPartAppearance.PlanRepairs(ArmorAppearanceCatalog.Definitions[AppearanceType.Human], "pfh0",
            Saved, _ => 0, models.Contains);
        var unsupported = ArmorPartAppearance.PlanRepairs(null, "pfe0", Saved, _ => 0, _ => false);

        Assert.That(human, Is.EquivalentTo(new Dictionary<AppearanceArmor, int>
        {
            [AppearanceArmor.LeftThigh] = 0,
            [AppearanceArmor.LeftShin] = 1,
        }));
        Assert.That(unsupported, Is.Empty);
    }
}
