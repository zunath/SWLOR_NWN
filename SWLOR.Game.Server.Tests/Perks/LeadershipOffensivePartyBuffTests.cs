using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LeadershipOffensivePartyBuffTests
{
    private static readonly Func<AbilityDetail, int, bool, bool> IsSuccessfulImpact = typeof(Combat)
        .GetMethod("IsSuccessfulHostileAbilityImpact", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Func<AbilityDetail, int, bool, bool>>();

    [TestCase(SkillType.Leadership)]
    [TestCase(SkillType.Rifle)]
    [TestCase(SkillType.Force)]
    [TestCase(SkillType.Mimicry)]
    [TestCase(SkillType.BeastMastery)]
    public void SuccessfulHostileImpact_QualifiesAcrossSkills(SkillType skill)
    {
        var ability = new AbilityDetail { SkillType = skill, IsHostileAbility = true };
        IsSuccessfulImpact(ability, 1, false).Should().BeTrue();
        IsSuccessfulImpact(ability, 0, true).Should().BeTrue();
    }

    [TestCase(true, 0, false)]
    [TestCase(true, -1, false)]
    [TestCase(false, 100, false)]
    [TestCase(false, 0, true)]
    public void FailedOrFriendlyImpact_DoesNotQualify(bool hostile, int damage, bool statusApplied)
    {
        IsSuccessfulImpact(new AbilityDetail { IsHostileAbility = hostile }, damage, statusApplied)
            .Should().BeFalse();
        IsSuccessfulImpact(null!, damage, statusApplied).Should().BeFalse();
    }

    [Test]
    public void FailedFirstTarget_DoesNotConsumeTheCastTrigger()
    {
        var ability = new AbilityDetail { IsHostileAbility = true, IsAreaAbility = true };
        var sequence = new AbilityImpactSequence();
        bool Trigger(int damage, bool statusApplied) =>
            IsSuccessfulImpact(ability, damage, statusApplied) && sequence.TryTriggerPartyBuff();

        Trigger(0, false).Should().BeFalse();
        Trigger(0, true).Should().BeTrue();
        Trigger(100, false).Should().BeFalse("all remaining targets and delayed phases share the cast trigger");
        new AbilityImpactSequence().TryTriggerPartyBuff().Should().BeTrue("the next cast has its own trigger");
    }

    [Test]
    public void SharedCooldown_BlocksOtherSkillsAndReopensAfter45Seconds()
    {
        var useTrigger = typeof(Combat).GetMethod("TryUseStatTrigger", BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(uint), typeof(StatType), typeof(int) }, null)!
            .CreateDelegate<Func<uint, StatType, int, bool>>();
        var cooldowns = (Dictionary<(uint, StatType), DateTime>)typeof(Combat)
            .GetField("_statTriggerCooldowns", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var key = (0xFFFFFFFEu, StatType.HostileAbilityPartyBuffDamagePercent);
        var otherKey = (0xFFFFFFFDu, key.Item2);
        cooldowns.Remove(key);
        cooldowns.Remove(otherKey);
        try
        {
            var before = DateTime.UtcNow;
            useTrigger(key.Item1, key.Item2, 45).Should().BeTrue();
            cooldowns[key].Should().BeOnOrAfter(before.AddSeconds(45));
            useTrigger(key.Item1, key.Item2, 45).Should().BeFalse();
            useTrigger(otherKey.Item1, otherKey.Item2, 45).Should().BeTrue("each leader has a separate cooldown");
            cooldowns[key] = DateTime.UtcNow.AddMilliseconds(-1);
            useTrigger(key.Item1, key.Item2, 45).Should().BeTrue();
        }
        finally
        {
            cooldowns.Remove(key);
            cooldowns.Remove(otherKey);
        }
    }

    [TestCase(1, 8, 10, 0, 0, EffectIconType.MarkTarget1StatusEffect, 16780505)]
    [TestCase(2, 12, 15, 10, 12, EffectIconType.MarkTarget2StatusEffect, 16780507)]
    public void MarkTarget_DeclaresBothRanksAndSharedCooldown(int rank, int damage, int maximumDamage,
        int accuracy, int maximumAccuracy, EffectIconType icon, int nameStrRef)
    {
        var definition = new LeadershipVanguardCommandPerkDefinition();
        typeof(LeadershipVanguardCommandPerkDefinition).GetMethod("MarkTarget", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(definition, null);
        var builder = typeof(LeadershipVanguardCommandPerkDefinition)
            .GetField("_builder", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(definition)!;
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;
        var level = perks[PerkType.MarkTarget].PerkLevels[rank];
        int Bonus(StatType stat) => level.StatBonuses.Where(x => x.Stat == stat).Sum(x => x.Calculate(0));
        Bonus(StatType.HostileAbilityPartyBuffDamagePercent).Should().Be(damage);
        Bonus(StatType.HostileAbilityPartyBuffMaximumDamagePercent).Should().Be(maximumDamage);
        Bonus(StatType.HostileAbilityPartyBuffAccuracyPercent).Should().Be(accuracy);
        Bonus(StatType.HostileAbilityPartyBuffMaximumAccuracyPercent).Should().Be(maximumAccuracy);
        Bonus(StatType.HostileAbilityPartyBuffDurationSeconds).Should().Be(30);
        Bonus(StatType.HostileAbilityPartyBuffCooldownSeconds).Should().Be(45);
        Bonus(StatType.HostileAbilityPartyBuffIcon).Should().Be((int)icon);
        Bonus(StatType.HostileAbilityPartyBuffNameStrRef).Should().Be(nameStrRef);
        level.Description.Should().Contain("from any skill").And.Contain("once every 45 seconds");
        Stat.GetStatTypeAggregation(StatType.HostileAbilityPartyBuffCooldownSeconds)
            .Should().Be(StatTypeAggregation.Maximum);
    }

    [TestCase(8, 0, EffectIconType.MarkTarget1StatusEffect)]
    [TestCase(10, 0, EffectIconType.MarkTarget1StatusEffect)]
    [TestCase(12, 10, EffectIconType.MarkTarget2StatusEffect)]
    [TestCase(15, 12, EffectIconType.MarkTarget2StatusEffect)]
    public void PartyBuff_PreservesConfiguredStatsWhenClonedAndStacksWithPressTheAttack(int damage, int accuracy,
        EffectIconType icon)
    {
        var effect = new HostileAbilityPartyBuffStatusEffect(damage, accuracy, 16780505, icon);
        effect.CanApply(0).Should().BeEmpty();
        var clone = effect.Clone();
        clone.Icon.Should().Be(icon);
        clone.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(damage);
        clone.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(accuracy);
        var tracker = new CreatureStatusEffect();
        tracker.Add(clone);
        var press = new PressTheAttack1StatusEffect();
        press.ApplyEffect(0, 0, -1);
        tracker.Add(press);
        tracker.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(damage + 6);
        new HostileAbilityPartyBuffStatusEffect().CanApply(0).Should().NotBeEmpty();
    }

    [Test]
    public void SharedImpactAndDirectDebuffPaths_UseTheSamePartyBuffGate()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var combat = File.ReadAllText(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var impact = combat[combat.IndexOf("public static void ApplySuccessfulAbilityImpactRiders(", StringComparison.Ordinal)..];
        impact = impact[..impact.IndexOf("private static void ApplyCostlyAbilityHitEffects(", StringComparison.Ordinal)];
        impact.Should().Contain("ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);");
        var buff = combat[combat.IndexOf("public static void ApplyHostileAbilityPartyBuff(", StringComparison.Ordinal)..];
        buff = buff[..buff.IndexOf("private static void ApplyPistolSkirmisherImpactRiders(", StringComparison.Ordinal)];
        buff.Should().NotContain("SkillType.").And.NotContain("PerkType.");
        buff.Should().Contain("IsSuccessfulHostileAbilityImpact(ability, damage, statusApplied)");
        buff.Should().Contain("TryUseStatTrigger(activator, StatType.HostileAbilityPartyBuffDamagePercent, cooldown)");
        buff.Should().Contain("sequence.TryTriggerPartyBuff()");
        var statuses = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        statuses.Should().Contain("source != creature && GetIsReactionTypeHostile(creature, source)");
        statuses.Should().Contain("Combat.ApplyHostileAbilityPartyBuff(source, Ability.GetActiveAbilityImpactSummary(source)?.Ability, 0, true);");
    }
}
