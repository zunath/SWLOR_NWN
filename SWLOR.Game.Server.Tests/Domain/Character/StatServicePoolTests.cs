using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>The FP and Stamina pools: reads, what restoring and spending do, and the order they do it in.</summary>
public class StatServicePoolTests : StatServiceTestBase
{
    private const string PlayerUuid = "uuid-100";

    [TestCase(10, 5, 0, 25)]
    [TestCase(0, 0, 7, 7)]
    [TestCase(-5, -2, 1, -10)]
    public void GetMaxFP_AddsThreePerWillpowerAndTheBonus(int baseFP, int willpower, int bonus, int expected) =>
        Service.GetMaxFP(baseFP, willpower, bonus).Should().Be(expected);

    [TestCase(10, 5, 0, 17)]
    [TestCase(10, 2, 3, 16)]
    [TestCase(0, 1, 0, 1)]
    public void GetMaxStamina_AddsOneAndAHalfPerMightRoundedDown(int baseStamina, int might, int bonus, int expected) =>
        Service.GetMaxStamina(baseStamina, might, bonus).Should().Be(expected);

    [Test]
    public void GetMaxFP_OfAPlayerReadsTheRecordOnceWhenNoneIsGiven()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 20;
        Bench.Abilities[(PlayerCreature, AbilityType.Willpower)] = 10;
        SetStat(PlayerCreature, StatType.MaxFP, 5);

        Service.GetMaxFP(PlayerCreature).Should().Be(55);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");

        Bench.Log.Clear();
        Service.GetMaxFP(PlayerCreature, record).Should().Be(55);
        Bench.Log.Should().BeEmpty("a supplied record is not read again");
    }

    [Test]
    public void GetMaxStamina_OfAPlayerReadsTheRecordOnceWhenNoneIsGiven()
    {
        var record = Bench.AddPlayer();
        record.MaxStamina = 20;
        Bench.Abilities[(PlayerCreature, AbilityType.Might)] = 4;
        SetStat(PlayerCreature, StatType.MaxStamina, 2);

        Service.GetMaxStamina(PlayerCreature).Should().Be(28);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");

        Bench.Log.Clear();
        Service.GetMaxStamina(PlayerCreature, record).Should().Be(28);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void GetMaxPools_OfAnNpcComeFromItsSkin()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.FP, -1, 40), (ItemPropertyType.Stamina, -1, 30));
        Bench.Abilities[(Npc, AbilityType.Willpower)] = 2;
        Bench.Abilities[(Npc, AbilityType.Might)] = 2;

        Service.GetMaxFP(Npc).Should().Be(46);
        Service.GetMaxStamina(Npc).Should().Be(33);
        Bench.Log.Should().BeEmpty();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void GetMaxPools_OfStaffAndPossessedCreaturesComeFromTheSkin(bool dm, bool possessed)
    {
        Bench.AddNpc();
        Bench.Players.Add(Npc);
        if (dm)
            Bench.Dms.Add(Npc);
        if (possessed)
            Bench.Possessed.Add(Npc);
        Bench.Skin(Npc, (ItemPropertyType.FP, -1, 40), (ItemPropertyType.Stamina, -1, 30));

        Service.GetMaxFP(Npc).Should().Be(40);
        Service.GetMaxStamina(Npc).Should().Be(30);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void CurrentPools_OfAPlayerComeFromTheRecord()
    {
        var record = Bench.AddPlayer();
        record.FP = 12;
        record.Stamina = 34;

        Service.GetCurrentFP(PlayerCreature).Should().Be(12);
        Service.GetCurrentStamina(PlayerCreature).Should().Be(34);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}", $"DB.Get:{PlayerUuid}");

        Bench.Log.Clear();
        Service.GetCurrentFP(PlayerCreature, record).Should().Be(12);
        Service.GetCurrentStamina(PlayerCreature, record).Should().Be(34);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void CurrentPools_OfAnNpcComeFromItsLocals()
    {
        Bench.AddNpc();
        Bench.SetLocal(Npc, "FP", 7);
        Bench.SetLocal(Npc, "STAMINA", 9);

        Service.GetCurrentFP(Npc).Should().Be(7);
        Service.GetCurrentStamina(Npc).Should().Be(9);
    }

    [TestCase(0, 50, -2, 0)]
    [TestCase(-3, 50, -2, 0)]
    [TestCase(10, 50, -2, 13)]
    [TestCase(10, 0, 0, 10)]
    [TestCase(7, 50, 0, 11)]
    [TestCase(10, -100, -5, 0)]
    public void GetAdjustedRequiredFP_AppliesThePercentRoundedUpThenTheFlatAmountAndNeverGoesNegative(int required, int percent, int flat, int expected)
    {
        SetStat(PlayerCreature, StatType.FPCostPercentAdjustment, percent);
        SetStat(PlayerCreature, StatType.FPCostFlatAdjustment, flat);

        Service.GetAdjustedRequiredFP(PlayerCreature, required).Should().Be(expected);
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void RestoreFP_OfNothingDoesNothing(int amount)
    {
        Service.RestoreFP(PlayerCreature, amount).Should().Be(0);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void RestoreFP_ThatItsAdjustmentRemovesDoesNothing()
    {
        Bench.AddPlayer();
        SetStat(PlayerCreature, StatType.FPRestorePercentAdjustment, -100);

        Service.RestoreFP(PlayerCreature, 10).Should().Be(0);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void RestoreFP_OfAPlayerWithNoRecordDoesNothing()
    {
        Bench.AddPlayer();
        Bench.Records.Clear();

        Service.RestoreFP(PlayerCreature, 10).Should().Be(0);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void RestoreFP_WritesThenRecordsThenRunsTheScriptThenReactsThenReports()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 40;
        record.FP = 20;
        Bench.Recording = true;

        Service.RestoreFP(PlayerCreature, 5).Should().Be(5);

        record.FP.Should().Be(25);
        Bench.Log.Should().Equal(
            $"DB.Get:{PlayerUuid}",
            $"DB.Set:{PlayerUuid}",
            "RecordChange:FP:5:25",
            "Script:pc_fp_adjusted",
            "FPRestoredEffects:100",
            "Feedback:5:FP");
    }

    [Test]
    public void RestoreFP_AppliesItsAdjustmentBeforeTheCapAndOnlyReactsToARealGain()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 40;
        record.FP = 38;
        SetStat(PlayerCreature, StatType.FPRestorePercentAdjustment, 50);

        Service.RestoreFP(PlayerCreature, 1, record).Should().Be(2, "1 becomes 2 and the cap leaves room for 2");
        Bench.Log.Should().Contain("FPRestoredEffects:100");

        Bench.Log.Clear();
        Service.RestoreFP(PlayerCreature, 1, record).Should().Be(0);
        Bench.Log.Should().NotContain("FPRestoredEffects:100");
        Bench.Log.Should().Contain("Feedback:0:FP", "the report still says nothing was gained");
    }

    [Test]
    public void RestoreFP_CanLeaveOutTheReport()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 40;

        Service.RestoreFP(PlayerCreature, 5, record, false).Should().Be(5);
        Bench.Log.Should().NotContain(entry => entry.StartsWith("Feedback", StringComparison.Ordinal));
    }

    [Test]
    public void RestoreFP_OfAnNpcUsesItsLocal()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.FP, -1, 10));
        Bench.SetLocal(Npc, "FP", 8);
        Bench.Recording = true;

        Service.RestoreFP(Npc, 5).Should().Be(2);

        Bench.Locals.Int(Npc, "FP").Should().Be(10);
        Bench.Log.Should().Equal("RecordChange:FP:2:10", "Script:pc_fp_adjusted", "FPRestoredEffects:200", "Feedback:2:FP");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ReduceFP_OfNothingDoesNothing(int amount)
    {
        Service.ReduceFP(PlayerCreature, amount);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void ReduceFP_RecordsBeforeItChangesThePool()
    {
        var record = Bench.AddPlayer();
        record.FP = 20;
        Bench.Recording = true;

        Service.ReduceFP(PlayerCreature, 3);

        record.FP.Should().Be(17);
        Bench.Log.Should().Equal(
            $"DB.Get:{PlayerUuid}",
            "RecordReduction:FP:3:20",
            $"DB.Get:{PlayerUuid}",
            $"DB.Set:{PlayerUuid}",
            "Script:pc_fp_adjusted");
    }

    [Test]
    public void ReduceFP_NeverGoesBelowZero()
    {
        var record = Bench.AddPlayer();
        record.FP = 2;

        Service.ReduceFP(PlayerCreature, 5, record);

        record.FP.Should().Be(0);
        Bench.Log.Should().Equal($"DB.Set:{PlayerUuid}", "Script:pc_fp_adjusted");
    }

    [TestCase(5, 2, 3)]
    [TestCase(2, 5, 0)]
    public void ReduceFP_OfAnNpcUsesItsLocal(int start, int by, int expected)
    {
        Bench.AddNpc();
        Bench.SetLocal(Npc, "FP", start);

        Service.ReduceFP(Npc, by);

        Bench.Locals.Int(Npc, "FP").Should().Be(expected);
        Bench.Log.Should().Equal("Script:pc_fp_adjusted");
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void StaminaRestoration_IsBlockedOnlyForAStealthedPlayer(bool isPlayer, bool inStealth, bool expected) =>
        Service.IsStaminaRestorationBlocked(isPlayer, inStealth).Should().Be(expected);

    [Test]
    public void RestoreStamina_OfAStealthedPlayerStopsBeforeTheRecordIsRead()
    {
        Bench.AddPlayer();
        Bench.ActionModes.Add((PlayerCreature, ActionMode.Stealth));

        Service.RestoreStamina(PlayerCreature, 5).Should().Be(0);
        Bench.Log.Should().BeEmpty();
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void RestoreStamina_OfNothingDoesNothing(int amount)
    {
        Service.RestoreStamina(PlayerCreature, amount).Should().Be(0);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void RestoreStamina_OfAPlayerWithNoRecordDoesNothing()
    {
        Bench.AddPlayer();
        Bench.Records.Clear();

        Service.RestoreStamina(PlayerCreature, 10).Should().Be(0);
        Bench.Log.Should().Equal($"DB.Get:{PlayerUuid}");
    }

    [Test]
    public void RestoreStamina_WritesThenRecordsThenRunsTheScriptThenReactsThenReports()
    {
        var record = Bench.AddPlayer();
        record.MaxStamina = 40;
        record.Stamina = 20;
        Bench.Recording = true;

        Service.RestoreStamina(PlayerCreature, 5).Should().Be(5);

        record.Stamina.Should().Be(25);
        Bench.Log.Should().Equal(
            $"DB.Get:{PlayerUuid}",
            $"DB.Set:{PlayerUuid}",
            "RecordChange:Stamina:5:25",
            "Script:pc_stm_adjusted",
            "StaminaRestoredEffects:100",
            "Feedback:5:STM");
    }

    [Test]
    public void RestoreStamina_AtTheCapDoesNotReactAndCanLeaveOutTheReport()
    {
        var record = Bench.AddPlayer();
        record.MaxStamina = 10;
        record.Stamina = 10;

        Service.RestoreStamina(PlayerCreature, 5, record, false).Should().Be(0);

        Bench.Log.Should().Equal($"DB.Set:{PlayerUuid}", "Script:pc_stm_adjusted");
    }

    [Test]
    public void RestoreStamina_OfAnNpcUsesItsLocal()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.Stamina, -1, 10));
        Bench.SetLocal(Npc, "STAMINA", 8);
        Bench.Recording = true;

        Service.RestoreStamina(Npc, 5).Should().Be(2);

        Bench.Locals.Int(Npc, "STAMINA").Should().Be(10);
        Bench.Log.Should().Equal("RecordChange:Stamina:2:10", "Script:pc_stm_adjusted", "StaminaRestoredEffects:200", "Feedback:2:STM");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ReduceStamina_OfNothingDoesNothing(int amount)
    {
        Service.ReduceStamina(PlayerCreature, amount);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void ReduceStamina_RecordsBeforeItChangesThePool()
    {
        var record = Bench.AddPlayer();
        record.Stamina = 20;
        Bench.Recording = true;

        Service.ReduceStamina(PlayerCreature, 25);

        record.Stamina.Should().Be(0);
        Bench.Log.Should().Equal(
            $"DB.Get:{PlayerUuid}",
            "RecordReduction:Stamina:25:20",
            $"DB.Get:{PlayerUuid}",
            $"DB.Set:{PlayerUuid}",
            "Script:pc_stm_adjusted");
    }

    [Test]
    public void ReduceStamina_OfAnOrdinaryNpcLeavesNoRegenerationDelay()
    {
        Bench.AddNpc();
        Bench.SetLocal(Npc, "STAMINA", 5);

        Service.ReduceStamina(Npc, 9);

        Bench.Locals.Int(Npc, "STAMINA").Should().Be(0);
        Bench.Locals.String(Npc, "BEAST_STAMINA_REGEN_AVAILABLE_AT").Should().BeEmpty();
    }

    [Test]
    public void ReduceStamina_OfABeastDelaysItsRegeneration()
    {
        Bench.AddNpc();
        Bench.Beasts.Add(Npc);
        Bench.SetLocal(Npc, "STAMINA", 5);
        var before = DateTime.UtcNow;

        Service.ReduceStamina(Npc, 2);

        Bench.Locals.Int(Npc, "STAMINA").Should().Be(3);
        var availableAt = new DateTime(long.Parse(Bench.Locals.String(Npc, "BEAST_STAMINA_REGEN_AVAILABLE_AT"), CultureInfo.InvariantCulture), DateTimeKind.Utc);
        availableAt.Should().BeOnOrAfter(before.AddSeconds(StatService.BeastNaturalStaminaRegenDelaySeconds));
    }

    [Test]
    public void ReduceStamina_OfABeastAlreadyAtZeroLeavesNoDelay()
    {
        Bench.AddNpc();
        Bench.Beasts.Add(Npc);

        Service.ReduceStamina(Npc, 2);

        Bench.Locals.String(Npc, "BEAST_STAMINA_REGEN_AVAILABLE_AT").Should().BeEmpty();
    }

    [Test]
    public void Recorder_IsNotAskedForTheValueWhileNothingRecords()
    {
        var record = Bench.AddPlayer();
        record.MaxFP = 40;

        Service.RestoreFP(PlayerCreature, 5, record);

        Bench.Log.Should().NotContain(entry => entry.StartsWith("RecordChange", StringComparison.Ordinal));
    }
}
