using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Attack, Defense, Accuracy, Evasion, Detection, Stealth and Lockpicking of a creature.</summary>
public class StatServiceRatingTests : StatServiceTestBase
{
    private const uint Weapon = 5000;

    private static PlayerSkill Rank(int rank) => new() { Rank = rank };

    [Test]
    public void GetAttack_OfAPlayerUsesTheSkillRecordAndEffects()
    {
        var record = Bench.AddPlayer();
        record.Skills[SkillType.Vibroblade] = Rank(10);
        record.Attack = 5;
        record.ForceAttack = 100;
        Bench.Abilities[(PlayerCreature, AbilityType.Might)] = 20;
        SetStat(PlayerCreature, StatType.Attack, 3);

        Service.GetAttack(PlayerCreature, AbilityType.Might, SkillType.Vibroblade).Should().Be(8 + 20 + 20 + 8);
    }

    [Test]
    public void GetAttack_OfAForcePlayerUsesForceAttackAndTheForcePercent()
    {
        var record = Bench.AddPlayer();
        record.Skills[SkillType.Force] = Rank(10);
        record.Attack = 100;
        record.ForceAttack = 2;
        SetStat(PlayerCreature, StatType.AttackPercentAdjustment, 10);
        SetStat(PlayerCreature, StatType.ForceAttackPercentAdjustment, 10);

        Service.GetAttack(PlayerCreature, AbilityType.Willpower, SkillType.Force).Should().Be(36, "30 plus a ceiling of 20 percent");
    }

    [Test]
    public void GetAttack_WithAnOverrideIgnoresTheRecordAndANegativeOneIsZero()
    {
        var record = Bench.AddPlayer();
        record.Attack = 100;

        Service.GetAttack(PlayerCreature, AbilityType.Might, SkillType.Invalid, 7).Should().Be(15);
        Service.GetAttack(PlayerCreature, AbilityType.Might, SkillType.Invalid, -7).Should().Be(108);
    }

    [Test]
    public void GetAttack_OfAnNpcUsesAMatchingSkillOrItsLevel()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.NPCLevel, -1, 10), (ItemPropertyType.NPCSkill, (int)SkillType.Vibroblade, 20),
            (ItemPropertyType.Attack, -1, 1), (ItemPropertyType.ForceAttack, -1, 2));

        Service.GetAttack(Npc, AbilityType.Might, SkillType.Vibroblade).Should().Be(8 + 40 + 1);
        Service.GetAttack(Npc, AbilityType.Might, SkillType.Force).Should().Be(8 + 20 + 2);
        Service.GetAttack(Npc, AbilityType.Might, SkillType.Vibroblade, 7).Should().Be(8 + 40 + 7);
        Service.GetAttack(Npc, AbilityType.Might, SkillType.Force, 7).Should().Be(8 + 20 + 7);
    }

    [Test]
    public void GetAttack_AddsEveryPostAttackAdjustmentBeforeThePercentAndNeverFallsBelowOne()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.AttackPercentAdjustment, 10);
        Service.NearbyStatusTargetAdjustment = 10;
        Service.LowHPAdjustment = 10;
        Service.LowFPAdjustment = 20;

        Service.GetAttack(Npc, AbilityType.Might, SkillType.Invalid).Should().Be(12, "8 plus a ceiling of 50 percent");

        SetStat(Npc, StatType.AttackPercentAdjustment, -500);
        Service.GetAttack(Npc, AbilityType.Might, SkillType.Invalid).Should().Be(1);
    }

    [Test]
    public void GetAttack_AddsTheHighPoolsBonusOnlyAboveTheThreshold()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 100;
        record.MaxStamina = 100;
        record.FP = 90;
        record.Stamina = 90;
        SetStat(PlayerCreature, StatType.HighFPAndStaminaAttackThresholdPercent, 90);
        SetStat(PlayerCreature, StatType.HighFPAndStaminaAttackPercentAdjustment, 100);

        Service.GetAttack(PlayerCreature, AbilityType.Might, SkillType.Invalid).Should().Be(16);

        record.Stamina = 89;
        Service.GetAttack(PlayerCreature, AbilityType.Might, SkillType.Invalid).Should().Be(8);

        record.Stamina = 90;
        record.FP = 89;
        Service.GetAttack(PlayerCreature, AbilityType.Might, SkillType.Invalid).Should().Be(8);
    }

    [TestCase(0, 100)]
    [TestCase(90, 0)]
    public void GetAttack_HasNoHighPoolsBonusWithoutAThresholdOrAnAdjustment(int threshold, int adjustment)
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.HighFPAndStaminaAttackThresholdPercent, threshold);
        SetStat(Npc, StatType.HighFPAndStaminaAttackPercentAdjustment, adjustment);

        Service.GetAttack(Npc, AbilityType.Might, SkillType.Invalid).Should().Be(8);
    }

    [Test]
    public void GetAttack_HasNoHighPoolsBonusWithEmptyPools()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.HighFPAndStaminaAttackThresholdPercent, 50);
        SetStat(Npc, StatType.HighFPAndStaminaAttackPercentAdjustment, 100);

        Service.GetAttack(Npc, AbilityType.Might, SkillType.Invalid).Should().Be(8);
    }

    [TestCase(1, 2, 3, 15)]
    [TestCase(0, 0, 0, 8)]
    public void GetAttack_FormulaIsEightPlusTwiceTheLevelPlusStatAndBonus(int level, int stat, int bonus, int expected)
    {
        Service.GetAttack(level, stat, bonus).Should().Be(expected);
        Service.GetAccuracy(level, stat, bonus).Should().Be(expected);
        Service.GetEvasion(level, stat, bonus).Should().Be(expected);
    }

    [TestCase(10, 10, 5, 35)]
    [TestCase(0, 3, 0, 11)]
    public void CalculateDefense_MultipliesTheSkillAndTruncates(int stat, int skill, int bonus, int expected) =>
        Service.CalculateDefense(stat, skill, bonus).Should().Be(expected);

    [Test]
    public void GetDefense_OfAPlayerUsesArmorRankRecordAndAdjustments()
    {
        var record = Bench.AddPlayer();
        record.Skills[SkillType.Armor] = Rank(10);
        record.Defenses[CombatDamageType.Physical] = 5;
        Bench.Abilities[(PlayerCreature, AbilityType.Vitality)] = 10;
        SetStat(PlayerCreature, StatType.Defense, 1);
        SetStat(PlayerCreature, StatType.PhysicalDefense, 2);
        SetStat(PlayerCreature, StatType.ForceDefense, 100);

        Service.GetDefense(PlayerCreature, CombatDamageType.Physical, AbilityType.Vitality).Should().Be(Service.CalculateDefense(10, 10, 8));
    }

    [Test]
    public void GetDefense_OfAPlayerToleratesMissingMapsAndUsesAnOverrideInstead()
    {
        var record = Bench.AddPlayer();
        record.Skills[SkillType.Armor] = Rank(0);
        record.Defenses = null!;

        Service.GetDefense(PlayerCreature, CombatDamageType.Force, AbilityType.Vitality).Should().Be(8);

        record.Defenses = new Dictionary<CombatDamageType, int> { [CombatDamageType.Force] = 50 };
        Service.GetDefense(PlayerCreature, CombatDamageType.Force, AbilityType.Vitality, 3).Should().Be(11);
        Service.GetDefense(PlayerCreature, CombatDamageType.Force, AbilityType.Vitality, -3).Should().Be(58);
    }

    [Test]
    public void GetDefense_OfAnNpcUsesItsLevelSkinAndAnOverrideInstead()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.NPCLevel, -1, 5), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 4));
        SetStat(Npc, StatType.ForceDefense, 2);

        Service.GetDefense(Npc, CombatDamageType.Force, AbilityType.Willpower).Should().Be(Service.CalculateDefense(0, 5, 6));
        Service.GetDefense(Npc, CombatDamageType.Force, AbilityType.Willpower, 1).Should().Be(Service.CalculateDefense(0, 5, 3));
    }

    [Test]
    public void GetDefensePercentAdjustment_CombinesTheAdjustmentsOfTheDamageType()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.DefensePercentAdjustment, 1);
        SetStat(Npc, StatType.PhysicalDefensePercentAdjustment, 2);
        SetStat(Npc, StatType.ForceDefensePercentAdjustment, 4);
        SetStat(Npc, StatType.PhysicalAndForceDefenseAuraPercentAdjustment, 8);
        SetStat(Npc, StatType.ShieldEquippedPhysicalDefensePercentAdjustment, 16);

        Service.GetDefensePercentAdjustment(Npc, CombatDamageType.Physical).Should().Be(1 + 2 + 8);
        Service.GetDefensePercentAdjustment(Npc, CombatDamageType.Force).Should().Be(1 + 4 + 8);
        Service.GetDefensePercentAdjustment(Npc, CombatDamageType.Fire).Should().Be(1);

        Bench.Wear(Npc, InventorySlot.LeftHand, BaseItem.SmallShield);
        Service.GetDefensePercentAdjustment(Npc, CombatDamageType.Physical).Should().Be(1 + 2 + 8 + 16);
    }

    [Test]
    public void GetDefense_ClampsAPercentReductionToOne()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.DefensePercentAdjustment, -500);

        Service.GetDefense(Npc, CombatDamageType.Physical, AbilityType.Vitality).Should().Be(1);
    }

    [Test]
    public void GetAccuracy_OfAPlayerUsesTheWeaponSkillStatRecordAndEffects()
    {
        var record = Bench.AddPlayer();
        record.Skills[SkillType.Vibroblade] = Rank(10);
        record.Accuracy = 2;
        Bench.Items.Add(Weapon, baseItem: BaseItem.Longsword);
        Bench.Items.AddProperty(Weapon, ItemPropertyType.Accuracy, costTableValue: 3);
        Service.WeaponSkills[Weapon] = SkillType.Vibroblade;
        Bench.Abilities[(PlayerCreature, AbilityType.Perception)] = 12;
        SetStat(PlayerCreature, StatType.Accuracy, 1);

        Service.GetAccuracy(PlayerCreature, Weapon, AbilityType.Invalid, SkillType.Invalid)
            .Should().Be(8 + 20 + 12 + 3 + 2 + 1);
    }

    [Test]
    public void GetAccuracy_AWeaponStatOverrideWinsUnlessIgnored()
    {
        Bench.AddNpc();
        Bench.Items.Add(Weapon, baseItem: BaseItem.Longsword);
        Bench.Items.AddProperty(Weapon, ItemPropertyType.AccuracyStat, subType: (int)AbilityType.Agility);
        Bench.Abilities[(Npc, AbilityType.Agility)] = 30;
        Bench.Abilities[(Npc, AbilityType.Willpower)] = 7;

        Service.GetAccuracy(Npc, Weapon, AbilityType.Willpower, SkillType.Force).Should().Be(8 + 30);
        Service.GetAccuracy(Npc, Weapon, AbilityType.Willpower, SkillType.Force, -1, true).Should().Be(8 + 7);
    }

    [Test]
    public void GetAccuracy_AnInvalidStatCountsAsZero()
    {
        Bench.AddNpc();
        Bench.Items.Add(Weapon, baseItem: BaseItem.Longsword);
        Service.WeaponAccuracyAbility = AbilityType.Invalid;

        Service.GetAccuracy(Npc, Weapon, AbilityType.Invalid, SkillType.Force).Should().Be(8);
        Bench.Engine.CountOf("GetAbilityScore").Should().Be(0);
    }

    [Test]
    public void GetAccuracy_OfAnNpcUsesItsLevelAndALevelOverrideReplacesIt()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.NPCLevel, -1, 5), (ItemPropertyType.Accuracy, -1, 4));
        Bench.Items.Add(Weapon, baseItem: BaseItem.Longsword);

        Service.GetAccuracy(Npc, Weapon, AbilityType.Might, SkillType.Force).Should().Be(8 + 10 + 4);
        Service.GetAccuracy(Npc, Weapon, AbilityType.Might, SkillType.Force, 1).Should().Be(8 + 2 + 4);
    }

    [Test]
    public void GetAccuracy_PowerAttackModesLowerIt()
    {
        Bench.AddNpc();
        Bench.Items.Add(Weapon, baseItem: BaseItem.Longsword);

        Bench.ActionModes.Add((Npc, ActionMode.PowerAttack));
        Service.GetAccuracy(Npc, Weapon, AbilityType.Might, SkillType.Force, 10).Should().Be(8 + 20 - 5);
        Bench.ActionModes.Clear();

        Bench.ActionModes.Add((Npc, ActionMode.ImprovedPowerAttack));
        Service.GetAccuracy(Npc, Weapon, AbilityType.Might, SkillType.Force, 10).Should().Be(8 + 20 - 10);
    }

    [Test]
    public void GetAccuracy_AppliesItsPercentAndNeverFallsBelowOne()
    {
        Bench.AddNpc();
        Bench.Items.Add(Weapon, baseItem: BaseItem.Longsword);
        SetStat(Npc, StatType.AccuracyPercentAdjustment, 50);
        Service.GetAccuracy(Npc, Weapon, AbilityType.Might, SkillType.Force).Should().Be(12);

        SetStat(Npc, StatType.AccuracyPercentAdjustment, -500);
        Service.GetAccuracy(Npc, Weapon, AbilityType.Might, SkillType.Force).Should().Be(1);
    }

    [Test]
    public void GetAccuracy_OfAPlayerWithNoSkillStillCountsTheRecord()
    {
        var record = Bench.AddPlayer();
        record.Accuracy = 4;
        Bench.Items.Add(Weapon, baseItem: BaseItem.Longsword);

        Service.GetAccuracy(PlayerCreature, Weapon, AbilityType.Might, SkillType.Invalid).Should().Be(12);
    }

    [Test]
    public void GetEvasion_OfAPlayerUsesTheArmorSkillAgilityRecordAndNativeArmor()
    {
        var record = Bench.AddPlayer();
        record.Skills[SkillType.Armor] = Rank(10);
        record.Evasion = 7;
        Bench.Abilities[(PlayerCreature, AbilityType.Agility)] = 12;
        Bench.AbilityModifiers[(AbilityType.Perception, PlayerCreature)] = 2;
        Bench.ArmorClass[PlayerCreature] = 15;
        SetStat(PlayerCreature, StatType.Evasion, 1);

        Service.GetEvasion(PlayerCreature, SkillType.Invalid).Should().Be(8 + 20 + 12 + 7 + 1 + (15 - 2 - 10) * 5);
    }

    [Test]
    public void GetEvasion_OfAPlayerUsesASkillOverride()
    {
        var record = Bench.AddPlayer();
        record.Skills[SkillType.Force] = Rank(1);
        Bench.ArmorClass[PlayerCreature] = 10;

        Service.GetEvasion(PlayerCreature, SkillType.Force).Should().Be(8 + 2);
    }

    [Test]
    public void GetEvasion_OfAnNpcWithAStatBudgetIgnoresNativeArmor()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.NPCLevel, -1, 5), (ItemPropertyType.Evasion, -1, 9));
        Bench.ArmorClass[Npc] = 30;

        Service.GetEvasion(Npc, SkillType.Invalid).Should().Be(8 + 10 + 9);
    }

    [Test]
    public void GetEvasion_OfAnNpcWithNoStatBudgetCountsNativeArmor()
    {
        Bench.AddNpc();
        Bench.ArmorClass[Npc] = 12;

        Service.GetEvasion(Npc, SkillType.Invalid).Should().Be(8 + 2 * 5);
    }

    [Test]
    public void GetEvasion_AddsTheRangedPercentOnlyAgainstARangedAttack()
    {
        Bench.AddNpc();
        Bench.ArmorClass[Npc] = 10;
        SetStat(Npc, StatType.EvasionPercentAdjustment, 50);
        SetStat(Npc, StatType.RangedEvasionPercentAdjustment, 50);
        Service.RangedSkills.Add(SkillType.Pistol);

        Service.GetEvasion(Npc, SkillType.Invalid).Should().Be(12);
        Service.GetEvasion(Npc, SkillType.Invalid, SkillType.Pistol).Should().Be(16);

        SetStat(Npc, StatType.EvasionPercentAdjustment, -500);
        Service.GetEvasion(Npc, SkillType.Invalid).Should().Be(1);
    }

    [Test]
    public void GetEvasion_FormulaOnlyCountsNativeArmorWithoutAStatBudget()
    {
        Service.GetEvasion(1, 2, 3, 4, false).Should().Be(8 + 2 + 2 + 3 + 20);
        Service.GetEvasion(1, 2, 3, 4, true).Should().Be(8 + 2 + 2 + 3);
    }

    [Test]
    public void GetDetection_OfAPlayerUsesTheRecordAndIsNotCapped()
    {
        var record = Bench.AddPlayer();
        record.Detection = 100;
        Bench.Abilities[(PlayerCreature, AbilityType.Perception)] = 10;
        Bench.Abilities[(PlayerCreature, AbilityType.Willpower)] = 5;
        SetStat(PlayerCreature, StatType.Detection, 2);
        Bench.ActionModes.Add((PlayerCreature, ActionMode.Detect));

        Service.GetDetection(PlayerCreature).Should().Be(122);
    }

    [Test]
    public void GetDetection_OfAnNpcUsesTheSkinAndIsCapped()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.Detection, -1, 3), (ItemPropertyType.Detection, -1, 4), (ItemPropertyType.Attack, -1, 100));
        Service.GetDetection(Npc).Should().Be(7);

        SetStat(Npc, StatType.Detection, 500);
        Service.GetDetection(Npc).Should().Be(StatService.MaximumNPCDetection);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void GetDetection_OfStaffIsNotCapped(bool dm, bool possessed)
    {
        Bench.AddNpc();
        if (dm)
            Bench.Dms.Add(Npc);
        if (possessed)
            Bench.Possessed.Add(Npc);
        SetStat(Npc, StatType.Detection, 500);

        Service.GetDetection(Npc).Should().Be(500);
    }

    [TestCase(1, 2, 3, true, 11)]
    [TestCase(1, 2, 3, false, 6)]
    [TestCase(-100, 0, 0, false, 0)]
    public void CalculateDetectionRating_AddsItsPartsAndIsNeverNegative(int perception, int willpower, int bonus, bool detect, int expected) =>
        Service.CalculateDetectionRating(perception, willpower, bonus, 0, detect).Should().Be(expected);

    [TestCase(60, true, 50)]
    [TestCase(60, false, 60)]
    [TestCase(-1, true, 0)]
    [TestCase(-1, false, 0)]
    public void ApplyNPCDetectionCap_CapsOnlyNpcsAndNeverGoesNegative(int detection, bool isNpc, int expected) =>
        Service.ApplyNPCDetectionCap(detection, isNpc).Should().Be(expected);

    [Test]
    public void GetLockpicking_OfAPlayerAddsTheRecordAndAdjustments()
    {
        var record = Bench.AddPlayer();
        record.Lockpicking = 6;
        SetStat(PlayerCreature, StatType.Lockpicking, 2);

        Service.GetLockpicking(PlayerCreature).Should().Be(8);
    }

    [Test]
    public void GetLockpicking_OfAnNpcAddsTheSkinAndAdjustments()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.Lockpicking, -1, 3));
        SetStat(Npc, StatType.Lockpicking, 2);

        Service.GetLockpicking(Npc).Should().Be(5);
    }

    [Test]
    public void GetStealth_OfAPlayerCountsAgilityTwiceThenTheRecordAndAdjustments()
    {
        var record = Bench.AddPlayer();
        record.Stealth = 5;
        Bench.Abilities[(PlayerCreature, AbilityType.Agility)] = 10;
        SetStat(PlayerCreature, StatType.Stealth, 1);

        Service.GetStealth(PlayerCreature).Should().Be(26);
    }

    [Test]
    public void GetStealth_OfAnNpcUsesTheSkinAndIsNeverNegative()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.Stealth, -1, 5));
        Service.GetStealth(Npc).Should().Be(5);

        SetStat(Npc, StatType.Stealth, -100);
        Service.GetStealth(Npc).Should().Be(0);
    }
}
