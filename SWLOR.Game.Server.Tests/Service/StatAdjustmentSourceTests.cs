using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class StatAdjustmentSourceTests
{
    private const uint Creature = 61249;
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static Dictionary<uint, CreatureStatusEffect> Effects =>
        (Dictionary<uint, CreatureStatusEffect>)typeof(StatusEffect)
            .GetField("_creatureEffects", PrivateStatic)!.GetValue(null)!;

    [TearDown]
    public void Cleanup()
    {
        Effects.Remove(Creature);
        ((IDictionary)typeof(TemporaryStatModifier).GetField("_modifiers", PrivateStatic)!.GetValue(null)!).Remove(Creature);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingStatusPayloadAndBuffReads_DoNotAllocateTrackersOrEffectSnapshots(bool hasUnrelatedEffects)
    {
        if (hasUnrelatedEffects)
        {
            for (var index = 0; index < 40; index++)
                AddStatus(Payload((StatType.AttackPercentAdjustment, 1)));
        }
        var otherEffectTypes = new HashSet<Type> { typeof(StatusEffectBase) };
        for (var index = 0; index < 100; index++)
        {
            StatusEffect.GetStatSources(Creature, StatType.AreaAbilityPulseDamage);
            StatusEffect.HasAnyActiveEffect(Creature, otherEffectTypes);
            StatusEffect.GetStatAdjustment(Creature, StatType.AreaAbilityPulseDamage);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var matches = 0;
        for (var index = 0; index < 100; index++)
        {
            matches += StatusEffect.GetStatSources(Creature, StatType.AreaAbilityPulseDamage).Count;
            matches += StatusEffect.HasAnyActiveEffect(Creature, otherEffectTypes) ? 1 : 0;
            matches += StatusEffect.GetStatAdjustment(Creature, StatType.AreaAbilityPulseDamage);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        matches.Should().Be(0);
        allocated.Should().BeLessThan(4096, "combat and AI reads must not build empty trackers or copy unrelated effects");
        Effects.ContainsKey(Creature).Should().Be(hasUnrelatedEffects);
    }

    [Test]
    public void StatusSourceSnapshot_AllowsEffectChangesAndExcludesExpiredPayloads()
    {
        var first = Payload((StatType.AreaAbilityPulseDamage, 8));
        var expired = Payload((StatType.AreaAbilityPulseDamage, 4));
        expired.Expire();
        AddStatus(first);
        AddStatus(expired);
        var sources = StatusEffect.GetStatSources(Creature, StatType.AreaAbilityPulseDamage);
        foreach (var source in sources)
        {
            Effects[Creature].Remove(first);
            AddStatus(Payload((StatType.AreaAbilityPulseDamage, 3)));
            source[StatType.AreaAbilityPulseDamage].Should().Be(8);
        }

        sources.Should().ContainSingle();
        StatusEffect.GetStatSources(Creature, StatType.AreaAbilityPulseDamage).Single()[StatType.AreaAbilityPulseDamage].Should().Be(3);
        var types = new HashSet<Type> { typeof(PayloadEffect) };
        StatusEffect.HasAnyActiveEffect(Creature, types).Should().BeTrue();
        foreach (var effect in Effects[Creature].GetAllEffects().Cast<PayloadEffect>())
            effect.Expire();
        StatusEffect.HasAnyActiveEffect(Creature, types).Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingConditionalSources_DoNotAllocatePerHitPipelines(bool hasUnrelatedSources)
    {
        if (hasUnrelatedSources)
        {
            for (var index = 0; index < 40; index++)
            {
                AddStatus(Payload((StatType.AttackPercentAdjustment, 1)));
                AddTemporary($"unrelated-{index}", StatType.AttackPercentAdjustment, 1);
            }
        }
        for (var index = 0; index < 100; index++)
            Combat.GetHighResourceAbilityDamageBonus(Creature);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var adjustment = 0;
        for (var index = 0; index < 1000; index++)
            adjustment += Combat.GetHighResourceAbilityDamageBonus(Creature);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        adjustment.Should().Be(0);
        allocated.Should().BeLessThan(1024, "missing conditional payloads must not allocate iterator or delegate pipelines per target");
    }

    [Test]
    public void TemporarySources_SnapshotOnlyMatchingGroupsBeforeCallersAddModifiers()
    {
        for (var index = 0; index < 40; index++)
            AddTemporary($"unrelated-{index}", StatType.AttackPercentAdjustment, 1);
        AddTemporary("pulse", StatType.AreaAbilityPulseDamage, 8);
        AddTemporary("pulse", StatType.AreaAbilityPulseRadiusMeters, 5);
        var sources = TemporaryStatModifier.GetStatSources(Creature, StatType.AreaAbilityPulseDamage);
        foreach (var source in sources)
        {
            AddTemporary("new-pulse", StatType.AreaAbilityPulseDamage, 2);
            source[StatType.AreaAbilityPulseDamage].Should().Be(8);
            source[StatType.AreaAbilityPulseRadiusMeters].Should().Be(5);
        }
        sources.Should().ContainSingle();
        TemporaryStatModifier.GetStatSources(Creature, StatType.AreaAbilityPulseDamage).Should().HaveCount(2);
    }

    [Test]
    public void ConditionalPayloads_KeepEachStatusAndTemporarySourceThreshold()
    {
        var lower = Payload((StatType.HighFPAndStaminaAbilityDamageBonus, 12),
            (StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 60));
        var higher = Payload((StatType.HighFPAndStaminaAbilityDamageBonus, 20),
            (StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 70));
        AddStatus(lower);
        AddStatus(higher);
        AddTemporary("third-source", StatType.HighFPAndStaminaAbilityDamageBonus, 7);
        AddTemporary("third-source", StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 80);

        ReadDamageSources().Should().BeEquivalentTo(new[] { (12, 60), (20, 70), (7, 80) });
        Effects[Creature].Remove(higher);
        ReadDamageSources().Should().BeEquivalentTo(new[] { (12, 60), (7, 80) });
        Effects[Creature].ConsumeStat(StatType.HighFPAndStaminaAbilityDamageBonus);
        ReadDamageSources().Should().Equal((7, 80));
    }

    [Test]
    public void TemporaryPayloads_DoNotBorrowMissingConditionsFromAnotherGroup()
    {
        AddTemporary("amount-only", StatType.HighFPAndStaminaAbilityDamageBonus, 9);
        AddTemporary("condition-only", StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 40);
        ReadDamageSources().Should().Equal((9, 0));
    }

    [Test]
    public void ExpiredPayload_IsRemovedWithoutChangingTheOtherSource()
    {
        AddTemporary("expired", StatType.HighFPAndStaminaAbilityDamageBonus, 9);
        AddTemporary("expired", StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 40);
        AddTemporary("live", StatType.HighFPAndStaminaAbilityDamageBonus, 2);
        AddTemporary("live", StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 20);
        var modifiers = (IEnumerable)((IDictionary)typeof(TemporaryStatModifier)
            .GetField("_modifiers", PrivateStatic)!.GetValue(null)!)[Creature]!;
        foreach (var modifier in modifiers)
        {
            if ((string)modifier.GetType().GetProperty("Group")!.GetValue(modifier)! == "expired")
                modifier.GetType().GetProperty("Expiration")!.SetValue(modifier, DateTime.UtcNow.AddSeconds(-1));
        }
        // The heartbeat uses this same purge; the native UI notification is covered in engine tests.
        typeof(TemporaryStatModifier).GetMethod("PurgeExpired", PrivateStatic)!.Invoke(null, new object[] { Creature });
        ReadDamageSources().Should().Equal((2, 20));
    }

    [Test]
    public void HasteSources_KeepIndependentStackRulesThresholdsAndCaps()
    {
        AddStatus(Haste(5, 15, 2, false));
        AddStatus(Haste(4, 12, 2, true));
        AddStatus(Haste(3, 6, 5, true));

        var sources = Stat.GetStatSources(Creature, StatType.AreaAbilityHastePerStack).ToArray();
        sources.Select(source => (source[StatType.AreaAbilityHastePerStack],
                source[StatType.AreaAbilityHasteStackMaximumPercent],
                source[StatType.AreaAbilityHasteStackMinimumTargets],
                source[StatType.AreaAbilityHasteStacksPerAdditionalTarget]))
            .Should().BeEquivalentTo(new[] { (5, 15, 2, 0), (4, 12, 2, 1), (3, 6, 5, 1) });
        sources.Select(source => source.GetModifierGroup(StatType.AreaAbilityHastePerStack)).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void RefreshingAStatus_PreservesTheModifierGroupThatOwnsItsStackCap()
    {
        var effect = Haste(5, 15, 2, false);
        AddStatus(effect);
        var before = Stat.GetStatSources(Creature, StatType.AreaAbilityHastePerStack).Single()
            .GetModifierGroup(StatType.AreaAbilityHastePerStack);
        Effects[Creature].Remove(effect);
        AddStatus(Haste(5, 15, 2, false));
        Stat.GetStatSources(Creature, StatType.AreaAbilityHastePerStack).Single()
            .GetModifierGroup(StatType.AreaAbilityHastePerStack).Should().Be(before);
    }

    [Test]
    public void PulseSources_KeepEachDamageAmountWithItsOwnRadius()
    {
        AddStatus(Payload((StatType.AreaAbilityPulseDamage, 8), (StatType.AreaAbilityPulseRadiusMeters, 5)));
        AddStatus(Payload((StatType.AreaAbilityPulseDamage, 3), (StatType.AreaAbilityPulseRadiusMeters, 2)));
        var ability = new AbilityDetail { IsHostileAbility = true, IsAreaAbility = true };
        var pulses = (IReadOnlyList<StatAdjustmentSource>)typeof(Combat).GetMethod("GetAreaAbilityPulseSources", PrivateStatic)!
            .Invoke(null, new object[] { Creature, ability, true })!;
        pulses.Select(source => (source[StatType.AreaAbilityPulseDamage], source[StatType.AreaAbilityPulseRadiusMeters]))
            .Should().BeEquivalentTo(new[] { (8, 5), (3, 2) });
    }

    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, true, false)]
    [TestCase(true, true, true)]
    public void IneligibleOrMissingPulses_DoNotAllocatePerTarget(bool hostile, bool area, bool firstTarget)
    {
        var apply = typeof(Combat).GetMethod("ApplyAreaAbilityPulse", PrivateStatic)!
            .CreateDelegate<Action<uint, uint, AbilityDetail, bool>>();
        var ability = new AbilityDetail { IsHostileAbility = hostile, IsAreaAbility = area };
        for (var index = 0; index < 100; index++)
            apply(Creature, Creature, ability, firstTarget);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++)
            apply(Creature, Creature, ability, firstTarget);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().BeLessThan(1024, "pulse checks must return before creating per-target iterators or cast state");
    }

    private static IEnumerable<(int, int)> ReadDamageSources() =>
        Stat.GetStatSources(Creature, StatType.HighFPAndStaminaAbilityDamageBonus)
            .Select(source => (source[StatType.HighFPAndStaminaAbilityDamageBonus],
                source[StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent]));

    private static PayloadEffect Haste(int amount, int maximum, int threshold, bool perTarget) => Payload(
        (StatType.AreaAbilityHastePerStack, amount),
        (StatType.AreaAbilityHasteStackMaximumPercent, maximum),
        (StatType.AreaAbilityHasteStackMinimumTargets, threshold),
        (StatType.AreaAbilityHasteStackDurationSeconds, 30),
        (StatType.AreaAbilityHasteStacksPerAdditionalTarget, perTarget ? 1 : 0));

    private static PayloadEffect Payload(params (StatType Stat, int Value)[] stats)
    {
        var effect = new PayloadEffect();
        foreach (var (stat, value) in stats)
            effect.StatGroup.Stats[stat] = value;
        return effect;
    }

    private static void AddStatus(IStatusEffect effect)
    {
        if (!Effects.TryGetValue(Creature, out var tracker))
            Effects[Creature] = tracker = new CreatureStatusEffect();
        ((StatusEffectBase)effect).ReassignSource((uint)tracker.GetAllEffects().Count + 1);
        tracker.Add(effect);
    }

    private static void AddTemporary(string group, StatType stat, int amount) =>
        typeof(TemporaryStatModifier).GetMethod("AddInternal", PrivateStatic)!
            .Invoke(null, new object[] { Creature, stat, amount, 30f, group, false });

    private sealed class PayloadEffect : StatusEffectBase
    {
        public void Expire() => IsFlaggedForRemoval = true;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override string Name => "Test payload";
        public override EffectIconType Icon => EffectIconType.Invalid;
    }
}
