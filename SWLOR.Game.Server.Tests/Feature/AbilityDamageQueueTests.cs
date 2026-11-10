using SWLOR.Game.Server.Tests.Support;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityDamageQueueTests
{
    [Test]
    public void DelayedImpacts_ShareLazyCastStateAcrossSeparatePhases()
    {
        var impactType = AbilityInternals.TrackedImpactType;
        var service = AbilityInternals.NoNeighbours();
        object CreateImpact() => AbilityInternals.NewTrackedImpact(
            service, new AbilityDetail(), 0, 0, 0, 0, 0, true, 0, Array.Empty<TelegraphGeometry>(), null);
        var origin = CreateImpact();
        var firstPhase = CreateImpact();
        var secondPhase = CreateImpact();
        var ownerProperty = impactType.GetProperty("SequenceOwner")!;
        ownerProperty.SetValue(firstPhase, origin);
        ownerProperty.SetValue(secondPhase, origin);
        var storedSequence = impactType.GetField(
            "_sequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        foreach (var impact in new[] { origin, firstPhase, secondPhase })
            storedSequence.GetValue(impact).Should().BeNull("capturing or starting a delayed phase must not realize cast state");

        var sequenceProperty = impactType.GetProperty("Sequence")!;
        var firstSequence = (AbilityImpactSequence)sequenceProperty.GetValue(firstPhase)!;
        firstSequence.TryTriggerAreaPulse().Should().BeTrue();
        var secondSequence = (AbilityImpactSequence)sequenceProperty.GetValue(secondPhase)!;
        secondSequence.Should().BeSameAs(firstSequence);
        sequenceProperty.GetValue(origin).Should().BeSameAs(firstSequence);
        secondSequence.TryTriggerAreaPulse().Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TrackedAbilityImpact_CreatesASequenceOnDemandAndPreservesSharedSequences(bool hasSharedSequence)
    {
        var trackedImpactType = AbilityInternals.TrackedImpactType;
        var sharedSequence = hasSharedSequence ? new AbilityImpactSequence() : null;
        var trackedImpact = AbilityInternals.NewTrackedImpact(
            AbilityInternals.NoNeighbours(), new AbilityDetail(), 0, 0, 0, 0, 0, true, 0,
            Array.Empty<TelegraphGeometry>(), sharedSequence);
        var storedSequence = trackedImpactType.GetField(
            "_sequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        storedSequence.GetValue(trackedImpact).Should().BeSameAs(sharedSequence);

        var sequenceProperty = trackedImpactType.GetProperty("Sequence")!;
        var sequence = (AbilityImpactSequence)sequenceProperty.GetValue(trackedImpact)!;
        sequence.Should().NotBeNull();
        sequenceProperty.GetValue(trackedImpact).Should().BeSameAs(sequence);
        if (hasSharedSequence)
            sequence.Should().BeSameAs(sharedSequence);
        sequence.TryTriggerAreaPulse().Should().BeTrue();
        ((AbilityImpactSequence)sequenceProperty.GetValue(trackedImpact)!).TryTriggerAreaPulse().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that an impact initialized without activation markers still accumulates defense ignore.
    /// </summary>
    [Test]
    public void TrackedAbilityImpact_AddsDefenseIgnoreToTheCurrentImpact()
    {
        var trackedImpactType = AbilityInternals.TrackedImpactType;
        trackedImpactType.Should().NotBeNull();
        var trackedImpact = AbilityInternals.NewTrackedImpact(
            AbilityInternals.NoNeighbours(),
            new AbilityDetail(),
            0,
            0,
            5,
            0,
            0,
            true,
            0,
            Array.Empty<TelegraphGeometry>(),
            new AbilityImpactSequence());

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
        var impactIndex = completeBody.IndexOf("ExecuteAbilityImpact(", StringComparison.Ordinal);
        var resumeIndex = completeBody.IndexOf("ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f)", StringComparison.Ordinal);

        impactIndex.Should().BeGreaterThanOrEqualTo(0);
        resumeIndex.Should().BeGreaterThanOrEqualTo(0);
        impactIndex.Should().BeLessThan(resumeIndex);
    }

    [Test]
    public void ChoreographedImpacts_ResolveBeforeBusyStateAndAttackResumeAreReleased()
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
        var delayedBranch = completeBody.Substring(
            completeBody.IndexOf("if (ability.ImpactDelay > 0f)", StringComparison.Ordinal),
            completeBody.IndexOf("else\n                {", StringComparison.Ordinal) -
            completeBody.IndexOf("if (ability.ImpactDelay > 0f)", StringComparison.Ordinal));

        delayedBranch.Should().Contain("Activity.SetBusy(activator, ActivityStatusType.AbilityActivation);");
        delayedBranch.Should().Contain("DelayCommand(ability.ImpactDelay, () =>");
        delayedBranch.Should().Contain("pendingActivation.ActivationId != activationId");
        delayedBranch.Should().Contain("GetLocalInt(activator, activationId) != (int)ActivationStatus.Started");
        delayedBranch.IndexOf("IsDelayedImpactTargetValid(activator, target, targetLocation, ability)", StringComparison.Ordinal)
            .Should().BeLessThan(delayedBranch.IndexOf("ResolveImpact();", StringComparison.Ordinal));
        delayedBranch.IndexOf("ResolveImpact();", StringComparison.Ordinal)
            .Should().BeLessThan(delayedBranch.IndexOf("Activity.ClearBusy(activator);", StringComparison.Ordinal));

        File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Domain", "Abilities", "AbilityActiveActivation.cs"))
            .Should().Contain("public bool IsAwaitingImpact { get; set; }");
        source.Should().Contain("if (activation.IsAwaitingImpact)\n                GameServices.CombatResources.CompleteAbilityStaminaCostContext(activator, activation.Ability);");

        var delayedTargetValidation = source.Substring(
            source.IndexOf("private static bool IsDelayedImpactTargetValid", StringComparison.Ordinal),
            source.IndexOf("private static void ResumeAttackAfterDelay", StringComparison.Ordinal) -
            source.IndexOf("private static bool IsDelayedImpactTargetValid", StringComparison.Ordinal));
        delayedTargetValidation.Should().Contain("!LineOfSightObject(activator, target)");
        delayedTargetValidation.Should().Contain("!LineOfSightVector(GetPosition(activator), GetPosition(target))");
        delayedTargetValidation.Should().Contain("GetDistanceBetween(activator, target) > ability.MaxRange");
        delayedTargetValidation.Should().Contain("!GetIsReactionTypeHostile(target, activator)");

        var resolveImpact = completeBody.Substring(
            completeBody.IndexOf("void ResolveImpact()", StringComparison.Ordinal),
            completeBody.IndexOf("if (ability.ImpactDelay > 0f)", StringComparison.Ordinal) -
            completeBody.IndexOf("void ResolveImpact()", StringComparison.Ordinal));
        var executeImpactIndex = resolveImpact.IndexOf("ExecuteAbilityImpact(", StringComparison.Ordinal);
        var delayedResumeIndex = resolveImpact.IndexOf(
            "ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f)",
            StringComparison.Ordinal);
        executeImpactIndex.Should().BeGreaterThanOrEqualTo(0);
        delayedResumeIndex.Should().BeGreaterThanOrEqualTo(0);
        executeImpactIndex.Should().BeLessThan(delayedResumeIndex);
    }

    [Test]
    public void TrackedAbilityImpacts_FlushQueuedDamageEffectsTogether()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
        var endImpactStart = source.IndexOf(
            "public AbilityImpactSummary EndAbilityImpact",
            StringComparison.Ordinal);
        var trackedImpactLookupStart = source.IndexOf(
            "private TrackedAbilityImpact GetTrackedAbilityImpact",
            StringComparison.Ordinal);
        var queueDamageStart = source.IndexOf(
            "public void QueueDamageEffect",
            StringComparison.Ordinal);
        var flushDamageStart = source.IndexOf(
            "public void FlushDamageEffects",
            StringComparison.Ordinal);
        var pendingDamageEffectStart = source.IndexOf(
            "private sealed class PendingDamageEffect",
            StringComparison.Ordinal);
        endImpactStart.Should().BeGreaterThan(-1);
        trackedImpactLookupStart.Should().BeGreaterThan(endImpactStart);
        queueDamageStart.Should().BeGreaterThan(-1);
        flushDamageStart.Should().BeGreaterThan(queueDamageStart);
        pendingDamageEffectStart.Should().BeGreaterThan(flushDamageStart);
        var endImpactBody = source.Substring(
            endImpactStart,
            trackedImpactLookupStart - endImpactStart);
        var queueBody = source.Substring(
            queueDamageStart,
            flushDamageStart - queueDamageStart);
        var flushBody = source.Substring(
            flushDamageStart,
            pendingDamageEffectStart - flushDamageStart);

        endImpactBody.Should().Contain("impact.FlushDamageEffects(activator);");
        source.Should().Contain("trackedImpact.QueueDamageEffect(");
        source.Should().Contain("trackedImpact.QueueDirectDamageEffect(");
        queueBody.Should().Contain("public void QueueDirectDamageEffect(");
        queueBody.Should().Contain("QueueDamageEffect(target, damage, damageType, combatDamageType);");
        queueBody.Should().Contain("_pendingDamageEffects.Add(new PendingDamageEffect(");
        flushBody.Should().Contain("var effects = _pendingDamageEffects.ToArray();");
        flushBody.Should().Contain("AssignCommand(activator, () =>");
        flushBody.Should().Contain("foreach (var effect in effects)");
        flushBody.Should().Contain("EffectDamage(effect.Damage, effect.DamageType)");

        var preDamageValidation = flushBody.IndexOf(
            "_statusEffect.NotifyPreDamageStatusEffects(",
            StringComparison.Ordinal);
        var damageApplication = flushBody.IndexOf(
            "EffectDamage(effect.Damage, effect.DamageType)",
            StringComparison.Ordinal);
        var reflection = flushBody.IndexOf(
            "_service.ApplyDamageReflectionEffects(",
            StringComparison.Ordinal);
        preDamageValidation.Should().BeGreaterThan(-1);
        preDamageValidation.Should().BeLessThan(
            reflection,
            "each queued hit must validate source-dependent defenses before calculating reflection");
        reflection.Should().BeLessThan(
            damageApplication,
            "the ward-consuming hit must reflect before its damage is applied, while the next loop iteration revalidates the consumed pool");
    }

    [Test]
    public void MultiHitWeaponAbilities_ResolveConditionalReflectionPerQueuedHit()
    {
        var root = FindRepositoryRoot();
        var weaponAbilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "WeaponActiveAbilityDefinitionBase.cs")).Replace("\r\n", "\n");
        var configureMultiHitStart = weaponAbilitySource.IndexOf(
            "protected static void ConfigureMultiHit(",
            StringComparison.Ordinal);
        var configureInterruptStart = weaponAbilitySource.IndexOf(
            "protected static void ConfigureInterrupt(",
            StringComparison.Ordinal);
        configureMultiHitStart.Should().BeGreaterThan(-1);
        configureInterruptStart.Should().BeGreaterThan(configureMultiHitStart);
        var configureMultiHit = weaponAbilitySource.Substring(
            configureMultiHitStart,
            configureInterruptStart - configureMultiHitStart);

        configureMultiHit.Should().Contain("for (var i = 0; i < hits; i++)");
        configureMultiHit.Should().Contain("Ability.ApplyCombatImpact(",
            "every hit is queued independently against the same target");

        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
        var publicImpactStart = abilitySource.IndexOf(
            "public int ApplyHostileCombatImpact(",
            StringComparison.Ordinal);
        var privateImpactStart = abilitySource.IndexOf(
            "private int ApplyHostileCombatImpact(",
            StringComparison.Ordinal);
        publicImpactStart.Should().BeGreaterThan(-1);
        privateImpactStart.Should().BeGreaterThan(publicImpactStart);
        var primaryImpact = abilitySource.Substring(
            publicImpactStart,
            privateImpactStart - publicImpactStart);

        primaryImpact.Should().Contain("trackedImpact.QueueDirectDamageEffect(");
        primaryImpact.Should().Contain("if (trackedImpact == null)\n                    ApplyDamageReflectionEffects(",
            "tracked hits must not reflect until each queued damage effect is applied");
    }

    [Test]
    public void UnscaledCombatImpacts_RetainPrimaryDamageHandlingWithoutFormulaScaling()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
        var primaryImpactBody = source.Substring(
            source.IndexOf("public int ApplyHostileCombatImpact(", StringComparison.Ordinal),
            source.IndexOf("private int ApplyHostileCombatImpact(", StringComparison.Ordinal) -
            source.IndexOf("public int ApplyHostileCombatImpact(", StringComparison.Ordinal));
        var formulaSelectionBody = source.Substring(
            source.IndexOf("private int ApplyHostileCombatImpact(", StringComparison.Ordinal),
            source.IndexOf("private bool ShouldResolveCombatImpactHit", StringComparison.Ordinal) -
            source.IndexOf("private int ApplyHostileCombatImpact(", StringComparison.Ordinal));
        var unscaledDamageBody = source.Substring(
            source.IndexOf("private int CalculateUnscaledCombatImpactDamage(", StringComparison.Ordinal),
            source.IndexOf("private int CalculateCombatImpactDamage(", StringComparison.Ordinal) -
            source.IndexOf("private int CalculateUnscaledCombatImpactDamage(", StringComparison.Ordinal));

        source.Should().Contain("useUnscaledDamage: useUnscaledDamage",
            "the public combat-impact entry points must propagate unscaled damage through every area path");
        formulaSelectionBody.Should().Contain("useUnscaledDamage\n                ? CalculateUnscaledCombatImpactDamage(");
        formulaSelectionBody.Should().Contain("return ApplyHostileCombatImpact(",
            "unscaled damage must retain the primary ability damage, rider, and enmity path");
        primaryImpactBody.Should().Contain("SendTemporaryHitPointDamageFeedback(activator, target, damage);");
        primaryImpactBody.Should().Contain("trackedImpact.QueueDirectDamageEffect(");

        unscaledDamageBody.Should().Contain("var trackedImpact = GetTrackedAbilityImpact(activator);");
        unscaledDamageBody.Should().Contain("baseDamage + (trackedImpact?.NextAbilityDamageBonus ?? 0)",
            "queued flat next-ability damage must not be consumed without affecting an unscaled hit");
        unscaledDamageBody.Should().Contain("ApplyDamageDealtModifiers(");
        unscaledDamageBody.Should().Contain("isAbilityDamage: true");
        unscaledDamageBody.Should().Contain("ApplyCombatReadinessToActivatedAbilityMagnitude(activator, damage)");
        unscaledDamageBody.Should().Contain("_resistance.ApplyResistanceToDamage(target, damageType, damage)");
        unscaledDamageBody.Should().Contain("ApplyDamageTakenModifiers(");
        unscaledDamageBody.Should().NotContain("CalculateDamageWithCriticalMitigation");
        unscaledDamageBody.Should().NotContain("ApplyForceAffinityMagnitude");
        unscaledDamageBody.Should().NotContain("GetAbilityDamageBonus");
    }

    [Test]
    public void FailedAbilityImpacts_AbortTrackedStateForCastAndQueuedPaths()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
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
            abilitySource.IndexOf("public void AbortAbilityImpact(uint activator)", StringComparison.Ordinal),
            abilitySource.IndexOf("public bool TryQueueTrackedDamageEffect", StringComparison.Ordinal) -
            abilitySource.IndexOf("public void AbortAbilityImpact(uint activator)", StringComparison.Ordinal));

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
    public void QueuedWeaponAbilities_AreRevalidatedBeforeAttackAndImpact()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs")).Replace("\r\n", "\n");
        var queuedLookupBody = source.Substring(
            source.IndexOf("public static bool TryGetQueuedWeaponAbility(uint activator, out AbilityDetail ability)", StringComparison.Ordinal),
            source.IndexOf("private static List<string> DisplayActivationTargetingTelegraphs", StringComparison.Ordinal) -
            source.IndexOf("public static bool TryGetQueuedWeaponAbility(uint activator, out AbilityDetail ability)", StringComparison.Ordinal));
        var queuedImpactBody = source.Substring(
            source.IndexOf("public static void ProcessQueuedWeaponAbility()", StringComparison.Ordinal),
            source.IndexOf("/// Whenever a player enters the server", StringComparison.Ordinal) -
            source.IndexOf("public static void ProcessQueuedWeaponAbility()", StringComparison.Ordinal));

        queuedLookupBody.Should().Contain("IsQueuedWeaponAbilityStillAvailable(activator, activeWeaponAbility, ability)");
        queuedLookupBody.Should().Contain("GetHasFeat(feat, activator)");
        queuedLookupBody.Should().Contain("Perk.GetPerkLevel(activator, ability.EffectiveLevelPerkType)");
        queuedLookupBody.Should().Contain("ability.AbilityLevel > effectivePerkLevel");
        queuedLookupBody.Should().Contain("Perk.ShouldEnforceActiveAbilityFeatReplacement(");
        queuedLookupBody.Should().Contain("Perk.IsCurrentActiveAbilityFeat(");
        queuedLookupBody.Should().Contain("ClearQueuedAbility(activator);");
        queuedImpactBody.Should().Contain("if (!TryGetQueuedWeaponAbility(activator, out var abilityDetail))");
        queuedImpactBody.IndexOf("TryGetQueuedWeaponAbility", StringComparison.Ordinal)
            .Should().BeLessThan(queuedImpactBody.IndexOf("Ability.BeginAbilityImpact", StringComparison.Ordinal));
    }

    [Test]
    public void TwinFangFlurryTriggeredDamageDuringTrackedAbilityImpact_QueuesWithAbilityDamageEffects()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
        var combatSource = CombatSource.Read().Replace("\r\n", "\n");
        var queueMethod = abilitySource.Substring(
            abilitySource.IndexOf("public bool TryQueueTrackedDamageEffect", StringComparison.Ordinal),
            abilitySource.IndexOf("private TrackedAbilityImpact GetTrackedAbilityImpact", StringComparison.Ordinal) -
            abilitySource.IndexOf("public bool TryQueueTrackedDamageEffect", StringComparison.Ordinal));
        var triggeredDamage = CombatSource.Members("ApplyTriggeredDamage", "ApplyTriggeredDamageTargetAdjustment", "GetRangedDeflectionReflectionAmount", "GetRangedDeflectionReflectionRates", "ApplyRangedDeflectionReflection", "BuildDeflectingReturnCombatLogMessage");
        var damageRiders = CombatSource.Members("ApplyAbilityDamageRiders", "ApplyFoggyMindResourceDrain", "GetAreaAbilityPulseSources", "ApplyAreaAbilityPulse", "ApplyAreaAbilityFragmentation", "GetAreaAbilityFragmentationStatAdjustment");

        queueMethod.Should().Contain("GetTrackedAbilityImpact(activator)");
        queueMethod.Should().Contain("trackedImpact.QueueDamageEffect(target, damage, damageType);");
        damageRiders.Should().Contain("StatType.KatarVenomCurrentSecondStrikeDamageBonus");
        damageRiders.Should().Contain("ApplyTriggeredDamage(activator, target, bonus, damageType);");
        triggeredDamage.Should().Contain("_ability.TryQueueTrackedDamageEffect(activator, target, damage, effectDamageType)");
        triggeredDamage.Should().Contain("AssignCommand(");
    }

    [Test]
    public void DelayedTelegraphedImpacts_PreserveQueuedDefenseIgnoreBonus()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
        var buildActionCall = source.Substring(
            source.IndexOf("var action = BuildTelegraphedCombatImpactAction(", StringComparison.Ordinal),
            source.IndexOf("switch (shape)", StringComparison.Ordinal) -
            source.IndexOf("var action = BuildTelegraphedCombatImpactAction(", StringComparison.Ordinal));
        var buildActionSignature = source.Substring(
            source.IndexOf("private ApplyTelegraphEffect BuildTelegraphedCombatImpactAction", StringComparison.Ordinal),
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal) -
            source.IndexOf("private ApplyTelegraphEffect BuildTelegraphedCombatImpactAction", StringComparison.Ordinal));
        var delayedImpactBody = source.Substring(
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal),
            source.IndexOf("private int ApplyCombatImpactToCreatures", StringComparison.Ordinal) -
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
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
        var delayedImpactBody = source.Substring(
            source.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal),
            source.IndexOf("private int ApplyCombatImpactToCreatures", StringComparison.Ordinal) -
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
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
        var combatSource = CombatSource.Read().Replace("\r\n", "\n");
        var telegraphMethod = abilitySource.Substring(
            abilitySource.IndexOf("public int ApplyTelegraphedCombatImpact(", StringComparison.Ordinal),
            abilitySource.IndexOf("private void ShowAreaImpactFlash(", StringComparison.Ordinal) -
            abilitySource.IndexOf("public int ApplyTelegraphedCombatImpact(", StringComparison.Ordinal));
        var delayedImpactBody = abilitySource.Substring(
            abilitySource.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal),
            abilitySource.IndexOf("private int ApplyCombatImpactToCreatures", StringComparison.Ordinal) -
            abilitySource.IndexOf("return (creator, creatures) =>", StringComparison.Ordinal));

        telegraphMethod.Should().Contain("DeferAbilityStaminaCostContext(activator, trackedImpact?.Ability);");
        delayedImpactBody.Should().Contain("var impactStarted = false;");
        delayedImpactBody.Should().Contain("var impactEnded = false;");
        delayedImpactBody.Should().Contain("finally");
        delayedImpactBody.Should().Contain("if (impactStarted && !impactEnded)");
        delayedImpactBody.Should().Contain("AbortAbilityImpact(creator);");
        delayedImpactBody.Should().Contain("CompleteDeferredAbilityStaminaCostContext(creator, ability);");

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
            "Domain",
            "Abilities",
            "AbilityService.cs")).Replace("\r\n", "\n");
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
        weaponAbilitySource.Should().NotContain("GameServices.CombatAttackTiming.GrantNextSkillAbilityBonuses(activator, skillType, 0, 0, 1, defenseIgnore)",
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
