using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ordered call-and-draw transcript of the damage taken orchestrators, taken before <c>CombatDamageTakenService</c>
/// is split: <c>ApplyDamageTakenModifiers</c>, the after-damage handler <c>ApplyDamageTakenEffects</c>, <c>ApplyGuardedHitModifiers</c>
/// and the reflection and deflection members that run from them. Every stat is zero in one pass and raised in the other.
/// Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatDamageTakenTranscriptTests
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/DamageTaken.txt";

    [Test]
    public void TheDamageTakenOrchestratorsProduceTheCapturedTranscript()
    {
        using var bench = new CombatTranscriptBench();
        var taken = bench.Get<ICombatDamageTakenService>();
        var memory = bench.Get<ICombatDefenseMemoryService>();

        foreach (var stat in new[] { 0, 5 })
        {
            bench.DefaultStat = stat;
            var tag = " (stats " + stat + ")";
            bench.Step("physical damage taken" + tag, () =>
                taken.ApplyDamageTakenModifiers(Defender, 80, Attacker));
            bench.Step("force damage taken" + tag, () =>
                taken.ApplyDamageTakenModifiers(Defender, 80, Attacker, CombatDamageType.Force));
            bench.Step("fire damage taken from nobody" + tag, () =>
                taken.ApplyDamageTakenModifiers(Defender, 80, damageType: CombatDamageType.Fire));
            bench.Step("triggered damage taken" + tag, () =>
                taken.ApplyDamageTakenModifiers(Defender, 80, Attacker, deliveryType: CombatDamageDeliveryType.Triggered, targetStatusDamagePercentAdjustment: 10));
            bench.Step("damage taken that is not a landed attack" + tag, () =>
                taken.ApplyDamageTakenModifiers(Defender, 80, Attacker, isLandedAttack: false, typedLeadershipReductionAlreadyApplied: true));
            bench.Step("damage over time taken" + tag, () =>
                taken.ApplyDamageOverTimeTakenModifiers(Defender, 30, CombatDamageType.Poison));
            bench.Step("fatal damage prevented or not" + tag, () =>
                taken.TryPreventFatalDamageAndGrantTemporaryHP(Defender, 250, true));
            bench.Step("guarded hit" + tag, () =>
                taken.ApplyGuardedHitModifiers(Defender, Attacker, 60, CombatDamageType.Physical, true));
            bench.Step("guarded hit that did not land" + tag, () =>
                taken.ApplyGuardedHitModifiers(Defender, Attacker, 60, CombatDamageType.Force, false));
            bench.Step("track a guarded hit" + tag, () => taken.TrackGuardedHit(Defender));
            bench.Step("track a deflection" + tag, () => taken.TrackDeflection(Defender, DeflectionSource.Melee));
            bench.Step("track an avoided attack" + tag, () => taken.TrackAvoidedAttack(Defender, Attacker));
            bench.Step("the after-damage handler" + tag, () =>
            {
                bench.Engine.SetReturn("OBJECT_SELF", Defender);
                bench.Engine.SetReturn("GetLastDamager", Attacker);
                bench.Engine.SetReturn("GetTotalDamageDealt", 45);
                taken.ApplyDamageTakenEffects();
            });
            bench.Step("after-damage effects of a dead defender" + tag, () =>
            {
                bench.Engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Defender);
                try
                {
                    taken.ApplyDamageTakenEffects(Defender, Attacker, 45);
                }
                finally
                {
                    bench.Engine.SetReturnHandler("GetIsDead", args => false);
                }
            });
            bench.Step("after-damage effects of a hit that dealt nothing" + tag, () => taken.ApplyDamageTakenEffects(Defender, Attacker, 0));
            bench.Step("melee damage taken" + tag, () => taken.ApplyMeleeDamageTakenEffects(Defender, Attacker));
            bench.Step("damage reflection" + tag, () => taken.ApplyDamageReflectionEffects(Attacker, Defender, 40, CombatDamageType.Physical));
            bench.Step("ranged deflection reflection" + tag, () => taken.ApplyRangedDeflectionReflection(Defender, Attacker, SkillType.Rifle));
            bench.Step("incoming physical converted to force" + tag, () =>
            {
                var physical = 60;
                taken.ApplyIncomingPhysicalToForceConversion(Attacker, Defender, CombatDamageType.Physical, ref physical, 0, null);
            });
            bench.Step("forget the defender" + tag, () => memory.ForgetDefenseMemory(Defender));
        }

        AssertGolden(FixturePath, bench.Text);
    }
}
