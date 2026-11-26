#nullable enable
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The lines the cleanup transcripts keep for the steps of <see cref="CombatService"/>'s cleanup. Before the cleanup walked the
/// state owners it called one forget per step, and the committed transcripts name those forgets. A step that two owners share
/// is written by the first of them, and the other owners write nothing, so the transcripts stay as they were; the order of the
/// owners inside a shared step is pinned by <c>CombatStateOwnerTests</c>.
/// </summary>
public static class CombatForgetSteps
{
    private static readonly Dictionary<(string Label, CombatForgetPhase Phase), string> StepNames = new()
    {
        [("Resources", CombatForgetPhase.ResourceTriggers)] = "ForgetResourceTriggerState",
        [("Activity", CombatForgetPhase.Activity)] = "ForgetActivityState",
        [("DefenseMemory", CombatForgetPhase.DefenseMemory)] = "ForgetDefenseMemory",
        [("Suppression", CombatForgetPhase.Suppression)] = "ForgetSuppressionState",
        [("OffenseEffects", CombatForgetPhase.Activation)] = "ForgetActivationState",
        [("HitEffects", CombatForgetPhase.HitEffects)] = "ForgetHitEffectState",
        [("Impact", CombatForgetPhase.AbilityImpact)] = "ForgetImpactState",
        [("Activity", CombatForgetPhase.OpeningActivity)] = "ForgetOpeningActivityState",
        [("Resources", CombatForgetPhase.AbilityStaminaCost)] = "ForgetAbilityStaminaCostState",
        [("AttackTiming", CombatForgetPhase.AttackTiming)] = "ForgetAttackTimingState",
        [("RepeatedTarget", CombatForgetPhase.DamageModifiers)] = "ForgetDamageModifierState",
    };

    /// <summary>
    /// Describes a call of an owner: null for a member that is not <see cref="ICombatCreatureStateOwner.ForgetCreatureState"/>,
    /// the line of the step it was part of when the owner writes it, and an empty string when another owner writes the step.
    /// </summary>
    public static string? Describe(string label, string member, object?[] args, Func<object?, string> format) =>
        member != nameof(ICombatCreatureStateOwner.ForgetCreatureState)
            ? null
            : StepNames.TryGetValue((label, (CombatForgetPhase)args[1]!), out var step) ? $"{label}.{step}({format(args[0])})" : string.Empty;
}
