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
    public void ASelectorlessAccuracySource_AppliesToEverySkill()
    {
        // Eclipse of Resolve, Alpha Rhythm and Improved Attentiveness are all authored as
        // affecting every ability, and none of them declares a skill of its own.
        var eclipse = StatusSource(new EclipseOfResolve1StatusEffect());
        var forceConvergence = StatusSource(new ForceConvergenceStatusEffect());

        Sum(new[] { eclipse }, SkillType.Force).Should().Be(-15);
        Sum(new[] { eclipse }, SkillType.Vibroblade).Should().Be(-15,
            "a source with no selector is not scoped to one skill");

        Sum(new[] { eclipse, forceConvergence }, SkillType.Force).Should().Be(-10,
            "the global debuff and the Force-scoped buff both apply to a Force ability");
        Sum(new[] { eclipse, forceConvergence }, SkillType.Vibroblade).Should().Be(-15,
            "only the global debuff reaches a non-Force ability");
    }

    [Test]
    public void ASelectorlessBleedingSource_DoesNotLeakOntoEverySkill()
    {
        // Here the selector is the scope: "thrown abilities against bleeding targets". A source
        // that failed to declare it must grant nothing rather than apply to every weapon.
        var unscoped = new StatAdjustmentSource(
            "unscoped",
            new Dictionary<StatType, int> { [StatType.AbilityDamageToBleedingTargetBonus] = 12 });

        foreach (var skill in new[] { SkillType.Throwing, SkillType.Vibroblade })
        {
            Combat.SumSkillSelectedStatAdjustment(
                    new[] { unscoped },
                    skill,
                    StatType.AbilityDamageToBleedingTargetSkillType,
                    StatType.AbilityDamageToBleedingTargetBonus)
                .Should().Be(0, $"the thrown payload is scoped by its selector, so it must not reach {skill}");
        }
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

        // Ability hit chance and critical rate both have selectorless sources that are authored
        // as global, so those call sites must opt into the global fallback.
        var hitOrCritical = ExtractMethod(combat, "private static int GetAbilityHitOrCriticalAdjustment(");
        hitOrCritical.Should().Contain("unselectedSourcesApplyToEverySkill: true",
            "Eclipse of Resolve, Alpha Rhythm and Improved Attentiveness declare no skill of their own");

        var incoming = ExtractMethod(combat, "private static int GetIncomingAbilityHitChanceAdjustment(");
        incoming.Should().Contain("unselectedSourcesApplyToEverySkill: true");

        // The thrown bleeding payloads are scoped by their selector and must stay strict.
        var bleedRefresh = ExtractMethod(combat, "private static void ApplyBleedingTargetAbilityBleedRefresh(");
        bleedRefresh.Should().NotContain("unselectedSourcesApplyToEverySkill");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1, $"'{signature}' must exist");
        var depth = 0;
        for (var index = source.IndexOf('{', start); index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"Unbalanced braces after '{signature}'.");
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
            StatType.AbilityHitChancePercentAdjustment,
            unselectedSourcesApplyToEverySkill: true);

    private static int SumCritical(IReadOnlyList<StatAdjustmentSource> sources, SkillType skillType) =>
        Combat.SumSkillSelectedStatAdjustment(
            sources,
            skillType,
            StatType.AbilityCriticalRatePercentAdjustmentSkillType,
            StatType.AbilityCriticalRatePercentAdjustment,
            unselectedSourcesApplyToEverySkill: true);

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
