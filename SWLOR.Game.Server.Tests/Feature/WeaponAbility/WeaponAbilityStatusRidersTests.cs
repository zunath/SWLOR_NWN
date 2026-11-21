using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>The statuses a landed hit adds to its target, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityStatusRidersTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityStatusRiders _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityStatusRiders>();
        _bench.Leaf<IStatusEffectApplicationService>().Script(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => true);
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    [Test]
    public void ConditionalStatusNeedsARecentRangedDeflection()
    {
        var profile = new WeaponAbilityProfile
        {
            ConditionalStatusEffect = typeof(DisorientedStatusEffect),
            ConditionalStatusDurationSeconds = 10,
            ConditionalStatusAfterDeflectionWindowSeconds = 5
        };
        StatusEffectCategory? categories = null;

        var text = _bench.Run(() => categories = _part.ApplyConditionalStatus(profile, Attacker, Defender, CombatDamageType.Physical));
        categories.Should().BeNull();
        text.Should().NotContain("ApplyStatusEffect");

        _bench.Leaf<ICombatDefenseMemoryService>().Script(nameof(ICombatDefenseMemoryService.HasRecentDeflection), _ => true);
        text = _bench.Run(() => categories = _part.ApplyConditionalStatus(profile, Attacker, Defender, CombatDamageType.Physical));
        categories.Should().Be(new DisorientedStatusEffect().Categories);
        text.Should().Contain("ApplyStatusEffect(Attacker, Defender, DisorientedStatusEffect, 10");
    }

    [Test]
    public void TargetConditionalStatusCanRequireTheAttackerBehindTheTarget()
    {
        var profile = new WeaponAbilityProfile
        {
            ConditionalTargetStatusEffect = typeof(HamstringStatusEffect),
            ConditionalTargetStatusDurationSeconds = 8,
            RequireBehindForConditionalStatus = true
        };
        StatusEffectCategory? categories = null;

        _bench.Run(() => categories = _part.ApplyTargetConditionalStatus(profile, Attacker, Defender, CombatDamageType.Physical));
        categories.Should().BeNull();

        _bench.Leaf<ICombatQueriesService>().Script(nameof(ICombatQueriesService.IsTargetNotFacingAttacker), _ => true);
        var text = _bench.Run(() => categories = _part.ApplyTargetConditionalStatus(profile, Attacker, Defender, CombatDamageType.Physical));
        categories.Should().NotBeNull();
        text.Should().Contain("ApplyStatusEffect(Attacker, Defender, HamstringStatusEffect, 8");
    }

    [Test]
    public void TargetConditionalStatusCanRequireAStatusOnTheTarget()
    {
        var profile = new WeaponAbilityProfile
        {
            ConditionalTargetStatusEffect = typeof(DisorientedStatusEffect),
            ConditionalTargetStatusDurationSeconds = 8,
            RequiredTargetStatusCategoryForConditionalStatus = StatusEffectCategory.Bleeding
        };

        _bench.Run(() => _part.ApplyTargetConditionalStatus(profile, Attacker, Defender, CombatDamageType.Physical))
            .Should().NotContain("ApplyStatusEffect");

        _bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.HasStatusEffectCategory), _ => true);
        _bench.Run(() => _part.ApplyTargetConditionalStatus(profile, Attacker, Defender, CombatDamageType.Physical))
            .Should().Contain("ApplyStatusEffect(Attacker, Defender, DisorientedStatusEffect");
    }

    [Test]
    public void ASpreadReadsTheSourceOnceAndAppliesNothingWhenItHasNoSpreadableStatus()
    {
        var profile = new WeaponAbilityProfile { SpreadBleedFromTarget = true };
        var snapshot = new WeaponAbilityProfile.StatusSpreadSnapshot(1);

        var text = _bench.Run(() =>
        {
            _part.ApplyStatusSpread(profile, Attacker, Defender, CombatDamageType.Physical, snapshot);
            _part.ApplyStatusSpread(profile, Attacker, Defender, CombatDamageType.Physical, snapshot);
        });

        text.Should().NotContain("ApplyStatusEffect");
        text.Split("HasStatusEffectCategory").Length.Should().Be(2, "the snapshot remembers the first read");
    }

    [Test]
    public void SpreadReadsNothingWhenNoSpreadIsConfigured()
    {
        _bench.Run(() => _part.ApplyStatusSpread(new WeaponAbilityProfile(), Attacker, Defender, CombatDamageType.Physical,
            new WeaponAbilityProfile.StatusSpreadSnapshot())).Should().NotContain("HasStatusEffect");
    }

    [Test]
    public void SuppressionAppliesAStackOnHitAndDisorientsAtTheRequiredCount()
    {
        var profile = new WeaponAbilityProfile
        {
            ApplySuppressionStackOnHit = true,
            SuppressionStackDurationSeconds = 10,
            SuppressionStackEvasionPenaltyPercent = 4,
            SuppressionDisorientedRequiredStacks = 3,
            SuppressionDisorientedDurationSeconds = 6
        };

        var text = _bench.Run(() => _part.ApplySuppressionEffects(profile, Attacker, Defender, CombatDamageType.Physical));
        text.Should().Contain("ApplySuppressionStack(Attacker, Defender, 4, 10").And.NotContain("DisorientedStatusEffect");

        _bench.Leaf<ICombatSuppressionService>().Script(nameof(ICombatSuppressionService.GetSuppressionStackCount), _ => 3);
        text = _bench.Run(() => _part.ApplySuppressionEffects(profile, Attacker, Defender, CombatDamageType.Physical));
        text.Should().Contain("ApplyStatusEffect(Attacker, Defender, DisorientedStatusEffect, 6");
    }
}
