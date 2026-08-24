using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityDamageQueueTests
{
    [Test]
    public void TrackedAbilityImpact_AddsDefenseIgnoreToTheCurrentImpact()
    {
        var trackedImpactType = typeof(Ability).GetNestedType(
            "TrackedAbilityImpact",
            System.Reflection.BindingFlags.NonPublic);
        trackedImpactType.Should().NotBeNull();
        var constructor = trackedImpactType!.GetConstructors().Single();
        var trackedImpact = constructor.Invoke(new object[]
        {
            new AbilityDetail(),
            0,
            0,
            5,
            0,
            0,
            true,
            0
        });

        trackedImpactType.GetMethod("AddDefenseIgnorePercentAdjustment")!
            .Invoke(trackedImpact, new object[] { 25 });

        trackedImpactType.GetProperty("NextAbilityDefenseIgnorePercentAdjustment")!
            .GetValue(trackedImpact)
            .Should()
            .Be(30);
    }

    [Test]
    public void CompletedCastedAbilities_ApplyImpactBeforeDelayedAttackResume()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs")).Replace("\r\n", "\n");
        var completeBody = source.Substring(
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal),
            source.IndexOf("// Begin the main process", StringComparison.Ordinal) -
            source.IndexOf("void CompleteActivation", StringComparison.Ordinal));

        source.Should().Contain("private static void ResumeAttack(uint activator, uint target, bool clearActions = true)");
        source.Should().Contain("Enmity.IssueAttackCommand(activator, target, clearActions);");
        source.Should().Contain("private static void ResumeAttackAfterDelay(uint activator, uint target, float delay, bool clearActions = true)");
        source.Should().Contain("DelayCommand(delay, () =>");
        var impactIndex = completeBody.IndexOf("ExecuteAbilityImpact(activator, target, feat, ability, targetLocation)", StringComparison.Ordinal);
        var resumeIndex = completeBody.IndexOf("ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f)", StringComparison.Ordinal);

        impactIndex.Should().BeGreaterThanOrEqualTo(0);
        resumeIndex.Should().BeGreaterThanOrEqualTo(0);
        impactIndex.Should().BeLessThan(resumeIndex);
    }

    [Test]
    public void TrackedAbilityImpacts_FlushQueuedDamageEffectsTogether()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var endImpactBody = source.Substring(
            source.IndexOf("public static AbilityImpactSummary EndAbilityImpact", StringComparison.Ordinal),
            source.IndexOf("private static TrackedAbilityImpact GetTrackedAbilityImpact", StringComparison.Ordinal) -
            source.IndexOf("public static AbilityImpactSummary EndAbilityImpact", StringComparison.Ordinal));
        var queueBody = source.Substring(
            source.IndexOf("public void QueueDamageEffect", StringComparison.Ordinal),
            source.IndexOf("public void FlushDamageEffects", StringComparison.Ordinal) -
            source.IndexOf("public void QueueDamageEffect", StringComparison.Ordinal));
        var flushBody = source.Substring(
            source.IndexOf("public void FlushDamageEffects", StringComparison.Ordinal),
            source.IndexOf("private sealed class PendingDamageEffect", StringComparison.Ordinal) -
            source.IndexOf("public void FlushDamageEffects", StringComparison.Ordinal));

        endImpactBody.Should().Contain("impact.FlushDamageEffects(activator);");
        source.Should().Contain("trackedImpact.QueueDamageEffect(");
        queueBody.Should().Contain("_pendingDamageEffects.Add(new PendingDamageEffect(target, damage, damageType));");
        flushBody.Should().Contain("var effects = _pendingDamageEffects.ToArray();");
        flushBody.Should().Contain("AssignCommand(activator, () =>");
        flushBody.Should().Contain("foreach (var effect in effects)");
        flushBody.Should().Contain("EffectDamage(effect.Damage, effect.DamageType)");
    }

    [Test]
    public void FailedAbilityImpacts_AbortTrackedStateForCastAndQueuedPaths()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var usePerkFeatSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs")).Replace("\r\n", "\n");
        var castImpactBody = usePerkFeatSource.Substring(
            usePerkFeatSource.IndexOf("private static void ExecuteAbilityImpact(", StringComparison.Ordinal),
            usePerkFeatSource.IndexOf("/// Handles casting abilities.", StringComparison.Ordinal) -
            usePerkFeatSource.IndexOf("private static void ExecuteAbilityImpact(", StringComparison.Ordinal));
        var queuedImpactBody = usePerkFeatSource.Substring(
            usePerkFeatSource.IndexOf("public static void ProcessQueuedWeaponAbility()", StringComparison.Ordinal),
            usePerkFeatSource.IndexOf("/// Whenever a player enters the server", StringComparison.Ordinal) -
            usePerkFeatSource.IndexOf("public static void ProcessQueuedWeaponAbility()", StringComparison.Ordinal));
        var abortImpactBody = abilitySource.Substring(
            abilitySource.IndexOf("public static void AbortAbilityImpact(uint activator)", StringComparison.Ordinal),
            abilitySource.IndexOf("public static bool TryQueueTrackedDamageEffect", StringComparison.Ordinal) -
            abilitySource.IndexOf("public static void AbortAbilityImpact(uint activator)", StringComparison.Ordinal));

        abortImpactBody.Should().Contain("_trackedAbilityImpacts.Remove(activator);");
        abortImpactBody.Should().Contain("Log.WriteStructured(");
        abortImpactBody.Should().Contain("LogGroup.Error");
        castImpactBody.Should().Contain("var impactEnded = false;");
        castImpactBody.Should().Contain("impactEnded = true;");
        castImpactBody.Should().Contain("if (!impactEnded)");
        castImpactBody.Should().Contain("Ability.AbortAbilityImpact(activator);");
        queuedImpactBody.Should().Contain("var impactEnded = false;");
        queuedImpactBody.Should().Contain("impactEnded = true;");
        queuedImpactBody.Should().Contain("if (!impactEnded)");
        queuedImpactBody.Should().Contain("Ability.AbortAbilityImpact(activator);");
    }

    [Test]
    public void TwinFangFlurryTriggeredDamageDuringTrackedAbilityImpact_QueuesWithAbilityDamageEffects()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var combatSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Combat.cs")).Replace("\r\n", "\n");
        var queueMethod = abilitySource.Substring(
            abilitySource.IndexOf("public static bool TryQueueTrackedDamageEffect", StringComparison.Ordinal),
            abilitySource.IndexOf("private static TrackedAbilityImpact GetTrackedAbilityImpact", StringComparison.Ordinal) -
            abilitySource.IndexOf("public static bool TryQueueTrackedDamageEffect", StringComparison.Ordinal));
        var triggeredDamage = combatSource.Substring(
            combatSource.IndexOf("public static int ApplyTriggeredDamage", StringComparison.Ordinal),
            combatSource.IndexOf("private static void ApplyGuardiansResolve", StringComparison.Ordinal) -
            combatSource.IndexOf("public static int ApplyTriggeredDamage", StringComparison.Ordinal));
        var damageRiders = combatSource.Substring(
            combatSource.IndexOf("private static void ApplyAbilityDamageRiders", StringComparison.Ordinal),
            combatSource.IndexOf("private static void ApplyRicochetDamage", StringComparison.Ordinal) -
            combatSource.IndexOf("private static void ApplyAbilityDamageRiders", StringComparison.Ordinal));

        queueMethod.Should().Contain("GetTrackedAbilityImpact(activator)");
        queueMethod.Should().Contain("trackedImpact.QueueDamageEffect(target, damage, damageType);");
        damageRiders.Should().Contain("StatType.KatarVenomCurrentSecondStrikeDamageBonus");
        damageRiders.Should().Contain("ApplyTriggeredDamage(activator, target, bonus, damageType);");
        triggeredDamage.Should().Contain("Ability.TryQueueTrackedDamageEffect(activator, target, damage, effectDamageType)");
        triggeredDamage.Should().Contain("AssignCommand(");
    }

    [Test]
    public void DelayedTelegraphedImpacts_PreserveQueuedDefenseIgnoreBonus()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var buildActionCall = source.Substring(
            source.IndexOf("var action = BuildTelegraphedCombatImpactAction(", StringComparison.Ordinal),
            source.IndexOf("switch (shape)", StringComparison.Ordinal) -
            source.IndexOf("var action = BuildTelegraphedCombatImpactAction(", StringComparison.Ordinal));
        var buildActionSignature = source.Substring(
            source.IndexOf("private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction", StringComparison.Ordinal),
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal) -
            source.IndexOf("private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction", StringComparison.Ordinal));
        var delayedImpactBody = source.Substring(
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal),
            source.IndexOf("private static int ApplyCombatImpactToCreatures", StringComparison.Ordinal) -
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal));

        buildActionCall.Should().Contain("trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0");
        buildActionSignature.Should().Contain("int nextAbilityDefenseIgnorePercentAdjustment");
        delayedImpactBody.Should().Contain("nextAbilityDefenseIgnorePercentAdjustment");
        delayedImpactBody.Should().Contain("BeginAbilityImpact(");
        delayedImpactBody.Should().Contain("nextAbilityDamageBonus");
        delayedImpactBody.Should().Contain("nextAbilityCriticalRatePercentAdjustment");
    }

    [Test]
    public void DelayedTelegraphedImpacts_DoNotCountTheOriginatingCastTwice()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var delayedImpactBody = source.Substring(
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal),
            source.IndexOf("private static int ApplyCombatImpactToCreatures", StringComparison.Ordinal) -
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal));

        delayedImpactBody.Should().Contain("countsAsAttackAttempt: false",
            "the originating cast already spends its one limited-speed charge before the telegraph resolves");
    }

    [Test]
    public void DelayedTelegraphedImpacts_RetainCostContextAndCleanUpEveryExitPath()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var combatSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Combat.cs")).Replace("\r\n", "\n");
        var telegraphMethod = abilitySource.Substring(
            abilitySource.IndexOf("public static int ApplyTelegraphedCombatImpact(", StringComparison.Ordinal),
            abilitySource.IndexOf("private static void ShowAreaImpactFlash(", StringComparison.Ordinal) -
            abilitySource.IndexOf("public static int ApplyTelegraphedCombatImpact(", StringComparison.Ordinal));
        var delayedImpactBody = abilitySource.Substring(
            abilitySource.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal),
            abilitySource.IndexOf("private static int ApplyCombatImpactToCreatures", StringComparison.Ordinal) -
            abilitySource.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal));

        telegraphMethod.Should().Contain("Combat.DeferAbilityStaminaCostContext(activator, trackedImpact?.Ability);");
        delayedImpactBody.Should().Contain("var impactStarted = false;");
        delayedImpactBody.Should().Contain("var impactEnded = false;");
        delayedImpactBody.Should().Contain("finally");
        delayedImpactBody.Should().Contain("if (impactStarted && !impactEnded)");
        delayedImpactBody.Should().Contain("AbortAbilityImpact(creator);");
        delayedImpactBody.Should().Contain("Combat.CompleteDeferredAbilityStaminaCostContext(creator, ability);");

        combatSource.Should().Contain("state.DeferredImpactCount++;");
        combatSource.Should().Contain("state.DeferredImpactCount = Math.Max(0, state.DeferredImpactCount - 1);");
    }

    [Test]
    public void DelayedTelegraphedImpacts_CaptureActivationIdleSnapshotsBeforeSharedCleanup()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var weaponAbilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "WeaponActiveAbilityDefinitionBase.cs")).Replace("\r\n", "\n");
        var telegraphedAreaBranch = weaponAbilitySource.Substring(
            weaponAbilitySource.IndexOf("if (isArea && ShouldUseTelegraphedCombatImpact", StringComparison.Ordinal),
            weaponAbilitySource.IndexOf("var totalDamage = 0;", StringComparison.Ordinal) -
            weaponAbilitySource.IndexOf("if (isArea && ShouldUseTelegraphedCombatImpact", StringComparison.Ordinal));

        telegraphedAreaBranch.Should().Contain("profile.GetBaseDamageAdjustment(\n                                    activator,\n                                    impactedTarget,\n                                    activationIdleBonusSnapshot)",
            "delayed damage must read the activation's immutable snapshot rather than shared creature state");
        telegraphedAreaBranch.Should().Contain("finally\n                        {\n                            profile.ClearActivationIdleBonusSnapshots(activator);\n                        }",
            "shared snapshots must be cleared even if telegraph creation fails");
        telegraphedAreaBranch.Should().Contain("clearActivationIdleBonusSnapshots: false",
            "the post-impact hook must not repeat the cleanup performed by the guarded scheduling block");
        abilitySource.Should().NotContain("afterDeferredCompletion",
            "an older telegraph completion must never clear a newer activation's shared snapshots");

        var hostileImpactSetup = weaponAbilitySource.Substring(
            weaponAbilitySource.IndexOf("profile.SpendHitPoints(activator);", StringComparison.Ordinal),
            weaponAbilitySource.IndexOf("if (isArea && ShouldUseTelegraphedCombatImpact", StringComparison.Ordinal) -
            weaponAbilitySource.IndexOf("profile.SpendHitPoints(activator);", StringComparison.Ordinal));
        hostileImpactSetup.Should().Contain("var activationIdleBonusSnapshot = profile.CaptureActivationIdleBonusSnapshot(activator);");
        hostileImpactSetup.Should().Contain("Ability.AddActiveAbilityDefenseIgnorePercentAdjustment(");
        hostileImpactSetup.Should().Contain("profile.GetDefenseIgnorePercent(activator, activationIdleBonusSnapshot)",
            "the activation's defense-ignore snapshot must join the current tracked impact before instant or delayed damage resolves");
        weaponAbilitySource.Should().NotContain("Combat.GrantNextSkillAbilityBonuses(activator, skillType, 0, 0, 1, defenseIgnore)",
            "granting a next-ability rider after BeginAbilityImpact would leak defense ignore to a later ability");
        abilitySource.Should().Contain("NextAbilityDefenseIgnorePercentAdjustment += adjustment;");
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
