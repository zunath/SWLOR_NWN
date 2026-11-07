using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ResistanceService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Max HP, the player records summed from worn gear, Combat Readiness and the record adjustments.</summary>
public class StatServiceEquipmentRecordTests : StatServiceTestBase
{
    private const string PlayerUuid = "uuid-100";

    [Test]
    public void AdjustPlayerMaxHP_GivesEveryLevelOneHitPointThenFillsTheLevelsInOrder()
    {
        var record = Bench.AddPlayer();
        record.MaxHP = 300;
        Bench.NativeLevels = 2;

        Service.AdjustPlayerMaxHP(record, PlayerCreature, 10);

        record.MaxHP.Should().Be(310);
        Bench.Creature.CallsTo("SetMaxHitPointsByLevel").Select(call => ((int)call.Arguments[1]!, (int)call.Arguments[2]!))
            .Should().Equal((1, 1), (2, 1), (1, 255), (2, 55));
        Bench.Log.Should().Equal("LimitCheck:100");
    }

    [Test]
    public void AdjustPlayerMaxHP_BelowTheLevelFloorOnlyWritesTheFloor()
    {
        var record = Bench.AddPlayer();
        record.MaxHP = 2;
        Bench.NativeLevels = 3;

        Service.AdjustPlayerMaxHP(record, PlayerCreature, -10);

        record.MaxHP.Should().Be(-8);
        Bench.Creature.CallsTo("SetMaxHitPointsByLevel").Select(call => (int)call.Arguments[2]!).Should().Equal(1, 1, 1);
    }

    [TestCase(500, 310, true)]
    [TestCase(310, 310, false)]
    [TestCase(100, 310, false)]
    public void AdjustPlayerMaxHP_BringsCurrentHitPointsDownToTheNewMaximumOnly(int current, int maximum, bool lowered)
    {
        var record = Bench.AddPlayer();
        Bench.CurrentHitPoints[PlayerCreature] = current;
        Bench.MaxHitPoints[PlayerCreature] = () => maximum;

        Service.AdjustPlayerMaxHP(record, PlayerCreature, 0);

        Bench.Engine.CountOf("SetCurrentHitPoints").Should().Be(lowered ? 1 : 0);
        if (lowered)
            Bench.Engine.CallsTo("SetCurrentHitPoints").Single().Arguments.Should().Equal(PlayerCreature, maximum);
    }

    [Test]
    public void RefreshPlayerMaxHP_SetsTheRecordToTheBasePlusGearPlusFood()
    {
        var record = Bench.AddPlayer();
        record.MaxHP = 5;
        record.TemporaryFoodHP = 30;
        Bench.Wear(PlayerCreature, InventorySlot.Chest, BaseItem.Armor, (ItemPropertyType.HPBonus, -1, 125));

        Service.RefreshPlayerMaxHP(PlayerCreature).Should().BeTrue();

        record.MaxHP.Should().Be(StatService.BaseHP + 125 + 30);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}", "LimitCheck:100", $"DB.Set:{PlayerUuid}");
    }

    [Test]
    public void RefreshPlayerMaxHP_IgnoresNegativeFood()
    {
        var record = Bench.AddPlayer();
        record.MaxHP = 5;
        record.TemporaryFoodHP = -30;

        Service.RefreshPlayerMaxHP(PlayerCreature).Should().BeTrue();

        record.MaxHP.Should().Be(StatService.BaseHP);
    }

    [Test]
    public void RefreshPlayerMaxHP_OfACorrectRecordWritesNothing()
    {
        var record = Bench.AddPlayer();
        record.MaxHP = StatService.BaseHP;

        Service.RefreshPlayerMaxHP(PlayerCreature).Should().BeFalse();

        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void RefreshPlayerMaxHP_SkipsAnythingButAPlayerWithARecord()
    {
        Service.RefreshPlayerMaxHP(ObjectInvalid).Should().BeFalse();
        Bench.AddNpc();
        Service.RefreshPlayerMaxHP(Npc).Should().BeFalse();

        Bench.AddPlayer();
        Bench.Dms.Add(PlayerCreature);
        Service.RefreshPlayerMaxHP(PlayerCreature).Should().BeFalse();
        Bench.Dms.Clear();
        Bench.Possessed.Add(PlayerCreature);
        Service.RefreshPlayerMaxHP(PlayerCreature).Should().BeFalse();
        Bench.Possessed.Clear();
        Bench.Log.Should().BeEmpty();

        Bench.Records.Clear();
        Service.RefreshPlayerMaxHP(PlayerCreature).Should().BeFalse();
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void GetEquippedHPBonus_SumsEverySlotAndCountsAPowerGlovePerHand()
    {
        Bench.AddPlayer();
        Bench.Wear(PlayerCreature, InventorySlot.Chest, BaseItem.Armor, (ItemPropertyType.HPBonus, -1, 10), (ItemPropertyType.Attack, -1, 99));
        Bench.Wear(PlayerCreature, InventorySlot.Arms, BaseItem.PowerGlove, (ItemPropertyType.HPBonus, -1, 7));

        Service.GetEquippedHPBonus(PlayerCreature).Should().Be(24);
    }

    [Test]
    public void GetEquippedStatTotals_CountsWornItemsSkipsTheCompanionPistolAndDoublesAPowerGlove()
    {
        Bench.AddPlayer();
        Bench.Wear(PlayerCreature, InventorySlot.Chest, BaseItem.Armor, (ItemPropertyType.Evasion, -1, 10), (ItemPropertyType.FP, -1, 3));
        Bench.Wear(PlayerCreature, InventorySlot.Arms, BaseItem.PowerGlove, (ItemPropertyType.Stamina, -1, 7));
        Bench.Wear(PlayerCreature, InventorySlot.LeftHand, BaseItem.CompanionPistol, (ItemPropertyType.Evasion, -1, 100));

        var totals = Service.GetEquippedStatTotals(PlayerCreature);

        totals.Evasion.Should().Be(10);
        totals.FP.Should().Be(3);
        totals.Stamina.Should().Be(14);
    }

    [Test]
    public void RefreshPlayerEquipmentStats_ReadsThenCorrectsThenCapsThePoolsThenWritesOnce()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 100;
        record.MaxStamina = 100;
        record.FP = 90;
        record.Stamina = 90;
        Bench.Wear(PlayerCreature, InventorySlot.Chest, BaseItem.Armor, (ItemPropertyType.FP, -1, 5), (ItemPropertyType.Stamina, -1, 6));

        var corrected = Service.RefreshPlayerEquipmentStats(PlayerCreature, EquipmentRecordStats.FP | EquipmentRecordStats.Stamina);

        corrected.Should().Be(EquipmentRecordStats.FP | EquipmentRecordStats.Stamina);
        record.MaxFP.Should().Be(StatService.BaseFP + 5);
        record.MaxStamina.Should().Be(StatService.BaseSTM + 6);
        record.FP.Should().Be(15, "a lower maximum caps the current pool");
        record.Stamina.Should().Be(16);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}", $"DB.Set:{PlayerUuid}");
    }

    [Test]
    public void RefreshPlayerEquipmentStats_LeavesAPoolAloneWhenItsRecordDidNotChange()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = StatService.BaseFP;
        record.FP = 500;
        record.Evasion = 1;

        Service.RefreshPlayerEquipmentStats(PlayerCreature, EquipmentRecordStats.FP | EquipmentRecordStats.Evasion)
            .Should().Be(EquipmentRecordStats.Evasion);

        record.FP.Should().Be(500);
        record.Evasion.Should().Be(0);
    }

    [Test]
    public void RefreshPlayerEquipmentStats_WithNothingToCorrectWritesNothing()
    {
        var record = Bench.AddPlayer();
        record.Evasion = 0;

        Service.RefreshPlayerEquipmentStats(PlayerCreature, EquipmentRecordStats.Evasion).Should().Be(EquipmentRecordStats.None);

        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void RefreshPlayerEquipmentStats_SkipsNothingSelectedAndAnythingButAPlayerWithARecord()
    {
        Bench.AddPlayer();
        Service.RefreshPlayerEquipmentStats(PlayerCreature, EquipmentRecordStats.None).Should().Be(EquipmentRecordStats.None);
        Service.RefreshPlayerEquipmentStats(ObjectInvalid).Should().Be(EquipmentRecordStats.None);
        Bench.AddNpc();
        Service.RefreshPlayerEquipmentStats(Npc).Should().Be(EquipmentRecordStats.None);
        Bench.Dms.Add(PlayerCreature);
        Service.RefreshPlayerEquipmentStats(PlayerCreature).Should().Be(EquipmentRecordStats.None);
        Bench.Dms.Clear();
        Bench.Possessed.Add(PlayerCreature);
        Service.RefreshPlayerEquipmentStats(PlayerCreature).Should().Be(EquipmentRecordStats.None);
        Bench.Possessed.Clear();
        Bench.Log.Should().BeEmpty();

        Bench.Records.Clear();
        Service.RefreshPlayerEquipmentStats(PlayerCreature).Should().Be(EquipmentRecordStats.None);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void ApplyEquipmentStatTotals_BuildsMissingMapsAndOnlyReportsWhatChanged()
    {
        var record = new Player { Defenses = null!, Control = null!, Craftsmanship = null!, CPBonus = null!, Resistances = null! };
        var totals = new EquipmentStatTotals();
        totals.Add(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 4);
        totals.Add(ItemPropertyType.Control, 1, 2);
        totals.Add(ItemPropertyType.Craftsmanship, 1, 3);
        totals.Add(ItemPropertyType.CPBonus, 1, 5);
        totals.Add(ItemPropertyType.Attack, -1, 6);

        var corrected = Service.ApplyEquipmentStatTotals(record, totals, EquipmentRecordStats.All);

        corrected.Should().HaveFlag(EquipmentRecordStats.Defense);
        corrected.Should().HaveFlag(EquipmentRecordStats.Control);
        corrected.Should().HaveFlag(EquipmentRecordStats.Craftsmanship);
        corrected.Should().HaveFlag(EquipmentRecordStats.CPBonus);
        corrected.Should().HaveFlag(EquipmentRecordStats.Attack);
        record.Defenses[CombatDamageType.Physical].Should().Be(4);
        record.Control[SkillType.Smithery].Should().Be(2);
        record.Craftsmanship[SkillType.Smithery].Should().Be(3);
        record.CPBonus[SkillType.Smithery].Should().Be(5);
        record.Attack.Should().Be(6);
        record.Resistances.Should().NotBeNull();

        Service.ApplyEquipmentStatTotals(record, totals, EquipmentRecordStats.All).Should().Be(EquipmentRecordStats.None);
    }

    [Test]
    public void ApplyEquipmentStatTotals_LeavesUnselectedRecordsAlone()
    {
        var record = new Player { Attack = 9, Evasion = 9 };

        Service.ApplyEquipmentStatTotals(record, new EquipmentStatTotals(), EquipmentRecordStats.Evasion)
            .Should().Be(EquipmentRecordStats.Evasion);

        record.Attack.Should().Be(9);
        record.Evasion.Should().Be(0);
    }

    [Test]
    public void AdjustPlayerMaxFP_CapsThePoolAtTheNewMaximumAndNeverBelowZero()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 20;
        record.FP = 20;

        Service.AdjustPlayerMaxFP(record, -5, PlayerCreature);
        record.MaxFP.Should().Be(15);
        record.FP.Should().Be(15);

        Service.AdjustPlayerMaxFP(record, -30, PlayerCreature);
        record.MaxFP.Should().Be(-15);
        record.FP.Should().Be(0, "the maximum may go negative but the pool does not");
    }

    [Test]
    public void AdjustPlayerMaxSTM_CapsThePoolAtTheNewMaximumAndNeverBelowZero()
    {
        var record = Bench.AddPlayer();
        record.MaxStamina = 20;
        record.Stamina = 20;

        Service.AdjustPlayerMaxSTM(record, -5, PlayerCreature);
        record.MaxStamina.Should().Be(15);
        record.Stamina.Should().Be(15);

        Service.AdjustPlayerMaxSTM(record, -30, PlayerCreature);
        record.MaxStamina.Should().Be(-15);
        record.Stamina.Should().Be(0);
    }

    [Test]
    public void ReapplyFoodHP_HandlerOnlyActsOnPlayers()
    {
        var record = Bench.AddPlayer();
        record.TemporaryFoodHP = 30;
        record.MaxHP = 100;
        Bench.Dms.Add(PlayerCreature);
        Bench.Engine.SetReturn("OBJECT_SELF", PlayerCreature);

        Service.ReapplyFoodHP();
        record.TemporaryFoodHP.Should().Be(30);

        Bench.Dms.Clear();
        Service.ReapplyFoodHP();
        record.TemporaryFoodHP.Should().Be(0);
    }

    [Test]
    public void ReapplyFoodHP_RemovesTheHPOfFoodThatIsGoneThenWrites()
    {
        var record = Bench.AddPlayer();
        record.TemporaryFoodHP = 30;
        record.MaxHP = 100;

        Service.ReapplyFoodHP(PlayerCreature);

        record.MaxHP.Should().Be(70);
        record.TemporaryFoodHP.Should().Be(0);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}", "LimitCheck:100", $"DB.Set:{PlayerUuid}");
    }

    [Test]
    public void ReapplyFoodHP_KeepsTheHPOfActiveFood()
    {
        var record = Bench.AddPlayer();
        record.TemporaryFoodHP = 30;
        record.MaxHP = 100;
        Bench.ActiveFoodEffect = new SWLOR.Game.Server.Feature.StatusEffectDefinition.FoodStatusEffect();

        Service.ReapplyFoodHP(PlayerCreature);

        record.MaxHP.Should().Be(100);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void ReapplyFoodHP_WithNoFoodHPWritesNothing()
    {
        Bench.AddPlayer();

        Service.ReapplyFoodHP(PlayerCreature);

        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void ReapplyFoodHP_SkipsAnyoneButAPlayer(bool isPlayer, bool isDm)
    {
        Bench.AddNpc();
        if (isPlayer)
            Bench.Players.Add(Npc);
        if (isDm)
            Bench.Dms.Add(Npc);

        Service.ReapplyFoodHP(Npc);

        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void ApplyPlayerStat_WritesTheScoreAndChecksTemporaryHitPointsOnlyForVitality()
    {
        var record = Bench.AddPlayer();
        record.BaseStats[AbilityType.Vitality] = 10;
        record.UpgradedStats[AbilityType.Vitality] = 3;
        record.RacialStat = AbilityType.Vitality;
        record.BaseStats[AbilityType.Might] = 8;

        Service.ApplyPlayerStat(record, PlayerCreature, AbilityType.Vitality);
        Bench.Creature.CallsTo("SetRawAbilityScore").Last().Arguments.Should().Equal(PlayerCreature, AbilityType.Vitality, 14);
        Bench.Log.Should().Equal("LimitCheck:100");

        Bench.Log.Clear();
        Service.ApplyPlayerStat(record, PlayerCreature, AbilityType.Might);
        Bench.Creature.CallsTo("SetRawAbilityScore").Last().Arguments.Should().Equal(PlayerCreature, AbilityType.Might, 8);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void ApplyPlayerStat_SkipsAnyoneButAPlayerAndAnInvalidAbility()
    {
        var record = Bench.AddPlayer();

        Service.ApplyPlayerStat(record, PlayerCreature, AbilityType.Invalid);
        Service.ApplyPlayerStat(record, ObjectInvalid, AbilityType.Might);
        Bench.Dms.Add(PlayerCreature);
        Service.ApplyPlayerStat(record, PlayerCreature, AbilityType.Might);

        Bench.Creature.CountOf("SetRawAbilityScore").Should().Be(0);
    }

    [Test]
    public void RestorePlayerAttributes_AssignsEverySavedTotalOfAPlayer()
    {
        var record = Bench.AddPlayer();
        record.BaseStats[AbilityType.Might] = 5;
        record.UpgradedStats[AbilityType.Might] = 2;

        Service.RestorePlayerAttributes(PlayerCreature);

        var scores = Bench.Creature.CallsTo("SetRawAbilityScore");
        scores.Should().HaveCount(record.BaseStats.Count);
        scores.Single(call => (AbilityType)call.Arguments[1]! == AbilityType.Might).Arguments[2].Should().Be(7);
    }

    [Test]
    public void RestorePlayerAttributes_SkipsStaffAndNpcs()
    {
        Bench.AddNpc();
        Service.RestorePlayerAttributes(Npc);
        Bench.AddPlayer();
        Bench.Dms.Add(PlayerCreature);
        Service.RestorePlayerAttributes(PlayerCreature);
        Bench.Dms.Clear();
        Bench.Possessed.Add(PlayerCreature);
        Service.RestorePlayerAttributes(PlayerCreature);

        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void GetCombatReadinessPercent_AddsTheRecordAndCapsOnlyTheSum()
    {
        var record = Bench.AddPlayer();
        record.CombatReadiness = 40;
        SetStat(PlayerCreature, StatType.CombatReadinessPercent, -30);

        Service.GetCombatReadinessPercent(PlayerCreature).Should().Be(10);

        record.CombatReadiness = 400;
        Service.GetCombatReadinessPercent(PlayerCreature).Should().Be(StatService.MaximumCombatReadinessPercent);

        record.CombatReadiness = 0;
        Service.GetCombatReadinessPercent(PlayerCreature).Should().Be(0);
    }

    [Test]
    public void GetCombatReadinessPercent_ToleratesAMissingRecordAndReadsAnNpcSkin()
    {
        Bench.AddPlayer();
        Bench.Records.Clear();
        SetStat(PlayerCreature, StatType.CombatReadinessPercent, 4);
        Service.GetCombatReadinessPercent(PlayerCreature).Should().Be(4);

        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.CombatReadiness, -1, 7));
        Service.GetCombatReadinessPercent(Npc).Should().Be(7);

        Service.GetCombatReadinessPercent(ObjectInvalid).Should().Be(0);
    }

    [Test]
    public void RefreshCombatReadinessEquipment_WritesOnlyAChange()
    {
        var record = Bench.AddPlayer();
        Bench.Wear(PlayerCreature, InventorySlot.Chest, BaseItem.Armor, (ItemPropertyType.CombatReadiness, -1, 40));

        Service.RefreshCombatReadinessEquipment(PlayerCreature);
        record.CombatReadiness.Should().Be(40, "the cap belongs to the read");
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}", $"DB.Set:{PlayerUuid}");

        Bench.Log.Clear();
        Service.RefreshCombatReadinessEquipment(PlayerCreature);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void RefreshCombatReadinessEquipment_SkipsAnythingButAPlayerWithARecord()
    {
        Service.RefreshCombatReadinessEquipment(ObjectInvalid);
        Bench.AddNpc();
        Service.RefreshCombatReadinessEquipment(Npc);
        Bench.AddPlayer();
        Bench.Dms.Add(PlayerCreature);
        Service.RefreshCombatReadinessEquipment(PlayerCreature);
        Bench.Dms.Clear();
        Bench.Possessed.Add(PlayerCreature);
        Service.RefreshCombatReadinessEquipment(PlayerCreature);
        Bench.Possessed.Clear();
        Bench.Log.Should().BeEmpty();

        Bench.Records.Clear();
        Service.RefreshCombatReadinessEquipment(PlayerCreature);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void GetEquippedCombatReadiness_SkipsTheCompanionPistolAndDoublesAPowerGlove()
    {
        Bench.AddPlayer();
        Bench.Wear(PlayerCreature, InventorySlot.Chest, BaseItem.Armor, (ItemPropertyType.CombatReadiness, -1, 3), (ItemPropertyType.Attack, -1, 50));
        Bench.Wear(PlayerCreature, InventorySlot.Arms, BaseItem.PowerGlove, (ItemPropertyType.CombatReadiness, -1, 4));
        Bench.Wear(PlayerCreature, InventorySlot.LeftHand, BaseItem.CompanionPistol, (ItemPropertyType.CombatReadiness, -1, 100));

        Service.GetEquippedCombatReadiness(PlayerCreature).Should().Be(11);
    }

    [Test]
    public void TheRecordAdjustments_MoveTheirOwnNumberOnly()
    {
        var record = new Player();

        Service.AdjustCombatReadiness(record, 1);
        Service.AdjustHPRegen(record, 2);
        Service.AdjustFPRegen(record, 3);
        Service.AdjustSTMRegen(record, 4);
        Service.AdjustEvasion(record, 5);
        Service.AdjustStealth(record, 6);
        Service.AdjustDetection(record, 7);
        Service.AdjustTrapBonus(record, 8);
        Service.AdjustTrapDisarm(record, 9);
        Service.AdjustPoisonBonus(record, 10);
        Service.AdjustLockpicking(record, 11);
        Service.AdjustAttack(record, 12);
        Service.AdjustAccuracy(record, 13);
        Service.AdjustForceAttack(record, 14);

        new[]
        {
            record.CombatReadiness, record.HPRegen, record.FPRegen, record.STMRegen, record.Evasion, record.Stealth, record.Detection,
            record.TrapBonus, record.TrapDisarm, record.PoisonBonus, record.Lockpicking, record.Attack, record.Accuracy, record.ForceAttack
        }.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
    }

    [Test]
    public void AdjustDefense_AddsToDefenseTypesAndIgnoresElementalOnes()
    {
        var record = new Player();

        Service.AdjustDefense(record, CombatDamageType.Physical, 3);
        Service.AdjustDefense(record, CombatDamageType.Physical, 4);
        Service.AdjustDefense(record, CombatDamageType.Fire, 9);

        record.Defenses[CombatDamageType.Physical].Should().Be(7);
        record.Defenses.Should().NotContainKey(CombatDamageType.Fire);
    }

    [Test]
    public void TheCraftAdjustments_StartMissingSkillsFromZero()
    {
        var record = new Player();

        Service.AdjustControl(record, SkillType.Smithery, 2);
        Service.AdjustControl(record, SkillType.Smithery, 3);
        Service.AdjustCraftsmanship(record, SkillType.Smithery, 4);
        Service.AdjustCraftsmanship(record, SkillType.Smithery, 5);
        Service.AdjustCPBonus(record, SkillType.Smithery, 6);
        Service.AdjustCPBonus(record, SkillType.Smithery, 7);

        record.Control[SkillType.Smithery].Should().Be(5);
        record.Craftsmanship[SkillType.Smithery].Should().Be(9);
        record.CPBonus[SkillType.Smithery].Should().Be(13);
    }
}
