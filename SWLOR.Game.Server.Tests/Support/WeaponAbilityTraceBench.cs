#nullable enable
using System.Reflection;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The bench of the weapon ability call traces: the combat transcript bench with a recording stand-in at each of the services the weapon
/// ability base reaches, and an ability service stand-in that runs the callbacks a combat impact is handed for the creatures the scenario
/// names, so every closure the base builds runs and every call it makes reaches the one ordered transcript.
/// </summary>
public sealed class WeaponAbilityTraceBench : IDisposable
{
    /// <summary>The services the base reaches by lookup.</summary>
    public static readonly Type[] Leaves =
    {
        typeof(IAbilityService), typeof(IAbilityDamageInputService), typeof(IAbilityImmunityService), typeof(IAbilityImpactState),
        typeof(IAbilityStatusApplicationService), typeof(ICombatAbilityActivationService), typeof(ICombatAbilityBonusService),
        typeof(ICombatAbilityHitService), typeof(ICombatActivityService), typeof(ICombatDamageTakenService),
        typeof(ICombatDefenseMemoryService), typeof(ICombatQueriesService), typeof(ICombatRecorderService),
        typeof(ICombatResourcesService), typeof(ICombatSuppressionService), typeof(IEnmityService), typeof(IPartyService),
        typeof(IPlayerFeedbackService), typeof(IStatService), typeof(IStatusEffectService), typeof(IStatusEffectApplicationService),
        typeof(IStatusEffectCatalogService), typeof(IStatusEffectDurationService), typeof(ITemporaryStatModifierService)
    };

    private readonly CombatTranscriptBench _bench;
    private WeaponAbilityScenario _scenario = null!;
    private AbilityDetail _ability = null!;
    private int _answers;

    public WeaponAbilityTraceBench()
    {
        _bench = new CombatTranscriptBench(Leaves);
        foreach (var type in Leaves)
            _bench.Leaf(type).Fallback = AnswerCheck;

        _bench.Leaf<IAbilityService>()
            .Script(nameof(IAbilityService.ApplyCombatImpact), args => ApplyImpact(nameof(IAbilityService.ApplyCombatImpact), args, false))
            .Script(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args => ApplyImpact(nameof(IAbilityService.ApplyTelegraphedCombatImpact), args, true));
        // A status effect the code asks the catalog for is a real one, so the categories it reads are the effect's own.
        _bench.Leaf<IStatusEffectCatalogService>()
            .Script(nameof(IStatusEffectCatalogService.CreateStatusEffect), args => Activator.CreateInstance((Type)args[0]!));
        ConfigureEngine();
        _bench.Leaf<IAbilityImpactState>().Script(nameof(IActiveAbilityImpact.GetActiveAbilityImpactSummary), _ => _scenario.NoSummary ? null : Summary());
    }

    private const uint ObjectInvalid = 0x7F000000;

    /// <summary>The creatures an impact visits, one or three.</summary>
    private IReadOnlyList<uint> Targets =>
        _scenario.TargetGone ? new[] { Gone } : _scenario.Targets == 1 ? new[] { Defender } : new[] { Defender, Other, Ally };

    /// <summary>The transcript so far.</summary>
    public string Text => _bench.Text;

    /// <summary>The length of the transcript so far.</summary>
    public int Length => _bench.Length;

    /// <summary>The part of the transcript written since <paramref name="start"/>.</summary>
    public string TextFrom(int start) => _bench.TextFrom(start);

    /// <summary>Drives every closure of an ability once in a scenario, as one step of the transcript.</summary>
    public void Drive(string step, AbilityDetail ability, WeaponAbilityScenario scenario)
    {
        _scenario = scenario;
        _ability = ability;
        _answers = 0;
        _bench.DefaultStat = scenario.Stat;
        using var allies = ProtectedAlliesScope(scenario.ProtectedAllies);
        _bench.Step(step, () =>
        {
            var location = default(Location);
            Run("categories", () => ability.ResolveImpactStatusCategories?.Invoke(scenario.Caster));
            foreach (var target in ability.IsHostileAbility ? new[] { Defender } : scenario.FriendlySelf ? new[] { Ally, scenario.Caster } : new[] { Ally })
            {
                Run("validation of " + _bench.Name(target), () => ability.CustomValidation?.Invoke(_scenario.Caster, target, 3, location));
                Run("activation on " + _bench.Name(target), () => ability.ActivationAction?.Invoke(_scenario.Caster, target, 3, location));
                Run("impact on " + _bench.Name(target), () =>
                {
                    ability.ImpactAction?.Invoke(_scenario.Caster, target, 3, location);
                    return null;
                });
            }
        });
    }

    private void Run(string label, Func<object?> closure)
    {
        try
        {
            var result = closure();
            _bench.Lines.Add($"-> {label}: {(result == null ? "done" : _bench.Format(result))}");
        }
        catch (Exception exception) when (exception is not NUnit.Framework.AssertionException)
        {
            var frame = (exception.StackTrace ?? string.Empty).Split('\n')[0].Trim();
            var inFile = frame.IndexOf(" in ", StringComparison.Ordinal);
            _bench.Lines.Add($"-> {label}: threw {exception.GetType().Name} {(inFile < 0 ? frame : frame[..inFile])}");
        }
    }

    private (bool Handled, object? Value) AnswerCheck(MethodInfo method, object?[] arguments)
    {
        // A status effect the code reads from a creature is one the creature has when the scenario says its checks pass; a guarded one is the caster's.
        if (method.Name == nameof(IStatusEffectService.GetStatusEffect) && typeof(IStatusEffect).IsAssignableFrom(method.ReturnType))
        {
            var type = method.IsGenericMethod ? method.ReturnType : arguments.OfType<Type>().FirstOrDefault();
            if (type == null || type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null || !Next(method))
                return (true, null);

            var effect = (IStatusEffect)Activator.CreateInstance(type)!;
            effect.ReassignSource(Attacker);
            return (true, effect);
        }

        if (method.ReturnType == typeof(bool))
            return (true, Next(method));
        if (method.ReturnType == typeof(int))
            return (true, Next(method) ? 40 : 0);
        if (method.ReturnType == typeof(float))
            return (true, Next(method) ? 12f : 0f);
        if (method.ReturnType == typeof(DateTime))
            return (true, Next(method) ? new DateTime(2026, 11, 18, 11, 59, 0, DateTimeKind.Utc) : default(DateTime));
        return (false, null);
    }

    private bool Next(MethodInfo method)
    {
        switch (_scenario.Pattern)
        {
            case WeaponAbilityAnswerPattern.OnWithoutNamedStatuses:
                return !(method.Name == nameof(IStatusEffectService.HasStatusEffect) && !method.IsGenericMethod);
            case WeaponAbilityAnswerPattern.On:
                return true;
            case WeaponAbilityAnswerPattern.AlternateStartingTrue:
                return _answers++ % 2 == 0;
            case WeaponAbilityAnswerPattern.AlternateStartingFalse:
                return _answers++ % 2 == 1;
            case WeaponAbilityAnswerPattern.TwoTrueThenFalse:
                return _answers++ % 3 != 2;
            case WeaponAbilityAnswerPattern.EveryThird:
                return _answers++ % 3 == 0;
            case WeaponAbilityAnswerPattern.TwoOfThree:
                return _answers++ % 3 != 0;
            default:
                return false;
        }
    }

    private AbilityImpactSummary Summary() => new()
    {
        Ability = _ability,
        SkillType = _ability.SkillType,
        IsAreaAbility = _ability.IsAreaAbility,
        IsSingleTargetAbility = _ability.IsSingleTargetAbility,
        ImpactedTargetCount = Targets.Count,
        CriticalHitCount = 1,
        AttributedDamage = _scenario.HitDamage * Targets.Count,
        ImpactedTargets = Targets
    };

    // The combat impact is the seam the base hands its per-target callbacks to. The stand-in runs each callback for each creature the
    // scenario visits, in the order the real service does, writing what each answered, then answers with the damage the hits deal.
    private object ApplyImpact(string member, object?[] arguments, bool telegraphed)
    {
        var parameters = typeof(IAbilityService).GetMethod(member)!.GetParameters();
        T? Argument<T>(string name) => (T?)arguments[Array.FindIndex(parameters, parameter => parameter.Name == name)];

        var factory = Argument<Func<IStatusEffect>>("statusEffectFactory");
        if (factory != null)
            Run("status effect factory", () => factory().GetType());

        var damage = _scenario.HitDamage;
        var beforeImpact = Argument<Action<uint>>("beforeImpact");
        var baseAdjustment = Argument<Func<uint, int>>("baseDamageAdjustment");
        var percentAdjustment = Argument<Func<uint, int>>("damagePercentAdjustment");
        var riders = Argument<Func<uint, StatusEffectCategory?>>("beforeSuccessfulImpactRiders");
        var afterHit = Argument<Action<uint>>("afterSuccessfulHit");
        foreach (var target in Targets)
        {
            var name = _bench.Name(target);
            Run("beforeImpact " + name, () =>
            {
                beforeImpact?.Invoke(target);
                return null;
            });
            Run("baseDamageAdjustment " + name, () => baseAdjustment?.Invoke(target));
            Run("damagePercentAdjustment " + name, () => percentAdjustment?.Invoke(target));
            Run("beforeSuccessfulImpactRiders " + name, () => riders?.Invoke(target));
            if (damage > 0)
            {
                Run("afterSuccessfulHit " + name, () =>
                {
                    afterHit?.Invoke(target);
                    return null;
                });
            }
        }

        if (telegraphed)
        {
            var afterImpact = Argument<Action<AbilityImpactSummary>>("afterImpactAction");
            Run("afterImpactAction", () =>
            {
                afterImpact?.Invoke(Summary());
                return null;
            });
        }

        return damage * (telegraphed ? Targets.Count : 1);
    }

    // The engine answers what the targeting code asks: the attacker and the ally are one side, the defender and the other creature the opposite
    // side, the nearest creatures to a spot are the opposite side, and the creatures in a shape are the attacker and the ally.
    private void ConfigureEngine()
    {
        var engine = _bench.Engine;
        var opposite = new HashSet<uint> { Defender, Other };
        engine.SetReturnHandler("GetIsReactionTypeHostile", args => opposite.Contains((uint)args[0]!) != opposite.Contains((uint)args[1]!));
        engine.SetReturnHandler("GetAreaFromLocation", args => 900u);
        engine.SetReturnHandler("GetCurrentHitPoints", args => _scenario.HitPoints);
        engine.SetReturnHandler("GetNearestCreatureToLocation", args => (int)args[3]! switch { 1 => Other, 2 => Defender, _ => ObjectInvalid });
        var inShape = new[] { Attacker, Ally };
        var visited = 0;
        engine.SetReturnHandler("GetFirstObjectInShape", args =>
        {
            visited = 1;
            return inShape[0];
        });
        engine.SetReturnHandler("GetNextObjectInShape", args => visited < inShape.Length ? inShape[visited++] : ObjectInvalid);
    }

    // The statics the base reads hold what happened between creatures in dictionaries of their own; a scenario fills them for the caster and
    // empties them afterwards.
    private static IDisposable ProtectedAlliesScope(bool enabled)
    {
        if (!enabled)
            return new Restore(() => { });

        var now = DateTime.UtcNow;
        var ward = StaticField<Dictionary<(uint, uint), DateTime>>(typeof(WardBondStatusEffect), "RecentWardHits");
        var guarded = StaticField<Dictionary<(uint, uint), DateTime>>(typeof(GuardedStatusEffect), "RecentGuardedAllyHits");
        var guardedTargets = StaticField<Dictionary<uint, HashSet<uint>>>(typeof(GuardedStatusEffect), "GuardedTargetsBySource");
        ward[(Attacker, Defender)] = now;
        guarded[(Attacker, Defender)] = now;
        guardedTargets[Attacker] = new HashSet<uint> { Ally };
        return new Restore(() =>
        {
            ward.Remove((Attacker, Defender));
            guarded.Remove((Attacker, Defender));
            guardedTargets.Remove(Attacker);
        });
    }

    private static T StaticField<T>(Type owner, string name) =>
        (T)owner.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private sealed class Restore : IDisposable
    {
        private readonly Action _restore;

        public Restore(Action restore) => _restore = restore;

        public void Dispose() => _restore();
    }

    public void Dispose() => _bench.Dispose();
}
