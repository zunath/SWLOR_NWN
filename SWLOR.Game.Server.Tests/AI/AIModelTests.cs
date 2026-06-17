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
    public void AITarget_InferDefaultUsesAbilityMetadataForHostileTargets()
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
    public void CreatureAggroExit_KeepsActiveCombatUntilLeash()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var removeBody = aiSource.Substring(
            aiSource.IndexOf("private static void RemoveProximityEnmity", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool ShouldKeepCombatProximityEnmity", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void RemoveProximityEnmity", StringComparison.Ordinal));
        var keepBody = aiSource.Substring(
            aiSource.IndexOf("private static bool ShouldKeepCombatProximityEnmity", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool IsAIEnabled", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldKeepCombatProximityEnmity", StringComparison.Ordinal));

        var keepIndex = removeBody.IndexOf("ShouldKeepCombatProximityEnmity(target, enemy)", StringComparison.Ordinal);
        var leashEvadeIndex = removeBody.IndexOf("TryStartLeashEvade(enemy, target)", StringComparison.Ordinal);
        var removeIndex = removeBody.IndexOf("Enmity.RemoveProximityEnmity(target, enemy)", StringComparison.Ordinal);

        leashEvadeIndex.Should().BeGreaterThanOrEqualTo(0);
        leashEvadeIndex.Should().BeLessThan(removeIndex);
        keepIndex.Should().BeGreaterThanOrEqualTo(0);
        keepIndex.Should().BeLessThan(removeIndex);
        keepBody.Should().Contain("Enmity.GetHighestEnmityTarget(enemy) != target");
        keepBody.Should().Contain("ShouldLeashCombatTarget(enemy, target, homeLocation)");
        keepBody.Should().Contain("Activity.IsBusy(enemy)");
        keepBody.Should().Contain("GetIsInCombat(enemy)");
        keepBody.Should().Contain("GetAttackTarget(enemy) == target");
        keepBody.Should().Contain("currentAction == ActionType.MoveToPoint");
    }

    [Test]
    public void AbilityResume_ClearsNpcCombatStateBeforeReattacking()
    {
        var source = ReadSource("SWLOR.Game.Server", "Feature", "UsePerkFeat.cs").Replace("\r\n", "\n");
        var resumeBody = source.Substring(
            source.IndexOf("private static void ResumeAttack(", StringComparison.Ordinal),
            source.IndexOf("private static void ResumeAttackAfterDelay", StringComparison.Ordinal) -
            source.IndexOf("private static void ResumeAttack(", StringComparison.Ordinal));
        var animationBody = source.Substring(
            source.IndexOf("string ProcessAnimationAndVisualEffects", StringComparison.Ordinal),
            source.IndexOf("void CheckForActivationInterruption", StringComparison.Ordinal) -
            source.IndexOf("string ProcessAnimationAndVisualEffects", StringComparison.Ordinal));
        var completeBody = source.Substring(
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal),
            source.IndexOf("// Begin the main process", StringComparison.Ordinal) -
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal));

        resumeBody.Should().Contain("Enmity.IssueAttackCommand(activator, target, clearActions);");
        animationBody.Should().Contain("if (GetIsPC(activator))");
        animationBody.Should().Contain("ClearAllActions(true);");
        completeBody.Should().Contain("ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f);");
        completeBody.Should().NotContain("clearActions: false");
    }

    [Test]
    public void NpcAttackReissue_UsesGuardedEnmityAttackBeforeReattacking()
    {
        var enmitySource = ReadSource("SWLOR.Game.Server", "Service", "Enmity.cs").Replace("\r\n", "\n");
        var npcAiSource = ReadSource("SWLOR.Game.Server", "Service", "AIService", "NPCAI.cs").Replace("\r\n", "\n");
        var attackHighestIndex = enmitySource.IndexOf("public static void AttackHighestEnmityTarget", StringComparison.Ordinal);
        var executeActionIndex = npcAiSource.IndexOf("private static void ExecuteAction", StringComparison.Ordinal);
        var attackActionIndex = npcAiSource.IndexOf("case AIActionType.AttackHighestEnmity:", executeActionIndex, StringComparison.Ordinal);
        var fallbackIndex = npcAiSource.IndexOf("private static void ExecuteAbility", StringComparison.Ordinal);
        var attackHighestBody = enmitySource.Substring(
            attackHighestIndex,
            enmitySource.IndexOf("private static bool ShouldIssueAttackCommand", attackHighestIndex, StringComparison.Ordinal) -
            attackHighestIndex);
        var issueBody = enmitySource.Substring(
            enmitySource.IndexOf("public static void IssueAttackCommand", StringComparison.Ordinal),
            enmitySource.IndexOf("private static bool ShouldIssueAttackCommand", StringComparison.Ordinal) -
            enmitySource.IndexOf("public static void IssueAttackCommand", StringComparison.Ordinal));
        var attackActionBody = npcAiSource.Substring(
            attackActionIndex,
            npcAiSource.IndexOf("case AIActionType.MoveToTarget:", attackActionIndex, StringComparison.Ordinal) -
            attackActionIndex);
        var fallbackBody = npcAiSource.Substring(
            fallbackIndex,
            npcAiSource.IndexOf("private static bool IsOnCooldown", fallbackIndex, StringComparison.Ordinal) -
            fallbackIndex);

        attackHighestBody.Should().Contain("IssueAttackCommand(creature, target);");
        issueBody.Should().Contain("ClearAllActions(true);");
        issueBody.Should().Contain("ActionMoveToObject(target, true, MeleeAttackMoveRange);");
        attackActionBody.Should().Contain("Enmity.AttackHighestEnmityTarget(context.Self);");
        attackActionBody.Should().NotContain("ClearAllActions");
        attackActionBody.Should().NotContain("ActionAttack");
        fallbackBody.Should().Contain("ClearAllActions(true);");
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
            aiSource.IndexOf("private static bool ShouldUseCombatLeash", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal));

        var policyIndex = leashBody.IndexOf("if (!ShouldUseCombatLeash(creature))", StringComparison.Ordinal);
        var radiusIndex = leashBody.IndexOf("var leashRadius = GetCombatLeashRadius(creature, target);", StringComparison.Ordinal);
        var creatureOutsideIndex = leashBody.IndexOf(
            "var creatureOutsideLeashRadius = IsOutsideHomeRadius(creature, homeLocation, leashRadius);",
            StringComparison.Ordinal);
        var activeTargetIndex = leashBody.IndexOf("IsNearActiveCombatTarget(creature, target)", StringComparison.Ordinal);
        var hostileCombatantIndex = leashBody.IndexOf("IsNearHostilePlayerOrCompanion(creature)", StringComparison.Ordinal);
        var targetOutsideIndex = leashBody.IndexOf("if (!IsOutsideHomeRadius(target, homeLocation, leashRadius))", StringComparison.Ordinal);

        processFlagsBody.Should().Contain("ShouldStartCombatLeashEvade(self, highestEnmityTarget, homeLocation)");
        policyIndex.Should().BeGreaterThanOrEqualTo(0);
        policyIndex.Should().BeLessThan(radiusIndex);
        radiusIndex.Should().BeGreaterThanOrEqualTo(0);
        radiusIndex.Should().BeLessThan(creatureOutsideIndex);
        creatureOutsideIndex.Should().BeGreaterThanOrEqualTo(0);
        activeTargetIndex.Should().BeGreaterThan(creatureOutsideIndex);
        hostileCombatantIndex.Should().BeGreaterThan(activeTargetIndex);
        hostileCombatantIndex.Should().BeLessThan(targetOutsideIndex);
        activeTargetIndex.Should().BeLessThan(targetOutsideIndex);
        targetOutsideIndex.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void CombatLeash_UsesCompanionMasterBeforeResettingCombat()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var leashBody = aiSource.Substring(
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal),
            aiSource.IndexOf("public static bool IsLeashEvading", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal));

        var activeTargetIndex = leashBody.IndexOf("IsNearActiveCombatTarget(creature, target)", StringComparison.Ordinal);
        var hostileCombatantIndex = leashBody.IndexOf("IsNearHostilePlayerOrCompanion(creature)", StringComparison.Ordinal);
        var targetOutsideIndex = leashBody.IndexOf(
            "if (!IsOutsideHomeRadius(target, homeLocation, leashRadius))",
            StringComparison.Ordinal);
        var creatureOutsideIndex = leashBody.IndexOf(
            "var creatureOutsideLeashRadius = IsOutsideHomeRadius(creature, homeLocation, leashRadius);",
            StringComparison.Ordinal);
        var masterIndex = leashBody.IndexOf("var targetMaster = GetMaster(target);", StringComparison.Ordinal);
        var masterInsideIndex = leashBody.IndexOf(
            "!IsOutsideHomeRadius(targetMaster, homeLocation, leashRadius)",
            StringComparison.Ordinal);
        var creatureInsideIndex = leashBody.IndexOf("!creatureOutsideLeashRadius", StringComparison.Ordinal);

        creatureOutsideIndex.Should().BeGreaterThanOrEqualTo(0);
        activeTargetIndex.Should().BeGreaterThan(creatureOutsideIndex);
        hostileCombatantIndex.Should().BeGreaterThan(activeTargetIndex);
        hostileCombatantIndex.Should().BeLessThan(targetOutsideIndex);
        activeTargetIndex.Should().BeLessThan(targetOutsideIndex);
        targetOutsideIndex.Should().BeGreaterThanOrEqualTo(0);
        masterIndex.Should().BeGreaterThan(targetOutsideIndex);
        masterInsideIndex.Should().BeGreaterThan(masterIndex);
        creatureInsideIndex.Should().BeGreaterThan(masterInsideIndex);
        leashBody.Should().Contain("GetIsPC(targetMaster)");
        leashBody.Should().Contain("IsWithinCombatEngagementRange(creature, targetMaster)");
        leashBody.Should().Contain("return true;");
    }

    [Test]
    public void CombatLeash_RequiresReturnHomeHostilityBeforeResettingCombat()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var leashBody = aiSource.Substring(
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool ShouldUseCombatLeash", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal));
        var policyBody = aiSource.Substring(
            aiSource.IndexOf("private static bool ShouldUseCombatLeash", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool IsNearActiveCombatTarget", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldUseCombatLeash", StringComparison.Ordinal));
        var hostileCombatantBody = aiSource.Substring(
            aiSource.IndexOf("private static bool IsNearHostilePlayerOrCompanion", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool IsWithinCombatEngagementRange", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool IsNearHostilePlayerOrCompanion", StringComparison.Ordinal));

        leashBody.Should().Contain("if (!ShouldUseCombatLeash(creature))");
        policyBody.Should().Contain("GetAIFlag(creature).HasFlag(AIFlag.ReturnHome)");
        policyBody.Should().Contain("for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())");
        policyBody.Should().Contain("if (GetIsDM(player))");
        policyBody.Should().Contain("GetArea(player) == GetArea(creature)");
        policyBody.Should().Contain("GetIsEnemy(player, creature)");
        hostileCombatantBody.Should().Contain("GetIsDM(player)");
        hostileCombatantBody.Should().Contain("GetArea(player) != GetArea(creature)");
        hostileCombatantBody.Should().Contain("!GetIsEnemy(player, creature)");
        hostileCombatantBody.Should().Contain("IsWithinCombatEngagementRange(creature, player)");
        hostileCombatantBody.Should().Contain("GetAssociate(AssociateType.Henchman, player)");
        hostileCombatantBody.Should().Contain("IsWithinCombatEngagementRange(creature, companion)");
    }

    [Test]
    public void CombatLeash_UsesHitDistanceAndActiveEngagementBeforeResettingCombat()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var combatLeashRadius = ReadConstFloat(
            "CombatLeashRadius",
            "SWLOR.Game.Server",
            "Service",
            "AI.cs");
        var activeCombatLeashRadius = ReadConstFloat(
            "ActiveCombatLeashRadius",
            "SWLOR.Game.Server",
            "Service",
            "AI.cs");
        var nearBody = aiSource.Substring(
            aiSource.IndexOf("private static bool IsNearActiveCombatTarget", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool IsNearHostilePlayerOrCompanion", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool IsNearActiveCombatTarget", StringComparison.Ordinal));
        var hostileCombatantBody = aiSource.Substring(
            aiSource.IndexOf("private static bool IsNearHostilePlayerOrCompanion", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool IsWithinCombatEngagementRange", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool IsNearHostilePlayerOrCompanion", StringComparison.Ordinal));
        var engagementBody = aiSource.Substring(
            aiSource.IndexOf("private static bool IsWithinCombatEngagementRange", StringComparison.Ordinal),
            aiSource.IndexOf("private static float GetActiveCombatLeashRadius", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool IsWithinCombatEngagementRange", StringComparison.Ordinal));
        var activeRadiusBody = aiSource.Substring(
            aiSource.IndexOf("private static float GetActiveCombatLeashRadius", StringComparison.Ordinal),
            aiSource.IndexOf("private static float GetCombatLeashRadius", StringComparison.Ordinal) -
            aiSource.IndexOf("private static float GetActiveCombatLeashRadius", StringComparison.Ordinal));
        var combatRadiusBody = aiSource.Substring(
            aiSource.IndexOf("private static float GetCombatLeashRadius", StringComparison.Ordinal),
            aiSource.IndexOf("private static float GetHitDistance", StringComparison.Ordinal) -
            aiSource.IndexOf("private static float GetCombatLeashRadius", StringComparison.Ordinal));
        var hitDistanceBody = aiSource.Substring(
            aiSource.IndexOf("private static float GetHitDistance", StringComparison.Ordinal),
            aiSource.IndexOf("public static bool IsLeashEvading", StringComparison.Ordinal) -
            aiSource.IndexOf("private static float GetHitDistance", StringComparison.Ordinal));

        combatLeashRadius.Should().BeGreaterThan(35f);
        activeCombatLeashRadius.Should().BeGreaterThan(0f);
        nearBody.Should().Contain("IsWithinCombatEngagementRange(creature, target)");
        nearBody.Should().Contain("IsWithinCombatEngagementRange(creature, targetMaster)");
        hostileCombatantBody.Should().Contain("IsWithinCombatEngagementRange(creature, player)");
        hostileCombatantBody.Should().Contain("IsWithinCombatEngagementRange(creature, companion)");
        engagementBody.Should().Contain("GetArea(creature) == GetArea(target)");
        engagementBody.Should().Contain("GetDistanceBetween(creature, target) <= GetActiveCombatLeashRadius(creature, target)");
        activeRadiusBody.Should().Contain("ActiveCombatLeashRadius + GetHitDistance(creature) + GetHitDistance(target)");
        combatRadiusBody.Should().Contain("CombatLeashRadius + GetHitDistance(creature) + GetHitDistance(target)");
        hitDistanceBody.Should().Contain("CreaturePlugin.GetHitDistance(creature)");
    }

    [Test]
    public void CombatLeash_RequiresPersistentCandidateBeforeStartingEvade()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var processFlagsBody = aiSource.Substring(
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal),
            aiSource.IndexOf("private static void ProcessCreatureAllies()", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal));
        var startBody = aiSource.Substring(
            aiSource.IndexOf("private static bool ShouldStartCombatLeashEvade", StringComparison.Ordinal),
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldStartCombatLeashEvade", StringComparison.Ordinal));
        var tryStartBody = aiSource.Substring(
            aiSource.IndexOf("private static bool TryStartLeashEvade", StringComparison.Ordinal),
            aiSource.IndexOf("private static uint GetHighestOrEventTarget", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool TryStartLeashEvade", StringComparison.Ordinal));
        var graceBody = aiSource.Substring(
            aiSource.IndexOf("private static bool HasCombatLeashGraceExpired", StringComparison.Ordinal),
            aiSource.IndexOf("private static void ClearCombatLeashCandidate", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool HasCombatLeashGraceExpired", StringComparison.Ordinal));
        var clearBody = aiSource.Substring(
            aiSource.IndexOf("private static void ClearCombatLeashCandidate", StringComparison.Ordinal),
            aiSource.IndexOf("public static bool IsLeashEvading", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void ClearCombatLeashCandidate", StringComparison.Ordinal));

        var combatLeashGraceSeconds = ReadConstFloat(
            "CombatLeashGraceSeconds",
            "SWLOR.Game.Server",
            "Service",
            "AI.cs");
        var leashCheckIndex = processFlagsBody.IndexOf(
            "ShouldStartCombatLeashEvade(self, highestEnmityTarget, homeLocation)",
            StringComparison.Ordinal);
        var startEvadeIndex = processFlagsBody.IndexOf("StartLeashEvade(self, homeLocation)", StringComparison.Ordinal);
        var clearIdleIndex = processFlagsBody.IndexOf("ClearCombatLeashCandidate(self)", StringComparison.Ordinal);

        combatLeashGraceSeconds.Should().BeGreaterThan(0f);
        processFlagsBody.Should().Contain("var hasCombatState = GetIsInCombat(self) || GetIsObjectValid(highestEnmityTarget);");
        leashCheckIndex.Should().BeGreaterThanOrEqualTo(0);
        startEvadeIndex.Should().BeGreaterThan(leashCheckIndex);
        clearIdleIndex.Should().BeGreaterThan(startEvadeIndex);
        startBody.Should().Contain("ShouldLeashCombatTarget(creature, target, homeLocation)");
        startBody.Should().Contain("ClearCombatLeashCandidate(creature)");
        startBody.Should().Contain("HasCombatLeashGraceExpired(creature)");
        tryStartBody.Should().Contain("ShouldStartCombatLeashEvade(creature, target, homeLocation)");
        graceBody.Should().Contain("_combatLeashCandidateTimes.TryGetValue(creature, out var firstDetectedAt)");
        graceBody.Should().Contain("_combatLeashCandidateTimes[creature] = now");
        graceBody.Should().Contain("CombatLeashGraceSeconds");
        clearBody.Should().Contain("_combatLeashCandidateTimes.Remove(creature)");
    }

    [Test]
    public void CombatLeash_StartsFullEvadeBeforeIdleEffectGuards()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var processFlagsBody = aiSource.Substring(
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal),
            aiSource.IndexOf("private static void ProcessCreatureAllies()", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal));

        var activeEvadeIndex = processFlagsBody.IndexOf("if (IsLeashEvading(self))", StringComparison.Ordinal);
        var idleEffectGuardIndex = processFlagsBody.IndexOf("var effects = new[]", StringComparison.Ordinal);
        var leashCheckIndex = processFlagsBody.IndexOf("ShouldStartCombatLeashEvade(self, highestEnmityTarget, homeLocation)", StringComparison.Ordinal);
        var startEvadeIndex = processFlagsBody.IndexOf("StartLeashEvade(self, homeLocation)", StringComparison.Ordinal);

        activeEvadeIndex.Should().BeGreaterThanOrEqualTo(0);
        activeEvadeIndex.Should().BeLessThan(idleEffectGuardIndex);
        leashCheckIndex.Should().BeGreaterThanOrEqualTo(0);
        leashCheckIndex.Should().BeLessThan(idleEffectGuardIndex);
        startEvadeIndex.Should().BeGreaterThan(leashCheckIndex);
        startEvadeIndex.Should().BeLessThan(idleEffectGuardIndex);
        processFlagsBody.Should().Contain("ContinueLeashEvadeReturn(self, homeLocation)");
        processFlagsBody.Should().Contain("EndLeashEvade(self)");
    }

    [Test]
    public void CombatRecovery_HeartbeatRechecksHighestEnmityBeforeIdleReturn()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var processFlagsBody = aiSource.Substring(
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal),
            aiSource.IndexOf("private static void ProcessCreatureAllies()", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void ProcessFlags()", StringComparison.Ordinal));

        var idleEffectGuardIndex = processFlagsBody.IndexOf("var effects = new[]", StringComparison.Ordinal);
        var recoveryIndex = processFlagsBody.IndexOf("Enmity.AttackHighestEnmityTarget(self)", StringComparison.Ordinal);
        var idleReturnIndex = processFlagsBody.IndexOf("if (IsInConversation(self) ||", StringComparison.Ordinal);

        recoveryIndex.Should().BeGreaterThan(idleEffectGuardIndex);
        recoveryIndex.Should().BeLessThan(idleReturnIndex);
        processFlagsBody.Should().Contain("if (GetIsObjectValid(highestEnmityTarget))");
        processFlagsBody.Should().Contain("if (!IsInConversation(self))");
    }

    [Test]
    public void CombatLeash_EvadeUsesPlotProtectionAndRestoresPreviousPlotState()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var evadeMovementRate = ReadConstFloat(
            "LeashEvadeMovementRateFactor",
            "SWLOR.Game.Server",
            "Service",
            "AI.cs");
        var startBody = aiSource.Substring(
            aiSource.IndexOf("private static void StartLeashEvade", StringComparison.Ordinal),
            aiSource.IndexOf("private static void ContinueLeashEvadeReturn", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void StartLeashEvade", StringComparison.Ordinal));
        var continueBody = aiSource.Substring(
            aiSource.IndexOf("private static void ContinueLeashEvadeReturn", StringComparison.Ordinal),
            aiSource.IndexOf("private static void EndLeashEvade", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void ContinueLeashEvadeReturn", StringComparison.Ordinal));
        var endBody = aiSource.Substring(
            aiSource.IndexOf("private static void EndLeashEvade", StringComparison.Ordinal),
            aiSource.IndexOf("private static void RemoveEnemySourcedStatusEffects", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void EndLeashEvade", StringComparison.Ordinal));

        evadeMovementRate.Should().BeGreaterThan(2f);
        startBody.Should().Contain("SetLocalBool(creature, LeashEvadeRestorePlotFlagVariable, GetPlotFlag(creature))");
        startBody.Should().Contain("SetLocalInt(creature, LeashEvadeRestoreMovementRateVariable, GetMovementRate(creature))");
        startBody.Should().Contain("SetPlotFlag(creature, true)");
        startBody.Should().Contain("RemoveEnemySourcedStatusEffects(creature)");
        startBody.Should().Contain("SetCurrentHitPoints(creature, GetMaxHitPoints(creature))");
        startBody.Should().Contain("Enmity.ClearEnmityTable(creature)");
        startBody.Should().Contain("NPCAI.ClearState(creature)");
        startBody.Should().Contain("ApplyLeashEvadeMovementRate(creature)");
        startBody.Should().Contain("DelayCommand(0.2f");
        continueBody.Should().Contain("ApplyLeashEvadeMovementRate(creature)");
        continueBody.Should().Contain("ClearAllActions(true)");
        continueBody.Should().Contain("ActionForceMoveToLocation(homeLocation, true, 60f)");
        endBody.Should().Contain("SetCurrentHitPoints(creature, GetMaxHitPoints(creature))");
        endBody.Should().Contain("SetPlotFlag(creature, GetLocalBool(creature, LeashEvadeRestorePlotFlagVariable))");
        endBody.Should().Contain("DeleteLocalBool(creature, LeashEvadeRestorePlotFlagVariable)");
        endBody.Should().Contain("DeleteLocalBool(creature, LeashEvadeActiveVariable)");
        endBody.Should().Contain("RestoreLeashEvadeMovementRate(creature)");
        endBody.Should().Contain("CreaturePlugin.SetMovementRate(creature, MovementRate.DMFast)");
        endBody.Should().Contain("Stat.ApplyCreatureMovementRate(creature)");
        endBody.Should().Contain("CreaturePlugin.SetMovementRateFactor(creature, LeashEvadeMovementRateFactor)");
    }

    [Test]
    public void CombatLeash_EvadingCreaturesIgnoreNewAggroAndEnmity()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var enmitySource = ReadSource("SWLOR.Game.Server", "Service", "Enmity.cs").Replace("\r\n", "\n");
        var aggroEnterBody = aiSource.Substring(
            aiSource.IndexOf("public static void CreatureAggroEnter()", StringComparison.Ordinal),
            aiSource.IndexOf("public static void CreatureAggroExit()", StringComparison.Ordinal) -
            aiSource.IndexOf("public static void CreatureAggroEnter()", StringComparison.Ordinal));
        var attackedBody = aiSource.Substring(
            aiSource.IndexOf("public static void CreaturePhysicalAttacked()", StringComparison.Ordinal),
            aiSource.IndexOf("public static void CreatureDamaged()", StringComparison.Ordinal) -
            aiSource.IndexOf("public static void CreaturePhysicalAttacked()", StringComparison.Ordinal));
        var damagedBody = aiSource.Substring(
            aiSource.IndexOf("public static void CreatureDamaged()", StringComparison.Ordinal),
            aiSource.IndexOf("public static void CreatureDeath()", StringComparison.Ordinal) -
            aiSource.IndexOf("public static void CreatureDamaged()", StringComparison.Ordinal));
        var roundEndBody = aiSource.Substring(
            aiSource.IndexOf("public static void CreatureCombatRoundEnd()", StringComparison.Ordinal),
            aiSource.IndexOf("public static void CreatureConversation()", StringComparison.Ordinal) -
            aiSource.IndexOf("public static void CreatureCombatRoundEnd()", StringComparison.Ordinal));
        var modifyBody = enmitySource.Substring(
            enmitySource.IndexOf("public static void ModifyEnmity", StringComparison.Ordinal),
            enmitySource.IndexOf("private static int CalculateEnmityAdjustment", StringComparison.Ordinal) -
            enmitySource.IndexOf("public static void ModifyEnmity", StringComparison.Ordinal));

        aggroEnterBody.Should().Contain("if (IsLeashEvading(self))");
        attackedBody.Should().Contain("if (IsLeashEvading(creature))");
        attackedBody.Should().Contain("TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastAttacker(creature)))");
        damagedBody.Should().Contain("if (IsLeashEvading(creature))");
        damagedBody.Should().Contain("TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastDamager(creature)))");
        roundEndBody.Should().Contain("TryStartLeashEvade(creature, Enmity.GetHighestEnmityTarget(creature))");
        modifyBody.Should().Contain("if (AI.IsLeashEvading(enemy))");
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
            .Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
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
