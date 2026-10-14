using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class TwinPistolPerkTests
{
    [TestCase(PerkCategoryType.TwinPistolBarrage)]
    [TestCase(PerkCategoryType.TwinPistolCrossfire)]
    public void IndependentLine_HasTenPerksEighteenPurchasesAndSixtySP(PerkCategoryType category)
    {
        var perks = ReadPerks().Values.Where(x => x.Category == category).ToArray();
        perks.Should().HaveCount(10);
        var levels = perks.SelectMany(x => x.PerkLevels.Values).ToArray();
        levels.Should().HaveCount(18);
        levels.Sum(x => x.Price).Should().Be(60);
        var requirements = levels.SelectMany(x => x.Requirements).OfType<PerkRequirementSkill>().ToArray();
        requirements.Should().OnlyContain(x => x.Type == SkillType.TwinPistol);
        requirements.Select(x => x.RequiredRank).OrderBy(x => x).Should().Equal(
            2, 5, 8, 10, 12, 15, 18, 20, 22, 25, 28, 30, 32, 35, 38, 40, 45, 50);
    }

    [Test]
    public void HandlingBonuses_OfferExclusiveSpeedOrDamagePurchasesAtEveryRank()
    {
        var perks = ReadPerks();
        perks[PerkType.MatchedRhythm].Category.Should().Be(PerkCategoryType.TwinPistolBarrage);
        perks[PerkType.PairedHandling].Category.Should().Be(PerkCategoryType.TwinPistolCrossfire);
        var speed = perks[PerkType.MatchedRhythm].PerkLevels.Values.ToArray();
        var damage = perks[PerkType.PairedHandling].PerkLevels.Values.ToArray();
        speed.SelectMany(x => x.StatBonuses).Should().OnlyContain(x => x.Stat == StatType.AttackDelayReductionPercent);
        damage.SelectMany(x => x.StatBonuses).Should().OnlyContain(x => x.Stat == StatType.PairedRangedOrdinaryDamagePercentAdjustment);
        speed.Select(x => x.StatBonuses.Single().Calculate(0)).Should().Equal(3, 6, 8);
        damage.Select(x => x.StatBonuses.Single().Calculate(0)).Should().Equal(10, 20, 25);
        foreach (var (levels, forbidden) in new[] { (speed, PerkType.PairedHandling), (damage, PerkType.MatchedRhythm) })
        foreach (var level in levels)
        {
            var exclusion = level.Requirements.OfType<PerkRequirementCannotHavePerk>().Single();
            typeof(PerkRequirementCannotHavePerk).GetField("_cannotHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(exclusion).Should().Be(forbidden);
        }
        ReadPerks().Values.SelectMany(x => x.PerkLevels.Values).Sum(x => x.Price).Should().Be(120);
        (120 - speed.Sum(x => x.Price)).Should().Be(110, "the handling exclusion limits the legal combined purchase budget");
    }

    [Test]
    public void PassiveStatBonuses_DoNotRequireAnEquippedPair()
    {
        var bonuses = ReadPerks().Values.SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses).ToArray();
        bonuses.Should().NotBeEmpty();
        bonuses.Select(bonus => bonus.Calculate(0)).Should().OnlyContain(value => value > 0,
            "cross-skill stat bonuses apply without inspecting a character's equipment");
    }

    [Test]
    public void Abilities_KeepTheirSharedPistolCooldowns_AndGroundRushAllowsEmptyDestinations()
    {
        var volley = new PairedVolleyAbilityDefinition().BuildAbilities();
        var slide = new BlasterSlideAbilityDefinition().BuildAbilities();
        var focus = new FocusedBurstAbilityDefinition().BuildAbilities();
        var grapple = new GrappleShotAbilityDefinition().BuildAbilities();
        var controlled = new ControlledBurstAbilityDefinition().BuildAbilities();
        var rush = new PistolRushAbilityDefinition().BuildAbilities();
        foreach (var ability in volley.Values.Concat(slide.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.TwinVolley);
            ability.RecastDelay(0).Should().Be(18f);
        }
        foreach (var ability in focus.Values.Concat(grapple.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.DoubleShot);
            ability.RecastDelay(0).Should().Be(24f);
        }
        foreach (var ability in controlled.Values.Concat(rush.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.FanTheHammer);
            ability.RecastDelay(0).Should().Be(30f);
        }
        foreach (var ability in rush.Values)
        {
            ability.RequiresTarget.Should().BeFalse();
            ability.RequiresLocationTarget.Should().BeTrue();
            ability.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Rect);
            ability.Targeting.SizeX.Should().Be(6f);
            ability.Targeting.SizeY.Should().Be(4f);
            ability.MaxRange.Should().Be(6f);
            ability.Targeting.Spell.Should().NotBe(Spell.Invalid);
        }
        rush.Values.Select(x => x.Targeting.Spell).Should().OnlyHaveUniqueItems();
        var all = volley.Values.Concat(slide.Values).Concat(focus.Values).Concat(grapple.Values)
            .Concat(controlled.Values).Concat(rush.Values).ToArray();
        all.Should().OnlyContain(x => x.SkillType == SkillType.TwinPistol && x.IsHostileAbility);
        all.SelectMany(x => x.Requirements).OfType<AbilityRequirementStamina>().Should().HaveCount(all.Length);
        volley.Values.OrderBy(x => x.AbilityLevel).Select(x => x.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM)
            .Should().Equal(3, 5, 8, 10);
    }

    [Test]
    public void Capstones_HaveSeparateMasteryGates_AndShareGlobalFinisherCooldown()
    {
        var perks = ReadPerks();
        var sunsGate = perks[PerkType.TwinSuns].PerkLevels[1].Requirements.OfType<PerkRequirementQuest>().Single();
        var salvoGate = perks[PerkType.FinalSalvo].PerkLevels[1].Requirements.OfType<PerkRequirementQuest>().Single();
        sunsGate.QuestId.Should().NotBe(salvoGate.QuestId);
        var quests = new TwinPistolCapstoneQuestDefinition().BuildQuests();
        quests.Keys.Should().Contain(new[] { sunsGate.QuestId, salvoGate.QuestId });
        foreach (var ability in new TwinSunsAbilityDefinition().BuildAbilities().Values
                     .Concat(new FinalSalvoAbilityDefinition().BuildAbilities().Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.Capstone);
            ability.RecastDelay(0).Should().Be(90f);
            ability.ActivationDelay(0, 0, 1).Should().Be(2f);
            ability.MaxRange.Should().Be(25f);
        }
    }

    [Test]
    public void Stances_AreExclusive_AndSeparateOrdinaryPenaltiesFromActivatedDamage()
    {
        var akimbo = new AkimboStanceStatusEffect();
        var crossfire = new CrossfireStanceStatusEffect();
        akimbo.SourceType.Should().Be(StatusEffectSourceType.Stance);
        crossfire.SourceType.Should().Be(StatusEffectSourceType.Stance);
        akimbo.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(10);
        akimbo.StatGroup.Stats[StatType.OrdinaryAttackHasteRampMaximumStacks].Should().Be(5);
        akimbo.StatGroup.Stats[StatType.OrdinaryAttackDamagePercentAdjustment].Should().Be(-15);
        crossfire.StatGroup.Stats[StatType.OrdinaryAttackDamagePercentAdjustment].Should().Be(-10);
        akimbo.StatGroup.Stats[StatType.PairedRangedOrdinaryDamageMultiplierPercent].Should().Be(0);
        crossfire.StatGroup.Stats[StatType.PairedRangedOrdinaryDamageMultiplierPercent].Should().Be(0);
        crossfire.StatGroup.Stats[StatType.PairedRangedMomentumExtendedDurationSeconds].Should().Be(18);
        akimbo.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        crossfire.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        akimbo.CanApply(0).Should().BeEmpty("the stance has no equipment gate");
        crossfire.CanApply(0).Should().BeEmpty("the stance has no equipment gate");
        var stances = new AkimboStanceAbilityDefinition().BuildAbilities().Values
            .Concat(new CrossfireStanceAbilityDefinition().BuildAbilities().Values);
        stances.Should().OnlyContain(x => x.CustomValidation == null);
        stances.Should().OnlyContain(x => x.RecastDelay(0) == 30f && x.ActivationDelay(0, 0, 1) == 2f);
    }

    private static Dictionary<PerkType, PerkDetail> ReadPerks()
    {
        var definition = new TwinPistolPerkDefinition();
        foreach (var method in typeof(TwinPistolPerkDefinition)
                     .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0 &&
                                      !method.Name.Contains('<')))
            method.Invoke(definition, null);
        var builder = typeof(TwinPistolPerkDefinition).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
    }
}
