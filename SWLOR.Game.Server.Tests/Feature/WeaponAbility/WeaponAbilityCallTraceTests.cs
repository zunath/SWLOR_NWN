using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.WeaponAbilityAnswerPattern;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>
/// The ordered call traces of every weapon ability, taken before the weapon ability base is split: each ability's categories, validation,
/// activation and impact closures run against recording stand-ins of the services the base reaches, in each scenario of
/// <see cref="Scenarios"/>. A trace that an earlier ability already produced in the same scenario is written as "same as" that ability.
/// The identity of a profile's modifier source (a guid) is written as a token numbered by first appearance in the step.
/// Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityCallTraceTests
{
    private static readonly Regex Guid = new("ability:[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

    /// <summary>The states every ability is driven in.</summary>
    public static readonly WeaponAbilityScenario[] Scenarios =
    {
        new("quiet", Off, 1, 90, 0, true),
        new("ready", On, 3, 90, 40, true),
        new("ready, one target", On, 1, 90, 40),
        new("alternating from true", AlternateStartingTrue, 3, 90, 20),
        new("alternating from false", AlternateStartingFalse, 1, 90, 20),
        new("every third", EveryThird, 3, 90, 10),
        new("two of three", TwoOfThree, 1, 90, 10),
        new("no damage", On, 3, 0, 40),
        new("wounded", On, 1, 90, 40, HitPoints: 1),
        new("protected allies", On, 3, 90, 40, true, ProtectedAllies: true),
        new("open ground", OnWithoutNamedStatuses, 3, 90, 40),
        new("protected allies alternating", AlternateStartingTrue, 1, 90, 20, true, ProtectedAllies: true),
        new("protected allies every third", EveryThird, 1, 90, 20, true, ProtectedAllies: true),
        new("protected allies two then one", TwoTrueThenFalse, 1, 90, 20, true, ProtectedAllies: true),
        new("creature caster without a summary", On, 1, 90, 40, true, Caster: CombatTranscriptBench.Master, NoSummary: true),
        new("target gone", On, 1, 90, 40, TargetGone: true),
        new("no damage without a summary", On, 1, 0, 40, NoSummary: true)
    };

    [Test]
    public void EveryWeaponAbilityProducesTheCapturedCallTrace()
    {
        WeaponAbilityGolden.Assert("SWLOR.Game.Server.Tests/Fixtures/WeaponAbility/WeaponAbilityCallTrace.txt.gz", Trace());
    }

    /// <summary>The states every shipped ability is driven in.</summary>
    public static readonly WeaponAbilityScenario[] ShippedScenarios = { Scenarios[0], Scenarios[1], Scenarios[4] };

    /// <summary>The states a profile with one setting is driven in.</summary>
    public static readonly WeaponAbilityScenario[] SingleSettingScenarios = { Scenarios[0], Scenarios[1], Scenarios[3] };

    /// <summary>The shapes a profile with one setting is configured in.</summary>
    public static readonly WeaponAbilityProbeShape[] SingleSettingShapes =
    {
        WeaponAbilityProbeShape.HostileSingle, WeaponAbilityProbeShape.HostileTelegraphedArea,
        WeaponAbilityProbeShape.FriendlyTarget, WeaponAbilityProbeShape.SelfStatus
    };

    /// <summary>The call trace of every ability, for the coverage run and the golden alike.</summary>
    public static string Trace()
    {
        var text = new StringBuilder();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        using var bench = new WeaponAbilityTraceBench();

        void Drive(string label, AbilityDetail ability, IEnumerable<WeaponAbilityScenario> scenarios)
        {
            foreach (var scenario in scenarios)
            {
                var before = bench.Length;
                bench.Drive($"{label} {scenario.Name}", ability, scenario);
                var step = bench.TextFrom(before);
                var body = StableGuids(step[(step.IndexOf('\n') + 1)..]);
                text.Append("== ").Append(label).Append(": ").Append(scenario.Name).Append('\n');
                var key = scenario.Name + "\n" + body;
                if (seen.TryGetValue(key, out var first))
                {
                    text.Append("same as ").Append(first).Append('\n');
                    continue;
                }

                seen[key] = label;
                text.Append(body);
            }
        }

        foreach (var entry in WeaponAbilityCatalogReader.Build())
            Drive(entry.Feat.ToString(), entry.Detail, ShippedScenarios);

        void DriveProbe(string label, WeaponAbilityProbeShape shape, object profile, IEnumerable<WeaponAbilityScenario> scenarios)
        {
            AbilityDetail ability;
            try
            {
                ability = WeaponAbilityProbe.Build(shape, profile);
            }
            catch (InvalidOperationException exception)
            {
                // The base refuses a guarded-hit status beside a status of the ability's own.
                text.Append("== ").Append(label).Append(": refused: ").Append(exception.Message).Append('\n');
                return;
            }

            Drive(label, ability, scenarios);
        }

        // Some settings keep a profile from running as a whole: a guarded-hit status is refused beside an impact status, clearing the target's
        // actions reaches the engine's native library (the call stops there), and a queued ability skips the idle critical rate. Each such
        // setting is left out of the profile with every setting, which runs once without them and once as a queued ability.
        var native = new[] { "RecentGuardedHitStatusEffect", "ClearTargetActionsOnHit" };
        foreach (var shape in Enum.GetValues<WeaponAbilityProbeShape>())
        {
            DriveProbe($"probe default {shape}", shape, WeaponAbilityProfileVariants.Default(), Scenarios);
            DriveProbe($"probe every setting {shape}", shape, WeaponAbilityProfileVariants.Maximal(native.Append("IsQueuedWeaponAbility")), Scenarios);
            DriveProbe($"probe every setting queued {shape}", shape, WeaponAbilityProfileVariants.Maximal(native), Scenarios);
        }

        DriveProbe("probe every setting but the guarded ally HostileSingle", WeaponAbilityProbeShape.HostileSingle,
            WeaponAbilityProfileVariants.Maximal(native.Concat(new[] { "RequireRecentGuardedAllyHitForConditionalStatus" })), Scenarios);
        DriveProbe("probe every setting but the ward HostileSingle", WeaponAbilityProbeShape.HostileSingle,
            WeaponAbilityProfileVariants.Maximal(native.Concat(new[] { "RequireRecentWardHitForConditionalStatus" })), Scenarios);
        foreach (var shape in new[] { WeaponAbilityProbeShape.HostileSingle, WeaponAbilityProbeShape.FriendlyTarget, WeaponAbilityProbeShape.SelfStatus })
        {
            DriveProbe($"probe every setting but the self status factory {shape}", shape,
                WeaponAbilityProfileVariants.Maximal(native.Concat(new[] { "IsQueuedWeaponAbility", "SelfStatusEffectFactory" })), Scenarios);
        }

        DriveProbe("probe every setting but the permanent, hasted and maximum hit points settings HostileSingle", WeaponAbilityProbeShape.HostileSingle,
            WeaponAbilityProfileVariants.Maximal(native.Concat(new[]
            {
                "IsQueuedWeaponAbility", "SelfStatusEffectOnCriticalHitIsPermanent", "SelfHasteMaximumPercent", "SelfHasteMaximumPercentIfAllHitsLand",
                "FriendlyTargetTemporaryHPUsesActivatorMaximum"
            })), Scenarios);
        DriveProbe("probe every setting but the maximum hit points setting FriendlyTarget", WeaponAbilityProbeShape.FriendlyTarget,
            WeaponAbilityProfileVariants.Maximal(native.Concat(new[] { "IsQueuedWeaponAbility", "FriendlyTargetTemporaryHPUsesActivatorMaximum" })), Scenarios);
        DriveProbe("probe every setting with an empty extended status HostileSingle", WeaponAbilityProbeShape.HostileSingle,
            WeaponAbilityProfileVariants.Maximal(native, new Dictionary<string, object?>
            {
                ["SourceStatusEffectsToExtend"] = new Type?[] { null, typeof(BleedStatusEffect) }
            }), Scenarios);
        foreach (var name in WeaponAbilityProfileVariants.PropertyNames())
        {
            foreach (var shape in SingleSettingShapes)
                DriveProbe($"probe {name} {shape}", shape, WeaponAbilityProfileVariants.Single(name), SingleSettingScenarios);
        }

        return text.ToString();
    }

    private static string StableGuids(string step)
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        return Guid.Replace(step, match =>
        {
            if (!tokens.TryGetValue(match.Value, out var token))
                tokens[match.Value] = token = "ability:<profile " + (tokens.Count + 1) + ">";
            return token;
        });
    }
}
