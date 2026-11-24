using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The dealt effects part of <c>Combat</c>: the steps a landed direct hit grants the attacker, and the tracking of a hit that deals damage.
/// </summary>
[TestFixture]
public class CombatDealtEffectsServiceTests : CombatHitEffectsTestBase
{
    [Test]
    public void ADirectHostileHitIsRecordedForTheTargetsTraitAfterNativeMitigation()
    {
        _directTrait = true;
        Hit(10, SkillType.Rifle);
        Lines("DirectHostile").Should().Equal($"DirectHostile {Defender}");

        _log.Clear();
        _hostile = false;
        Hit(10, SkillType.Rifle);
        _hostile = true;
        _directTrait = false;
        Hit(10, SkillType.Rifle);
        _service.ApplyDamageDealtEffects(Defender, Defender, 10, SkillType.Rifle);
        _invalid.Add(Gone);
        _service.ApplyDamageDealtEffects(Gone, Defender, 10, SkillType.Rifle);
        _directTrait = true;
        _service.ApplyDamageDealtEffects(Attacker, Gone, 10, SkillType.Rifle);
        Lines("DirectHostile").Should().BeEmpty();

        // A physical auto-attack already passed native mitigation; anything else is probed in the engine.
        Private("HasPositiveDirectDamageAfterNativeMitigation", Attacker, Defender, 5, CombatDamageType.Physical, false, SkillType.Rifle)
            .Should().Be(true);
        Private("HasPositiveDirectDamageAfterNativeMitigation", Attacker, Defender, 0, CombatDamageType.Physical, false, SkillType.Rifle)
            .Should().Be(false);
    }

    [Test]
    public void APredatorsMarkIsPlacedByAnAbilityAndFollowedUpByAnyHit()
    {
        Stat(Attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent, 10);
        Stat(Attacker, StatType.PredatorsMarkDurationSeconds, 12);
        Hit(10);
        Lines("Apply").Should().BeEmpty("an auto-attack does not place the mark");
        Hit(10, ability: true);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(PredatorsMark1StatusEffect)} mark 12 {ResistanceType.Trauma}");

        _log.Clear();
        _sourceStatuses.Add((Defender, typeof(PredatorsMark1StatusEffect), Attacker));
        Stat(Attacker, StatType.PredatorsMarkHastePercentPerStack, 5);
        Stat(Attacker, StatType.PredatorsMarkAbilityHitChancePercentPerStack, 4);
        Stat(Attacker, StatType.PredatorsMarkFollowUpDurationSeconds, 8);
        Stat(Attacker, StatType.PredatorsMarkFollowUpMaximumStacks, 3);
        Hit(10);
        Lines("AddCapped").Should().Equal(
            $"AddCapped {Attacker} {StatType.AttackDelayReductionPercent} 5 8 15 {StatType.PredatorsMarkHastePercentPerStack} 1 False",
            $"AddCapped {Attacker} {StatType.AbilityHitChancePercentAdjustment} 4 8 12 {StatType.PredatorsMarkAbilityHitChancePercentPerStack} 1 False");
        Lines("Replace").Should().Contain($"Replace {Attacker} {StatType.AbilityHitChancePercentAdjustmentSkillType} {(int)SkillType.BeastMastery} 8 {StatType.PredatorsMarkAbilityHitChancePercentPerStack}");
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void APredatorsMarkNeedsLivingCreaturesAndItsValues()
    {
        Stat(Attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent, 10);
        _dead.Add(Defender);
        Hit(10, ability: true);
        _dead.Clear();
        Hit(10, ability: true);
        Lines("Apply").Should().BeEmpty("no duration");

        _sourceStatuses.Add((Defender, typeof(PredatorsMark1StatusEffect), Attacker));
        Stat(Attacker, StatType.PredatorsMarkFollowUpDurationSeconds, 8);
        Stat(Attacker, StatType.PredatorsMarkFollowUpMaximumStacks, 3);
        Hit(10);
        Lines("AddCapped").Should().BeEmpty("no haste or hit chance");
        Stat(Attacker, StatType.PredatorsMarkFollowUpMaximumStacks, 0);
        Stat(Attacker, StatType.PredatorsMarkHastePercentPerStack, 5);
        Hit(10);
        _dead.Add(Attacker);
        Hit(10);
        Lines("AddCapped").Should().BeEmpty();
    }

    [Test]
    public void ASideHitRestoresStaminaOnItsCooldownAndHastensTheAttacker()
    {
        _side = true;
        Stat(Attacker, StatType.SideAttackStaminaRestore, 2);
        Stat(Attacker, StatType.SideAttackStaminaRestoreCooldownSeconds, 6);
        Stat(Attacker, StatType.SideAttackDelayReductionPercent, 10);
        Stat(Attacker, StatType.SideAttackDelayReductionDurationSeconds, 4);

        Hit(10, SkillType.Vibroblade);
        _log.Should().ContainInOrder(
            $"Trigger {Attacker} {StatType.SideAttackStaminaRestore} 6",
            $"HitStamina {Attacker} {StatType.SideAttackStaminaRestore} 2",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 10 4 {StatType.SideAttackDelayReductionPercent}");

        _log.Clear();
        _triggerReady = false;
        Stat(Attacker, StatType.SideAttackDelayReductionDurationSeconds, 0);
        Hit(10, SkillType.Vibroblade);
        Lines("HitStamina").Should().BeEmpty();
        Lines("Replace").Should().BeEmpty();
        _side = false;
        Hit(10, SkillType.Vibroblade);
        Lines("Trigger").Should().ContainSingle();
        Private("ApplySideAttackDamageEffects", Attacker, Defender, SkillType.Vibroblade, 0);
        Lines("Trigger").Should().ContainSingle("no damage");
    }

    [Test]
    public void LandedDamageRestoresStaminaAndHastensForTheConfiguredSkill()
    {
        Stat(Attacker, StatType.DamageDealtStaminaRestore, 3);
        Stat(Attacker, StatType.DamageDealtStaminaRestoreCooldownSeconds, 5);
        Stat(Attacker, StatType.DamageDealtStaminaRestoreSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionPercent, 8);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds, 6);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionSkillType, (int)SkillType.Rifle);

        Hit(10, SkillType.Rifle);
        _log.Should().ContainInOrder(
            $"Trigger {Attacker} {StatType.DamageDealtStaminaRestore} 5",
            $"HitStamina {Attacker} {StatType.DamageDealtStaminaRestore} 3",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 8 6 {StatType.DamageDealtAttackDelayReductionPercent}");

        _log.Clear();
        Hit(10, SkillType.Pistol);
        _triggerReady = false;
        Hit(10, SkillType.Rifle);
        Lines("HitStamina").Should().BeEmpty();
        Lines("Replace").Should().ContainSingle("the haste still applies to the Rifle hit");
        _log.Clear();
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds, 0);
        Hit(10, SkillType.Rifle);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds, 6);
        Stat(Attacker, StatType.DamageDealtAttackDelayReductionPercent, 0);
        Stat(Attacker, StatType.DamageDealtStaminaRestore, 0);
        Hit(10, SkillType.Rifle);
        Lines("Replace").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle("no stamina to restore skips the cooldown");
    }

    [Test]
    public void ForceErosionAppliesOnADirectHitWithADuration()
    {
        Stat(Attacker, StatType.DamageDealtForceErosionDurationSeconds, 9);
        Stat(Attacker, StatType.DamageDealtForceErosionFPLossPerTick, 1);
        Stat(Attacker, StatType.DamageDealtForceErosionStaminaLossPerTick, 2);
        Hit(10);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(ForceErosionStatusEffect)} erosion 9 {CombatDamageType.Physical}");

        _log.Clear();
        Private("ApplyDamageDealtForceErosionEffect", Attacker, Defender, CombatDamageDeliveryType.Triggered);
        Stat(Attacker, StatType.DamageDealtForceErosionDurationSeconds, 0);
        Hit(10);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void TheDamageDealtHamstringRollsItsChanceForTheConfiguredSkill()
    {
        Stat(Attacker, StatType.DamageDealtHamstringChance, 100);
        Stat(Attacker, StatType.DamageDealtHamstringDurationSeconds, 5);
        Stat(Attacker, StatType.DamageDealtHamstringSkillType, (int)SkillType.Spear);
        Hit(10, SkillType.Spear, CombatDamageType.Fire);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(HamstringStatusEffect)} 5 {CombatDamageType.Fire}");

        _log.Clear();
        Hit(10, SkillType.Rifle);
        Stat(Attacker, StatType.DamageDealtHamstringDurationSeconds, 0);
        Hit(10, SkillType.Spear);
        Lines("Apply").Should().BeEmpty();
        Stat(Attacker, StatType.DamageDealtHamstringDurationSeconds, 5);
        Stat(Attacker, StatType.DamageDealtHamstringChance, 1);
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > 1);
        Rolls(seed, 1);
        Hit(10, SkillType.Spear);
        Lines("Apply").Should().BeEmpty("the roll missed");
        Stat(Attacker, StatType.DamageDealtHamstringChance, 100);
        Private("ApplyDamageDealtHamstringEffect", Attacker, Gone, SkillType.Spear, CombatDamageType.Physical);
        _invalid.Add(Gone);
        Private("ApplyDamageDealtHamstringEffect", Attacker, Gone, SkillType.Spear, CombatDamageType.Physical);
        Lines("Apply").Should().ContainSingle(line => line.Contains($">{Gone} "), "only the valid target is hamstrung");
    }

    [Test]
    public void EachMimicryProcRollsInTheTablesOrderAndUsesItsDuration()
    {
        Stat(Attacker, StatType.DamageDealtBleedChance, 100);
        Stat(Attacker, StatType.DamageDealtShockChance, 100);
        Stat(Attacker, StatType.DamageDealtPoisonChance, 100);
        Stat(Attacker, StatType.DamageDealtPoisonDurationSeconds, 30);

        Hit(10);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} {nameof(BleedStatusEffect)} 12 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} {nameof(ShockStatusEffect)} 10 {CombatDamageType.Electrical}",
            $"Apply {Attacker}>{Defender} {nameof(PoisonStatusEffect)} 30 {CombatDamageType.Poison}");

        _log.Clear();
        Hit(10, procs: false);
        Stat(Attacker, StatType.DamageDealtPoisonDurationSeconds, 0);
        Lines("Apply").Should().BeEmpty("the hit carries no procs");
        Hit(10);
        Lines("Apply").Should().Contain($"Apply {Attacker}>{Defender} {nameof(PoisonStatusEffect)} 12 {CombatDamageType.Poison}");
        _log.Clear();
        Private("ApplyDamageDealtMimicryTraitProcs", Attacker, Gone);
        _invalid.Add(Gone);
        Private("ApplyDamageDealtMimicryTraitProcs", Attacker, Gone);
        Lines("Apply").Should().HaveCount(3, "only the valid target");
    }

    [Test]
    public void TheMimicryProcTableKeepsItsOrderDurationsAndDamageTypes()
    {
        var table = typeof(CombatDealtEffectsService).GetField("MimicryTraitProcs", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(_dealt)!;
        var rows = ((System.Collections.IEnumerable)table).Cast<object>().Select(row => row.ToString()).ToArray();
        rows.Should().Equal(
            $"({StatType.DamageDealtBleedChance}, {typeof(BleedStatusEffect)}, {CombatDamageType.Physical}, 12, {StatType.Invalid})",
            $"({StatType.DamageDealtFreezingChance}, {typeof(FreezingStatusEffect)}, {CombatDamageType.Ice}, 6, {StatType.Invalid})",
            $"({StatType.DamageDealtShockChance}, {typeof(ShockStatusEffect)}, {CombatDamageType.Electrical}, 10, {StatType.Invalid})",
            $"({StatType.DamageDealtSunderChance}, {typeof(SunderStatusEffect)}, {CombatDamageType.Physical}, 14, {StatType.Invalid})",
            $"({StatType.DamageDealtHemorrhageChance}, {typeof(HemorrhageStatusEffect)}, {CombatDamageType.Physical}, 12, {StatType.Invalid})",
            $"({StatType.DamageDealtPoisonChance}, {typeof(PoisonStatusEffect)}, {CombatDamageType.Poison}, 12, {StatType.DamageDealtPoisonDurationSeconds})");
    }

    [Test]
    public void TheMimicryProcsDrawOneRollPerConfiguredChanceInTableOrder()
    {
        Stat(Attacker, StatType.DamageDealtFreezingChance, 50);
        Stat(Attacker, StatType.DamageDealtHemorrhageChance, 50);
        for (var seed = 1; seed <= 10; seed++)
        {
            _log.Clear();
            var rolls = Rolls(seed, 3);
            Private("ApplyDamageDealtMimicryTraitProcs", Attacker, Defender);
            Random.D100(1).Should().Be(rolls[2], "two chances drew two rolls");
            Lines("Apply").Should().Equal(new[]
            {
                rolls[0] <= 50 ? $"Apply {Attacker}>{Defender} {nameof(FreezingStatusEffect)} 6 {CombatDamageType.Ice}" : null,
                rolls[1] <= 50 ? $"Apply {Attacker}>{Defender} {nameof(HemorrhageStatusEffect)} 12 {CombatDamageType.Physical}" : null,
            }.Where(line => line != null));
        }
    }

    [Test]
    public void TheNextDamageDealtBleedIsConsumedByTheHit()
    {
        _temporary[StatType.NextDamageDealtBleedDurationSeconds] = 6;
        Hit(10, type: CombatDamageType.Fire);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} {nameof(BleedStatusEffect)} 6 {CombatDamageType.Fire}");
        _log.Clear();
        Hit(10);
        Lines("Apply").Should().BeEmpty("consumed");
    }

    [Test]
    public void HittingABleedingTargetRestoresStaminaThroughItsThreeChannels()
    {
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestoreChance, 100);
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestore, 1);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreChance, 100);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestore, 2);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreCooldownSeconds, 4);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreSkillType, (int)SkillType.Katar);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreChance, 100);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestore, 3);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreCooldownSeconds, 5);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType, (int)SkillType.Katar);

        Hit(10, SkillType.Katar, ability: true);
        _log.Where(line => line.StartsWith("HitStamina") || line.StartsWith("Trigger")).Should().Equal(
            $"Trigger {Attacker} {StatType.DamageDealtBleedingTargetStaminaRestore} 0",
            $"HitStamina {Attacker} {StatType.DamageDealtBleedingTargetStaminaRestore} 1",
            $"Trigger {Attacker} {StatType.SkillDamageBleedingTargetStaminaRestore} 4",
            $"HitStamina {Attacker} {StatType.SkillDamageBleedingTargetStaminaRestore} 2",
            $"Trigger {Attacker} {StatType.SkillAbilityBleedingTargetStaminaRestore} 5",
            $"HitStamina {Attacker} {StatType.SkillAbilityBleedingTargetStaminaRestore} 3");

        _log.Clear();
        Hit(10, SkillType.Rifle);
        Lines("HitStamina").Should().Equal(
            $"HitStamina {Attacker} {StatType.DamageDealtBleedingTargetStaminaRestore} 1",
            $"HitStamina {Attacker} {StatType.SkillDamageBleedingTargetStaminaRestore} 2");

        _log.Clear();
        _triggerReady = false;
        Hit(10, SkillType.Katar, ability: true);
        Lines("HitStamina").Should().BeEmpty("every channel is on cooldown");
        _triggerReady = true;
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestore, 0);
        Stat(Attacker, StatType.SkillDamageBleedingTargetStaminaRestoreChance, 0);
        Stat(Attacker, StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType, (int)SkillType.Rifle);
        Hit(10, SkillType.Katar, ability: true);
        Lines("HitStamina").Should().BeEmpty();
        _categories.Clear();
        _log.Clear();
        Hit(10, SkillType.Katar, ability: true);
        Lines("Trigger").Should().BeEmpty("the target is not bleeding");
        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > 1);
        Rolls(seed, 1);
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestore, 1);
        Stat(Attacker, StatType.DamageDealtBleedingTargetStaminaRestoreChance, 1);
        Private("ApplyBleedingTargetStaminaRestore", Attacker, Defender, SkillType.Rifle, false);
        Lines("Trigger").Should().BeEmpty("the roll missed");
        _invalid.Add(Gone);
        Private("ApplyBleedingTargetStaminaRestore", Attacker, Gone, SkillType.Rifle, false);
    }

    [Test]
    public void ToxicRushStacksAgainstAPoisonedTargetAndRestoresStaminaAtItsMaximum()
    {
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        Stat(Attacker, StatType.KatarToxicRushHastePercentPerStack, 4);
        Stat(Attacker, StatType.KatarToxicRushMaximumStacks, 2);
        Stat(Attacker, StatType.KatarToxicRushDurationSeconds, 10);

        Hit(10, SkillType.Katar);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Attacker} {nameof(ToxicRushStatusEffect)} stacks 1 10");
        Lines("HitStamina").Should().BeEmpty();

        _log.Clear();
        _toxicRush = new ToxicRushStatusEffect(2, 4, 0);
        Hit(10, SkillType.Katar);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Attacker} {nameof(ToxicRushStatusEffect)} stacks 2 10");
        Lines("HitStamina").Should().Equal($"HitStamina {Attacker} {StatType.KatarToxicRushMaximumStacks} 2");

        _log.Clear();
        Private("ApplyToxicRushDamageDealtEffects", Attacker, Defender, CombatDamageDeliveryType.DamageOverTime);
        Stat(Attacker, StatType.KatarToxicRushHastePercentPerStack, 0);
        Hit(10, SkillType.Katar);
        Stat(Attacker, StatType.KatarToxicRushAttackPercentPerStack, 3);
        Stat(Attacker, StatType.KatarToxicRushDurationSeconds, 0);
        Hit(10, SkillType.Katar);
        _statuses.Clear();
        Stat(Attacker, StatType.KatarToxicRushDurationSeconds, 10);
        Hit(10, SkillType.Katar);
        _invalid.Add(Gone);
        Private("ApplyToxicRushDamageDealtEffects", Gone, Defender, CombatDamageDeliveryType.Direct);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void HeavyVibrobladeAndLowHPHealingFollowTheirConditions()
    {
        Stat(Attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow, 1);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, 7);
        Stat(Attacker, StatType.LowHPDamageDealtHPRestoreThresholdPercent, 50);
        Stat(Attacker, StatType.LowHPDamageDealtHPPercentRestore, 9);
        _hitPoints[Attacker] = (40, 100);

        Hit(10, type: CombatDamageType.Fire);
        Lines("Heal").Should().Equal($"Heal {Attacker} 10 7 False 0", $"Heal {Attacker} 10 9 False 0");

        _log.Clear();
        _hitPoints[Attacker] = (50, 100);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, 0);
        Hit(10, type: CombatDamageType.Fire);
        _hitPoints[Attacker] = (0, 0);
        Hit(10, type: CombatDamageType.Fire);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow, 0);
        Stat(Attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, 7);
        Stat(Attacker, StatType.LowHPDamageDealtHPPercentRestore, 0);
        Hit(10, type: CombatDamageType.Fire);
        Lines("Heal").Should().BeEmpty();
    }

    [Test]
    public void TheTrackingRecordsTheBeastHitAndTheTraitOnlyForADirectHit()
    {
        _directTrait = true;
        _dealt.ApplyDamageDealtTracking(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, false);
        _log.Should().Equal($"Beast {Attacker} {Defender}", $"DirectHostile {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");

        _log.Clear();
        _dealt.ApplyDamageDealtTracking(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered, false);
        _dealt.ApplyDamageDealtTracking(Attacker, Defender, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime, false);
        _log.Should().Equal($"Activity {Attacker}", $"Recent {Attacker} {Defender}", $"Activity {Attacker}", $"Recent {Attacker} {Defender}");

        _log.Clear();
        _dealt.ApplyDamageDealtTracking(Attacker, Attacker, 10, SkillType.Rifle, CombatDamageType.Physical, CombatDamageDeliveryType.Direct, false);
        _log.Should().Equal($"Beast {Attacker} {Attacker}", $"Activity {Attacker}", $"Recent {Attacker} {Attacker}");
    }

    [Test]
    public void TheTwoHPPercentRestoresReadTheirOwnStatAndThePhysicalOneNeedsAPhysicalType()
    {
        Stat(Attacker, StatType.DamageDealtHPPercentRestore, 5);
        Stat(Attacker, StatType.PhysicalDamageDealtHPPercentRestore, 6);

        _dealt.ApplyDamageDealtHPPercentRestore(Attacker, 10);
        Lines("Heal").Should().Equal($"Heal {Attacker} 10 5 False 0");

        _log.Clear();
        _dealt.ApplyPhysicalDamageDealtHPPercentRestore(Attacker, 10, CombatDamageType.Physical);
        Lines("Heal").Should().Equal($"Heal {Attacker} 10 6 False 0");

        _log.Clear();
        _dealt.ApplyPhysicalDamageDealtHPPercentRestore(Attacker, 10, CombatDamageType.Force);
        _log.Should().BeEmpty("a Force hit is not physical damage");

        _stats.Clear();
        _dealt.ApplyDamageDealtHPPercentRestore(Attacker, 10);
        _dealt.ApplyPhysicalDamageDealtHPPercentRestore(Attacker, 10, CombatDamageType.Physical);
        _log.Should().BeEmpty("a restore at zero percent heals nothing");
    }
}
