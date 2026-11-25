using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability damage bonus part of <c>Combat</c>: the base bonus of an impact (the skill traits, the first hit, direct, guarded and high
/// resource bonuses), the high resource bonus and the costly ability bonus.
/// </summary>
[TestFixture]
public class CombatAbilityDamageBonusServiceTests : CombatAbilityImpactTestBase
{
    protected override object PrivateTarget => _damageBonus;

    [Test]
    public void TheBaseDamageBonusIsZeroWithoutAnAbilityOrAValidActivator()
    {
        Stat(StatType.LightsaberOffenseAreaDamageBonus, 5);
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, null!, SkillType.Lightsaber).Should().Be(0);
        _invalid.Add(Attacker);
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber, true), SkillType.Lightsaber).Should().Be(0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AHostileImpactAddsItsSkillBonusThenTheFirstHitDirectGuardedAndHighResourceBonusesInThatOrder()
    {
        Stat(StatType.StaffCrusherFinisherDamageBonus, 3);
        _firstHitBonus = 5;
        _directBonus = 7;
        _guardedBonus = 11;
        _highResource = 13;
        var ability = Hostile(SkillType.Staff);

        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, ability, SkillType.Staff).Should().Be(3 + 5 + 7 + 11 + 13);
        _log.Should().Equal(
            $"FirstHitBonus {Attacker}",
            $"DirectBonus {Attacker} {Defender}",
            $"GuardedBonus {Attacker} {SkillType.Staff}",
            $"HighResource {Attacker} {StatType.HighFPAndStaminaAbilityDamageBonus} {StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent} Staff");
    }

    [Test]
    public void ANonHostileImpactAddsOnlyTheGuardedHitBonus()
    {
        Stat(StatType.StaffCrusherFinisherDamageBonus, 3);
        _firstHitBonus = 5;
        _guardedBonus = 11;
        _highResource = 13;
        var ability = new AbilityDetail { SkillType = SkillType.Staff };

        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, ability, SkillType.Staff).Should().Be(11);
        _log.Should().Equal($"GuardedBonus {Attacker} {SkillType.Staff}");
    }

    [Test]
    public void ALightsaberImpactAddsTheAreaBonusAndTheDebuffedTargetBonusForASingleTarget()
    {
        Stat(StatType.LightsaberOffenseAreaDamageBonus, 4);
        Stat(StatType.LightsaberOffenseDebuffedTargetDamageBonus, 6);
        _categories.Add((Defender, StatusEffectCategory.Debuff));

        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber, true), SkillType.Lightsaber).Should().Be(4);
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber).Should().Be(6);
        _categories.Clear();
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber).Should().Be(0);
        _categories.Add((Defender, StatusEffectCategory.Debuff));
        _invalid.Add(Defender);
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Lightsaber), SkillType.Lightsaber).Should().Be(0);
    }

    [TestCase(0, 0)]
    [TestCase(1, 10)]
    [TestCase(2, 22)]
    [TestCase(3, 22)]
    public void AVibroknifeImpactAddsTheToxicCoatingBonusByRank(int rank, int bonus)
    {
        Stat(StatType.VibroknifeSaboteurToxicCoatingRank, rank);
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Vibroknife), SkillType.Vibroknife).Should().Be(bonus);
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife)
            .Should().Be(0, "only a hostile ability is coated");
    }

    [Test]
    public void ASaberstaffImpactAddsTheConduitFlareBonus()
    {
        Stat(StatType.SaberstaffConduitFlareDamageBonus, 8);
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.Saberstaff), SkillType.Saberstaff).Should().Be(8);
    }

    [Test]
    public void ATwinBladeImpactConsumesTheReversalCutBonusOnlyWhenTheAbilityTriggersIt()
    {
        _temporary[StatType.TwinBladeDuelistReversalCutDamageBonus] = 9;
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.TwinBlade), SkillType.TwinBlade).Should().Be(0);
        Lines("Consume").Should().BeEmpty();

        _reversalCut = true;
        _damageBonus.GetAbilityImpactBaseDamageBonus(Attacker, Defender, Hostile(SkillType.TwinBlade), SkillType.TwinBlade).Should().Be(9);
        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.TwinBladeDuelistReversalCutDamageBonus} {StatType.TwinBladeDuelistReversalCut}");
    }

    [Test]
    public void TheHighResourceBonusAsksTheResourcesPartWithTheAbilityOrNone()
    {
        _highResource = 4;
        _damageBonus.GetHighResourceAbilityDamageBonus(Attacker, Hostile(SkillType.Pistol)).Should().Be(4);
        _damageBonus.GetHighResourceAbilityDamageBonus(Attacker).Should().Be(4);
        _log.Should().Equal(
            $"HighResource {Attacker} {StatType.HighFPAndStaminaAbilityDamageBonus} {StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent} Pistol",
            $"HighResource {Attacker} {StatType.HighFPAndStaminaAbilityDamageBonus} {StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent} null");
    }

    [Test]
    public void TheCostlyAbilityBonusNeedsAPaidCostAtLeastTheMinimumForTheSelectedSkill()
    {
        var ability = Hostile();
        Stat(StatType.CostlyAbilityDamageBonus, 12);
        _damageBonus.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0, "nothing was paid");

        _costState = new CombatAbilityStaminaCostState { Cost = 8 };
        _damageBonus.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0, "no minimum is set");

        Stat(StatType.CostlyAbilityDamageMinimumStaminaCost, 10);
        _damageBonus.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0, "the cost is below the minimum");

        _costState = new CombatAbilityStaminaCostState { Cost = 10 };
        _damageBonus.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(12, "any skill when none is selected");

        Stat(StatType.CostlyAbilityDamageBonusSkillType, (int)SkillType.Pistol);
        _damageBonus.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Rifle).Should().Be(0);
        _damageBonus.GetCostlyAbilityDamageBonus(Attacker, ability, SkillType.Pistol).Should().Be(12);
    }
}
