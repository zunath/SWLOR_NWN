using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Component traits (plan section 14, W9a): what they do to a module, how items store them, the salvage rolls and
/// Trait Hunter's rare-trait points.
/// </summary>
public class ShipTraitRulesTests
{
    private static ShipModuleStep Laser(int rank) => RevampModuleCatalog.Steps.Single(x => x.Tag == $"sm_laser_{rank:00}");

    // The trait table

    [Test]
    public void CraftedVariants_AreTradeOffs()
    {
        foreach (var trait in RevampComponentCatalog.Traits.Where(x => x.Kind == ShipTraitKind.Crafted))
        {
            trait.Effects.Should().Contain(x => IsGood(x), trait.Name);
            trait.Effects.Should().Contain(x => !IsGood(x), $"{trait.Name}: section 14, nothing becomes a strict upgrade");
            trait.Families.Should().ContainSingle(trait.Name);
        }
    }

    [Test]
    public void SalvagedTraits_OnlyHelp_AndRareOnesAreStronger()
    {
        foreach (var trait in RevampComponentCatalog.Traits.Where(x => x.Kind != ShipTraitKind.Crafted))
            trait.Effects.Should().OnlyContain(x => IsGood(x), trait.Name);

        foreach (var trait in RevampComponentCatalog.Traits.Where(x => x.Kind == ShipTraitKind.Common))
            trait.Effects.Should().ContainSingle(trait.Name);

        foreach (var trait in RevampComponentCatalog.Traits.Where(x => x.Kind == ShipTraitKind.Rare))
            trait.Effects.Count.Should().BeGreaterThan(1, trait.Name);
    }

    [Test]
    public void EverySalvageFamily_HasCommonAndRareTraits_AndLifeSupportIsNeverSalvaged()
    {
        RevampComponentCatalog.SalvageFamilies.Should().HaveCount(8).And.NotContain(ShipComponentFamily.LifeSupportSystem);
        foreach (var family in RevampComponentCatalog.SalvageFamilies)
        {
            ShipTraitRules.Pool(family, ShipTraitKind.Common).Should().NotBeEmpty(family.ToString());
            ShipTraitRules.Pool(family, ShipTraitKind.Rare).Should().NotBeEmpty(family.ToString());
            ShipTraitRules.Pool(family, ShipTraitKind.Crafted).Count.Should().BeInRange(2, 3, $"{family}: 2-3 crafted variants");
        }
    }

    [Test]
    public void RareTraits_AreNeverCrafted()
    {
        var crafted = RevampComponentCatalog.Recipes.Select(x => x.Trait).ToHashSet();
        RevampComponentCatalog.Traits.Where(x => x.Kind != ShipTraitKind.Crafted)
            .Should().OnlyContain(x => !crafted.Contains(x.Type));
        ShipTraitRules.Get(ShipComponentTraitType.SithTuned).Name.Should().Be("Sith-tuned", "section 14's example");
    }

    [Test]
    public void TraitText_IsPlainAscii()
    {
        foreach (var trait in RevampComponentCatalog.Traits)
        {
            (trait.Name + trait.Description).All(c => c < 128).Should().BeTrue(trait.Name);
            trait.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    private static bool IsGood(ShipTraitEffect effect) =>
        effect.Stat == ShipTraitStat.CapacitorCost ? effect.Value < 0 : effect.Value > 0;

    // Effects on a module

    [TestCase(100, 6, 106)]
    [TestCase(5, -3, 5)]
    [TestCase(50, 3, 52)]
    [TestCase(-12, 5, -13)]
    [TestCase(3, -25, 2)]
    [TestCase(3, 25, 4)]
    [TestCase(0, 6, 0)]
    public void Scale_RoundsHalfAwayFromZero(int value, int percent, int expected)
    {
        ShipTraitRules.Scale(value, percent).Should().Be(expected);
    }

    [Test]
    public void Overcharged_RaisesTheLasersDamageAndItsCapacitorUse()
    {
        var laser = Laser(45);
        var overcharged = ShipTraitRules.Apply(laser, new[] { ShipComponentTraitType.Overcharged });

        overcharged.Should().NotBeSameAs(laser);
        overcharged.Dmg.Should().Be(ShipTraitRules.Scale(laser.Dmg, 6));
        overcharged.CapacitorCost.Should().Be(4, "3 x 1.25 = 3.75");
        overcharged.Accuracy.Should().Be(laser.Accuracy);
        laser.Dmg.Should().Be(RevampModuleCatalog.Steps.Single(x => x.Tag == "sm_laser_45").Dmg, "the catalog row never changes");
    }

    [Test]
    public void TwoComponentsTraits_AddUp()
    {
        var laser = Laser(45);
        var both = ShipTraitRules.Apply(laser, new[] { ShipComponentTraitType.Focused, ShipComponentTraitType.Aligned });

        both.Accuracy.Should().Be(ShipTraitRules.Scale(laser.Accuracy, 9), "+6% and +3%");
        both.Dmg.Should().Be(ShipTraitRules.Scale(laser.Dmg, -3));
    }

    [Test]
    public void NoTraits_LeaveTheStepUntouched()
    {
        var laser = Laser(20);
        ShipTraitRules.Apply(laser, null).Should().BeSameAs(laser);
        ShipTraitRules.Apply(laser, new ShipComponentTraitType[0]).Should().BeSameAs(laser);
    }

    [Test]
    public void DamageNeverDropsBelowOne()
    {
        var weak = new ShipModuleStep { Dmg = 1, Repair = 1 };
        var result = ShipTraitRules.Apply(weak, new[] { ShipComponentTraitType.Focused, ShipComponentTraitType.Rapid });
        result.Dmg.Should().Be(1);
        result.Repair.Should().Be(1);
    }

    [Test]
    public void ShieldProjectorTraits_ChangeShieldRepairAndTheBulwarkAura()
    {
        var booster = RevampModuleCatalog.Steps.Single(x => x.Tag == "sm_shbst_45");
        var repairer = RevampModuleCatalog.Steps.Single(x => x.Tag == "sm_shrep_45");
        var bulwark = RevampModuleCatalog.Steps.Single(x => x.Tag == "sm_bulw_45");

        ShipTraitRules.Apply(booster, new[] { ShipComponentTraitType.Dense }).Shield.Should().Be(ShipTraitRules.Scale(booster.Shield, 6));
        ShipTraitRules.Apply(repairer, new[] { ShipComponentTraitType.Dense }).Repair.Should().Be(ShipTraitRules.Scale(repairer.Repair, -4));
        ShipTraitRules.Apply(repairer, new[] { ShipComponentTraitType.RepublicGrade }).Repair.Should().Be(ShipTraitRules.Scale(repairer.Repair, 5));
        ShipTraitRules.Apply(bulwark, new[] { ShipComponentTraitType.Charged }).AuraShieldPercent.Should().Be(21, "20% x 1.03");
    }

    [Test]
    public void ReactorTraits_ChangeCapacitorAndRegen()
    {
        var battery = RevampModuleCatalog.Steps.Single(x => x.Tag == "sm_capb_45");
        var regulator = RevampModuleCatalog.Steps.Single(x => x.Tag == "sm_capr_45");

        ShipTraitRules.Apply(battery, new[] { ShipComponentTraitType.DeepCell }).MaxCapacitor.Should().Be(ShipTraitRules.Scale(battery.MaxCapacitor, 8));
        ShipTraitRules.Apply(regulator, new[] { ShipComponentTraitType.Overclocked }).CapacitorRegen
            .Should().BeApproximately(regulator.CapacitorRegen * 1.08, 0.001);
    }

    [TestCase(3, new[] { ShipComponentTraitType.Efficient }, 2)]
    [TestCase(3, new[] { ShipComponentTraitType.Insulated }, 2)]
    [TestCase(20, new[] { ShipComponentTraitType.Insulated }, 16)]
    [TestCase(3, new[] { ShipComponentTraitType.Bulk }, 4)]
    [TestCase(0, new[] { ShipComponentTraitType.Efficient }, 0)]
    [TestCase(3, new ShipComponentTraitType[0], 3)]
    public void CapacitorCost_FollowsTheTraits(int cost, ShipComponentTraitType[] traits, int expected)
    {
        ShipTraitRules.CapacitorCost(cost, traits).Should().Be(expected);
    }

    [TestCase(0, new[] { ShipComponentTraitType.Bulk }, 2)]
    [TestCase(3, new[] { ShipComponentTraitType.WideBore, ShipComponentTraitType.CzerkaPattern }, 6)]
    [TestCase(0, new[] { ShipComponentTraitType.Precision }, 0)]
    [TestCase(4, new[] { ShipComponentTraitType.Precision }, 3)]
    public void Yield_AddsToTheModuleBonusAndNeverGoesNegative(int bonus, ShipComponentTraitType[] traits, int expected)
    {
        ShipTraitRules.ModuleBonus(bonus, traits).Should().Be(expected);
    }

    [Test]
    public void FlatStats_AreSummedForTheShip()
    {
        ShipTraitRules.Sum(new[] { ShipComponentTraitType.Precision, ShipComponentTraitType.Keen }, ShipTraitStat.RareFind).Should().Be(8);
        ShipTraitRules.Sum(new[] { ShipComponentTraitType.CorellianModified }, ShipTraitStat.Speed).Should().Be(2);
        ShipTraitRules.Sum(null, ShipTraitStat.Speed).Should().Be(0);
    }

    // The firing module's context

    [Test]
    public void TraitContext_IsTheFiringModulesAndNests()
    {
        ShipModuleTraitContext.Current.Should().BeEmpty();
        using (ShipModuleTraitContext.Begin(new[] { ShipComponentTraitType.Tuned }))
        {
            ShipModuleTraitContext.Current.Should().Equal(ShipComponentTraitType.Tuned);
            using (ShipModuleTraitContext.Begin(null))
                ShipModuleTraitContext.Current.Should().BeEmpty();
            ShipModuleTraitContext.Current.Should().Equal(ShipComponentTraitType.Tuned);
        }

        ShipModuleTraitContext.Current.Should().BeEmpty();
    }

    // Item storage

    [Test]
    public void StoredTraits_RoundTrip_AndUnknownNumbersAreDropped()
    {
        var traits = new[] { ShipComponentTraitType.Focused, ShipComponentTraitType.SithTuned };
        var stored = ShipTraitRules.Format(traits);
        stored.Should().Be("7,41");
        ShipTraitRules.Parse(stored).Should().Equal(traits);
        ShipTraitRules.Parse("7, 999,x,,21").Should().Equal(ShipComponentTraitType.Focused, ShipComponentTraitType.Tuned);
        ShipTraitRules.Parse(null).Should().BeEmpty();
        ShipTraitRules.Format(new[] { ShipComponentTraitType.None }).Should().BeEmpty();
    }

    [Test]
    public void Names_AndExamineLines_ShowTheTraits()
    {
        ShipTraitRules.DecoratedName("Laser Cannon Mk X", new[] { ShipComponentTraitType.Focused, ShipComponentTraitType.SithTuned })
            .Should().Be("Laser Cannon Mk X [Focused, Sith-tuned]");
        ShipTraitRules.DecoratedName("Hull Frame (Tier 1)", new ShipComponentTraitType[0]).Should().Be("Hull Frame (Tier 1)");
        ShipTraitRules.DescribeLines(new[] { ShipComponentTraitType.Focused, ShipComponentTraitType.SithTuned }).Should().Equal(
            "Focused: +6% Accuracy, -3% DMG",
            "Sith-tuned (rare): +5% DMG, +5% Accuracy");
    }

    // Salvaged components

    /// <summary>A scripted random source: each call returns the next value, checked against its bound.</summary>
    private static Func<int, int> Rolls(params int[] values)
    {
        var queue = new Queue<int>(values);
        return max =>
        {
            var value = queue.Dequeue();
            value.Should().BeInRange(0, max - 1);
            return value;
        };
    }

    [TestCase(WreckDifficulty.Normal, 10, 5)]
    [TestCase(WreckDifficulty.Elite, 20, 10)]
    [TestCase(WreckDifficulty.Boss, 30, 20)]
    public void SalvageOdds_RiseWithTheWrecksDifficulty(WreckDifficulty difficulty, int component, int rare)
    {
        ShipTraitRules.SalvagedComponentPercent(difficulty).Should().Be(component);
        ShipTraitRules.RareTraitPercent(difficulty, 0).Should().Be(rare);
    }

    [TestCase(WreckDifficulty.Normal, 10, 15)]
    [TestCase(WreckDifficulty.Normal, 20, 25)]
    [TestCase(WreckDifficulty.Boss, 20, 40)]
    [TestCase(WreckDifficulty.Boss, 500, 100)]
    [TestCase(WreckDifficulty.Elite, -10, 10)]
    public void TraitHunter_AddsPointsToTheRareTraitChance(WreckDifficulty difficulty, int bonus, int expected)
    {
        ShipTraitRules.RareTraitPercent(difficulty, bonus).Should().Be(expected);
    }

    [Test]
    public void AFailedComponentRoll_YieldsNothing()
    {
        ShipTraitRules.RollSalvagedComponent(3, WreckDifficulty.Normal, 0, Rolls(10)).Should().BeNull("10 is not below 10%");
    }

    [Test]
    public void ASalvagedComponent_IsOfTheWrecksTierAndCarriesACommonTrait()
    {
        // Component roll 9 (< 10%), family 0 (Hull Frame), rare roll 5 (not below 5%), first common trait.
        var result = ShipTraitRules.RollSalvagedComponent(3, WreckDifficulty.Normal, 0, Rolls(9, 0, 5, 0));

        result.Should().NotBeNull();
        result!.Value.Resref.Should().Be("sc_hframe_3");
        result.Value.Trait.Should().Be(ShipTraitRules.Pool(ShipComponentFamily.HullFrame, ShipTraitKind.Common)[0].Type);
    }

    [Test]
    public void TraitHunter_TurnsAMissedRareRollIntoARareTrait()
    {
        var family = RevampComponentCatalog.SalvageFamilies.IndexOf(ShipComponentFamily.EnergyEmitter);

        ShipTraitRules.RollSalvagedComponent(5, WreckDifficulty.Normal, 0, Rolls(0, family, 12, 0))!.Value.Trait
            .Should().NotBe(ShipComponentTraitType.SithTuned, "a 12 misses the normal wreck's 5%");
        ShipTraitRules.RollSalvagedComponent(5, WreckDifficulty.Normal, 10, Rolls(0, family, 12, 0))!.Value.Trait
            .Should().Be(ShipComponentTraitType.SithTuned, "Trait Hunter I makes it 15%");
    }

    [Test]
    public void TierSixWrecks_YieldTierFiveComponents()
    {
        ShipTraitRules.RollSalvagedComponent(6, WreckDifficulty.Boss, 0, Rolls(0, 1, 99, 0))!.Value.Resref.Should().EndWith("_5");
    }

    [Test]
    public void OverManyCycles_TheRatesMatchTheTable()
    {
        var random = new System.Random(7);
        var components = 0;
        var rare = 0;
        const int Cycles = 200000;
        for (var i = 0; i < Cycles; i++)
        {
            var result = ShipTraitRules.RollSalvagedComponent(2, WreckDifficulty.Elite, 20, random.Next);
            if (result == null) continue;
            components++;
            if (ShipTraitRules.Get(result.Value.Trait).Kind == ShipTraitKind.Rare) rare++;
        }

        (components / (double)Cycles).Should().BeApproximately(0.20, 0.01);
        (rare / (double)components).Should().BeApproximately(0.30, 0.01, "elite 10% + Trait Hunter II 20");
    }
}

file static class ListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> list, T value)
    {
        for (var i = 0; i < list.Count; i++)
            if (EqualityComparer<T>.Default.Equals(list[i], value))
                return i;
        return -1;
    }
}
