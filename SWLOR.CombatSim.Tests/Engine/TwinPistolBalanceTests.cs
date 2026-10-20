using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class TwinPistolBalanceTests
{
    [Test]
    public void SeededRotations_ReportBurstSustainResourcesAndAssumptions()
    {
        const int samples = 64;
        const int firstSeed = 20261017;
        var catalog = AbilityCatalog.Load();
        var rows = new List<object>();
        foreach (var crossfire in new[] { false, true })
        foreach (var rhythm in new[] { false, true })
        foreach (var variant in new[] { "opening", "sustain", "three targets", "excess targets", "pvp", "pvp ramp", "pressure", "blocked movement", "stationary fallback", "high defense", "high evasion", "deflection", "slow", "haste" })
        {
            var seconds = variant == "opening" ? 12 : variant == "pvp ramp" ? 360 : 120;
            var scenario = TwinPistolScenarios.Create(crossfire, rhythm, seconds,
                targets: variant == "three targets" ? 3 : variant == "excess targets" ? 5 : 1,
                pvp: variant is "pvp" or "pvp ramp", incomingPressure: variant == "pressure",
                movementAvailable: variant is not ("blocked movement" or "stationary fallback"), haste: variant == "slow" ? -50 : variant == "haste" ? 50 : 0, catalog: catalog);
            if (variant == "stationary fallback")
            {
                foreach (var use in scenario.Teams[0].Members[0].Abilities.Where(use => catalog.Get(Enum.Parse<SWLOR.NWN.API.NWScript.Enum.FeatType>(use.Feat)).TwinPistol?.SpendsMomentum == true))
                {
                    use.MinimumFlow = 0;
                    use.MovementMeters = 0;
                    use.MovementSeconds = 0;
                }
                scenario.Description += " Stationary fallback overrides that policy: spenders may fire at zero Flow, and Grapple starts inside stopping range.";
            }
            foreach (var target in scenario.Teams[1].Members)
            {
                if (variant == "high defense") target.Defense = 250;
                if (variant == "high evasion") target.Evasion = 250;
                if (variant == "deflection") target.WeaponAvoidancePercent = 50;
            }
            var results = Enumerable.Range(firstSeed, samples).Select(seed => new Simulation(scenario, RuleSets.Current(), catalog, seed).Run())
                .Select(result => result.Combatants[0]).ToArray();
            var damage = results.Select(actor => actor.Stats.DamageDealt).Order().ToArray();
            var mean = damage.Average();
            var standardDeviation = Math.Sqrt(damage.Sum(value => Math.Pow(value - mean, 2)) / (samples - 1));
            foreach (var actor in results)
            {
                actor.Stm.Should().BeInRange(0, actor.Spec.MaxStm);
                actor.Stats.StmSpent.Should().BeGreaterThanOrEqualTo(actor.Stats.StmRefunded);
                actor.Stats.AbilityImpacts.Should().BeGreaterThanOrEqualTo(actor.Stats.AbilityHits);
                actor.Stats.OrdinaryAttempts.Should().BeGreaterThanOrEqualTo(actor.Stats.OrdinaryHits);
                actor.Stats.FlowSpent.Should().BeLessThanOrEqualTo(actor.Stats.AbilitiesUsed * 3);
            }
            rows.Add(new
            {
                line = crossfire ? "Crossfire" : "Barrage", handling = rhythm ? "Matched Rhythm" : "Paired Handling",
                variant, seconds, samples, firstSeed, lastSeed = firstSeed + samples - 1,
                skillPoints = catalog.Perks.GetPurchaseCost(scenario.Teams[0].Members[0].Perks),
                damageMean = mean, damageP10 = damage[(int)(samples * .1)], damageP90 = damage[(int)(samples * .9)],
                damageMean95HalfWidth = 1.96 * standardDeviation / Math.Sqrt(samples), dps = mean / seconds,
                ordinaryAttempts = results.Average(actor => actor.Stats.OrdinaryAttempts),
                ordinaryHitRate = results.Sum(actor => actor.Stats.OrdinaryHits) / (double)Math.Max(1, results.Sum(actor => actor.Stats.OrdinaryAttempts)),
                abilityImpacts = results.Average(actor => actor.Stats.AbilityImpacts),
                abilityHitRate = results.Sum(actor => actor.Stats.AbilityHits) / (double)Math.Max(1, results.Sum(actor => actor.Stats.AbilityImpacts)),
                staminaSpent = results.Average(actor => actor.Stats.StmSpent), staminaRefunded = results.Average(actor => actor.Stats.StmRefunded),
                staminaEnd = results.Average(actor => actor.Stm), starvedSeconds = results.Average(actor => actor.Stats.StarvedSeconds),
                flowSpent = results.Average(actor => actor.Stats.FlowSpent),
                abilityUses = results.SelectMany(actor => actor.Stats.AbilityUses).GroupBy(entry => entry.Key)
                    .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value) / (double)samples),
                assumptions = scenario.Description
            });
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-twin-simulator");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "twin-pistol-sensitivity.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = "64 fixed seeds per variant. Logical stat-based Twin simulation uses production damage/timing functions, extracted ability payloads and shared trait states. " +
                "No Pistol baseline: its procs are not implemented by the generic simulator. Native equal69-SP comparison is separate evidence. " +
                "These scenarios test internal pressure/mobility tradeoffs and sensitivity; they are not proof of global, equipment, movement or PvP balance.",
            rows
        }, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path, $"Twin Pistol {rows.Count * samples}-run sensitivity report");
        TestContext.Progress.WriteLine($"Twin Pistol: {rows.Count} variants x {samples} seeds; {path}");
    }
}
