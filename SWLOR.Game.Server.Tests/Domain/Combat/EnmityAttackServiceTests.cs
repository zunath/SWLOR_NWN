using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>The enmity attack part: the attack target by enmity and the attack commands that follow it.</summary>
[TestFixture]
public class EnmityAttackServiceTests : EnmityPartsTestBase
{
    [Test]
    public void GetHighestEnmityAttackTarget_SkipsThoseTheEnemyCannotAttack()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _change.ModifyEnmity(Player2, Enemy, 9);

        _dead.Add(Player2);
        _attack.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _dead.Clear();
        _hitPoints[Player2] = 0;
        _attack.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _hitPoints.Clear();
        _areas[Player2] = AreaB;
        _attack.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _areas.Clear();
        _attack.Hidden.Add((Enemy, Player2));
        _attack.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _attack.Hidden.Add((Enemy, Player));
        _attack.GetHighestEnmityAttackTarget(Enemy).Should().Be(ObjectInvalid);
    }

    [Test]
    public void AttackHighestEnmityTarget_SendsTheEnemyAfterItsTargetOnceAndRunsTheCommand()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _change.ModifyEnmity(Player, Enemy, 5);
        _engine.CallsTo("AssignCommand").Should().ContainSingle("the second call is throttled by the recent command");

        _distances[(Enemy, Player)] = 30f;
        _attack.WeaponSkill = SkillType.Rifle;
        RunAssigned();

        _engine.CountOf("ClearAllActions").Should().Be(1);
        _engine.CountOf("ActionMoveToObject").Should().Be(1);
        _engine.CallsTo("ActionAttack").Should().ContainSingle().Which.Arguments[0].Should().Be(Player);
    }

    [Test]
    public void IssueAttackCommand_StaysWhereItIsWhenTheTargetIsInRange()
    {
        _attack.IssueAttackCommand(Enemy, Player, clearActions: false);
        _distances[(Enemy, Player)] = 1f;
        RunAssigned();

        _engine.CountOf("ClearAllActions").Should().Be(0);
        _engine.CountOf("ActionMoveToObject").Should().Be(0);
        _engine.CountOf("ActionAttack").Should().Be(1);
    }

    [Test]
    public void IssueAttackCommand_DoesNotMovePlayersAndDoesNotAttackWhatItCannotSee()
    {
        _attack.IssueAttackCommand(Player, Enemy);
        _distances[(Player, Enemy)] = 30f;
        RunAssigned();
        _engine.CountOf("ActionMoveToObject").Should().Be(0);

        _engine.ClearCalls();
        _attack.IssueAttackCommand(Enemy, Player);
        _attack.Hidden.Add((Enemy, Player));
        RunAssigned();
        _engine.CountOf("ActionAttack").Should().Be(0);
    }

    [Test]
    public void IssueAttackCommand_RefusesInvalidTargetsOtherAreasDeniedCommandsAndLeashEvades()
    {
        _attack.IssueAttackCommand(ObjectInvalid, Player);
        _attack.IssueAttackCommand(Enemy, ObjectInvalid);
        _areas[Player] = AreaB;
        _attack.IssueAttackCommand(Enemy, Player);
        _areas.Clear();
        _attack.DeniedCommands.Add((Enemy, Player));
        _attack.IssueAttackCommand(Enemy, Player);
        _attack.DeniedCommands.Clear();
        _evadeStarts.Add((Enemy, Player));
        _attack.IssueAttackCommand(Enemy, Player);

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void TheQueuedCommand_IsDroppedWhenTheTargetChangedDiedLeftOrEvadesBeforeItRuns()
    {
        _attack.IssueAttackCommand(Enemy, Player);
        _attack.IssueAttackCommand(Enemy, Player2);
        var first = (Action)_engine.CallsTo("AssignCommand")[0].Arguments[1]!;
        first();
        _engine.CountOf("ClearAllActions").Should().Be(0, "a newer command replaced it");

        var second = (Action)_engine.CallsTo("AssignCommand")[1].Arguments[1]!;
        _evadeStarts.Add((Enemy, Player2));
        second();
        _engine.CountOf("ClearAllActions").Should().Be(0, "it starts a leash evade");
        _evadeStarts.Clear();

        second();
        var attack = (Action)_engine.CallsTo("ActionDoCommand").Single().Arguments[0]!;
        _dead.Add(Player2);
        attack();
        _dead.Clear();
        _areas[Player2] = AreaB;
        attack();
        _areas.Clear();
        _evadeStarts.Add((Enemy, Player2));
        attack();
        _engine.CountOf("ActionAttack").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_LeavesABusyCreatureAndOneAlreadyAttackingItsTarget()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _attack.ResumeAttackAfterActionsCleared(Enemy);
        _engine.CallsTo("AssignCommand").Should().ContainSingle("clearing the throttle lets a new command through");

        _engine.ClearCalls();
        _busy.Add(Enemy);
        _attack.ResumeAttackAfterActionsCleared(Enemy);
        _engine.CountOf("AssignCommand").Should().Be(0);
        _busy.Clear();

        _attackTargets[Enemy] = Player;
        _actions[Enemy] = ActionType.AttackObject;
        _attack.RecentAttack = true;
        _attack.AttackHighestEnmityTarget(Enemy);
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_RecoversAStaleAttackAndLogsIt()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _attackTargets[Enemy] = Player;
        _actions[Enemy] = ActionType.AttackObject;
        _attack.RecentAttack = false;
        _now = Start.AddSeconds(30);

        _attack.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
        _attack.Logs.Should().ContainSingle().Which.Should().Contain("recovered stale attack");
    }

    [Test]
    public void AttackHighestEnmityTarget_SwitchesWhenTheNativeTargetDiffersFromTheLeader()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _now = Start.AddSeconds(30);
        _attackTargets[Enemy] = Player2;
        _actions[Enemy] = ActionType.AttackObject;
        _attack.RecentAttack = true;

        _attack.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
    }

    [Test]
    public void AttackHighestEnmityTarget_ReissuesAnApproachWhenTheLeaderChanges()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _change.ModifyEnmity(Player2, Enemy, 50);

        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Enemy);
    }

    [Test]
    public void AttackHighestEnmityTarget_DropsAStaleProximityTargetAndStopsCombatWhenNothingIsLeft()
    {
        _beyondAggroRange.Add((Enemy, Player));
        _change.ModifyProximityEnmity(Player, Enemy, 5);
        _engine.ClearCalls();

        _attack.AttackHighestEnmityTarget(Enemy);

        _enmity.HasProximityEnmity(Player, Enemy).Should().BeFalse();
        _combatStopped.Should().Equal(Enemy);
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_KeepsAProximityTargetStillInAggroRange()
    {
        _change.ModifyProximityEnmity(Player, Enemy, 5);
        _aggroRange.Add((Enemy, Player));
        _engine.ClearCalls();
        _attack.ResumeAttackAfterActionsCleared(Enemy);

        _enmity.HasProximityEnmity(Player, Enemy).Should().BeTrue();
        _engine.CallsTo("AssignCommand").Should().ContainSingle();
    }

    [Test]
    public void AttackHighestEnmityTarget_HoldsWhileEveryTargetIsHiddenAndKeepsTheEnmity()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _attack.Hidden.Add((Enemy, Player));
        _actions[Enemy] = ActionType.AttackObject;

        _attack.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
        RunAssigned();
        _engine.CountOf("ClearAllActions").Should().Be(1);
        _enmity.GetEnmityTable(Enemy).Should().ContainKey(Player);
    }

    [Test]
    public void HoldingForHiddenTargets_DoesNothingWhenBusyEvadingOrAlreadyIdle()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _attack.Hidden.Add((Enemy, Player));
        _engine.ClearCalls();

        _busy.Add(Enemy);
        _attack.AttackHighestEnmityTarget(Enemy);
        _busy.Clear();
        _evading.Add(Enemy);
        _attack.AttackHighestEnmityTarget(Enemy);
        _evading.Clear();
        _actions[Enemy] = ActionType.Invalid;
        _attack.AttackHighestEnmityTarget(Enemy);

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_ForACompanionFollowsTheAuthorizedTarget()
    {
        _attack.Companions.Add(Enemy);
        _attack.CompanionTargets[Enemy] = Player2;

        _attack.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
    }

    [Test]
    public void AttackHighestEnmityTarget_ForACompanionWithNoTargetStopsCombatOnlyAfterProximityLoss()
    {
        _attack.Companions.Add(Enemy);
        _attack.CompanionTargets[Enemy] = ObjectInvalid;

        _attack.AttackHighestEnmityTarget(Enemy);

        _engine.CountOf("AssignCommand").Should().Be(0);
        _combatStopped.Should().BeEmpty();
    }

    [Test]
    public void AttackTarget_IsSkippedInAnotherAreaAndWhenALeashEvadeStarts()
    {
        _attack.Companions.Add(Enemy);
        _attack.CompanionTargets[Enemy] = Player;
        _areas[Player] = AreaB;
        _attack.AttackHighestEnmityTarget(Enemy);
        _areas.Clear();
        _evadeStarts.Add((Enemy, Player));
        _attack.AttackHighestEnmityTarget(Enemy);

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void ReevaluateEnemyAttackTargets_MakesEveryEnemyOnTheCreatureChooseAgain()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _change.ModifyEnmity(Player, Enemy2, 5);
        _engine.ClearCalls();
        _attack.ReevaluateEnemyAttackTargets(Player2);
        _engine.CountOf("AssignCommand").Should().Be(0);

        _attack.ResumeAttackAfterActionsCleared(Enemy);
        _attack.ResumeAttackAfterActionsCleared(Enemy2);
        _engine.ClearCalls();
        _attack.ReevaluateEnemyAttackTargets(Player);

        _engine.CountOf("AssignCommand").Should().Be(0, "both enemies were commanded a moment ago");
    }
}
