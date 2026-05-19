using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class EnmityTests
{
    [SetUp]
    public void SetUp()
    {
        EnemyEnmityTables().Clear();
        CreatureToEnemies().Clear();
    }

    [TearDown]
    public void TearDown()
    {
        EnemyEnmityTables().Clear();
        CreatureToEnemies().Clear();
    }

    [Test]
    public void ClearEnmityTable_RemovesEnemyFromAllTrackedTargets()
    {
        const uint enemy = 100;
        const uint otherEnemy = 200;
        const uint targetOne = 1;
        const uint targetTwo = 2;

        EnemyEnmityTables()[enemy] = new Dictionary<uint, int>
        {
            [targetOne] = 3,
            [targetTwo] = 5
        };
        EnemyEnmityTables()[otherEnemy] = new Dictionary<uint, int>
        {
            [targetOne] = 7
        };
        CreatureToEnemies()[targetOne] = new List<uint> { enemy, otherEnemy };
        CreatureToEnemies()[targetTwo] = new List<uint> { enemy };

        Enmity.ClearEnmityTable(enemy);

        EnemyEnmityTables().Should().NotContainKey(enemy);
        EnemyEnmityTables().Should().ContainKey(otherEnemy);
        CreatureToEnemies()[targetOne].Should().ContainSingle().Which.Should().Be(otherEnemy);
        CreatureToEnemies().Should().NotContainKey(targetTwo);
    }

    [Test]
    public void ShouldIssueAttackCommand_ReissuesWhenTargetIsStaleButNoAttackActionIsRunning()
    {
        ShouldIssueAttackCommand(1, 1, ActionType.Invalid, false)
            .Should()
            .BeTrue();
    }

    [Test]
    public void ShouldIssueAttackCommand_DoesNotInterruptBusyCreature()
    {
        ShouldIssueAttackCommand(1, 2, ActionType.Invalid, true)
            .Should()
            .BeFalse();
    }

    [Test]
    public void ShouldIssueAttackCommand_SkipsOnlyWhenAlreadyAttackingDesiredTarget()
    {
        ShouldIssueAttackCommand(1, 1, ActionType.AttackObject, false)
            .Should()
            .BeFalse();
    }

    private static Dictionary<uint, Dictionary<uint, int>> EnemyEnmityTables()
    {
        return GetField<Dictionary<uint, Dictionary<uint, int>>>("_enemyEnmityTables");
    }

    private static Dictionary<uint, List<uint>> CreatureToEnemies()
    {
        return GetField<Dictionary<uint, List<uint>>>("_creatureToEnemies");
    }

    private static T GetField<T>(string name)
    {
        return (T)typeof(Enmity)
            .GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
    }

    private static bool ShouldIssueAttackCommand(
        uint attackTarget,
        uint desiredTarget,
        ActionType currentAction,
        bool isBusy)
    {
        return (bool)typeof(Enmity)
            .GetMethod("ShouldIssueAttackCommand", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { attackTarget, desiredTarget, currentAction, isBusy })!;
    }
}
