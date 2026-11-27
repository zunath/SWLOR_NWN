using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The forget order of <c>CombatService</c>: a bench with every table the cleanup clears populated for the leaving creature
/// and for others, run through the real parts and the real temporary stat modifier service, with the status effect service, the
/// recorder and the refresh publisher recording. The transcript lists the tables before and after, and in order every call the
/// forgets cause: the overwatch status removal of the suppression forget (it runs once per source that held a pending use),
/// the refresh notice of the temporary stat modifier clear, the second activity and resources forgets, and the final
/// <c>TemporaryStatModifier.Clear</c>. The owner list walked in phases produces the same transcript. Regenerate with
/// <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatCleanupOrderTests
{
    private static readonly Type[] RealParts =
    {
        typeof(ITemporaryStatModifierService), typeof(ICombatResourcesService), typeof(ICombatActivityService),
        typeof(ICombatAttackTimingService), typeof(ICombatSuppressionService)
    };

    private static readonly Type[] Leaves = DefaultLeaves.Except(RealParts).Concat(new[] { typeof(IGuiRefreshPublisher) }).ToArray();

    [Test]
    public void TheCleanupForgetsEveryPartInTheCapturedOrder()
    {
        // The twelve real owners and the temporary stat modifier service are decorated: a step of the cleanup (or the temporary modifier clear) is written to the transcript,
        // in the order CombatService makes them, and then runs on the real part.
        using var bench = new CombatTranscriptBench(Leaves, (services, host) =>
        {
            var lines = host.Lines;
            Func<object?, string> format = host.Format;
            Func<string, bool> forgets = member => member.StartsWith("Forget", StringComparison.Ordinal) || member == "Clear";
            string? Describe(string label, string member, object?[] args) => CombatForgetSteps.Describe(label, member, args, format);
            ForwardingRecorder.Decorate<ICombatResourcesService>(services, "Resources", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatActivityService>(services, "Activity", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatDefenseMemoryService>(services, "DefenseMemory", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatSuppressionService>(services, "Suppression", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatAbilityOffenseEffectsService>(services, "OffenseEffects", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatCriticalHitEffectsService>(services, "CriticalHitEffects", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatHitEffectsService>(services, "HitEffects", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatAbilityHitRidersService>(services, "Impact", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatAbilityAreaImpactService>(services, "AreaImpact", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatAttackTimingService>(services, "AttackTiming", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatRateModifiersService>(services, "RateModifiers", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ICombatRepeatedTargetService>(services, "RepeatedTarget", lines, format, forgets, Describe);
            ForwardingRecorder.Decorate<ITemporaryStatModifierService>(services, "TemporaryStatModifier", lines, format, forgets);
        });
        var combat = (CombatService)bench.Get<ICombatService>();
        var tables = new object[]
        {
            bench.Get<ICombatResourcesService>(), bench.Get<ICombatActivityService>(), bench.Get<ICombatDefenseMemoryService>(),
            bench.Get<ICombatSuppressionService>(), bench.Get<ICombatAbilityOffenseEffectsService>(), bench.Get<ICombatHitEffectsService>(), bench.Get<ICombatCriticalHitEffectsService>(),
            bench.Get<ICombatAbilityImpactService>(), bench.Get<ICombatAbilityHitRidersService>(), bench.Get<ICombatAbilityAreaImpactService>(), bench.Get<ICombatAttackTimingService>(), bench.Get<ICombatRateModifiersService>(), bench.Get<ICombatRepeatedTargetService>(),
            bench.Get<ITemporaryStatModifierService>()
        }.Select(ForwardingRecorder.Unwrap).ToArray();
        var temporary = bench.Get<ITemporaryStatModifierService>();
        var creatures = new[] { Attacker, Defender, Other, Ally };
        // Both players hold a temporary modifier, so the clear is a refresh notice for a player; the others hold one that is silent.
        bench.Engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! is Attacker or Defender);

        void Populate(string name)
        {
            bench.Step("populate: " + name, () =>
            {
                foreach (var creature in creatures)
                    temporary.Replace(creature, StatType.AttackPercentAdjustment, 5, 60, StatType.DamageTakenAttackPercentAdjustment);
                bench.Lines.Add("filled " + string.Join(", ", CombatTableState.Populate(tables, creatures)));
            });
            bench.Step("tables before " + name, () =>
            {
                foreach (var line in CombatTableState.Dump(tables, bench.Format))
                    bench.Lines.Add(line);
            });
        }

        void Dump(string name) => bench.Step("tables after " + name, () =>
        {
            foreach (var line in CombatTableState.Dump(tables, bench.Format))
                bench.Lines.Add(line);
        });

        Populate("exit of the attacker");
        bench.Step("exit of the attacker", () =>
        {
            bench.Engine.SetReturn("GetExitingObject", Attacker);
            combat.ClearStatTriggerState();
        });
        Dump("exit of the attacker");

        Populate("death of the defender");
        bench.Step("death of the defender, killed by the other", () =>
        {
            bench.Engine.SetReturn("OBJECT_SELF", Defender);
            bench.Engine.SetReturn("GetLastKiller", Other);
            combat.ApplyDefeatedEnemyEffects();
        });
        Dump("death of the defender");

        bench.Step("exit of a creature that holds nothing", () =>
        {
            bench.Engine.SetReturn("GetExitingObject", Master);
            combat.ClearStatTriggerState();
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/CleanupOrder.txt", bench.Text);
    }
}
