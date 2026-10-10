using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

public class TankThreatTests
{
    [TestCase(100, 30, 130)]
    [TestCase(100, 80, 150)]
    [TestCase(100, -80, 50)]
    [TestCase(1, -50, 1)]
    [TestCase(3, 30, 3)]
    [TestCase(-100, 50, -100)]
    [TestCase(-100, -50, -100)]
    [TestCase(int.MaxValue, 50, int.MaxValue)]
    public void GenerationUsesIntegerRoundingAndCannotAmplifyReductions(int amount, int percent, int expected)
        => Enmity.CalculateAdjustedEnmity(amount, percent).Should().Be(expected);

    [TestCase(2000, 20000, 910, 20910)]
    [TestCase(30000, 20000, 910, 30000)]
    [TestCase(20910, 20000, 910, 20910)]
    [TestCase(0, 0, 910, 910)]
    [TestCase(0, int.MaxValue, 910, int.MaxValue)]
    public void TauntRecoversDeficitsWithoutCompoundingItsOwnLead(int current, int rival, int bonus, int expected)
        => Enmity.CalculateTauntEnmity(current, rival, bonus).Should().Be(expected);

    [Test]
    public void TwoTanksCanExchangeAnEnemyAndLoseItToBurstWithoutALock()
    {
        var first = Enmity.CalculateTauntEnmity(1000, 20000, 910);
        var second = Enmity.CalculateTauntEnmity(20000, first, 840);
        second.Should().Be(21750);
        Enmity.CalculateTauntEnmity(second, first, 840).Should().Be(second);
        (first + 841).Should().BeGreaterThan(second);
    }

    [TestCase(100, 900, 1000, 50)]
    [TestCase(1000, 990, 1000, 5)]
    [TestCase(1000, 1000, 1000, 0)]
    [TestCase(1000, 0, 1000, 0)]
    [TestCase(1, 1, 2, 0)]
    [TestCase(20, 100, 90, 0)]
    public void HealingThreatUsesOnlyRestoredHealth(int requested, int before, int after, int expected)
        => Enmity.CalculateHealingEnmity(requested, before, after).Should().Be(expected);

    [TestCase(typeof(BastionStanceStatusEffect), -20, 20)]
    [TestCase(typeof(DefensiveStanceStatusEffect), -20, 30)]
    [TestCase(typeof(ImmovableStanceStatusEffect), -25, 30)]
    [TestCase(typeof(IronWallStanceStatusEffect), -25, 30)]
    [TestCase(typeof(SentinelStanceStatusEffect), -15, 0)]
    public void TankStancesBeatAnOffensiveBuildWithSustainableAreaRotations(Type type, int attackPenalty, int generalBonus)
    {
        var stance = (IStatusEffect)Activator.CreateInstance(type)!;
        stance.ApplyEffect(1, 1, -1);
        var damageBonus = stance.StatGroup.Stats[StatType.DamageEnmityPercentAdjustment];
        damageBonus.Should().Be(100);
        stance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(attackPenalty);

        // 60-second equal-gear fixture: ordinary damage is 100/landed attack at 2s.
        // Offensive comparator has +25% Attack and +15% Haste (Berserker's actual stats).
        // A conservative tank uses two 30s area strikes for 6 STM each; omits Guard,
        // threat passives, Provoke and Fortress Strike. Covers each enemy independently.
        var tankDamage = 100 * (100 + attackPenalty) / 100;
        var tank = 30 * Enmity.CalculateAdjustedEnmity(
            Enmity.CalculateDamageEnmity(tankDamage, damageBonus), generalBonus);
        tank += 2 * (Enmity.CalculateAdjustedEnmity(
            Enmity.CalculateDamageEnmity(tankDamage + 15, damageBonus), generalBonus) +
            Enmity.CalculateAdjustedEnmity(100, generalBonus));
        var damageDealer = 35 * 125 + 2 * (140 + 100);
        var healer = 12 * Enmity.CalculateHealingEnmity(500, 500, 1000);
        tank.Should().BeGreaterThan((int)(damageDealer * 1.10), "the active tank needs at least 10% margin");
        tank.Should().BeGreaterThan(healer);
        tank.Should().BeLessThan(damageDealer * 2, "ordinary damage must remain relevant");
        // This low-use floor remains viable with the revised recovery baseline.
        var carry = 0;
        var recovery = Enumerable.Range(0, 10).Sum(_ => NaturalRegeneration.GetStaminaRegenPerHeartbeat(20, 0, 0, ref carry));
        (2 * 6).Should().BeLessThanOrEqualTo(recovery);
    }

    // Chiro level-50 weapons: Bible Equipment - Weapons rows 34, 66, 111, 345, 439.
    // Equal MGT/VIT 20, skill 50, attack/defense 128; no crits, food or gear regen.
    // Matching offensive build uses Berserker's real +25% Attack and +15% delay reduction.
    [TestCase(typeof(BastionStanceStatusEffect), "HeavyVibroblade.EarthshatterAbilityDefinition", FeatType.Earthshatter1, 44, 300, 20, 1, 20)]
    [TestCase(typeof(DefensiveStanceStatusEffect), "Vibroblade.CoveringStrikeAbilityDefinition", FeatType.CoveringStrike1, 24, 230, 15, 2, 50)]
    [TestCase(typeof(ImmovableStanceStatusEffect), "Lightsaber.GuardiansChallengeAbilityDefinition", FeatType.GuardiansChallenge1, 24, 240, 12, 2, 50)]
    [TestCase(typeof(IronWallStanceStatusEffect), "Katar.GuardCounterAbilityDefinition", FeatType.GuardCounter1, 19, 220, 8, 3, 30)]
    [TestCase(typeof(SentinelStanceStatusEffect), "Staff.LineBreakerAbilityDefinition", FeatType.LineBreaker1, 24, 270, 8, 3, 20)]
    public void RepresentativeGearAndSustainableKitsKeepUsefulThreatMargins(Type stanceType, string definitionName,
        FeatType feat, int weaponDamage, int delay, int abilityDamage, int usesPerMinute, int generalBonus)
    {
        var stance = (IStatusEffect)Activator.CreateInstance(stanceType)!;
        stance.ApplyEffect(1, 1, -1);
        var definitionType = typeof(IAbilityListDefinition).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AbilityDefinition." + definitionName)!;
        var ability = ((IAbilityListDefinition)Activator.CreateInstance(definitionType)!).BuildAbilities()[feat];
        var stamina = ability.Requirements.OfType<AbilityRequirementStamina>().Sum(requirement => requirement.RequiredSTM);
        (stamina * usesPerMinute).Should().BeLessThanOrEqualTo(12, "MGT 20 supplies 12 STM/min without gear or consumables");
        (usesPerMinute * ability.RecastDelay(1)).Should().BeLessThanOrEqualTo(60);
        var attack = Stat.GetAttack(50, 20, 0);
        var tankAttack = attack * (100 + stance.StatGroup.Stats[StatType.AttackPercentAdjustment]) / 100;
        var offenseAttack = attack * 125 / 100;
        double Damage(int value, int power)
        {
            var (minimum, maximum) = Combat.CalculateDamageRange(power, value, 20, attack, 20, 0);
            return (minimum + maximum) / 2.0;
        }
        double Attacks(int haste) => 60000.0 / Combat.CalculateEffectiveAttackDelay(
            Combat.CalculateAttackDelayMilliseconds(delay, 0, haste, 0));
        // Equal hit rate is factored into both sides. Reprisals and critical bonuses
        // are excluded; Staff includes its +20% Sentinel Guard protection rider,
        // Vibroblade includes Covering Strike's source bonus, Lightsaber its hit condition.
        var landed = Combat.CalculateHitRate(100, 100, 0) / 100.0;
        double Threat(int power, int damageMultiplier, int percent, int haste, bool oldDuplicate, AbilityDetail rotation, int bonusDamage, double uses)
        {
            var normal = Damage(weaponDamage, power);
            var activated = Damage(weaponDamage + bonusDamage, power);
            // Guard Counter replaces a normal attack; casted area strikes add an impact.
            var normalAttacks = Attacks(haste) - (rotation.ActivationType == AbilityActivationType.Weapon ? uses : 0);
            return landed * (normalAttacks * (normal * damageMultiplier + 1) +
                uses * (100 + activated * (damageMultiplier + (oldDuplicate ? 1 : 0)))) * (100 + percent) / 100.0;
        }
        var (offensiveDefinition, offensiveFeat, offensiveDamage, offensiveUses) = weaponDamage == 44
            ? ("HeavyVibroblade.SoulStrikeAbilityDefinition", FeatType.SoulStrike1, 15, 2.5)
            : delay == 230 ? ("Vibroblade.RendingStrikeAbilityDefinition", FeatType.RendingStrike1, 18, 60.0 / 45)
            : delay == 240 ? ("Lightsaber.ShatteringStrikeAbilityDefinition", FeatType.ShatteringStrike1, 18, 2.5)
            : delay == 220 ? ("Katar.HookingStrikeAbilityDefinition", FeatType.HookingStrike1, 8, 60.0 / 18)
            : ("Staff.SlamAbilityDefinition", FeatType.Slam1, 16, 60.0 / 18);
        var offensiveType = typeof(IAbilityListDefinition).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AbilityDefinition." + offensiveDefinition)!;
        var offensiveAbility = ((IAbilityListDefinition)Activator.CreateInstance(offensiveType)!).BuildAbilities()[offensiveFeat];
        (offensiveAbility.Requirements.OfType<AbilityRequirementStamina>().Sum(cost => cost.RequiredSTM) * offensiveUses)
            .Should().BeLessThanOrEqualTo(12);
        (offensiveUses * offensiveAbility.RecastDelay(1)).Should().BeLessThanOrEqualTo(60.01);
        // Existing intended-kit riders: Heavy Defense +150 on its one Earthshatter;
        // Iron Guard I expects three Guards from twenty landed physical hits at 80 damage.
        // No Guard pulse, counter proc, Fortress Strike or taunt is needed by this fixture.
        var kitThreat = (weaponDamage == 44 ? 150 * landed : delay == 220 ? 3 * 80 : 0) * (100 + generalBonus) / 100.0;
        var before = Threat(tankAttack, 1, generalBonus, 0, true, ability, abilityDamage, usesPerMinute) + kitThreat;
        var after = Threat(tankAttack, 2, generalBonus, 0, false, ability, abilityDamage, usesPerMinute) + kitThreat;
        var offense = Threat(offenseAttack, 1, 0, 15, false, offensiveAbility, offensiveDamage, offensiveUses);
        var previousOffense = Threat(offenseAttack, 1, 0, 15, true, offensiveAbility, offensiveDamage, offensiveUses);
        after.Should().BeGreaterThan(offense * 1.10, "active comparable tank kit needs at least 10% sustained margin");
        after.Should().BeLessThan(offense * 2, "burst and gear advantages must remain meaningful");
        after.Should().BeGreaterThan(12 * Enmity.CalculateHealingEnmity(100, 900, 1000));
        // Stopping participation cannot hold against a continuing attacker; this rate has
        // no passive taunt or free per-tick generation, and catch-up is tested separately.
        TestContext.WriteLine($"{stanceType.Name}: old={before:F0}/min new={after:F0}/min oldDPS={previousOffense:F0}/min DPS={offense:F0}/min margin={after / offense - 1:P0} STM={stamina * usesPerMinute}/min");

        // PR #2464 permits both builds to use BOTH actual low-rank attacks on cooldown.
        // Include enemy-specific Covering/Guardian bonuses for the offensive build too.
        // Five-minute comparison, with no cost discounts, refunds, food or support.
        foreach (var might in new[] { 20, 26, 27 })
        {
            var remainder = 0;
            var recovery = Enumerable.Range(0, 10).Sum(_ => NaturalRegeneration.GetStaminaRegenPerHeartbeat(might, 0, 0, ref remainder));
            recovery.Should().Be(might == 20 ? 30 : 32);
            var primaryRate = 60 / ability.RecastDelay(1);
            var secondaryRate = 60 / offensiveAbility.RecastDelay(1);
            var secondaryCost = offensiveAbility.Requirements.OfType<AbilityRequirementStamina>().Sum(cost => cost.RequiredSTM);
            var spending = primaryRate * stamina + secondaryRate * secondaryCost;
            spending.Should().BeLessThanOrEqualTo(recovery);
            var enemyDefense = Stat.GetAttack(50, might, 0);
            double Rotation(int multiplier, int penalty, int haste, int percent, bool duplicate)
            {
                var power = enemyDefense * (100 + penalty) / 100;
                double Hit(int bonus)
                {
                    var (low, high) = Combat.CalculateDamageRange(power, weaponDamage + bonus, 20, enemyDefense, 20, 0);
                    return (low + high) / 2.0;
                }
                var normal = Hit(0);
                var total = Attacks(haste) * (normal * multiplier + 1);
                foreach (var (action, bonus, rate) in new[] { (ability, abilityDamage, primaryRate), (offensiveAbility, offensiveDamage, secondaryRate) })
                {
                    if (action.ActivationType == AbilityActivationType.Weapon)
                        total -= rate * (normal * multiplier + 1);
                    total += rate * (100 + Hit(bonus) * (multiplier + (duplicate ? 1 : 0)));
                }
                return total * landed * (100 + percent) / 100.0;
            }
            var offenseGeneral = delay == 230 ? 25 : delay == 240 ? 20 : 0;
            var protection = kitThreat * (weaponDamage == 44 ? primaryRate / usesPerMinute : 1);
            var revisedTank = Rotation(2, stance.StatGroup.Stats[StatType.AttackPercentAdjustment], 0, generalBonus, false) + protection;
            var revisedOffense = Rotation(1, 25, 15, offenseGeneral, false);
            var oldTank = Rotation(1, stance.StatGroup.Stats[StatType.AttackPercentAdjustment], 0, generalBonus, true) + protection;
            var oldOffense = Rotation(1, 25, 15, offenseGeneral, true);
            revisedTank.Should().BeGreaterThan(revisedOffense * 1.10, "both builds can spend the newly available stamina");
            revisedTank.Should().BeLessThan(revisedOffense * 2);
            // Opening burst pays actual costs from the unchanged pool with ten STM reserved.
            var maximum = Stat.GetMaxStamina(Stat.BaseSTM, might, 0);
            var current = maximum;
            remainder = 0;
            for (var second = 0; second < 300; second++)
            {
                if (second > 0 && second % 6 == 0)
                    current = Math.Min(maximum, current + NaturalRegeneration.GetStaminaRegenPerHeartbeat(might, 0, 0, ref remainder));
                foreach (var action in new[] { ability, offensiveAbility })
                    if (second % (int)action.RecastDelay(1) == 0)
                        current -= action.Requirements.OfType<AbilityRequirementStamina>().Sum(cost => cost.RequiredSTM);
                current.Should().BeGreaterThanOrEqualTo(10, "opening and five-minute rotation retain a role reserve without refunds");
            }
            // The Force build also funds all casts from WIL20's unchanged 70 FP pool
            // and 12 FP/minute recovery; these rates are not claimed indefinite in FP.
            var forceSpending = (ability.Requirements.OfType<AbilityRequirementFP>().Sum(cost => cost.RequiredFP) * primaryRate +
                offensiveAbility.Requirements.OfType<AbilityRequirementFP>().Sum(cost => cost.RequiredFP) * secondaryRate) * 5;
            forceSpending.Should().BeLessThanOrEqualTo(70 + 12 * 5);
            TestContext.WriteLine($"New stamina MGT{might} {stanceType.Name}: tank {oldTank:F0}->{revisedTank:F0}/min DPS {oldOffense:F0}->{revisedOffense:F0}/min margin {revisedTank / revisedOffense - 1:P0}; STM {spending:F1}/{recovery}/min; 300s reserve >=10");
        }
    }

    [Test]
    public void CompanionStancesKeepThreatOnTheCompanionAndPreserveOffensiveTradeoff()
    {
        var guarding = new GuardingBondStanceBeastStatusEffect();
        var predatory = new PredatoryBondStanceBeastStatusEffect();
        var tank = Enmity.CalculateAdjustedEnmity(Enmity.CalculateDamageEnmity(100,
            guarding.StatGroup.Stats[StatType.DamageEnmityPercentAdjustment]), 50 + 45);
        var offense = Enmity.CalculateAdjustedEnmity(125,
            predatory.StatGroup.Stats[StatType.EnmityPercentAdjustment]);
        tank.Should().Be(225);
        offense.Should().Be(75);
        new GuardingBondStanceStatusEffect().StatGroup.Stats[StatType.DamageEnmityPercentAdjustment].Should().Be(0);
    }
}
