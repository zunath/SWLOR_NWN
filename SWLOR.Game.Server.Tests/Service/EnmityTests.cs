using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

public class EnmityTests
{
    [SetUp]
    public void SetUp()
    {
        EnemyEnmityTables().Clear();
        CreatureToEnemies().Clear();
        ProximityEnmityAmounts().Clear();
        AttackCommandTimes().Clear();
    }

    [TearDown]
    public void TearDown()
    {
        EnemyEnmityTables().Clear();
        CreatureToEnemies().Clear();
        ProximityEnmityAmounts().Clear();
        AttackCommandTimes().Clear();
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
        ProximityEnmityAmounts()[enemy] = new Dictionary<uint, int>
        {
            [targetOne] = 1
        };

        Enmity.ClearEnmityTable(enemy);

        EnemyEnmityTables().Should().NotContainKey(enemy);
        EnemyEnmityTables().Should().ContainKey(otherEnemy);
        CreatureToEnemies()[targetOne].Should().ContainSingle().Which.Should().Be(otherEnemy);
        CreatureToEnemies().Should().NotContainKey(targetTwo);
        ProximityEnmityAmounts().Should().NotContainKey(enemy);
    }

    [Test]
    public void RemoveProximityEnmity_RemovesProximityOnlyEntries()
    {
        const uint enemy = 100;
        const uint otherEnemy = 200;
        const uint target = 1;

        EnemyEnmityTables()[enemy] = new Dictionary<uint, int>
        {
            [target] = 1
        };
        EnemyEnmityTables()[otherEnemy] = new Dictionary<uint, int>
        {
            [target] = 1
        };
        CreatureToEnemies()[target] = new List<uint> { enemy, otherEnemy };
        ProximityEnmityAmounts()[enemy] = new Dictionary<uint, int>
        {
            [target] = 1
        };

        Enmity.RemoveProximityEnmity(target, enemy)
            .Should()
            .BeTrue();

        EnemyEnmityTables().Should().NotContainKey(enemy);
        EnemyEnmityTables().Should().ContainKey(otherEnemy);
        CreatureToEnemies()[target].Should().ContainSingle().Which.Should().Be(otherEnemy);
        ProximityEnmityAmounts().Should().NotContainKey(enemy);
    }

    [Test]
    public void RemoveProximityEnmity_SubtractsProximityFromCombatEntries()
    {
        const uint enemy = 100;
        const uint target = 1;

        EnemyEnmityTables()[enemy] = new Dictionary<uint, int>
        {
            [target] = 5
        };
        CreatureToEnemies()[target] = new List<uint> { enemy };
        ProximityEnmityAmounts()[enemy] = new Dictionary<uint, int>
        {
            [target] = 2
        };

        Enmity.RemoveProximityEnmity(target, enemy)
            .Should()
            .BeTrue();

        EnemyEnmityTables()[enemy][target].Should().Be(3);
        CreatureToEnemies()[target].Should().ContainSingle().Which.Should().Be(enemy);
        ProximityEnmityAmounts().Should().NotContainKey(enemy);
    }

    [Test]
    public void HasProximityEnmity_OnlyMatchesTrackedEnemyAndTarget()
    {
        const uint enemy = 100;
        const uint otherEnemy = 200;
        const uint target = 1;
        const uint otherTarget = 2;

        ProximityEnmityAmounts()[enemy] = new Dictionary<uint, int>
        {
            [target] = 1
        };

        Enmity.HasProximityEnmity(target, enemy).Should().BeTrue();
        Enmity.HasProximityEnmity(otherTarget, enemy).Should().BeFalse();
        Enmity.HasProximityEnmity(target, otherEnemy).Should().BeFalse();
    }

    [Test]
    public void HasOnlyProximityEnmity_MatchesTrackedProximityContribution()
    {
        const uint enemy = 100;
        const uint proximityOnlyTarget = 1;
        const uint combatTarget = 2;
        const uint untrackedTarget = 3;

        EnemyEnmityTables()[enemy] = new Dictionary<uint, int>
        {
            [proximityOnlyTarget] = 1,
            [combatTarget] = 5,
            [untrackedTarget] = 3
        };
        ProximityEnmityAmounts()[enemy] = new Dictionary<uint, int>
        {
            [proximityOnlyTarget] = 1,
            [combatTarget] = 2
        };

        HasOnlyProximityEnmity(proximityOnlyTarget, enemy).Should().BeTrue();
        HasOnlyProximityEnmity(combatTarget, enemy).Should().BeFalse();
        HasOnlyProximityEnmity(untrackedTarget, enemy).Should().BeFalse();
        HasOnlyProximityEnmity(999, enemy).Should().BeFalse();
    }

    [Test]
    public void HasNonProximityEnmity_AllowsAuraOnlyTrafficButRejectsCombatEnmity()
    {
        const uint proximityOnlyEnemy = 100;
        const uint combatEnemy = 200;
        const uint untrackedEnemy = 300;
        const uint target = 1;

        EnemyEnmityTables()[proximityOnlyEnemy] = new Dictionary<uint, int>
        {
            [target] = 1
        };
        EnemyEnmityTables()[combatEnemy] = new Dictionary<uint, int>
        {
            [target] = 5
        };
        EnemyEnmityTables()[untrackedEnemy] = new Dictionary<uint, int>
        {
            [target] = 3
        };
        ProximityEnmityAmounts()[proximityOnlyEnemy] = new Dictionary<uint, int>
        {
            [target] = 1
        };
        ProximityEnmityAmounts()[combatEnemy] = new Dictionary<uint, int>
        {
            [target] = 2
        };

        Enmity.HasNonProximityEnmity(proximityOnlyEnemy).Should().BeFalse();
        Enmity.HasNonProximityEnmity(combatEnemy).Should().BeTrue();
        Enmity.HasNonProximityEnmity(untrackedEnemy).Should().BeTrue();
        Enmity.HasNonProximityEnmity(999).Should().BeFalse();
    }

    [Test]
    public void HasNonProximityEnmityForCreature_AllowsOverlappingAuraTrafficButRejectsRealCombat()
    {
        const uint creature = 1;
        const uint proximityOnlyEnemy = 100;
        const uint combatEnemy = 200;

        CreatureToEnemies()[creature] = new List<uint> { proximityOnlyEnemy };
        EnemyEnmityTables()[proximityOnlyEnemy] = new Dictionary<uint, int>
        {
            [creature] = 1
        };
        ProximityEnmityAmounts()[proximityOnlyEnemy] = new Dictionary<uint, int>
        {
            [creature] = 1
        };

        Enmity.HasNonProximityEnmityForCreature(creature).Should().BeFalse();

        CreatureToEnemies()[creature].Add(combatEnemy);
        EnemyEnmityTables()[combatEnemy] = new Dictionary<uint, int>
        {
            [creature] = 3
        };

        Enmity.HasNonProximityEnmityForCreature(creature).Should().BeTrue();
        Enmity.HasNonProximityEnmityForCreature(999).Should().BeFalse();
    }

    [Test]
    public void HasNonProximityEnmityOutsidePair_IgnoresPairCombatAndAuraTrafficButRejectsOtherCombat()
    {
        const uint player = 1;
        const uint otherPlayer = 2;
        const uint npc = 100;
        const uint otherNpc = 200;

        CreatureToEnemies()[player] = new List<uint> { npc, otherNpc };
        EnemyEnmityTables()[npc] = new Dictionary<uint, int>
        {
            [player] = 5
        };
        EnemyEnmityTables()[otherNpc] = new Dictionary<uint, int>
        {
            [player] = 1
        };
        ProximityEnmityAmounts()[otherNpc] = new Dictionary<uint, int>
        {
            [player] = 1
        };

        Enmity.HasNonProximityEnmity(player, npc).Should().BeTrue();
        Enmity.HasNonProximityEnmity(player, otherNpc).Should().BeFalse();
        Enmity.HasNonProximityEnmityOutsidePair(player, npc).Should().BeFalse();

        EnemyEnmityTables()[otherNpc][player] = 2;
        Enmity.HasNonProximityEnmityOutsidePair(player, npc).Should().BeTrue();

        EnemyEnmityTables()[otherNpc][player] = 1;
        EnemyEnmityTables()[npc][otherPlayer] = 3;
        ProximityEnmityAmounts()[npc] = new Dictionary<uint, int>
        {
            [otherPlayer] = 1
        };

        Enmity.HasNonProximityEnmityOutsidePair(player, npc).Should().BeTrue();

        EnemyEnmityTables()[npc].Remove(otherPlayer);
        ProximityEnmityAmounts().Remove(npc);
        CreatureToEnemies()[npc] = new List<uint> { otherNpc };
        EnemyEnmityTables()[otherNpc][npc] = 1;
        ProximityEnmityAmounts()[otherNpc][npc] = 1;

        Enmity.HasNonProximityEnmityOutsidePair(player, npc).Should().BeFalse();

        EnemyEnmityTables()[otherNpc][npc] = 2;
        Enmity.HasNonProximityEnmityOutsidePair(player, npc).Should().BeTrue();
    }

    [Test]
    public void ShouldIssueAttackCommand_ReissuesWhenTargetIsStaleButNoAttackActionIsRunning()
    {
        ShouldIssueAttackCommand(1, 1, ActionType.Invalid, false, commandIssuedAt: DateTime.UtcNow.AddSeconds(-7))
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

    [Test]
    public void ShouldIssueAttackCommand_ReissuesStaleAttackAction()
    {
        ShouldIssueAttackCommand(1, 1, ActionType.AttackObject, false, true)
            .Should()
            .BeTrue();
    }

    [Test]
    public void ShouldIssueAttackCommand_AllowsRecentAttackCommandToSettle()
    {
        var now = DateTime.UtcNow;

        ShouldIssueAttackCommand(
                1,
                1,
                ActionType.Invalid,
                false,
                now: now,
                commandIssuedAt: now.AddSeconds(-2),
                recoverySeconds: 6f)
            .Should()
            .BeFalse();
    }

    [Test]
    public void ShouldIssueAttackCommand_AllowsRecentCommandToSettleBeforeAttackTargetIsPopulated()
    {
        var now = DateTime.UtcNow;

        ShouldIssueAttackCommand(
                OBJECT_INVALID,
                1,
                ActionType.Invalid,
                false,
                now: now,
                commandIssuedAt: now.AddSeconds(-2),
                recoverySeconds: 6f)
            .Should()
            .BeFalse();
    }

    [Test]
    public void ShouldIssueAttackCommand_SwitchesTargetsWithoutWaitingForSettlingCommand()
    {
        var now = DateTime.UtcNow;

        ShouldIssueAttackCommand(
                1,
                2,
                ActionType.AttackObject,
                false,
                now: now,
                commandIssuedAt: now,
                recoverySeconds: 6f)
            .Should()
            .BeTrue();
    }

    [Test]
    public void ShouldRecoverStaleAttack_RequiresSameAttackActionWithoutRecentAttack()
    {
        var now = DateTime.UtcNow;

        ShouldRecoverStaleAttack(
                1,
                1,
                ActionType.AttackObject,
                now,
                now.AddSeconds(-7),
                false,
                6f)
            .Should()
            .BeTrue();

        ShouldRecoverStaleAttack(
                1,
                1,
                ActionType.AttackObject,
                now,
                now.AddSeconds(-7),
                true,
                6f)
            .Should()
            .BeFalse();

        ShouldRecoverStaleAttack(
                1,
                2,
                ActionType.AttackObject,
                now,
                now.AddSeconds(-7),
                false,
                6f)
            .Should()
            .BeFalse();
    }

    [Test]
    public void ShouldRecoverStaleAttack_WaitsForRecoveryWindowAfterKnownCommand()
    {
        var now = DateTime.UtcNow;

        ShouldRecoverStaleAttack(
                1,
                1,
                ActionType.AttackObject,
                now,
                now.AddSeconds(-5),
                false,
                6f)
            .Should()
            .BeFalse();
    }

    [Test]
    public void ShouldRecoverStaleAttack_ReissuesUntrackedAttackAction()
    {
        ShouldRecoverStaleAttack(
                1,
                1,
                ActionType.AttackObject,
                DateTime.UtcNow,
                null,
                false,
                6f)
            .Should()
            .BeTrue();
    }

    [Test]
    public void GetStaleAttackRecoverySeconds_UsesShorterWindowForMultiAttackSwingCadence()
    {
        var recoverySeconds = GetStaleAttackRecoverySeconds(Combat.MinimumAttackDelayMilliseconds);

        recoverySeconds.Should().BeApproximately(4.5f, 0.01f);
        recoverySeconds.Should().BeLessThan(6f);
    }

    [Test]
    public void GetStaleAttackRecoverySeconds_ScalesWithSlowerSwingCadence()
    {
        var recoverySeconds = GetStaleAttackRecoverySeconds(3000);

        recoverySeconds.Should().BeApproximately(7f, 0.01f);
    }

    [Test]
    public void GetWeaponEngagementRange_UsesTheEquippedWeaponSkill()
    {
        Combat.GetWeaponEngagementRange(SkillType.Rifle).Should().Be(10f);
        Combat.GetWeaponEngagementRange(SkillType.Pistol).Should().Be(10f);
        Combat.GetWeaponEngagementRange(SkillType.Throwing).Should().Be(10f);
        Combat.GetWeaponEngagementRange(SkillType.Lightsaber).Should().Be(1.5f);
        Combat.GetWeaponEngagementRange(SkillType.Katar).Should().Be(1.5f);
        Combat.GetWeaponEngagementRange(SkillType.Invalid).Should().Be(1.5f);
    }

    [Test]
    public void AttackMovement_UsesWeaponRangeInsteadOfAppearancePreferredDistance()
    {
        var repositoryRoot = FindRepositoryRoot().FullName;
        var nativeSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "SWLOR.Game.Server",
            "Native",
            "OnAIActionAttackObject.cs"));
        var enmitySource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "SWLOR.Game.Server",
            "Service",
            "Enmity.cs"));

        nativeSource.Should().Contain("Combat.GetWeaponEngagementRange(attackSkillType)");
        nativeSource.Should().Contain("Combat.IsRangedWeaponSkill(attackSkillType)");
        nativeSource.Should().Contain("var isPlayerCreature = pCreature.m_bPlayerCharacter == 1");
        nativeSource.Should().Contain("? pCreature.GetRangeWeaponEquipped() == 1");
        nativeSource.Should().Contain("? ResolveEngineDesiredAttackRange(");
        nativeSource.Should().Contain(": ResolveWeaponEngagementRange(");
        enmitySource.Should().Contain("Combat.GetWeaponEngagementRange(skillType)");
        enmitySource.Should().NotContain("GetPreferredAttackDistance");

        ResolveNativeWeaponEngagementRange(10f, 30f, true).Should().Be(10f);
        ResolveNativeWeaponEngagementRange(10f, 5f, true).Should().BeApproximately(4.99f, 0.001f,
            "a ranged weapon's real maximum range must cap the standard engagement distance");
        ResolveNativeWeaponEngagementRange(10f, 1.5f, true).Should().BeApproximately(1.49f, 0.001f,
            "even a very short real ranged weapon range must be respected");
        ResolveNativeWeaponEngagementRange(10f, 0f, true).Should().Be(10f,
            "missing ranged metadata must not collapse a ranged creature to melee distance");
        ResolveNativeWeaponEngagementRange(1.5f, 30f, false).Should().Be(1.5f);

        ResolveNativeEngineDesiredAttackRange(8f, 30f, true).Should().Be(8f,
            "players must retain the engine's weapon- and target-specific desired range");
        ResolveNativeEngineDesiredAttackRange(0f, 30f, true).Should().Be(10f);
        ResolveNativeEngineDesiredAttackRange(1.25f, 30f, false).Should().Be(1.25f);

        ResolveNativeMaximumAttackRange(10f, 0f, true).Should().Be(10f,
            "missing ranged metadata must repair the maximum range as well as the desired range");
        ResolveNativeMaximumAttackRange(4.99f, 5f, true).Should().Be(5f);
        ResolveNativeMaximumAttackRange(8f, 30f, true).Should().Be(30f);
        ResolveNativeMaximumAttackRange(0f, 0f, false).Should().Be(0f);
    }

    [Test]
    public void ShouldMoveIntoAttackRange_UsesWeaponRangeForRangedEnemies()
    {
        ShouldMoveIntoAttackRange(10.25f, SkillType.Rifle, 10f).Should().BeFalse();
        ShouldMoveIntoAttackRange(10.26f, SkillType.Rifle, 10f).Should().BeTrue();
    }

    [Test]
    public void ShouldMoveIntoAttackRange_PreservesMeleeMovementThreshold()
    {
        ShouldMoveIntoAttackRange(2.25f, SkillType.Lightsaber, 1.5f).Should().BeFalse();
        ShouldMoveIntoAttackRange(2.26f, SkillType.Lightsaber, 1.5f).Should().BeTrue();
    }

    private static Dictionary<uint, Dictionary<uint, int>> EnemyEnmityTables()
    {
        return GetField<Dictionary<uint, Dictionary<uint, int>>>("_enemyEnmityTables");
    }

    private static Dictionary<uint, List<uint>> CreatureToEnemies()
    {
        return GetField<Dictionary<uint, List<uint>>>("_creatureToEnemies");
    }

    private static Dictionary<uint, Dictionary<uint, int>> ProximityEnmityAmounts()
    {
        return GetField<Dictionary<uint, Dictionary<uint, int>>>("_proximityEnmityAmounts");
    }

    private static Dictionary<uint, DateTime> AttackCommandTimes()
    {
        return GetField<Dictionary<uint, DateTime>>("_attackCommandTimes");
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
        bool isBusy,
        bool shouldRecoverStaleAttack = false,
        DateTime? now = null,
        DateTime? commandIssuedAt = null,
        float recoverySeconds = 6f)
    {
        return (bool)typeof(Enmity)
            .GetMethod("ShouldIssueAttackCommand", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[]
            {
                attackTarget,
                desiredTarget,
                currentAction,
                isBusy,
                shouldRecoverStaleAttack,
                now ?? DateTime.UtcNow,
                commandIssuedAt,
                recoverySeconds
            })!;
    }

    private static bool ShouldRecoverStaleAttack(
        uint attackTarget,
        uint desiredTarget,
        ActionType currentAction,
        DateTime now,
        DateTime? commandIssuedAt,
        bool hasRecentAttack,
        float recoverySeconds)
    {
        return (bool)typeof(Enmity)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "ShouldRecoverStaleAttack" &&
                method.GetParameters().Length == 7)
            .Invoke(null, new object[]
            {
                attackTarget,
                desiredTarget,
                currentAction,
                now,
                commandIssuedAt,
                hasRecentAttack,
                recoverySeconds
            })!;
    }

    private static bool ShouldMoveIntoAttackRange(float distance, SkillType skillType, float moveRange)
    {
        return (bool)typeof(Enmity)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "ShouldMoveIntoAttackRange" &&
                method.GetParameters().Length == 3)
            .Invoke(null, new object[] { distance, skillType, moveRange })!;
    }

    private static float GetStaleAttackRecoverySeconds(int effectiveDelayMilliseconds)
    {
        return (float)typeof(Enmity)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "GetStaleAttackRecoverySeconds" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(int))
            .Invoke(null, new object[] { effectiveDelayMilliseconds })!;
    }

    private static float ResolveNativeWeaponEngagementRange(
        float desiredAttackRange,
        float maxAttackRange,
        bool hasRangedWeapon)
    {
        return (float)typeof(OnAIActionAttackObject)
            .GetMethod("ResolveWeaponEngagementRange", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { desiredAttackRange, maxAttackRange, hasRangedWeapon })!;
    }

    private static float ResolveNativeEngineDesiredAttackRange(
        float desiredAttackRange,
        float maxAttackRange,
        bool hasRangedWeapon)
    {
        return (float)typeof(OnAIActionAttackObject)
            .GetMethod("ResolveEngineDesiredAttackRange", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { desiredAttackRange, maxAttackRange, hasRangedWeapon })!;
    }

    private static float ResolveNativeMaximumAttackRange(
        float desiredAttackRange,
        float maxAttackRange,
        bool hasRangedWeapon)
    {
        return (float)typeof(OnAIActionAttackObject)
            .GetMethod("ResolveMaximumAttackRange", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { desiredAttackRange, maxAttackRange, hasRangedWeapon })!;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull();
        return directory!;
    }

    private static bool HasOnlyProximityEnmity(uint creature, uint enemy)
    {
        return (bool)typeof(Enmity)
            .GetMethod("HasOnlyProximityEnmity", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { creature, enemy })!;
    }
}
