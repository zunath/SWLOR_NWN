using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Deflection chances and what a deflection sets off. The native chance reads need a real engine.</summary>
public class StatServiceDeflectionTests : StatServiceTestBase
{
    private uint WieldWeapon(uint creature, InventorySlot slot = InventorySlot.RightHand)
    {
        var weapon = Bench.Wear(creature, slot, BaseItem.Longsword);
        Service.WeaponSkills[weapon] = SkillType.Vibroblade;
        return weapon;
    }

    [Test]
    public void WeaponDeflection_NeedsAWeaponAndNoShield()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.MeleeDeflection, 20);
        SetStat(Npc, StatType.RangedDeflection, 30);

        Service.GetMeleeDeflectionChance(Npc).Should().Be(0);
        Service.GetRangedDeflectionChance(Npc).Should().Be(0);

        WieldWeapon(Npc);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(20);
        Service.GetRangedDeflectionChance(Npc).Should().Be(30);

        Bench.Wear(Npc, InventorySlot.LeftHand, BaseItem.SmallShield);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(0);
        Service.GetRangedDeflectionChance(Npc).Should().Be(0);
    }

    [Test]
    public void WeaponDeflection_CountsAWeaponInEitherHandThatHasASkill()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.MeleeDeflection, 20);

        Bench.Wear(Npc, InventorySlot.RightHand, BaseItem.Armor);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(0, "an item with no weapon skill is not a weapon");

        WieldWeapon(Npc, InventorySlot.LeftHand);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(20);
    }

    [Test]
    public void WeaponDeflection_IsCappedAndTheCapCanOnlyRiseToTheMaximum()
    {
        Bench.AddNpc();
        WieldWeapon(Npc);
        SetStat(Npc, StatType.MeleeDeflection, 95);
        SetStat(Npc, StatType.RangedDeflection, 95);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(StatService.DefaultMeleeDeflectionChanceCap);
        Service.GetRangedDeflectionChance(Npc).Should().Be(StatService.DefaultRangedDeflectionChanceCap);

        SetStat(Npc, StatType.MeleeDeflectionChanceCap, 10);
        SetStat(Npc, StatType.RangedDeflectionChanceCap, 20);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(60);
        Service.GetRangedDeflectionChance(Npc).Should().Be(70);

        SetStat(Npc, StatType.MeleeDeflectionChanceCap, -40);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(StatService.DefaultMeleeDeflectionChanceCap);

        SetStat(Npc, StatType.RangedDeflectionChanceCap, 999);
        Service.GetRangedDeflectionChance(Npc).Should().Be(95);

        SetStat(Npc, StatType.MeleeDeflection, -5);
        Service.GetMeleeDeflectionChance(Npc).Should().Be(0);
    }

    [Test]
    public void ShieldDeflection_AddsTheShieldPropertyAndTheStatUpToTheMaximum()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.ShieldDeflection, 10);

        Service.GetShieldDeflectionChance(Npc).Should().Be(0, "no shield, no deflection");

        Bench.Wear(Npc, InventorySlot.LeftHand, BaseItem.SmallShield,
            (ItemPropertyType.ShieldDeflection, -1, 5), (ItemPropertyType.ShieldDeflection, -1, 2), (ItemPropertyType.Attack, -1, 90));
        Service.GetShieldDeflectionChance(Npc).Should().Be(17);

        SetStat(Npc, StatType.ShieldDeflection, 500);
        Service.GetShieldDeflectionChance(Npc).Should().Be(StatService.MaximumShieldDeflectionChance);
    }

    [TestCase(20, 20)]
    [TestCase(-5, 0)]
    [TestCase(500, 100)]
    public void GuardChance_IsTheClampedStat(int stat, int expected)
    {
        SetStat(Npc, StatType.Guard, stat);

        Service.GetGuardChance(Npc).Should().Be(expected);
    }

    [Test]
    public void ADeflectionWithNothingToDoStillTracksItAndGrantsNothing()
    {
        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Melee);

        Bench.Log.Should().Equal(
            "TrackDeflection:Melee",
            "NextSkillBonuses:Invalid:0:0:0",
            "NextAutoAttackCritical:Invalid:0:0");
    }

    [Test]
    public void AMeleeDeflection_RestoresThroughItsGatesInOrder()
    {
        SetStat(PlayerCreature, StatType.MeleeDeflectionStaminaRestore, 3);
        SetStat(PlayerCreature, StatType.MeleeDeflectionStaminaRestoreCooldownSeconds, 6);
        SetStat(PlayerCreature, StatType.MeleeDeflectionFPRestore, 2);
        SetStat(PlayerCreature, StatType.MeleeDeflectionFPRestoreCooldownSeconds, 7);
        // These belong to the other sources, so a melee deflection must not read them.
        SetStat(PlayerCreature, StatType.ShieldDeflectionStaminaRestore, 100);
        SetStat(PlayerCreature, StatType.DeflectionFPRestore, 100);
        var record = Bench.AddPlayer();
        record.MaxFP = 100;
        record.MaxStamina = 100;

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Melee);

        Bench.Log.Should().StartWith(new[]
        {
            "TrackDeflection:Melee",
            "TryTrigger:MeleeDeflectionStaminaRestore:6",
            "TriggerAttribution:MeleeDeflectionStaminaRestore",
            "DB.Get:uuid-100",
            "DB.Set:uuid-100",
            "Script:pc_stm_adjusted",
            "StaminaRestoredEffects:100",
            "Feedback:3:STM",
            "TryTrigger:MeleeDeflectionFPRestore:7",
            "TriggerAttribution:MeleeDeflectionFPRestore",
            "DB.Get:uuid-100",
            "DB.Set:uuid-100",
            "Script:pc_fp_adjusted",
            "FPRestoredEffects:100",
            "Feedback:2:FP",
        });
        record.Stamina.Should().Be(3);
        record.FP.Should().Be(2);
    }

    [Test]
    public void ARestoreWhoseGateIsClosedDoesNotRestore()
    {
        SetStat(PlayerCreature, StatType.MeleeDeflectionStaminaRestore, 3);
        SetStat(PlayerCreature, StatType.MeleeDeflectionFPRestore, 2);
        Service.StatTriggerAvailable = false;

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Melee);

        Bench.Log.Should().Equal(
            "TrackDeflection:Melee",
            "TryTrigger:MeleeDeflectionStaminaRestore:0",
            "TryTrigger:MeleeDeflectionFPRestore:0",
            "NextSkillBonuses:Invalid:0:0:0",
            "NextAutoAttackCritical:Invalid:0:0");
    }

    [Test]
    public void AnyDeflection_RestoresAPercentOfMaxStaminaWithoutAGate()
    {
        var record = Bench.AddPlayer();
        record.MaxStamina = 200;
        SetStat(PlayerCreature, StatType.DeflectionStaminaRestorePercent, 10);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Ranged);

        record.Stamina.Should().Be(20);
        Bench.Log.Should().Contain("TriggerAttribution:DeflectionStaminaRestorePercent");
        Bench.Log.Should().NotContain(entry => entry.StartsWith("TryTrigger", StringComparison.Ordinal));
    }

    [Test]
    public void AShieldDeflection_ReplacesTheEvasionAndEnmityBoostsAsAPair()
    {
        SetStat(PlayerCreature, StatType.DeflectionEvasionPercentAdjustment, 5);
        SetStat(PlayerCreature, StatType.DeflectionEvasionEnmityPercentAdjustment, 6);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Shield);

        Bench.Log.Should().ContainInOrder(
            "Temporary:EvasionPercentAdjustment:5:30:DeflectionEvasionPercentAdjustment",
            "Temporary:EnmityPercentAdjustment:6:30:DeflectionEvasionPercentAdjustment");
    }

    [Test]
    public void ARangedDeflection_ReplacesTheEnmityAndDefenseBoosts()
    {
        SetStat(PlayerCreature, StatType.DeflectionEnmityPercentAdjustment, 5);
        SetStat(PlayerCreature, StatType.DeflectionDefensePercentAdjustment, 6);
        SetStat(PlayerCreature, StatType.DeflectionForceDefensePercentAdjustment, 7);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Ranged);

        Bench.Log.Should().ContainInOrder(
            "Temporary:EnmityPercentAdjustment:5:30:DeflectionEnmityPercentAdjustment",
            "Temporary:PhysicalDefensePercentAdjustment:6:30:DeflectionDefensePercentAdjustment",
            "Temporary:ForceDefensePercentAdjustment:7:30:DeflectionDefensePercentAdjustment");
    }

    [Test]
    public void TheBoostsOfAnotherSourceAreNotRead()
    {
        SetStat(PlayerCreature, StatType.DeflectionEvasionPercentAdjustment, 5);
        SetStat(PlayerCreature, StatType.DeflectionEnmityPercentAdjustment, 5);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Melee);

        Bench.Log.Should().NotContain(entry => entry.StartsWith("Temporary", StringComparison.Ordinal));
    }

    [Test]
    public void TheRecastGate_IsOnlySpentWhenThereIsACooldownToShorten()
    {
        SetStat(PlayerCreature, StatType.DeflectionRecastReductionGroupId, (int)RecastGroup.Earthshatter);
        SetStat(PlayerCreature, StatType.DeflectionRecastReductionSeconds, 4);
        SetStat(PlayerCreature, StatType.DeflectionRecastReductionCooldownSeconds, 9);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Shield);
        Bench.Log.Should().Contain("IsOnRecastDelay:Earthshatter");
        Bench.Log.Should().NotContain(entry => entry.StartsWith("TryTrigger", StringComparison.Ordinal));
        Bench.Log.Should().NotContain(entry => entry.StartsWith("ReduceRecastDelay", StringComparison.Ordinal));

        Bench.Log.Clear();
        Bench.OnRecastDelay = true;
        Service.StatTriggerAvailable = false;
        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Shield);
        Bench.Log.Should().ContainInOrder("IsOnRecastDelay:Earthshatter", "TryTrigger:DeflectionRecastReductionSeconds:9");
        Bench.Log.Should().NotContain(entry => entry.StartsWith("ReduceRecastDelay", StringComparison.Ordinal));

        Bench.Log.Clear();
        Service.StatTriggerAvailable = true;
        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Shield);
        Bench.Log.Should().ContainInOrder(
            "IsOnRecastDelay:Earthshatter",
            "TryTrigger:DeflectionRecastReductionSeconds:9",
            "ReduceRecastDelay:Earthshatter:4");
    }

    [TestCase(0, 4)]
    [TestCase(99999, 4)]
    [TestCase(3, 0)]
    public void TheRecastGate_NeedsAKnownGroupAndAReduction(int group, int seconds)
    {
        SetStat(PlayerCreature, StatType.DeflectionRecastReductionGroupId, group);
        SetStat(PlayerCreature, StatType.DeflectionRecastReductionSeconds, seconds);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Shield);

        Bench.Log.Should().NotContain(entry => entry.StartsWith("IsOnRecastDelay", StringComparison.Ordinal));
    }

    [Test]
    public void TheNextAbilityBonuses_UseTheLongerWindowAndComeLastInOrder()
    {
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilitySkillType, (int)SkillType.Pistol);
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilityDamageBonus, 5);
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilityCriticalRatePercentAdjustment, 6);
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds, 7);
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilityCriticalRateWindowSeconds, 8);
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilityNoDelay, 1);
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilityNoDelayWindowSeconds, 9);
        SetStat(PlayerCreature, StatType.DeflectionNextAutoAttackCriticalRateSkillType, (int)SkillType.Vibroblade);
        SetStat(PlayerCreature, StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment, 10);
        SetStat(PlayerCreature, StatType.DeflectionNextAutoAttackCriticalRateWindowSeconds, 11);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Ranged);

        Bench.Log.Should().Equal(
            "TrackDeflection:Ranged",
            "NextSkillBonuses:Pistol:5:6:8",
            "NextAutoAttackCritical:Vibroblade:10:11",
            "NextAbilityNoDelay:Pistol:9");
    }

    [Test]
    public void AnUnknownSkillInAStatMeansNoSkill()
    {
        SetStat(PlayerCreature, StatType.DeflectionNextSkillAbilitySkillType, 99999);

        Service.ApplyDeflectionEffectsNative(null!, DeflectionSource.Ranged);

        Bench.Log.Should().Contain("NextSkillBonuses:Invalid:0:0:0");
    }

}
