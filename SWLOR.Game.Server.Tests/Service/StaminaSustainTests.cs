using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Feature.AbilityDefinition.Rifle;
using SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class StaminaSustainTests
{
    [Test]
    public void OrdinaryRotations_PreserveUsefulActionsAcrossShortAndLongEncounters()
    {
        var pistol = new DoubleShotAbilityDefinition().BuildAbilities();
        var interrupt = new InterruptingShotAbilityDefinition().BuildAbilities()[FeatType.InterruptingShot1];
        var rifle = new AimedShotAbilityDefinition().BuildAbilities();
        var riot = new RiotBladeAbilityDefinition().BuildAbilities()[FeatType.RiotBlade4];
        var rend = new RendingStrikeAbilityDefinition().BuildAbilities()[FeatType.RendingStrike2];
        var bash = new ShieldBashAbilityDefinition().BuildAbilities()[FeatType.ShieldBash3];
        var cover = new CoveringStrikeAbilityDefinition().BuildAbilities()[FeatType.CoveringStrike3];
        var arc = new FocusedArcAbilityDefinition().BuildAbilities()[FeatType.FocusedArc3];

        // Equipment assumptions: one weapon and one armor STM enhancement (rank III
        // +4 each or rank V +6 each). Food uses one rank-appropriate Cloves/STM Regen
        // enhancement (+3 or +5 per 30s). No passive auto-attack/kill/deflection refunds
        // are needed for these conservative solo budgets. Support is base Field Recovery II.
        foreach (var (name, might, gear, food, reserve, rotation, conduit) in new[]
        {
            ("Early pistol, rank 10", 10, 0, 0, 4, new[] { (pistol[FeatType.DoubleShot1], 24, 2, 2) }, false),
            ("Late pistol, bare low Might", 10, 0, 0, 4, new[] { (pistol[FeatType.DoubleShot3], 24, 2, 4), (interrupt, 60, 1, 0) }, false),
            ("Mid rifle, rank 28", 10, 8, 3, 4, new[] { (rifle[FeatType.AimedShot3], 24, 1, 0) }, false),
            ("Late rifle, rank 40", 10, 12, 5, 4, new[] { (rifle[FeatType.AimedShot4], 30, 1, 0) }, false),
            ("Late melee, bare high Might", 26, 0, 0, 4, new[] { (riot, 18, 1, 0), (rend, 45, 1, 0) }, false),
            ("Mid tank, rank 38", 16, 8, 3, 10, new[] { (bash, 20, 1, 0), (cover, 60, 1, 0) }, false),
            ("Mid hybrid, rank 28", 16, 8, 3, 4, new[] { (arc, 30, 1, 0) }, true),
        })
        {
            foreach (var (supported, accuracy) in new[] { (false, 0.65), (false, 0.85), (true, 0.85) })
            foreach (var length in new[] { 30, 60, 120, 300 })
            {
                var before = Simulate(might, gear, food, reserve, rotation, conduit, supported, accuracy, length, false);
                var after = Simulate(might, gear, food, reserve, rotation, conduit, supported, accuracy, length, true);
                TestContext.Out.WriteLine($"{name}, {(supported ? "Field Recovery II" : "solo")}, hit {accuracy:P0}, {length}s: " +
                    $"casts {before.Casts:F1}->{after.Casts:F1}/{after.Attempts:F0}, end STM {before.Stamina:F1}->{after.Stamina:F1}, " +
                    $"first deferred {before.FirstDeferred:F0}->{after.FirstDeferred:F0}s (duration+1 means none), landed hits {after.Hits:F1}");
                after.Casts.Should().BeGreaterThanOrEqualTo(before.Casts);
                after.Stamina.Should().BeGreaterThanOrEqualTo(reserve);
                if (length <= 120 || supported || gear > 0 || might >= 26 || rotation.Length == 1)
                    after.Casts.Should().Be(after.Attempts, "the modest rotation should preserve its role reserve through this encounter");
            }
        }
    }

    [Test]
    public void AggressiveBurst_StillExhaustsTheLowMightPool()
    {
        var ability = new DoubleShotAbilityDefinition().BuildAbilities()[FeatType.DoubleShot3];
        var extra = new FanTheHammerAbilityDefinition().BuildAbilities()[FeatType.FanTheHammer2];
        var result = Simulate(10, 0, 0, 4, [(ability, 24, 2, 4), (extra, 30, 1, 0)],
            false, false, 0.75, 120, true);
        TestContext.Out.WriteLine($"Aggressive pistol: {result.Casts:F1}/{result.Attempts:F0} casts, " +
            $"first deferred {result.FirstDeferred:F1}s, end STM {result.Stamina:F1}");
        result.Casts.Should().BeLessThan(result.Attempts * 0.75);
        result.FirstDeferred.Should().BeLessThan(60, "even conditional critical refunds cannot fund this burst for a minute");
    }

    private static (double Casts, double Attempts, double Stamina, double FirstDeferred, double Hits) Simulate(
        int might, int gear, int food, int reserve, (AbilityDetail Ability, int Interval, int HitCount, int Refund)[] rotation,
        bool conduit, bool supported, double accuracy, int seconds, bool revised)
    {
        const int trials = 200;
        double casts = 0, attempts = 0, stamina = 0, firstDeferred = 0, hits = 0;
        var maximum = Stat.GetMaxStamina(Stat.BaseSTM, might, gear);
        for (var trial = 0; trial < trials; trial++)
        {
            var random = new System.Random(1729 + trial);
            var current = maximum;
            var remainder = 0;
            var deferred = seconds + 1;
            // Conduit model: WIL 20 => 70 FP; a 6-FP Force cast every 24s restores
            // floor(6 * .35)=2 STM. Each 8-STM arc restores floor(8 * .35)=2 FP.
            // Both pools must fund their actual costs; neither conversion triggers itself.
            var fp = 70;
            for (var time = 0; time < seconds; time++)
            {
                if (time > 0 && time % 6 == 0 && revised)
                    current = Math.Min(maximum, current + NaturalRegeneration.GetStaminaRegenPerHeartbeat(might, 0, food, ref remainder));
                if (time > 0 && time % 30 == 0)
                {
                    if (!revised) current = Math.Min(maximum, current + 1 + might / 4 + food);
                    fp = Math.Min(70, fp + 6);
                }
                if (supported && time > 0 && time % 4 == 0) current = Math.Min(maximum, current + 2);
                if (conduit && time % 24 == 0 && fp >= 6)
                {
                    fp -= 6;
                    current = Math.Min(maximum, current + 2);
                }
                foreach (var (ability, interval, count, refund) in rotation)
                {
                    var cost = ability.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM;
                    interval.Should().BeGreaterThanOrEqualTo((int)ability.RecastDelay(0), "schedules must respect authored cooldowns");
                    if (time % interval != 0) continue;
                    attempts++;
                    if (current < cost + reserve)
                    {
                        deferred = Math.Min(deferred, time);
                        continue;
                    }
                    current -= cost;
                    casts++;
                    var critical = false;
                    for (var hit = 0; hit < count; hit++)
                    {
                        if (random.NextDouble() >= accuracy) continue;
                        hits++;
                        // 10% conditional critical rate is conservative at ranks 38-50
                        // (5 base + skill/10 + at most 3 PER-vs-VIT), without Gambler.
                        critical |= random.NextDouble() < 0.10;
                    }
                    if (critical) current = Math.Min(maximum, current + Combat.CalculateAbilityHitStaminaRestore(cost, 0, refund));
                    if (conduit) fp = Math.Min(70, fp + cost * 35 / 100);
                }
            }
            stamina += current;
            firstDeferred += deferred;
        }
        return (casts / trials, attempts / trials, stamina / trials, firstDeferred / trials, hits / trials);
    }
}
