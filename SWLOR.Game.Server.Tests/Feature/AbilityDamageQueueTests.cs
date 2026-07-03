using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityDamageQueueTests
{
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
    public void TwinFangFlurryTriggeredDamageDuringTrackedAbilityImpact_QueuesWithAbilityDamageEffects()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var combatSource = CombatSourceReader.Read(root).Replace("\r\n", "\n");
        var queueMethod = abilitySource.Substring(
            abilitySource.IndexOf("public static bool TryQueueTrackedDamageEffect", StringComparison.Ordinal),
            abilitySource.IndexOf("private static TrackedAbilityImpact GetTrackedAbilityImpact", StringComparison.Ordinal) -
            abilitySource.IndexOf("public static bool TryQueueTrackedDamageEffect", StringComparison.Ordinal));
        var triggeredDamage = ExtractBetween(
            combatSource,
            "public static int ApplyTriggeredDamage",
            "internal static void ApplyGuardiansResolve");
        var damageRiders = ExtractBetween(
            combatSource,
            "internal static void ApplyAbilityDamageRiders",
            "internal static void ApplyFoggyMindResourceDrain");

        queueMethod.Should().Contain("GetTrackedAbilityImpact(activator)");
        queueMethod.Should().Contain("trackedImpact.QueueDamageEffect(target, damage, damageType);");
        damageRiders.Should().Contain("StatType.KatarVenomCurrentSecondStrikeDamageBonus");
        damageRiders.Should().Contain("TriggeredCombatEffects.ApplyTriggeredDamage(activator, target, bonus, damageType);");
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

    private static string ExtractBetween(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        if (end < 0)
            return source.Substring(start);

        end.Should().BeGreaterThan(start);

        return source.Substring(start, end - start);
    }
}
