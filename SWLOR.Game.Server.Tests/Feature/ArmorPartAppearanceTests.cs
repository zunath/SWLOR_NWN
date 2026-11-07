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
    public void CatalogsOfferNoShoulderOrBeltThatShowsNothing()
    {
        foreach (var (type, definition) in ArmorAppearanceCatalog.Definitions)
        {
            Assert.That(definition.Shoulder, Does.Not.Contain(1).And.Not.Contain(2).And.Contain(0), $"{type} shoulder");
            Assert.That(definition.Belt, Does.Not.Contain(1).And.Not.Contain(2).And.Contain(0), $"{type} belt");
        }
    }

    [TestCase(AppearanceArmor.LeftShoulder, "shol")]
    [TestCase(AppearanceArmor.RightShoulder, "shor")]
    [TestCase(AppearanceArmor.Belt, "belt")]
    public void SavedShouldersAndBeltsAtStyleOneOrTwoRepairToNoPart(AppearanceArmor part, string model)
    {
        // The models exist, but the engine draws the wearer's own (empty) body part for these styles.
        var models = new HashSet<string> { $"pfh0_{model}001", $"pfh0_{model}002" };
        var human = ArmorAppearanceCatalog.Definitions[AppearanceType.Human];

        foreach (var style in new[] { 1, 2 })
        {
            Assert.That(ArmorPartAppearance.IsRenderable("pfh0", part, style, _ => 0, models.Contains), Is.False);
            var repairs = ArmorPartAppearance.PlanRepairs(human, 0, "pfh0",
                saved => saved == part ? style : 0, _ => 0, models.Contains);
            Assert.That(repairs, Is.EquivalentTo(new Dictionary<AppearanceArmor, int> { [part] = 0 }));
        }
    }

    [TestCase(AppearanceArmor.Torso, "chest")]
    [TestCase(AppearanceArmor.Pelvis, "pelvis")]
    [TestCase(AppearanceArmor.LeftBicep, "bicepl")]
    [TestCase(AppearanceArmor.LeftForearm, "forel")]
    [TestCase(AppearanceArmor.LeftHand, "handl")]
    [TestCase(AppearanceArmor.LeftThigh, "legl")]
    [TestCase(AppearanceArmor.LeftShin, "shinl")]
    [TestCase(AppearanceArmor.LeftFoot, "footl")]
    public void BareBodyPartsAtStyleOneAreKept(AppearanceArmor part, string model)
    {
        // Style 1 shows the wearer's own bare body part. Hundreds of blueprints wear it, and the
        // general shin list and most droid lists offer no 0, so it stays until Xisifer decides.
        var models = new HashSet<string> { $"pfh0_{model}001" };
        var human = ArmorAppearanceCatalog.Definitions[AppearanceType.Human];
        var droid = ArmorAppearanceCatalog.Definitions[AppearanceType.Droid];

        Assert.That(ArmorAppearanceCatalog.GetStyles(human, part), Does.Contain(1));
        Assert.That(ArmorAppearanceCatalog.GetStyles(droid, part), Does.Contain(1));
        Assert.That(ArmorPartAppearance.PlanRepairs(human, 0, "pfh0",
            saved => saved == part ? 1 : 0, _ => 0, models.Contains), Is.Empty);
    }

    [Test]
    public void NoCatalogOffersStyleTwo()
    {
        foreach (var (type, definition) in ArmorAppearanceCatalog.Definitions)
        foreach (var part in ArmorParts)
        {
            Assert.That(ArmorAppearanceCatalog.GetStyles(definition, part),
                Does.Not.Contain(ArmorPartAppearance.RemovedBareStyle), $"{type} {part}");
        }
    }

    [Test]
    public void StyleTwoRepairsToAStyleTheListOffers()
    {
        foreach (var (type, definition) in ArmorAppearanceCatalog.Definitions)
        foreach (var part in ArmorParts)
        {
            // The editor always offers 0 (None) for the neck.
            var offered = ArmorAppearanceCatalog.GetStyles(definition, part).AsEnumerable();
            if (part == AppearanceArmor.Neck)
                offered = offered.Prepend(0);
            Assert.That(offered, Does.Contain(ArmorPartAppearance.GetBareStyle(definition, part)), $"{type} {part}");
        }
    }

    [TestCase(AppearanceType.Human, AppearanceArmor.Neck, "neck", 0)]
    [TestCase(AppearanceType.Human, AppearanceArmor.Torso, "chest", 0)]
    [TestCase(AppearanceType.Human, AppearanceArmor.Pelvis, "pelvis", 0)]
    [TestCase(AppearanceType.Human, AppearanceArmor.RightBicep, "bicepr", 0)]
    [TestCase(AppearanceType.Human, AppearanceArmor.LeftForearm, "forel", 0)]
    [TestCase(AppearanceType.Human, AppearanceArmor.RightHand, "handr", 0)]
    [TestCase(AppearanceType.Human, AppearanceArmor.LeftThigh, "legl", 0)]
    [TestCase(AppearanceType.Human, AppearanceArmor.RightShin, "shinr", 1)]
    [TestCase(AppearanceType.Human, AppearanceArmor.LeftFoot, "footl", 0)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.Neck, "neck", 0)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.Torso, "chest", 1)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.Pelvis, "pelvis", 1)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.LeftBicep, "bicepl", 1)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.RightForearm, "forer", 1)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.LeftHand, "handl", 1)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.RightThigh, "legr", 1)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.LeftShin, "shinl", 1)]
    [TestCase(AppearanceType.Droid, AppearanceArmor.RightFoot, "footr", 1)]
    public void SavedStyleTwoRepairsToZeroWhereListedOtherwiseOne(AppearanceType type, AppearanceArmor part, string model, int expected)
    {
        // Style 2 shows the wearer's own body part like 0 and 1. Its models exist, so only the
        // retirement moves it; every other saved part is kept.
        var body = type == AppearanceType.Droid ? "pmz0" : "pfh0";
        var models = new HashSet<string> { $"{body}_{model}001", $"{body}_{model}002", $"{body}_chest004" };
        var saved = new Dictionary<AppearanceArmor, int> { [AppearanceArmor.Torso] = 4, [part] = 2 };

        var repairs = ArmorPartAppearance.PlanRepairs(ArmorAppearanceCatalog.Definitions[type], 0, body,
            slot => saved.TryGetValue(slot, out var style) ? style : 0, _ => 0, models.Contains);

        Assert.That(repairs, Is.EquivalentTo(new Dictionary<AppearanceArmor, int> { [part] = expected }));
    }

    [Test]
    public void StyleTwoOnUncataloguedCreaturesIsKept()
    {
        Assert.That(ArmorPartAppearance.PlanRepairs(null, 0, "pmh0", _ => 2, _ => 0, _ => true), Is.Empty);
        Assert.That(ArmorPartAppearance.PlanRepairs(ArmorAppearanceCatalog.Definitions[AppearanceType.Human], 3,
            "pmh0", _ => 2, _ => 0, _ => true), Is.Empty);
    }

    private static readonly AppearanceArmor[] ArmorParts =
    {
        AppearanceArmor.Neck, AppearanceArmor.Torso, AppearanceArmor.Belt, AppearanceArmor.Pelvis,
        AppearanceArmor.LeftShoulder, AppearanceArmor.RightShoulder, AppearanceArmor.LeftBicep,
        AppearanceArmor.RightBicep, AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm,
        AppearanceArmor.LeftHand, AppearanceArmor.RightHand, AppearanceArmor.LeftThigh, AppearanceArmor.RightThigh,
        AppearanceArmor.LeftShin, AppearanceArmor.RightShin, AppearanceArmor.LeftFoot, AppearanceArmor.RightFoot,
        AppearanceArmor.Robe,
    };

    [Test]
    public void OnlyPlayableAppearancesAreRepaired()
    {
        var models = new HashSet<string> { "pfh0_legl001", "pfh0_shinl001" };
        int Saved(AppearanceArmor part) => part == AppearanceArmor.LeftThigh ? 30 : part == AppearanceArmor.LeftShin ? 23 : 0;

        var human = ArmorPartAppearance.PlanRepairs(ArmorAppearanceCatalog.Definitions[AppearanceType.Human], 0, "pfh0",
            Saved, _ => 0, models.Contains);
        var unsupported = ArmorPartAppearance.PlanRepairs(null, 0, "pfe0", Saved, _ => 0, _ => false);
        var large = ArmorPartAppearance.PlanRepairs(ArmorAppearanceCatalog.Definitions[AppearanceType.Human], 2, "pfh2",
            Saved, _ => 0, _ => false);

        Assert.That(human, Is.EquivalentTo(new Dictionary<AppearanceArmor, int>
        {
            [AppearanceArmor.LeftThigh] = 0,
            [AppearanceArmor.LeftShin] = 1,
        }));
        Assert.That(unsupported, Is.Empty);
        Assert.That(large, Is.Empty);
    }
}
