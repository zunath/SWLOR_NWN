using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Walk mode and the movement rate.</summary>
public class StatServiceMovementTests : StatServiceTestBase
{
    private const string WalkToggle = "WALK_TOGGLE";
    private const string WalkRestore = "WALK_RESTORE_MOVEMENT_RATE";
    private const string WalkRestoreBase = "WALK_RESTORE_BASE_MOVEMENT_RATE";
    private const string WalkRestoreMaximum = "WALK_RESTORE_MAX_MOVEMENT_RATE";

    [SetUp]
    public void TrackTheNativeFactor()
    {
        Bench.Creature.SetReturn("GetMovementRateFactor", 1.25f);
    }

    private float LastFactor => (float)Bench.Creature.CallsTo("SetMovementRateFactor").Last().Arguments[1]!;

    private void SetFloatLocal(uint creature, string name, float value) => Bench.Engine.SetLocalFloat(creature, name, value);

    [Test]
    public void IsWalkModeEnabled_NeedsTheToggleAndAPlayerOrStaff()
    {
        Bench.AddNpc();
        Bench.SetLocal(Npc, WalkToggle, 1);
        Service.IsWalkModeEnabled(Npc).Should().BeFalse();

        Bench.Players.Add(Npc);
        Service.IsWalkModeEnabled(Npc).Should().BeTrue();
        Bench.Players.Clear();

        Bench.Dms.Add(Npc);
        Service.IsWalkModeEnabled(Npc).Should().BeTrue();
        Bench.Dms.Clear();

        Bench.Possessed.Add(Npc);
        Service.IsWalkModeEnabled(Npc).Should().BeTrue();

        Bench.SetLocal(Npc, WalkToggle, 0);
        Service.IsWalkModeEnabled(Npc).Should().BeFalse();
    }

    [Test]
    public void ToggleWalkMode_OfAPlayerSetsTheToggleThenTheEngineFlagThenTheRate()
    {
        Bench.AddPlayer();

        Service.ToggleWalkMode(PlayerCreature).Should().BeTrue();

        Bench.Locals.Int(PlayerCreature, WalkToggle).Should().Be(1);
        Bench.PlayerPlugin.CallsTo("SetAlwaysWalk").Single().Arguments.Should().Equal(PlayerCreature, true);
        Bench.Creature.CallsTo("SetMovementRate").Should().ContainSingle();
        LastFactor.Should().Be(1f);
        Bench.Locals.Has(PlayerCreature, WalkRestore).Should().BeFalse("only staff snapshot their rate");

        Service.ToggleWalkMode(PlayerCreature).Should().BeFalse();
        Bench.PlayerPlugin.CallsTo("SetAlwaysWalk").Last().Arguments.Should().Equal(PlayerCreature, false);
        LastFactor.Should().BeApproximately(1.25f, 0.001f);
    }

    [Test]
    public void ToggleWalkMode_OfStaffOnTheCalculatedRateSnapshotsTheNormalBaseAndCap()
    {
        Bench.AddNpc();
        Bench.Dms.Add(Npc);
        Bench.Creature.SetReturn("GetMovementRateFactor", 1.3f);

        Service.ToggleWalkMode(Npc).Should().BeTrue();

        Bench.Locals.Float(Npc, WalkRestore).Should().BeApproximately(1.3f, 0.001f);
        Bench.Locals.Float(Npc, WalkRestoreBase).Should().BeApproximately(1.3f, 0.001f);
        Bench.Locals.Float(Npc, WalkRestoreMaximum).Should().Be(StatService.MaximumMovementSpeedMultiplier);
    }

    [Test]
    public void ToggleWalkMode_OfStaffOnAForeignRateSnapshotsItWithoutACap()
    {
        Bench.AddNpc();
        Bench.Possessed.Add(Npc);
        Bench.Creature.SetReturn("GetMovementRateFactor", 2f);

        Service.ToggleWalkMode(Npc);

        Bench.Locals.Float(Npc, WalkRestore).Should().Be(2f);
        Bench.Locals.Float(Npc, WalkRestoreBase).Should().Be(2f);
        Bench.Locals.Float(Npc, WalkRestoreMaximum).Should().Be(float.MaxValue);
    }

    [Test]
    public void ToggleWalkMode_OfStaffWhoCannotMoveMarksTheSnapshotPending()
    {
        Bench.AddNpc();
        Bench.Dms.Add(Npc);
        SetStat(Npc, StatType.MovementSpeedDisabled, 1);

        Service.ToggleWalkMode(Npc);

        Bench.Locals.Float(Npc, WalkRestore).Should().Be(-1f);
        Bench.Locals.Has(Npc, WalkRestoreBase).Should().BeFalse();
    }

    [Test]
    public void ToggleWalkMode_OfStaffKeepsAnExistingSnapshot()
    {
        Bench.AddNpc();
        Bench.Dms.Add(Npc);
        SetFloatLocal(Npc, WalkRestore, 1.7f);
        SetFloatLocal(Npc, WalkRestoreBase, 1.7f);
        SetFloatLocal(Npc, WalkRestoreMaximum, float.MaxValue);

        Service.ToggleWalkMode(Npc);

        Bench.Locals.Float(Npc, WalkRestore).Should().Be(1.7f);
        LastFactor.Should().Be(1f, "a walking creature is capped at the walk rate");
    }

    [Test]
    public void ToggleWalkMode_OfStaffLeavingWithoutASnapshotKeepsTheNativeRate()
    {
        Bench.AddNpc();
        Bench.Dms.Add(Npc);
        Bench.SetLocal(Npc, WalkToggle, 1);

        Service.ToggleWalkMode(Npc).Should().BeFalse();

        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(0);
    }

    [Test]
    public void ApplyCreatureMovementRate_SkipsWhatIsNotAValidCreature()
    {
        Service.ApplyCreatureMovementRate(ObjectInvalid);
        Bench.AddNpc();
        Bench.ObjectTypes[Npc] = ObjectType.Placeable;
        Service.ApplyCreatureMovementRate(Npc);

        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(0);
    }

    [Test]
    public void ApplyCreatureMovementRate_OfAPlayerAlsoSetsTheEngineRate()
    {
        Bench.AddPlayer();

        Service.ApplyCreatureMovementRate(PlayerCreature);

        Bench.Creature.CallsTo("SetMovementRate").Single().Arguments.Should().Equal(PlayerCreature, MovementRate.PC);
        LastFactor.Should().BeApproximately(1.25f, 0.001f);
    }

    [Test]
    public void ApplyCreatureMovementRate_OfAnNpcLeavesTheEngineRate()
    {
        Bench.AddNpc();

        Service.ApplyCreatureMovementRate(Npc);

        Bench.Creature.CountOf("SetMovementRate").Should().Be(0);
        LastFactor.Should().BeApproximately(1.3f, 0.001f);
    }

    [Test]
    public void ApplyCreatureMovementRate_ClearsAToggleTheCreatureCanNoLongerHold()
    {
        Bench.AddNpc();
        Bench.SetLocal(Npc, WalkToggle, 1);

        Service.ApplyCreatureMovementRate(Npc);

        Bench.Locals.Int(Npc, WalkToggle).Should().Be(0);
        LastFactor.Should().BeApproximately(1.3f, 0.001f);
    }

    [Test]
    public void ApplyCreatureMovementRate_ClearsTheSnapshotOnceTheCreatureCanMoveAndDoesNotWalk()
    {
        Bench.AddNpc();
        Bench.Dms.Add(Npc);
        SetFloatLocal(Npc, WalkRestore, 1.4f);
        SetFloatLocal(Npc, WalkRestoreBase, 1.4f);
        SetFloatLocal(Npc, WalkRestoreMaximum, 1.5f);

        Service.ApplyCreatureMovementRate(Npc);

        LastFactor.Should().BeApproximately(1.4f, 0.001f);
        Bench.Locals.Float(Npc, WalkRestore).Should().Be(0f);
        Bench.Locals.Float(Npc, WalkRestoreBase).Should().Be(0f);
        Bench.Locals.Float(Npc, WalkRestoreMaximum).Should().Be(0f);
    }

    [Test]
    public void ApplyCreatureMovementRate_KeepsTheSnapshotWhileTheCreatureCannotMove()
    {
        Bench.AddNpc();
        Bench.Dms.Add(Npc);
        SetFloatLocal(Npc, WalkRestore, 1.4f);
        SetStat(Npc, StatType.MovementSpeedDisabled, 1);

        Service.ApplyCreatureMovementRate(Npc);

        LastFactor.Should().Be(0f);
        Bench.Locals.Float(Npc, WalkRestore).Should().Be(1.4f);
    }

    [Test]
    public void RestoreWalkMode_SetsTheFlagFromTheToggleThenTheRate()
    {
        Bench.AddPlayer();
        Bench.SetLocal(PlayerCreature, WalkToggle, 1);

        Service.RestoreWalkMode(PlayerCreature);

        Bench.PlayerPlugin.CallsTo("SetAlwaysWalk").Single().Arguments.Should().Equal(PlayerCreature, true);
        LastFactor.Should().Be(1f);
    }

    [Test]
    public void ApplyPlayerStats_RestoresWalkModeForAnEnteringPlayerOnly()
    {
        Bench.AddPlayer();
        Bench.Engine.SetReturn("GetEnteringObject", PlayerCreature);
        Service.ApplyPlayerStats();
        Bench.PlayerPlugin.CountOf("SetAlwaysWalk").Should().Be(1);

        Bench.Dms.Add(PlayerCreature);
        Service.ApplyPlayerStats();
        Bench.PlayerPlugin.CountOf("SetAlwaysWalk").Should().Be(1);

        Bench.AddNpc();
        Bench.Engine.SetReturn("GetEnteringObject", Npc);
        Service.ApplyPlayerStats();
        Bench.PlayerPlugin.CountOf("SetAlwaysWalk").Should().Be(1);
    }

    [Test]
    public void ClearWalkModeBeforeDMPossessionChange_TurnsOffAPossessedCreaturesWalk()
    {
        Bench.AddNpc();
        Bench.Possessed.Add(Npc);
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);
        Bench.SetLocal(Npc, WalkToggle, 1);

        Service.ClearWalkModeBeforeDMPossessionChange();

        Bench.Locals.Int(Npc, WalkToggle).Should().Be(0);
        Bench.PlayerPlugin.CallsTo("SetAlwaysWalk").Single().Arguments.Should().Equal(Npc, false);
    }

    [Test]
    public void ClearWalkModeBeforeDMPossessionChange_SkipsAnyoneNotPossessedOrNotWalking()
    {
        Bench.AddNpc();
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);
        Service.ClearWalkModeBeforeDMPossessionChange();

        Bench.Possessed.Add(Npc);
        Service.ClearWalkModeBeforeDMPossessionChange();

        Bench.PlayerPlugin.CountOf("SetAlwaysWalk").Should().Be(0);
    }

    [Test]
    public void RefreshOutOfCombatMovementRate_AppliesOnlyWhenTheCombatStateChanges()
    {
        Bench.AddPlayer();
        Bench.Engine.SetReturn("OBJECT_SELF", PlayerCreature);
        SetStat(PlayerCreature, StatType.OutOfCombatMovementSpeedPercentAdjustment, 10);

        Service.RefreshOutOfCombatMovementRate();
        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(1, "the first heartbeat learns the state");

        Service.RefreshOutOfCombatMovementRate();
        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(1);

        Bench.InCombat.Add(PlayerCreature);
        Service.RefreshOutOfCombatMovementRate();
        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(2);
    }

    [Test]
    public void RefreshOutOfCombatMovementRate_ForgetsAPlayerWithoutTheStat()
    {
        Bench.AddPlayer();
        Bench.Engine.SetReturn("OBJECT_SELF", PlayerCreature);
        SetStat(PlayerCreature, StatType.OutOfCombatMovementSpeedPercentAdjustment, 10);
        Service.RefreshOutOfCombatMovementRate();

        SetStat(PlayerCreature, StatType.OutOfCombatMovementSpeedPercentAdjustment, 0);
        Service.RefreshOutOfCombatMovementRate();
        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(1);

        SetStat(PlayerCreature, StatType.OutOfCombatMovementSpeedPercentAdjustment, 10);
        Service.RefreshOutOfCombatMovementRate();
        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(2, "the state was forgotten, so this is a first heartbeat");
    }

    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    public void RefreshOutOfCombatMovementRate_SkipsStaffAndNpcs(bool npc, bool dm, bool possessed)
    {
        Bench.AddPlayer();
        var creature = PlayerCreature;
        if (npc)
            Bench.Players.Clear();
        if (dm)
            Bench.Dms.Add(creature);
        if (possessed)
            Bench.Possessed.Add(creature);
        Bench.Engine.SetReturn("OBJECT_SELF", creature);
        SetStat(creature, StatType.OutOfCombatMovementSpeedPercentAdjustment, 10);

        Service.RefreshOutOfCombatMovementRate();

        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(0);
    }

    [Test]
    public void ForgetOutOfCombatMovementState_MakesTheNextHeartbeatAFirstOne()
    {
        Bench.AddPlayer();
        Bench.Engine.SetReturn("OBJECT_SELF", PlayerCreature);
        Bench.Engine.SetReturn("GetExitingObject", PlayerCreature);
        SetStat(PlayerCreature, StatType.OutOfCombatMovementSpeedPercentAdjustment, 10);
        Service.RefreshOutOfCombatMovementRate();

        Service.ForgetOutOfCombatMovementState();
        Service.RefreshOutOfCombatMovementRate();

        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(2);
    }

    [Test]
    public void GetMovementSpeedMultiplier_OfWhatIsNotAValidCreatureIsOne()
    {
        Service.GetMovementSpeedMultiplier(ObjectInvalid).Should().Be(1f);
        Bench.AddNpc();
        Bench.ObjectTypes[Npc] = ObjectType.Door;
        Service.GetMovementSpeedMultiplier(Npc).Should().Be(1f);
    }

    [Test]
    public void GetMovementSpeedMultiplier_OfADisabledCreatureIsZero()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.MovementSpeedDisabled, 1);

        Service.GetMovementSpeedMultiplier(Npc).Should().Be(0f);
    }

    [Test]
    public void GetMovementSpeedMultiplier_StartsFromTheBaseOfTheKindOfCreature()
    {
        Bench.AddPlayer();
        Service.GetMovementSpeedMultiplier(PlayerCreature).Should().BeApproximately(1.25f, 0.001f);

        Bench.AddNpc();
        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(1.30f, 0.001f);

        Service.Droids.Add(Npc);
        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(1.25f, 0.001f);
        Service.Droids.Clear();

        Bench.Beasts.Add(Npc);
        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(1.25f, 0.001f);
    }

    [Test]
    public void GetMovementSpeedMultiplier_AddsStatsAndNativeEffectsThenClamps()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.MovementSpeedPercentAdjustment, -10);
        var effects = new[] { (EffectTypeScript.MovementSpeedIncrease, 120), (EffectTypeScript.MovementSpeedDecrease, 5), (EffectTypeScript.Haste, 999) };
        var cursor = 0;
        Bench.Engine.SetReturnHandler("GetFirstEffect", _ => { cursor = 0; return EngineStructures.Create<Effect>(cursor + 1); });
        Bench.Engine.SetReturnHandler("GetNextEffect", _ => { cursor++; return EngineStructures.Create<Effect>(cursor < effects.Length ? cursor + 1 : 0); });
        Bench.Engine.SetReturnHandler("GetIsEffectValid", args => ((Effect)args[0]!).Handle != 0);
        Bench.Engine.SetReturnHandler("GetEffectType", _ => effects[cursor].Item1);
        Bench.Engine.SetReturnHandler("GetEffectInteger", _ => effects[cursor].Item2);

        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(1.30f - 0.10f + 0.20f - 0.05f, 0.001f);

        SetStat(Npc, StatType.MovementSpeedPercentAdjustment, 500);
        Service.GetMovementSpeedMultiplier(Npc).Should().Be(StatService.MaximumMovementSpeedMultiplier);

        SetStat(Npc, StatType.MovementSpeedPercentAdjustment, -500);
        Service.GetMovementSpeedMultiplier(Npc).Should().Be(StatService.MinimumMovementSpeedMultiplier);
    }

    [Test]
    public void GetMovementSpeedMultiplier_CountsOutOfCombatSpeedOnlyOutOfCombat()
    {
        Bench.AddNpc();
        SetStat(Npc, StatType.OutOfCombatMovementSpeedPercentAdjustment, 10);

        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(1.40f, 0.001f);

        Bench.InCombat.Add(Npc);
        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(1.30f, 0.001f);
    }

    [Test]
    public void GetMovementSpeedMultiplier_OfAWalkerIsCappedAtTheWalkRate()
    {
        Bench.AddPlayer();
        Bench.SetLocal(PlayerCreature, WalkToggle, 1);
        SetStat(PlayerCreature, StatType.MovementSpeedPercentAdjustment, 20);

        Service.GetMovementSpeedMultiplier(PlayerCreature).Should().Be(1f);

        SetStat(PlayerCreature, StatType.MovementSpeedPercentAdjustment, -20);
        Service.GetMovementSpeedMultiplier(PlayerCreature).Should().BeApproximately(0.8f, 0.001f);
    }

    [Test]
    public void GetMovementSpeedMultiplier_OfStaffUsesTheSnapshot()
    {
        Bench.AddNpc();
        Bench.Dms.Add(Npc);
        SetFloatLocal(Npc, WalkRestore, 1.4f);
        SetFloatLocal(Npc, WalkRestoreBase, 1.6f);
        SetFloatLocal(Npc, WalkRestoreMaximum, 1.55f);
        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(1.55f, 0.001f, "the snapshot's own cap applies");

        SetFloatLocal(Npc, WalkRestoreBase, 0.7f);
        Bench.SetLocal(Npc, WalkToggle, 1);
        Service.GetMovementSpeedMultiplier(Npc).Should().BeApproximately(0.7f, 0.001f, "a walker keeps a snapshot below the walk rate");

        SetFloatLocal(Npc, WalkRestoreBase, 1.6f);
        Service.GetMovementSpeedMultiplier(Npc).Should().Be(1f);
    }
}
