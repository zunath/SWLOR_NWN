using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
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
            IsHostileAbility = true,
            IsAreaAbility = true,
            AbilityLevel = 5,
            MaxRange = 12f
        }).Should().NotBeNull();
    }

    [Test]
    public void AIScore_DefensiveAbilitiesRequireCombatEnmity()
    {
        const uint self = 100;
        const uint target = 200;
        var score = AIScore.Ability(new AbilityDetail
        {
            AbilityLevel = 4
        });

        score(CreateContext(self: self)).Should().Be(0);

        EnemyEnmityTables()[self] = new Dictionary<uint, int>
        {
            [target] = 1
        };
        CreatureToEnemies()[target] = new List<uint> { self };

        score(CreateContext(self: self)).Should().Be(AIScoreBand.Defensive + 4);
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
    public void AITarget_SelfCenteredHostileAreasUseCasterAndNamedTargetPolicy()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "AIService", "AITarget.cs")
            .Replace("\r\n", "\n");
        var selectorBody = source.Substring(
            source.IndexOf("private static AITargetSelector SelfCenteredHostileArea", StringComparison.Ordinal),
            source.IndexOf("public static AITargetSelector AllyAttacker", StringComparison.Ordinal) -
            source.IndexOf("private static AITargetSelector SelfCenteredHostileArea", StringComparison.Ordinal));
        var inferDefaultBody = source.Substring(
            source.IndexOf("public static AITargetSelector InferDefault", StringComparison.Ordinal));

        source.Should().Contain("private const int DefaultAreaAbilityMinimumTargets = 2;");
        source.Should().NotContain("HostileCluster(ability.MaxRange, 2)");
        selectorBody.Should().Contain("ability.Targeting.ResolveSizeX(context.Self, true)");
        selectorBody.Should().Contain("context.SetEvaluatedTarget(context.Self);");
        selectorBody.Should().Contain("context.CountHostilesNearTarget(radius) >= DefaultAreaAbilityMinimumTargets");
        inferDefaultBody.Should().Contain("ability.Targeting?.Shape == AbilityTargetingShapeType.Sphere");
        inferDefaultBody.Should().Contain("AbilityTargetingFlags.OriginOnSelf");
        inferDefaultBody.Should().Contain("SelfCenteredHostileArea(ability)");
        inferDefaultBody.Should().Contain("HostileCluster(ability.MaxRange, DefaultAreaAbilityMinimumTargets)");
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
        var rangeStartIndex = aiSource.IndexOf("static bool IsInAggroRange", StringComparison.Ordinal);
        var rangeBody = aiSource.Substring(
            rangeStartIndex,
            aiSource.IndexOf("private static void TryAcquireAggro", StringComparison.Ordinal) -
            rangeStartIndex);

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
    public void CreatureAggroExit_RemovesProximityEnmityBeforeResumingCombat()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var removeStartIndex = aiSource.IndexOf("private static void RemoveProximityEnmity", StringComparison.Ordinal);
        var removeBody = aiSource.Substring(
            removeStartIndex,
            aiSource.IndexOf("private static bool IsAIEnabled", StringComparison.Ordinal) - removeStartIndex);

        var leashEvadeIndex = removeBody.IndexOf("TryStartLeashEvade(enemy, target)", StringComparison.Ordinal);
        var removeIndex = removeBody.IndexOf("Enmity.RemoveProximityEnmity(target, enemy)", StringComparison.Ordinal);
        var nextTargetIndex = removeBody.IndexOf("var nextTarget = Enmity.GetHighestEnmityTarget(enemy);", StringComparison.Ordinal);
        var resumeIndex = removeBody.IndexOf("Enmity.AttackHighestEnmityTarget(enemy);", StringComparison.Ordinal);
        var clearStateIndex = removeBody.IndexOf("NPCAI.ClearState(enemy);", StringComparison.Ordinal);
        var fastReturnIndex = removeBody.IndexOf("TryReturnHomeAfterCombat(enemy)", StringComparison.Ordinal);
        var clearActionsIndex = removeBody.IndexOf("ClearAllActions()", StringComparison.Ordinal);

        leashEvadeIndex.Should().BeGreaterThanOrEqualTo(0);
        leashEvadeIndex.Should().BeLessThan(removeIndex);
        removeIndex.Should().BeLessThan(nextTargetIndex);
        nextTargetIndex.Should().BeLessThan(resumeIndex);
        resumeIndex.Should().BeLessThan(clearStateIndex);
        fastReturnIndex.Should().BeGreaterThan(clearStateIndex);
        fastReturnIndex.Should().BeLessThan(clearActionsIndex);
        aiSource.Should().NotContain("ShouldKeepCombatProximityEnmity");
        removeBody.Should().NotContain("GetIsInCombat(enemy)");
    }

    [Test]
    public void CreatureSpawn_UsesExistingCreatureEventScriptsWithoutBaseNwnCombatScripts()
    {
        var spawnSource = ReadSource("SWLOR.Game.Server", "Service", "Spawn.cs").Replace("\r\n", "\n");
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var enmitySource = ReadSource("SWLOR.Game.Server", "Service", "Enmity.cs").Replace("\r\n", "\n");
        var attackedScript = ReadSource("Module", "nss", "nw_c2_default5.nss").Replace("\r\n", "\n");
        var damagedScript = ReadSource("Module", "nss", "nw_c2_default6.nss").Replace("\r\n", "\n");
        var disturbedScript = ReadSource("Module", "nss", "nw_c2_default8.nss").Replace("\r\n", "\n");
        var spawnScript = ReadSource("Module", "nss", "nw_c2_default9.nss").Replace("\r\n", "\n");
        var spellCastScript = ReadSource("Module", "nss", "nw_c2_defaultb.nss").Replace("\r\n", "\n");
        var adjustBody = spawnSource.Substring(
            spawnSource.IndexOf("private static void AdjustScripts", StringComparison.Ordinal),
            spawnSource.IndexOf("private static void AdjustStats", StringComparison.Ordinal) -
            spawnSource.IndexOf("private static void AdjustScripts", StringComparison.Ordinal));

        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnBlockedByDoor, \"x2_def_onblocked\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnEndCombatRound, \"x2_def_endcombat\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnDamaged, \"x2_def_ondamage\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnDeath, \"x2_def_ondeath\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnDisturbed, \"x2_def_ondisturb\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnHeartbeat, \"x2_def_heartbeat\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnNotice, \"x2_def_percept\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnMeleeAttacked, \"x2_def_attacked\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnRested, \"x2_def_rested\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnSpawnIn, \"x2_def_spawn\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnSpellCastAt, \"x2_def_spellcast\")");
        adjustBody.Should().Contain("SetEventScript(spawn, EventScript.Creature_OnUserDefined, \"x2_def_userdef\")");
        adjustBody.Should().Contain("ExecuteScript(\"x2_def_spawn\", spawn)");
        spawnSource.Should().Contain("Stat.LoadNPCStats(deserialized);");
        spawnSource.Should().Contain("if (spawnObject.Type == ObjectType.Creature)\n                {\n                    Stat.LoadNPCStats(spawn);\n                }");
        attackedScript.Should().Contain("ExecuteScript(\"crea_attack_bef\", OBJECT_SELF)");
        attackedScript.Should().Contain("ExecuteScript(\"crea_attack_aft\", OBJECT_SELF)");
        damagedScript.Should().Contain("ExecuteScript(\"crea_damaged_bef\", OBJECT_SELF)");
        damagedScript.Should().Contain("ExecuteScript(\"crea_damaged_aft\", OBJECT_SELF)");
        disturbedScript.Should().Contain("ExecuteScript(\"crea_disturb_bef\", OBJECT_SELF)");
        disturbedScript.Should().Contain("ExecuteScript(\"crea_disturb_aft\", OBJECT_SELF)");
        spawnScript.Should().Contain("#include \"x0_i0_walkway\"");
        spawnScript.Should().Contain("ExecuteScript(\"crea_spawn_bef\", OBJECT_SELF)");
        spawnScript.Should().Contain("WalkWayPoints();");
        spawnScript.Should().Contain("ExecuteScript(\"crea_spawn_aft\", OBJECT_SELF)");
        spellCastScript.Should().Contain("ExecuteScript(\"crea_splcast_bef\", OBJECT_SELF)");
        spellCastScript.Should().Contain("ExecuteScript(\"crea_splcast_aft\", OBJECT_SELF)");
        string.Join("\n", attackedScript, damagedScript, disturbedScript, spawnScript, spellCastScript)
            .Should()
            .NotContain("DetermineCombatRound")
            .And.NotContain("SetListeningPatterns")
            .And.NotContain("SetSummonHelpIfAttacked")
            .And.NotContain("NW_I_WAS_ATTACKED")
            .And.NotContain("NW_ATTACK_MY_TARGET");
        aiSource.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureAttackAfter)]");
        aiSource.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]");
        enmitySource.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureAttackBefore)]");
        enmitySource.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureDamagedBefore)]");
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
            source.IndexOf("ProcessAnimationAndVisualEffects", StringComparison.Ordinal),
            source.IndexOf("void CheckForActivationInterruption", StringComparison.Ordinal) -
            source.IndexOf("ProcessAnimationAndVisualEffects", StringComparison.Ordinal));
        var completeBody = source.Substring(
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal),
            source.IndexOf("// Begin the main process", StringComparison.Ordinal) -
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal));
        var delayedResumeBody = source.Substring(
            source.IndexOf("private static void ResumeAttackAfterDelay", StringComparison.Ordinal),
            source.IndexOf("/// <summary>\n        /// Breaks stealth", StringComparison.Ordinal) -
            source.IndexOf("private static void ResumeAttackAfterDelay", StringComparison.Ordinal));

        resumeBody.Should().Contain("Enmity.IssueAttackCommand(activator, target, clearActions);");
        resumeBody.Should().Contain("target = Enmity.GetHighestEnmityTarget(activator);");
        delayedResumeBody.Should().Contain("GetIsPC(activator) || GetIsPC(GetMaster(activator))");
        delayedResumeBody.Should().Contain("DelayCommand(delay, () =>");
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
        var processTriggerBody = npcAiSource.Substring(
            npcAiSource.IndexOf("public static bool ProcessTrigger", StringComparison.Ordinal),
            npcAiSource.IndexOf("private static AIState GetState", StringComparison.Ordinal) -
            npcAiSource.IndexOf("public static bool ProcessTrigger", StringComparison.Ordinal));
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

        processTriggerBody.Should().Contain("AI.TryStartCombatLeashEvade(creature, context.CurrentEnmityTarget)");
        processTriggerBody.IndexOf("AI.TryStartCombatLeashEvade(creature, context.CurrentEnmityTarget)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(processTriggerBody.IndexOf("SelectAction(context)", StringComparison.Ordinal));
        attackHighestBody.Should().Contain("ShouldRemoveStaleProximityTarget(creature, target)");
        attackHighestBody.Should().Contain("RemoveProximityEnmity(target, creature);");
        attackHighestBody.Should().Contain("AI.TryStartCombatLeashEvade(creature, target)");
        attackHighestBody.Should().Contain("IssueAttackCommand(creature, target);");
        issueBody.Should().Contain("AI.TryStartCombatLeashEvade(creature, target)");
        issueBody.Should().Contain("ActionDoCommand(() =>");
        issueBody.IndexOf("ActionDoCommand(() =>", StringComparison.Ordinal)
            .Should()
            .BeGreaterThan(issueBody.IndexOf("ActionMoveToObject(target, true, GetAttackMoveRange(creature));", StringComparison.Ordinal));
        issueBody.IndexOf("ActionAttack(target);", StringComparison.Ordinal)
            .Should()
            .BeGreaterThan(issueBody.LastIndexOf("AI.TryStartCombatLeashEvade(creature, target)", StringComparison.Ordinal));
        issueBody.Should().Contain("ClearAllActions(true);");
        issueBody.Should().Contain("ActionMoveToObject(target, true, GetAttackMoveRange(creature));");
        enmitySource.Should().Contain("Combat.GetWeaponEngagementRange(skillType)");
        enmitySource.Should().NotContain("GetPreferredAttackDistance");
        attackActionBody.Should().Contain("Enmity.AttackHighestEnmityTarget(context.Self);");
        attackActionBody.Should().NotContain("ClearAllActions");
        attackActionBody.Should().NotContain("ActionAttack");
        fallbackBody.Should().Contain("Enmity.IssueAttackCommand(creature, target);");
        fallbackBody.Should().NotContain("ActionAttack");
    }

    [Test]
    public void NativeAttackAction_CancelsLeashedCreaturesBeforePathingOrResolvingAttack()
    {
        var source = ReadSource("SWLOR.Game.Server", "Native", "OnAIActionAttackObject.cs").Replace("\r\n", "\n");
        var activeTargetIndex = source.IndexOf("if (bTargetActive)", StringComparison.Ordinal);
        var activeTargetBody = source.Substring(
            activeTargetIndex,
            source.IndexOf("pCreature.m_vLastAttackPosition = new Vector();", activeTargetIndex, StringComparison.Ordinal) -
            activeTargetIndex);
        var pendingAttackIndex = source.IndexOf("case CNWSCOMBATROUND_TYPE_ATTACK:", StringComparison.Ordinal);
        var pendingAttackBody = source.Substring(
            pendingAttackIndex,
            source.IndexOf("case CNWSCOMBATROUND_TYPE_PARRY:", pendingAttackIndex, StringComparison.Ordinal) -
            pendingAttackIndex);

        source.Should().Contain("private static bool TryCancelAttackForCombatLeash");
        source.Should().Contain("AI.TryStartCombatLeashEvade(pCreature.m_idSelf, target)");
        source.Should().Contain("_creatureAttackDelays.Remove(pCreature.m_idSelf);");
        source.Should().Contain("pCreature.ChangeAttackTarget(pNode, OBJECT_INVALID);");
        activeTargetBody.Should().Contain("TryCancelAttackForCombatLeash(pCreature, pNode, oidAttackTarget)");
        activeTargetBody.IndexOf("TryCancelAttackForCombatLeash(pCreature, pNode, oidAttackTarget)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(activeTargetBody.IndexOf("pCreature.AddActionToFront", StringComparison.Ordinal));
        pendingAttackBody.Should().Contain("TryCancelAttackForCombatLeash(pCreature, pNode, oidTarget)");
        pendingAttackBody.IndexOf("TryCancelAttackForCombatLeash(pCreature, pNode, oidTarget)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(pendingAttackBody.IndexOf("pCreature.ResolveAttack(oidTarget, nAttacks, nTimeAnimation);", StringComparison.Ordinal));
    }

    [TestCase(false, false, 1.5f, 1.5f, true, true)]
    [TestCase(false, true, 0.25f, 10f, false, false)]
    [TestCase(true, false, 10f, 10f, true, true)]
    [TestCase(true, true, 10f, 10f, true, true)]
    public void NativeAttackAction_BlockedLineMovementPlan_SelectsExecutablePath(
        bool isOutsideAttackRange,
        bool hasRangedWeapon,
        float expectedPathCompletionRange,
        float expectedAttackCheckRange,
        bool expectedTrackAttackTarget,
        bool expectedTargetDestination)
    {
        var plan = CreateBlockedLineMovementPlan(
            10f,
            0f,
            0f,
            0f,
            0f,
            0f,
            2,
            10f,
            1.5f,
            isOutsideAttackRange,
            hasRangedWeapon);

        ReadPlanProperty<float>(plan, "PathCompletionRange").Should().Be(expectedPathCompletionRange);
        ReadPlanProperty<float>(plan, "AttackCheckRange").Should().Be(expectedAttackCheckRange);
        ReadPlanProperty<bool>(plan, "TrackAttackTarget").Should().Be(expectedTrackAttackTarget);

        var destinationIsTarget =
            ReadPlanProperty<float>(plan, "DestinationX") == 0f &&
            ReadPlanProperty<float>(plan, "DestinationY") == 0f &&
            ReadPlanProperty<float>(plan, "DestinationZ") == 0f;
        destinationIsTarget.Should().Be(expectedTargetDestination);
    }

    [Test]
    public void NativeAttackAction_BlockedRangedLine_ForcesLateralMovementAtFiringDistance()
    {
        var plan = CreateBlockedLineMovementPlan(
            10f,
            0f,
            2f,
            0f,
            0f,
            0f,
            2,
            10f,
            1.5f,
            false,
            true);
        var destinationX = ReadPlanProperty<float>(plan, "DestinationX");
        var destinationY = ReadPlanProperty<float>(plan, "DestinationY");
        var destinationZ = ReadPlanProperty<float>(plan, "DestinationZ");

        destinationX.Should().NotBe(10f);
        destinationY.Should().NotBe(0f);
        ReadPlanProperty<float>(plan, "PathCompletionRange").Should().BeLessThan(2f);
        ReadPlanProperty<float>(plan, "AttackCheckRange").Should().Be(10f);
        ReadPlanProperty<bool>(plan, "TrackAttackTarget").Should().BeFalse();

        var repositionedRadiusSquared = MathF.Pow(destinationX, 2) + MathF.Pow(destinationY, 2);
        repositionedRadiusSquared.Should().BeApproximately(100f, 0.001f);
        destinationZ.Should().Be(2f);
    }

    [Test]
    public void NativeAttackAction_BlockedRangedLine_OverlapFallbackUsesFiringDistance()
    {
        var plan = CreateBlockedLineMovementPlan(
            0f,
            0f,
            2f,
            0f,
            0f,
            0f,
            3,
            10f,
            1.5f,
            false,
            true);
        var destinationX = ReadPlanProperty<float>(plan, "DestinationX");
        var destinationY = ReadPlanProperty<float>(plan, "DestinationY");

        var destinationRadius = MathF.Sqrt(MathF.Pow(destinationX, 2) + MathF.Pow(destinationY, 2));
        destinationRadius.Should().BeApproximately(10f, 0.001f);
        ReadPlanProperty<float>(plan, "AttackCheckRange").Should().Be(10f);
        ReadPlanProperty<bool>(plan, "TrackAttackTarget").Should().BeFalse();
    }

    [Test]
    public void NativeAttackAction_BlockedRangedLine_NearOverlapStillForcesMovement()
    {
        var plan = CreateBlockedLineMovementPlan(
            0.2f,
            0f,
            2f,
            0f,
            0f,
            0f,
            5,
            10f,
            1.5f,
            false,
            true);
        var destinationX = ReadPlanProperty<float>(plan, "DestinationX");
        var destinationY = ReadPlanProperty<float>(plan, "DestinationY");
        var distanceFromAttacker = MathF.Sqrt(
            MathF.Pow(destinationX - 0.2f, 2) +
            MathF.Pow(destinationY, 2));

        distanceFromAttacker.Should().BeGreaterThan(
            ReadPlanProperty<float>(plan, "PathCompletionRange"));
        MathF.Sqrt(MathF.Pow(destinationX, 2) + MathF.Pow(destinationY, 2))
            .Should()
            .BeApproximately(10f, 0.001f);
    }

    [Test]
    public void NativeAttackAction_BlockedRangedLine_PathFailureAlternatesSidestep()
    {
        const uint attackerId = 9001;
        var firstPlan = CreateBlockedLineMovementPlan(
            10f,
            0f,
            0f,
            0f,
            0f,
            0f,
            attackerId,
            10f,
            1.5f,
            false,
            true);

        AlternateRangedRepositionDirection(attackerId);

        var secondPlan = CreateBlockedLineMovementPlan(
            10f,
            0f,
            0f,
            0f,
            0f,
            0f,
            attackerId,
            10f,
            1.5f,
            false,
            true);

        ReadPlanProperty<float>(firstPlan, "DestinationX")
            .Should()
            .BeApproximately(ReadPlanProperty<float>(secondPlan, "DestinationX"), 0.001f);
        (ReadPlanProperty<float>(firstPlan, "DestinationY") *
         ReadPlanProperty<float>(secondPlan, "DestinationY"))
            .Should()
            .BeNegative();
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
        var invalidTargetIndex = leashBody.IndexOf("if (!GetIsObjectValid(target))", StringComparison.Ordinal);
        var targetOutsideIndex = leashBody.IndexOf("if (!IsOutsideHomeRadius(target, homeLocation, leashRadius))", StringComparison.Ordinal);

        processFlagsBody.Should().Contain("ShouldStartCombatLeashEvade(self, highestEnmityTarget, homeLocation)");
        policyIndex.Should().BeGreaterThanOrEqualTo(0);
        policyIndex.Should().BeLessThan(radiusIndex);
        radiusIndex.Should().BeGreaterThanOrEqualTo(0);
        radiusIndex.Should().BeLessThan(creatureOutsideIndex);
        creatureOutsideIndex.Should().BeGreaterThanOrEqualTo(0);
        invalidTargetIndex.Should().BeGreaterThan(creatureOutsideIndex);
        invalidTargetIndex.Should().BeLessThan(targetOutsideIndex);
        targetOutsideIndex.Should().BeGreaterThanOrEqualTo(0);
        leashBody.Should().NotContain("IsNearActiveCombatTarget");
        leashBody.Should().NotContain("IsNearHostilePlayerOrCompanion");
    }

    [Test]
    public void CombatLeash_UsesCompanionMasterBeforeResettingCombat()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var leashBody = aiSource.Substring(
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal),
            aiSource.IndexOf("public static bool IsLeashEvading", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldLeashCombatTarget", StringComparison.Ordinal));

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
        targetOutsideIndex.Should().BeGreaterThanOrEqualTo(0);
        targetOutsideIndex.Should().BeGreaterThan(creatureOutsideIndex);
        masterIndex.Should().BeGreaterThan(targetOutsideIndex);
        masterInsideIndex.Should().BeGreaterThan(masterIndex);
        creatureInsideIndex.Should().BeGreaterThan(masterInsideIndex);
        leashBody.Should().Contain("GetIsPC(targetMaster)");
        leashBody.Should().NotContain("IsWithinCombatEngagementRange");
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
            aiSource.IndexOf("private static float GetCombatLeashRadius", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool ShouldUseCombatLeash", StringComparison.Ordinal));

        leashBody.Should().Contain("if (!ShouldUseCombatLeash(creature))");
        policyBody.Should().Contain("GetAIFlag(creature).HasFlag(AIFlag.ReturnHome)");
        policyBody.Should().Contain("for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())");
        policyBody.Should().Contain("if (GetIsDM(player))");
        policyBody.Should().Contain("GetArea(player) == GetArea(creature)");
        policyBody.Should().Contain("GetIsEnemy(player, creature)");
        policyBody.Should().NotContain("IsNearActiveCombatTarget");
        policyBody.Should().NotContain("IsNearHostilePlayerOrCompanion");
    }

    [Test]
    public void CombatLeash_UsesHitDistanceToBufferHomeDistance()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var combatLeashRadius = ReadConstFloat(
            "CombatLeashRadius",
            "SWLOR.Game.Server",
            "Service",
            "AI.cs");
        var combatRadiusBody = aiSource.Substring(
            aiSource.IndexOf("private static float GetCombatLeashRadius", StringComparison.Ordinal),
            aiSource.IndexOf("private static float GetHitDistance", StringComparison.Ordinal) -
            aiSource.IndexOf("private static float GetCombatLeashRadius", StringComparison.Ordinal));
        var hitDistanceBody = aiSource.Substring(
            aiSource.IndexOf("private static float GetHitDistance", StringComparison.Ordinal),
            aiSource.IndexOf("public static bool IsLeashEvading", StringComparison.Ordinal) -
            aiSource.IndexOf("private static float GetHitDistance", StringComparison.Ordinal));

        combatLeashRadius.Should().BeGreaterThan(35f);
        combatRadiusBody.Should().Contain("CombatLeashRadius + GetHitDistance(creature) + GetHitDistance(target)");
        hitDistanceBody.Should().Contain("CreaturePlugin.GetHitDistance(creature)");
        aiSource.Should().NotContain("ActiveCombatLeashRadius");
        aiSource.Should().NotContain("IsWithinCombatEngagementRange");
    }

    [Test]
    public void CombatLeash_StartsEvadeImmediatelyWhenTargetLeavesLeash()
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

        var leashCheckIndex = processFlagsBody.IndexOf(
            "ShouldStartCombatLeashEvade(self, highestEnmityTarget, homeLocation)",
            StringComparison.Ordinal);
        var startEvadeIndex = processFlagsBody.IndexOf("StartLeashEvade(self, homeLocation)", StringComparison.Ordinal);

        processFlagsBody.Should().Contain("var hasCombatState = GetIsInCombat(self) || GetIsObjectValid(highestEnmityTarget);");
        leashCheckIndex.Should().BeGreaterThanOrEqualTo(0);
        startEvadeIndex.Should().BeGreaterThan(leashCheckIndex);
        startBody.Should().Contain("return ShouldLeashCombatTarget(creature, target, homeLocation);");
        startBody.Should().NotContain("HasCombatLeashGraceExpired");
        startBody.Should().NotContain("ClearCombatLeashCandidate");
        tryStartBody.Should().Contain("ShouldStartCombatLeashEvade(creature, target, homeLocation)");
        aiSource.Should().NotContain("CombatLeashGraceSeconds");
        aiSource.Should().NotContain("_combatLeashCandidateTimes");
        aiSource.Should().NotContain("HasCombatLeashGraceExpired");
        aiSource.Should().NotContain("ClearCombatLeashCandidate");
    }

    [Test]
    public void CombatLeash_ProximityClearStartsFastReturnHome()
    {
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs").Replace("\r\n", "\n");
        var returnBody = aiSource.Substring(
            aiSource.IndexOf("private static bool TryReturnHomeAfterCombat", StringComparison.Ordinal),
            aiSource.IndexOf("private static uint GetHighestOrEventTarget", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool TryReturnHomeAfterCombat", StringComparison.Ordinal));

        returnBody.Should().Contain("GetAIFlag(enemy).HasFlag(AIFlag.ReturnHome)");
        returnBody.Should().Contain("IsOutsideHomeRadius(enemy, homeLocation)");
        returnBody.Should().Contain("StartLeashEvade(enemy, homeLocation)");
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
        processFlagsBody.Should().Contain("TryEndLeashEvadeAtHome(self, homeLocation)");
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
    public void CombatLeash_EvadeUsesPlotProtectionAndEndsWhenReturnedHome()
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
            aiSource.IndexOf("private static bool TryEndLeashEvadeAtHome", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void ContinueLeashEvadeReturn", StringComparison.Ordinal));
        var tryEndBody = aiSource.Substring(
            aiSource.IndexOf("private static bool TryEndLeashEvadeAtHome", StringComparison.Ordinal),
            aiSource.IndexOf("private static void CompleteLeashEvadeReturn", StringComparison.Ordinal) -
            aiSource.IndexOf("private static bool TryEndLeashEvadeAtHome", StringComparison.Ordinal));
        var completeBody = aiSource.Substring(
            aiSource.IndexOf("private static void CompleteLeashEvadeReturn", StringComparison.Ordinal),
            aiSource.IndexOf("private static void EndLeashEvade", StringComparison.Ordinal) -
            aiSource.IndexOf("private static void CompleteLeashEvadeReturn", StringComparison.Ordinal));
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
        continueBody.Should().Contain("GetLocalBool(creature, LeashEvadeReturnQueuedVariable)");
        continueBody.Should().Contain("GetCurrentAction(creature) == ActionType.MoveToPoint");
        continueBody.Should().Contain("DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable)");
        continueBody.Should().Contain("SetLocalBool(creature, LeashEvadeReturnQueuedVariable, true)");
        continueBody.Should().Contain("ClearAllActions(true)");
        continueBody.Should().Contain("ActionForceMoveToLocation(homeLocation, true, 60f)");
        continueBody.Should().Contain("ActionDoCommand(() => CompleteLeashEvadeReturn(creature, homeLocation))");
        var queuedCheckIndex = continueBody.IndexOf("GetLocalBool(creature, LeashEvadeReturnQueuedVariable)", StringComparison.Ordinal);
        var moveGuardIndex = continueBody.IndexOf("GetCurrentAction(creature) == ActionType.MoveToPoint", StringComparison.Ordinal);
        var queuedDeleteIndex = continueBody.IndexOf("DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable)", StringComparison.Ordinal);
        var queuedSetIndex = continueBody.IndexOf("SetLocalBool(creature, LeashEvadeReturnQueuedVariable, true)", StringComparison.Ordinal);
        var clearActionsIndex = continueBody.IndexOf("ClearAllActions(true)", StringComparison.Ordinal);

        moveGuardIndex.Should().BeGreaterThan(queuedCheckIndex);
        queuedDeleteIndex.Should().BeGreaterThan(moveGuardIndex);
        queuedSetIndex.Should().BeGreaterThan(queuedDeleteIndex);
        clearActionsIndex.Should().BeGreaterThan(queuedSetIndex);
        tryEndBody.Should().Contain("IsOutsideHomeRadius(creature, homeLocation)");
        tryEndBody.Should().Contain("EndLeashEvade(creature)");
        completeBody.Should().Contain("DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable)");
        completeBody.Should().Contain("IsOutsideHomeRadius(creature, homeLocation)");
        completeBody.Should().Contain("ActionJumpToLocation(homeLocation)");
        completeBody.Should().Contain("ActionDoCommand(() => EndLeashEvade(creature))");
        endBody.Should().Contain("SetCurrentHitPoints(creature, GetMaxHitPoints(creature))");
        endBody.Should().Contain("SetPlotFlag(creature, GetLocalBool(creature, LeashEvadeRestorePlotFlagVariable))");
        endBody.Should().Contain("DeleteLocalBool(creature, LeashEvadeRestorePlotFlagVariable)");
        endBody.Should().Contain("DeleteLocalBool(creature, LeashEvadeActiveVariable)");
        endBody.Should().Contain("DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable)");
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
        modifyBody.Should().Contain("if (AI.TryStartCombatLeashEvade(enemy, creature))");
        modifyBody.IndexOf("if (AI.TryStartCombatLeashEvade(enemy, creature))", StringComparison.Ordinal)
            .Should()
            .BeLessThan(modifyBody.IndexOf("var enemyList = _creatureToEnemies.ContainsKey(creature)", StringComparison.Ordinal));
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
            "sw_2da",
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

    private static object CreateBlockedLineMovementPlan(
        float attackerX,
        float attackerY,
        float attackerZ,
        float targetX,
        float targetY,
        float targetZ,
        uint attackerId,
        float desiredAttackRange,
        float personalSpaceRange,
        bool isOutsideAttackRange,
        bool hasRangedWeapon)
    {
        var method = typeof(OnAIActionAttackObject).GetMethod(
            "CreateBlockedLineMovementPlan",
            BindingFlags.Static | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        return method!.Invoke(null, new object[]
        {
            attackerX,
            attackerY,
            attackerZ,
            targetX,
            targetY,
            targetZ,
            attackerId,
            desiredAttackRange,
            personalSpaceRange,
            isOutsideAttackRange,
            hasRangedWeapon
        })!;
    }

    private static T ReadPlanProperty<T>(object plan, string name)
    {
        var property = plan.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        property.Should().NotBeNull();
        return (T)property!.GetValue(plan)!;
    }

    private static void AlternateRangedRepositionDirection(uint attackerId)
    {
        var method = typeof(OnAIActionAttackObject).GetMethod(
            "AlternateRangedRepositionDirection",
            BindingFlags.Static | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.Invoke(null, new object[] { attackerId });
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
