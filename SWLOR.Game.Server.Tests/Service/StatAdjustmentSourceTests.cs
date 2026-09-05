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
        var pulses = (IEnumerable<(int, int)>)typeof(Combat).GetMethod("GetAreaAbilityPulses", PrivateStatic)!
            .Invoke(null, new object[] { Creature, ability, true })!;
        pulses.Should().BeEquivalentTo(new[] { (8, 5), (3, 2) });
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
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override string Name => "Test payload";
        public override EffectIconType Icon => EffectIconType.Invalid;
    }
}
