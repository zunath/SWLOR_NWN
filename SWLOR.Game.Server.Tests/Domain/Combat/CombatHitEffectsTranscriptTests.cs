using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ordered call-and-draw transcript of the hit effects orchestrators, taken before <c>CombatHitEffectsService</c>
/// is split: <c>ApplyDamageDealtEffects</c>, <c>ApplyTriggeredDamage</c>, <c>ApplyCriticalHitEffects</c>,
/// <c>ApplyAutoAttackDamageModifiers</c> and <c>ApplyDefeatedEnemyEffects</c>. The real container runs with recording stand-ins at
/// its leaves; each scenario runs twice, once with every stat at zero (the gates and their order) and once with every stat raised
/// (every rider taken). Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatHitEffectsTranscriptTests
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/HitEffects.txt";

    [Test]
    public void TheHitEffectsOrchestratorsProduceTheCapturedTranscript()
    {
        using var bench = new CombatTranscriptBench();
        var hitEffects = bench.Get<ICombatHitEffectsService>();
        var triggeredDamage = bench.Get<ICombatTriggeredDamageService>();

        foreach (var stat in new[] { 0, 5 })
        {
            bench.DefaultStat = stat;
            var tag = " (stats " + stat + ")";
            bench.Step("direct auto-attack damage" + tag, () =>
                hitEffects.ApplyDamageDealtEffects(Attacker, Defender, 40, SkillType.Vibroblade));
            bench.Step("direct ability damage" + tag, () =>
                hitEffects.ApplyDamageDealtEffects(Attacker, Defender, 40, SkillType.Force, CombatDamageType.Force, isAbilityDamage: true));
            bench.Step("direct damage without hit procs" + tag, () =>
                hitEffects.ApplyDamageDealtEffects(Attacker, Defender, 40, SkillType.Rifle, allowsHitProcs: false));
            bench.Step("triggered delivery stops after the tracking" + tag, () =>
                hitEffects.ApplyDamageDealtEffects(Attacker, Defender, 40, deliveryType: CombatDamageDeliveryType.Triggered));
            bench.Step("no damage" + tag, () => hitEffects.ApplyDamageDealtEffects(Attacker, Defender, 0));
            bench.Step("triggered damage" + tag, () =>
                triggeredDamage.ApplyTriggeredDamage(Attacker, Defender, 30, CombatDamageType.Physical, SkillType.Vibroblade));
            bench.Step("triggered force damage" + tag, () =>
                triggeredDamage.ApplyTriggeredDamage(Attacker, Defender, 30, CombatDamageType.Force, SkillType.Force));
            bench.Step("critical hit" + tag, () =>
                hitEffects.ApplyCriticalHitEffects(Attacker, Defender, 50, 120, true, SkillType.Vibroblade));
            bench.Step("critical hit of an auto-attack" + tag, () =>
                hitEffects.ApplyCriticalHitEffects(Attacker, Defender, 50, 120));
            bench.Step("non-critical ability" + tag, () =>
                hitEffects.ApplyNonCriticalAbilityEffects(Attacker, Defender, SkillType.Force));
            bench.Step("auto-attack damage modifiers" + tag, () =>
                hitEffects.ApplyAutoAttackDamageModifiers(Attacker, Defender, 40, SkillType.Vibroblade));
            bench.Step("auto-attack cycle bonus" + tag, () =>
                hitEffects.ConsumeAutoAttackCycleDamageBonus(Attacker, SkillType.Vibroblade));
            bench.Step("defeated enemy" + tag, () => hitEffects.ApplyDefeatedEnemyEffects(Attacker, Defender));
            bench.Step("recent damagers of a defeated enemy" + tag, () => hitEffects.ApplyRecentDamagerDefeatedEnemyEffects(Defender));
            bench.Step("forget the attacker" + tag, () => hitEffects.ForgetHitEffectState(Attacker));
        }

        AssertGolden(FixturePath, bench.Text);
    }
}
