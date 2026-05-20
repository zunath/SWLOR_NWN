using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.AI;

public class AIModelTests
{
    private enum TestPhase
    {
        Opening
    }

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
    public void AIActionDefinition_DefaultsAreNonExecutable()
    {
        var action = new AIActionDefinition();

        action.Type.Should().Be(AIActionType.Invalid);
        action.Feat.Should().Be(FeatType.Invalid);
        action.Priority.Should().Be(100);
        action.Score(new AIContext(0, AITriggerType.Invalid, 0, new AIProfile(), new AIState(), Array.Empty<uint>()))
            .Should()
            .Be(0);
    }

    [Test]
    public void AIProfile_DefaultsThrottleAndLimits()
    {
        var profile = new AIProfile();

        profile.Type.Should().Be(AIProfileType.Invalid);
        profile.Name.Should().BeNull();
        profile.DecisionThrottleSeconds.Should().Be(0.25f);
        profile.MaxCandidateActions.Should().Be(16);
        profile.IsBoss.Should().BeFalse();
        profile.Actions.Should().BeEmpty();
        profile.Phases.Should().BeEmpty();
        profile.PhaseOrder.Should().BeEmpty();
    }

    [Test]
    public void AIState_ClearActionCache_ResetsCachedSelections()
    {
        var state = new AIState
        {
            ActionCacheFeatCount = 3,
            ActionCacheFeatChecksum = 123
        };
        state.CachedActions.Add(new AIActionDefinition());
        state.CachedPhaseActions[AIPhaseId.Create(AIProfileType.Generic, TestPhase.Opening)] =
            new List<AIActionDefinition> { new() };

        state.ClearActionCache();

        state.ActionCacheFeatCount.Should().Be(-1);
        state.ActionCacheFeatChecksum.Should().Be(0);
        state.CachedActions.Should().BeEmpty();
        state.CachedPhaseActions.Should().BeEmpty();
    }

    [Test]
    public void AIPhaseId_CreateScopesPrivateEnumToProfile()
    {
        var id = AIPhaseId.Create(AIProfileType.BeastCompanion, TestPhase.Opening);

        id.Value.Should().Be("BeastCompanion.TestPhase.Opening");
        id.ToString().Should().Be(id.Value);
        AIPhaseId.Invalid.Value.Should().BeEmpty();
    }

    [Test]
    public void AIContext_DefaultsToInvalidEvaluatedTargetAndKeepsSuppliedState()
    {
        var state = new AIState();
        var profile = new AIProfile();
        var allies = new List<uint> { 1, 2 };

        var context = new AIContext(123, AITriggerType.Damaged, 456, profile, state, allies);

        context.Self.Should().Be(123);
        context.Trigger.Should().Be(AITriggerType.Damaged);
        context.EventTarget.Should().Be(456);
        context.Profile.Should().BeSameAs(profile);
        context.State.Should().BeSameAs(state);
        context.Allies.Should().BeSameAs(allies);
        context.EvaluatedTarget.Should().Be(OBJECT_INVALID);

        context.SetEvaluatedTarget(789);
        context.EvaluatedTarget.Should().Be(789);
    }

    [Test]
    public void AIPhase_PredicatesUseContextState()
    {
        var context = CreateContext(selfHealthPercent: 45, combatStartedSecondsAgo: 10);

        AIPhase.HealthAbove(40)(context).Should().BeTrue();
        AIPhase.HealthAbove(45)(context).Should().BeFalse();
        AIPhase.HealthAtOrBelow(45)(context).Should().BeTrue();
        AIPhase.HealthAtOrBelow(44)(context).Should().BeFalse();
        AIPhase.ElapsedCombatSecondsAtLeast(9)(context).Should().BeTrue();
        AIPhase.ElapsedCombatSecondsAtLeast(11)(context).Should().BeFalse();
        AIPhase.Always()(context).Should().BeTrue();
    }

    [Test]
    public void AIScore_FixedAndHealthScoresAreDeterministic()
    {
        var context = CreateContext(selfHealthPercent: 45);

        AIScore.Fixed(25)(context).Should().Be(25);
        AIScore.SelfHealthBelow(50, 100)(context).Should().Be(105);
        AIScore.SelfHealthBelow(44, 100)(context).Should().Be(0);
        AIScore.TargetHealthBelow(100, 100).Should().NotBeNull();
    }

    [Test]
    public void AIScore_AbilityChoosesExpectedScoreBands()
    {
        var context = CreateContext();

        AIScore.Ability(new AbilityDetail
        {
            IsHostileAbility = true,
            IsSingleTargetAbility = true,
            AbilityLevel = 2
        })(context).Should().Be(AIScoreBand.SingleTargetDamage + 2);

        AIScore.Ability(new AbilityDetail
        {
            RequiresTarget = true,
            AbilityLevel = 3
        }).Should().NotBeNull();

        AIScore.Ability(new AbilityDetail
        {
            AbilityLevel = 4
        })(context).Should().Be(AIScoreBand.Defensive + 4);

        AIScore.Ability(new AbilityDetail
        {
            IsHostileAbility = true,
            IsAreaAbility = true,
            AbilityLevel = 5,
            MaxRange = 12f
        }).Should().NotBeNull();
    }

    [Test]
    public void AITarget_SelectsSelfAndStoresDefaultOverrides()
    {
        var context = CreateContext(self: 123);
        var selector = AITarget.Self();

        selector(context).Should().Be(123);

        AITarget.RegisterDefault(FeatType.Provoke1, selector);
        AITarget.TryGetDefaultOverride(FeatType.Provoke1, out var registered)
            .Should()
            .BeTrue();

        registered.Should().BeSameAs(selector);
    }

    [Test]
    public void AITarget_HighestEnmity_IgnoresEventTargetWithoutEnmity()
    {
        var context = new AIContext(
            100,
            AITriggerType.Damaged,
            200,
            new AIProfile(),
            new AIState(),
            Array.Empty<uint>());

        AITarget.HighestEnmity()(context).Should().Be(OBJECT_INVALID);
    }

    [Test]
    public void AITarget_HostileCluster_IgnoresEventTargetWithoutEnmity()
    {
        var context = new AIContext(
            100,
            AITriggerType.Damaged,
            200,
            new AIProfile(),
            new AIState(),
            Array.Empty<uint>());

        AITarget.HostileCluster(5f, 2)(context).Should().Be(OBJECT_INVALID);
    }

    [Test]
    public void AITarget_HighestEnmity_UsesEnmityTable()
    {
        const uint enemy = 100;
        const uint target = 200;

        EnemyEnmityTables()[enemy] = new Dictionary<uint, int>
        {
            [target] = 1
        };
        CreatureToEnemies()[target] = new List<uint> { enemy };

        var context = new AIContext(
            enemy,
            AITriggerType.Damaged,
            300,
            new AIProfile(),
            new AIState(),
            Array.Empty<uint>());

        AITarget.HighestEnmity()(context).Should().Be(target);
    }

    [Test]
    public void AITarget_InferDefaultPrefersHostileMetadataOverFeat2DAFallback()
    {
        AITarget.InferDefault(FeatType.Bite, new AbilityDetail
        {
            IsHostileAbility = true,
            IsSingleTargetAbility = true
        }).Should().NotBeNull();

        AITarget.InferDefault(FeatType.FireBreath, new AbilityDetail
        {
            IsHostileAbility = true,
            IsAreaAbility = true,
            MaxRange = 10f
        }).Should().NotBeNull();
    }

    [Test]
    public void CreatureAggroEnter_EnforcesAggroRangeBeforeAddingProximityEnmity()
    {
        var aiSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "AI.cs")).Replace("\r\n", "\n");
        var enterBody = aiSource.Substring(
            aiSource.IndexOf("public static void CreatureAggroEnter()", StringComparison.Ordinal),
            aiSource.IndexOf("public static void CreatureAggroExit()", StringComparison.Ordinal) -
            aiSource.IndexOf("public static void CreatureAggroEnter()", StringComparison.Ordinal));

        var rangeGuardIndex = enterBody.IndexOf("if (!IsInAggroRange(self, entering))", StringComparison.Ordinal);
        var acquireIndex = enterBody.IndexOf("TryAcquireAggro(self, entering);", StringComparison.Ordinal);

        rangeGuardIndex.Should().BeGreaterThanOrEqualTo(0);
        rangeGuardIndex.Should().BeLessThan(acquireIndex);
    }

    [Test]
    public void CreatureAggroRange_RequiresLineOfSight()
    {
        var aiSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "AI.cs")).Replace("\r\n", "\n");
        var rangeBody = aiSource.Substring(
            aiSource.IndexOf("private static bool IsInAggroRange", StringComparison.Ordinal),
            aiSource.IndexOf("private static void TryAcquireAggro", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool IsInAggroRange", StringComparison.Ordinal));

        rangeBody.Should().Contain("LineOfSightObject(target, creature)");
    }

    [Test]
    public void AllyAggroAssist_RequiresLineOfSightBeforeAddingProximityEnmity()
    {
        var aiSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "AI.cs")).Replace("\r\n", "\n");
        var allyBody = aiSource.Substring(
            aiSource.IndexOf("private static void AddNearbyAllyProximityEnmity", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool TryAddProximityEnmity", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void AddNearbyAllyProximityEnmity", StringComparison.Ordinal));

        var lineOfSightIndex = allyBody.IndexOf("if (!LineOfSightObject(target, ally)) continue;", StringComparison.Ordinal);
        var addEnmityIndex = allyBody.IndexOf("TryAddProximityEnmity(target, ally);", StringComparison.Ordinal);

        lineOfSightIndex.Should().BeGreaterThanOrEqualTo(0);
        lineOfSightIndex.Should().BeLessThan(addEnmityIndex);
    }

    [Test]
    public void CreatureHeartbeat_DoesNotScanForAggroTargets()
    {
        var aiSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "AI.cs")).Replace("\r\n", "\n");
        var heartbeatBody = aiSource.Substring(
            aiSource.IndexOf("public static void CreatureHeartbeat()", StringComparison.Ordinal),
            aiSource.IndexOf("public static void CreaturePerception()", StringComparison.Ordinal) -
            aiSource.IndexOf("public static void CreatureHeartbeat()", StringComparison.Ordinal));

        heartbeatBody.Should().NotContain("GetFirstObjectInShape");
        heartbeatBody.Should().NotContain("AcquireNearbyHostiles");
        heartbeatBody.Should().NotContain("RemoveStaleProximityEnmity");
    }

    [Test]
    public void CombatLeash_UsesTargetDistanceFromHomeBeforeResettingCombat()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var processFlagsBody = aiSource.Substring(
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal),
            aiSource.IndexOf("private static void ProcessCreatureAllies()", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal));
        var leashBody = aiSource.Substring(
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool IsOutsideHomeRadius", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal));

        processFlagsBody.Should().Contain("ShouldLeashCombatTarget(self, highestEnmityTarget, homeLocation)");
        leashBody.Should().Contain("IsOutsideHomeRadius(target, homeLocation, CombatLeashRadius)");
        leashBody.Should().Contain("IsOutsideHomeRadius(creature, homeLocation, CombatLeashRadius)");
    }

    [Test]
    public void CombatLeashRadius_AllowsStandardRifleEngagementRange()
    {
        var combatLeashRadius = ReadConstFloat(
            "CombatLeashRadius",
            "SWLOR.Game.Server",
            "Service",
            "AI.cs");
        var rifleRange = ReadConstFloat(
            "Standard",
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Rifle",
            "RifleAbilityRange.cs");

        combatLeashRadius.Should().BeGreaterThan(rifleRange);
    }

    [Test]
    public void CustomAoePersistentVfx_DefinesCreatureAggroRadius()
    {
        var persistentVfx = File.ReadAllLines(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "vfx_persistent.2da"));

        var rows = persistentVfx
            .Select(line => line.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length > 0 && int.TryParse(columns[0], out _))
            .ToArray();
        var columns = rows[37];

        columns.Should().HaveCountGreaterThanOrEqualTo(9);
        columns[0].Should().Be("37");
        columns[1].Should().Be("VFX_CUSTOM");
        columns[2].Should().Be("C");
        columns[3].Should().Be("8.5");
    }

    private static AIContext CreateContext(
        uint self = 0,
        int selfHealthPercent = 100,
        int combatStartedSecondsAgo = 0)
    {
        var state = new AIState();
        if (combatStartedSecondsAgo > 0)
        {
            state.CombatStartedTime = DateTime.UtcNow.AddSeconds(-combatStartedSecondsAgo);
        }

        var context = new AIContext(
            self,
            AITriggerType.CombatRound,
            0,
            new AIProfile(),
            state,
            Array.Empty<uint>());

        typeof(AIContext)
            .GetField("_selfHealthPercent", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(context, selfHealthPercent);

        return context;
    }

    private static Dictionary<uint, Dictionary<uint, int>> EnemyEnmityTables()
    {
        return GetEnmityField<Dictionary<uint, Dictionary<uint, int>>>("_enemyEnmityTables");
    }

    private static Dictionary<uint, List<uint>> CreatureToEnemies()
    {
        return GetEnmityField<Dictionary<uint, List<uint>>>("_creatureToEnemies");
    }

    private static T GetEnmityField<T>(string name)
    {
        return (T)typeof(Enmity)
            .GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
    }

    private static string ReadSource(params string[] pathParts)
    {
        var fullPath = Path.Combine(new[] { FindRepositoryRoot().FullName }.Concat(pathParts).ToArray());
        return File.ReadAllText(fullPath);
    }

    private static float ReadConstFloat(string name, params string[] pathParts)
    {
        var source = ReadSource(pathParts);
        var match = Regex.Match(source, $@"const\s+float\s+{Regex.Escape(name)}\s*=\s*(\d+(?:\.\d+)?)f");

        match.Success.Should().BeTrue();
        return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
