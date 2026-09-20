using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// A selector stat holds a <see cref="SkillType"/> id, not a magnitude. Two sources that both
/// name the same skill must each keep their own selector: aggregating them first produces an id
/// that names a different skill or no skill at all, which silently disables every contributing
/// bonus.
/// </summary>
public class SkillSelectorStackingTests
{
    [Test]
    public void TwoForceAccuracyBuffs_BothApplyToForceAbilitiesAndNothingElse()
    {
        var sources = new[]
        {
            StatusSource(new CruelMomentumStatusEffect()),
            StatusSource(new ForceConvergenceStatusEffect())
        };

        Sum(sources, SkillType.Force).Should().Be(10,
            "Cruel Momentum and Force Convergence each grant +5% Force ability accuracy");

        // (int)SkillType.Force + (int)SkillType.Force == (int)SkillType.Fabrication.
        Sum(sources, SkillType.Fabrication).Should().Be(0,
            "summing two Force selectors must not redirect the bonus to Fabrication");
    }

    [Test]
    public void PistolAndDeviceCriticalBuffs_StayOnTheirOwnSkills()
    {
        var sources = new[]
        {
            StatusSource(new GunslingerFocusStatusEffect()),
            StatusSource(new StormcoreMatrixStatusEffect())
        };

        SumCritical(sources, SkillType.Pistol).Should().Be(
            new GunslingerFocusStatusEffect().StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustment]);
        SumCritical(sources, SkillType.Devices).Should().Be(8);
        SumCritical(sources, SkillType.Rifle).Should().Be(0,
            "Pistol 45 plus Devices 33 must not resolve to an unrelated or undefined skill id");
    }

    [Test]
    public void TwoDeviceAccuracyBuffs_DoNotCancelEachOther()
    {
        var sources = new[]
        {
            StatusSource(new TacticalUplinkStatusEffect()),
            StatusSource(new StormcoreMatrixStatusEffect())
        };

        Sum(sources, SkillType.Devices).Should().Be(13,
            "two Devices selectors would otherwise aggregate to an undefined skill id and grant nothing");
    }

    [Test]
    public void FlurryBleedAndRicochetToss_KeepTheThrowingSelectorForBothPayloads()
    {
        var flurryBleed = PerkSource<ThrowingPerkDefinition>("FlurryBleed", PerkType.FlurryBleed, 3);
        var ricochetToss = PerkSource<ThrowingPerkDefinition>("RicochetToss", PerkType.RicochetToss, 1);
        var sources = new[] { flurryBleed, ricochetToss };

        Combat.SumSkillSelectedStatAdjustment(
                sources,
                SkillType.Throwing,
                StatType.AbilityDamageToBleedingTargetSkillType,
                StatType.AbilityDamageToBleedingTargetBonus)
            .Should().Be(12, "Flurry Bleed III still grants its authored +12 DMG alongside Ricochet Toss");

        Combat.SumSkillSelectedStatAdjustment(
                sources,
                SkillType.Throwing,
                StatType.AbilityDamageToBleedingTargetSkillType,
                StatType.BleedingTargetAbilityBleedDurationExtensionSeconds)
            .Should().Be(6, "the authored Bleed refresh stays at 6 seconds");

        foreach (var skill in new[] { SkillType.Vibroblade, SkillType.Pistol, SkillType.Force })
        {
            Combat.SumSkillSelectedStatAdjustment(
                    sources,
                    skill,
                    StatType.AbilityDamageToBleedingTargetSkillType,
                    StatType.AbilityDamageToBleedingTargetBonus)
                .Should().Be(0, $"the Throwing payload must not leak onto {skill}");
        }

        // Ricochet Toss gates its own splash on the same selector.
        Combat.CanTriggerBleedingTargetAbilitySplash(ricochetToss, SkillType.Throwing, true, 25)
            .Should().BeTrue();
    }

    [Test]
    public void SkillSelectorStats_AreNeverReadAsAnAggregate()
    {
        var combat = ReadCombatSource();
        foreach (var selector in new[]
                 {
                     nameof(StatType.AbilityHitChancePercentAdjustmentSkillType),
                     nameof(StatType.AbilityCriticalRatePercentAdjustmentSkillType),
                     nameof(StatType.IncomingAbilityHitChancePercentAdjustmentSkillType),
                     nameof(StatType.AbilityDamageToBleedingTargetSkillType)
                 })
        {
            combat.Should().NotContain($"GetStatAdjustment(attacker, StatType.{selector})",
                $"{selector} holds a skill id and must be resolved per source");
            combat.Should().NotContain($"GetStatAdjustment(defender, StatType.{selector})",
                $"{selector} holds a skill id and must be resolved per source");
            combat.Should().NotContain($"GetStatAdjustment(creature, StatType.{selector})",
                $"{selector} holds a skill id and must be resolved per source");
        }

        // The aggregate read is only legitimate where a single trigger writes both halves.
        combat.Should().Contain("SumSkillSelectedStatAdjustment(");
    }

    private static string ReadCombatSource()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");

        return File.ReadAllText(Path.Combine(directory.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
    }

    private static int Sum(IReadOnlyList<StatAdjustmentSource> sources, SkillType skillType) =>
        Combat.SumSkillSelectedStatAdjustment(
            sources,
            skillType,
            StatType.AbilityHitChancePercentAdjustmentSkillType,
            StatType.AbilityHitChancePercentAdjustment);

    private static int SumCritical(IReadOnlyList<StatAdjustmentSource> sources, SkillType skillType) =>
        Combat.SumSkillSelectedStatAdjustment(
            sources,
            skillType,
            StatType.AbilityCriticalRatePercentAdjustmentSkillType,
            StatType.AbilityCriticalRatePercentAdjustment);

    private static StatAdjustmentSource StatusSource(StatusEffectBase statusEffect) =>
        new(statusEffect.Name, statusEffect.StatGroup.Stats.ToDictionary(x => x.Key, x => x.Value));

    private static StatAdjustmentSource PerkSource<T>(string method, PerkType type, int level) where T : new()
    {
        var definition = new T();
        typeof(T).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(definition, null);
        var builder = typeof(T).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
        var stats = new Dictionary<StatType, int>();
        foreach (var bonus in perks[type].PerkLevels[level].StatBonuses)
            stats[bonus.Stat] = Stat.AggregateStatAdjustment(
                bonus.Stat,
                stats.GetValueOrDefault(bonus.Stat),
                bonus.Calculate(0));

        return new StatAdjustmentSource(type.ToString(), stats);
    }
}
